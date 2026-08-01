using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Catalog;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Catalog;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Infrastructure.Modules.Shared;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Catalog;

internal sealed partial class CatalogService(
    TraderProDbContext dbContext,
    IAuthenticatedTraderProContext context,
    IClock clock,
    PostgreSqlIdempotentCommandExecutor idempotency) :
    ICatalogService,
    IBagTypeProductStandardUsageReader
{
    private const int EventVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<IdempotentCommandResult<ProductGroupResult>> CreateGroupAsync(
        CreateProductGroupCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var normalizedCode =
            CommercialMasterDataInfrastructure.NormalizeCode(command.Code);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateProductGroup,
            context,
            ("code", normalizedCode),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("description", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Description)));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.CreateProductGroup,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            201,
            async () =>
            {
                if (await dbContext.ProductGroups.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.NormalizedCode == normalizedCode,
                        cancellationToken))
                {
                    throw ProductGroupCodeExists();
                }

                var now = clock.UtcNow;
                var group = CommercialMasterDataInfrastructure.Domain(
                    () => ProductGroup.Create(
                        context.WorkspaceId,
                        context.CompanyId,
                        normalizedCode,
                        command.Name,
                        command.LocalName,
                        command.Description,
                        now));
                dbContext.ProductGroups.Add(group);
                AddFacts(
                    "Catalog.ProductGroup",
                    group.Id,
                    group.Version,
                    "Catalog.ProductGroup.Created",
                    "Catalog.ProductGroupCreated",
                    null,
                    ToProductGroupAuditSnapshot(group),
                    ToProductGroupEventPayload(group),
                    now);
                return ToResult(group);
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductGroupResult>> UpdateGroupAsync(
        UpdateProductGroupCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateProductGroup,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("description", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Description)));
        return ExecuteGroupMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.UpdateProductGroup,
            idempotencyKey,
            hash,
            "Catalog.ProductGroup.Updated",
            "Catalog.ProductGroupUpdated",
            (group, now) =>
            {
                CommercialMasterDataInfrastructure.Domain(
                    () => group.Update(
                        command.Name,
                        command.LocalName,
                        command.Description,
                        now));
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductGroupResult>>
        SetGroupStatusAsync(
            MasterStatusCommand command,
            bool activate,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var commandType = activate
            ? CommercialMasterDataCommandTypes.ReactivateProductGroup
            : CommercialMasterDataCommandTypes.DeactivateProductGroup;
        var hash = CommercialMasterDataRequestHash.Compute(
            commandType,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return ExecuteGroupMutationAsync(
            command.Id,
            command.ExpectedVersion,
            commandType,
            idempotencyKey,
            hash,
            activate
                ? "Catalog.ProductGroup.Reactivated"
                : "Catalog.ProductGroup.Deactivated",
            activate
                ? "Catalog.ProductGroupReactivated"
                : "Catalog.ProductGroupDeactivated",
            async (group, now) =>
            {
                if (activate)
                {
                    CommercialMasterDataInfrastructure.Domain(
                        () => group.Reactivate(now));
                    return;
                }

                var inUse = await dbContext.Products.AnyAsync(
                    item =>
                        item.CompanyId == context.CompanyId &&
                        item.ProductGroupId == group.Id &&
                        item.Status == MasterDataStatus.Active,
                    cancellationToken);
                CommercialMasterDataInfrastructure.Domain(
                    () => group.Deactivate(inUse, now));
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductResult>> CreateProductAsync(
        CreateProductCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var productType = CommercialMasterDataInputRules.ParseProductType(
            command.ProductType);
        var normalizedCode =
            CommercialMasterDataInfrastructure.NormalizeCode(command.Code);
        var family = CommercialMasterDataInfrastructure.Domain(
            () => Product.CanonicalizeProcessingFamily(
                command.ProcessingFamilyCode));
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateProduct,
            context,
            ("productGroupId", command.ProductGroupId),
            ("code", normalizedCode),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("productType", productType),
            ("isPurchasable", command.IsPurchasable),
            ("processingFamilyCode", family.Display),
            ("normalizedProcessingFamilyCode", family.Normalized),
            ("description", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Description)),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.CreateProduct,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            201,
            async () =>
            {
                await RequireActiveGroupAsync(
                    command.ProductGroupId,
                    cancellationToken);
                if (await dbContext.Products.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.NormalizedCode == normalizedCode,
                        cancellationToken))
                {
                    throw ProductCodeExists();
                }

                var now = clock.UtcNow;
                var product = CommercialMasterDataInfrastructure.Domain(
                    () => Product.Create(
                        context.WorkspaceId,
                        context.CompanyId,
                        command.ProductGroupId,
                        normalizedCode,
                        command.Name,
                        command.LocalName,
                        productType,
                        command.IsPurchasable,
                        family.Display,
                        command.Description,
                        command.Notes,
                        now));
                dbContext.Products.Add(product);
                AddFacts(
                    "Catalog.Product",
                    product.Id,
                    product.Version,
                    "Catalog.Product.Created",
                    "Catalog.ProductCreated",
                    null,
                    ToProductAuditSnapshot(product, []),
                    ToProductEventPayload(product),
                    now);
                return ToResult(product);
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductResult>> UpdateProductAsync(
        UpdateProductCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var productType = CommercialMasterDataInputRules.ParseProductType(
            command.ProductType);
        var family = CommercialMasterDataInfrastructure.Domain(
            () => Product.CanonicalizeProcessingFamily(
                command.ProcessingFamilyCode));
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateProduct,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion),
            ("productGroupId", command.ProductGroupId),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("productType", productType),
            ("isPurchasable", command.IsPurchasable),
            ("processingFamilyCode", family.Display),
            ("normalizedProcessingFamilyCode", family.Normalized),
            ("description", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Description)),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return ExecuteProductMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.UpdateProduct,
            idempotencyKey,
            hash,
            "Catalog.Product.Updated",
            "Catalog.ProductUpdated",
            async (product, now) =>
            {
                await RequireActiveGroupAsync(
                    command.ProductGroupId,
                    cancellationToken);
                var usage = await ReadProductUsageAsync(
                    product.Id,
                    cancellationToken);
                CommercialMasterDataInfrastructure.Domain(
                    () => product.Update(
                        command.ProductGroupId,
                        command.Name,
                        command.LocalName,
                        productType,
                        command.IsPurchasable,
                        family.Display,
                        command.Description,
                        command.Notes,
                        usage.SupplierScope,
                        usage.BagStandard,
                        now));
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductResult>> SetProductStatusAsync(
        MasterStatusCommand command,
        bool activate,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var commandType = activate
            ? CommercialMasterDataCommandTypes.ReactivateProduct
            : CommercialMasterDataCommandTypes.DeactivateProduct;
        var hash = CommercialMasterDataRequestHash.Compute(
            commandType,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return ExecuteProductMutationAsync(
            command.Id,
            command.ExpectedVersion,
            commandType,
            idempotencyKey,
            hash,
            activate
                ? "Catalog.Product.Reactivated"
                : "Catalog.Product.Deactivated",
            activate
                ? "Catalog.ProductReactivated"
                : "Catalog.ProductDeactivated",
            async (product, now) =>
            {
                if (activate)
                {
                    await RequireActiveGroupAsync(
                        product.ProductGroupId,
                        cancellationToken);
                    CommercialMasterDataInfrastructure.Domain(
                        () => product.Reactivate(now));
                    return;
                }

                var usage = await ReadProductUsageAsync(
                    product.Id,
                    cancellationToken);
                CommercialMasterDataInfrastructure.Domain(
                    () => product.Deactivate(
                        usage.SupplierScope,
                        usage.BagStandard,
                        now));
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        CreateBagStandardAsync(
            CreateProductStandardBagWeightCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateProductStandardBagWeight,
            context,
            ("productId", command.ProductId),
            ("bagTypeId", command.BagTypeId),
            ("label", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Label)),
            ("standardContentWeightKg", command.StandardContentWeightKg),
            ("isDefault", command.IsDefault));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.CreateProductStandardBagWeight,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            201,
            async () =>
            {
                await AcquireBagStandardLocksAsync(
                    command.ProductId,
                    command.BagTypeId,
                    cancellationToken);
                _ = await RequireActivePurchasableProductAsync(
                    command.ProductId,
                    cancellationToken);
                var bagType = await RequireActiveBagTypeAsync(
                    command.BagTypeId,
                    cancellationToken);
                if (command.IsDefault &&
                    await dbContext.ProductStandardBagWeights.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.ProductId == command.ProductId &&
                            item.Status == MasterDataStatus.Active &&
                            item.IsDefault,
                        cancellationToken))
                {
                    throw BagStandardDefaultConflict();
                }

                if (await dbContext.ProductStandardBagWeights.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.ProductId == command.ProductId &&
                            item.BagTypeId == command.BagTypeId &&
                            item.StandardContentWeightKg ==
                                command.StandardContentWeightKg,
                        cancellationToken))
                {
                    throw BagStandardExists();
                }

                var now = clock.UtcNow;
                var standard = CommercialMasterDataInfrastructure.Domain(
                    () => ProductStandardBagWeight.Create(
                        context.WorkspaceId,
                        context.CompanyId,
                        command.ProductId,
                        command.BagTypeId,
                        command.Label,
                        command.StandardContentWeightKg,
                        command.IsDefault,
                        now));
                dbContext.ProductStandardBagWeights.Add(standard);
                AddBagStandardFacts(
                    standard,
                    bagType.Code,
                    "Catalog.ProductStandardBagWeight.Created",
                    "Catalog.ProductStandardBagWeightCreated",
                    null,
                    now);
                return ToResult(standard, bagType.Code);
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        UpdateBagStandardAsync(
            UpdateProductStandardBagWeightCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateProductStandardBagWeight,
            context,
            ("productId", command.ProductId),
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion),
            ("label", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Label)),
            ("standardContentWeightKg", command.StandardContentWeightKg));
        return ExecuteBagStandardMutationAsync(
            command.ProductId,
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.UpdateProductStandardBagWeight,
            idempotencyKey,
            hash,
            "Catalog.ProductStandardBagWeight.Updated",
            "Catalog.ProductStandardBagWeightUpdated",
            async (standard, now) =>
            {
                await AcquireBagStandardLocksAsync(
                    standard.ProductId,
                    standard.BagTypeId,
                    cancellationToken);
                _ = await RequireActivePurchasableProductAsync(
                    standard.ProductId,
                    cancellationToken);
                _ = await RequireActiveBagTypeAsync(
                    standard.BagTypeId,
                    cancellationToken);
                if (await dbContext.ProductStandardBagWeights.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.Id != standard.Id &&
                            item.ProductId == standard.ProductId &&
                            item.BagTypeId == standard.BagTypeId &&
                            item.StandardContentWeightKg ==
                                command.StandardContentWeightKg,
                        cancellationToken))
                {
                    throw BagStandardExists();
                }

                CommercialMasterDataInfrastructure.Domain(
                    () => standard.Update(
                        command.Label,
                        command.StandardContentWeightKg,
                        now));
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        SetBagStandardStatusAsync(
            ProductStandardBagWeightStatusCommand command,
            bool activate,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var commandType = activate
            ? CommercialMasterDataCommandTypes
                .ReactivateProductStandardBagWeight
            : CommercialMasterDataCommandTypes
                .DeactivateProductStandardBagWeight;
        var hash = CommercialMasterDataRequestHash.Compute(
            commandType,
            context,
            ("productId", command.ProductId),
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return ExecuteBagStandardMutationAsync(
            command.ProductId,
            command.Id,
            command.ExpectedVersion,
            commandType,
            idempotencyKey,
            hash,
            activate
                ? "Catalog.ProductStandardBagWeight.Reactivated"
                : "Catalog.ProductStandardBagWeight.Deactivated",
            activate
                ? "Catalog.ProductStandardBagWeightReactivated"
                : "Catalog.ProductStandardBagWeightDeactivated",
            async (standard, now) =>
            {
                await AcquireBagStandardLocksAsync(
                    standard.ProductId,
                    standard.BagTypeId,
                    cancellationToken);
                if (activate)
                {
                    _ = await RequireActivePurchasableProductAsync(
                        standard.ProductId,
                        cancellationToken);
                    _ = await RequireActiveBagTypeAsync(
                        standard.BagTypeId,
                        cancellationToken);
                    CommercialMasterDataInfrastructure.Domain(
                        () => standard.Reactivate(now));
                }
                else
                {
                    CommercialMasterDataInfrastructure.Domain(
                        () => standard.Deactivate(now));
                }
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        SetDefaultBagStandardAsync(
            ProductStandardBagWeightStatusCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        CommercialMasterDataInputRules.RequireExpectedVersion(
            command.ExpectedVersion);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.SetDefaultProductStandardBagWeight,
            context,
            ("productId", command.ProductId),
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.SetDefaultProductStandardBagWeight,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            200,
            async () =>
            {
                await PostgreSqlCommercialCatalogLocks
                    .AcquireProductBagDefaultAsync(
                        dbContext,
                        context.WorkspaceId,
                        context.CompanyId,
                        command.ProductId,
                        cancellationToken);
                var standard = await FindBagStandardAsync(
                    command.ProductId,
                    command.Id,
                    cancellationToken);
                if (standard.Version != command.ExpectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        command.ExpectedVersion,
                        standard.Version);
                }

                _ = await RequireActivePurchasableProductAsync(
                    standard.ProductId,
                    cancellationToken);
                var bagType = await RequireActiveBagTypeAsync(
                    standard.BagTypeId,
                    cancellationToken);
                var before = ToBagStandardAuditSnapshot(standard);
                var now = clock.UtcNow;
                await dbContext.ProductStandardBagWeights
                    .Where(item =>
                        item.CompanyId == context.CompanyId &&
                        item.ProductId == standard.ProductId &&
                        item.Id != standard.Id &&
                        item.Status == MasterDataStatus.Active &&
                        item.IsDefault)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(item => item.IsDefault, false)
                            .SetProperty(
                                item => item.Version,
                                item => item.Version + 1)
                            .SetProperty(item => item.UpdatedAtUtc, now),
                        cancellationToken);
                CommercialMasterDataInfrastructure.Domain(
                    () => standard.SetDefault(now));
                AddBagStandardFacts(
                    standard,
                    bagType.Code,
                    "Catalog.ProductStandardBagWeight.DefaultChanged",
                    "Catalog.ProductStandardBagWeightDefaultChanged",
                    before,
                    now);
                return ToResult(standard, bagType.Code);
            },
            IsReplayable,
            () => ReadBagStandardConcurrencyProblemAsync(
                command.ProductId,
                command.Id,
                command.ExpectedVersion,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public async Task<bool> AcquireLockAndIsInUseAsync(
        Guid bagTypeId,
        CancellationToken cancellationToken)
    {
        RequireContext();
        await PostgreSqlCommercialCatalogLocks.AcquireBagTypeUsageAsync(
            dbContext,
            context.WorkspaceId,
            context.CompanyId,
            bagTypeId,
            cancellationToken);
        return await dbContext.ProductStandardBagWeights.AnyAsync(
            item =>
                item.CompanyId == context.CompanyId &&
                item.BagTypeId == bagTypeId &&
                item.Status == MasterDataStatus.Active,
            cancellationToken);
    }

    private Task<IdempotentCommandResult<ProductGroupResult>>
        ExecuteGroupMutationAsync(
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string hash,
            string action,
            string eventType,
            Func<ProductGroup, DateTimeOffset, Task> mutate,
            CancellationToken cancellationToken)
    {
        CommercialMasterDataInputRules.RequireExpectedVersion(expectedVersion);
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            commandType,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            200,
            async () =>
            {
                var group = await FindGroupAsync(id, cancellationToken);
                if (group.Version != expectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        expectedVersion,
                        group.Version);
                }

                var before = ToProductGroupAuditSnapshot(group);
                var now = clock.UtcNow;
                await mutate(group, now);
                AddFacts(
                    "Catalog.ProductGroup",
                    group.Id,
                    group.Version,
                    action,
                    eventType,
                    before,
                    ToProductGroupAuditSnapshot(group),
                    ToProductGroupEventPayload(group),
                    now);
                return ToResult(group);
            },
            IsReplayable,
            () => ReadVersionProblemAsync(
                dbContext.ProductGroups
                    .Where(item =>
                        item.Id == id &&
                        item.CompanyId == context.CompanyId)
                    .Select(item => (long?)item.Version),
                expectedVersion,
                ProductGroupNotFound,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private Task<IdempotentCommandResult<ProductResult>>
        ExecuteProductMutationAsync(
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string hash,
            string action,
            string eventType,
            Func<Product, DateTimeOffset, Task> mutate,
            CancellationToken cancellationToken)
    {
        CommercialMasterDataInputRules.RequireExpectedVersion(expectedVersion);
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            commandType,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            200,
            async () =>
            {
                var product = await FindProductAsync(id, cancellationToken);
                if (product.Version != expectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        expectedVersion,
                        product.Version);
                }

                var notesBefore = product.Notes;
                var before = ToProductAuditSnapshot(product, []);
                var now = clock.UtcNow;
                await mutate(product, now);
                var changedFields = string.Equals(
                    notesBefore,
                    product.Notes,
                    StringComparison.Ordinal)
                    ? Array.Empty<string>()
                    : ["notes"];
                AddFacts(
                    "Catalog.Product",
                    product.Id,
                    product.Version,
                    action,
                    eventType,
                    before,
                    ToProductAuditSnapshot(product, changedFields),
                    ToProductEventPayload(product),
                    now);
                return ToResult(product);
            },
            IsReplayable,
            () => ReadVersionProblemAsync(
                dbContext.Products
                    .Where(item =>
                        item.Id == id &&
                        item.CompanyId == context.CompanyId)
                    .Select(item => (long?)item.Version),
                expectedVersion,
                ProductNotFound,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        ExecuteBagStandardMutationAsync(
            Guid productId,
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string hash,
            string action,
            string eventType,
            Func<ProductStandardBagWeight, DateTimeOffset, Task> mutate,
            CancellationToken cancellationToken)
    {
        CommercialMasterDataInputRules.RequireExpectedVersion(expectedVersion);
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            commandType,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            200,
            async () =>
            {
                var standard = await FindBagStandardAsync(
                    productId,
                    id,
                    cancellationToken);
                if (standard.Version != expectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        expectedVersion,
                        standard.Version);
                }

                var bagType = await dbContext.BagTypes.SingleOrDefaultAsync(
                        item =>
                            item.Id == standard.BagTypeId &&
                            item.CompanyId == context.CompanyId,
                        cancellationToken) ??
                    throw BagStandardInvalid(
                        "The standard's Bag Type is unavailable.");
                var before = ToBagStandardAuditSnapshot(standard);
                var now = clock.UtcNow;
                await mutate(standard, now);
                AddBagStandardFacts(
                    standard,
                    bagType.Code,
                    action,
                    eventType,
                    before,
                    now);
                return ToResult(standard, bagType.Code);
            },
            IsReplayable,
            () => ReadBagStandardConcurrencyProblemAsync(
                productId,
                id,
                expectedVersion,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private async Task AcquireBagStandardLocksAsync(
        Guid productId,
        Guid bagTypeId,
        CancellationToken cancellationToken)
    {
        await PostgreSqlCommercialCatalogLocks.AcquireProductBagDefaultAsync(
            dbContext,
            context.WorkspaceId,
            context.CompanyId,
            productId,
            cancellationToken);
        await PostgreSqlCommercialCatalogLocks.AcquireBagTypeUsageAsync(
            dbContext,
            context.WorkspaceId,
            context.CompanyId,
            bagTypeId,
            cancellationToken);
    }

    private async Task RequireActiveGroupAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.ProductGroups.AnyAsync(
                item =>
                    item.Id == id &&
                    item.CompanyId == context.CompanyId &&
                    item.Status == MasterDataStatus.Active,
                cancellationToken))
        {
            throw ProductGroupInactive();
        }
    }

    private async Task<Product> RequireActivePurchasableProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Products.SingleOrDefaultAsync(
                item =>
                    item.Id == id &&
                    item.CompanyId == context.CompanyId &&
                    item.Status == MasterDataStatus.Active &&
                    item.IsPurchasable,
                cancellationToken) ??
            throw ProductNotPurchasable();
    }

    private async Task<BagType> RequireActiveBagTypeAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.BagTypes.SingleOrDefaultAsync(
                item =>
                    item.Id == id &&
                    item.CompanyId == context.CompanyId &&
                    item.Status == MasterDataStatus.Active,
                cancellationToken) ??
            throw BagStandardInvalid(
                "The Bag Type must be active and belong to the company.");
    }

    private async Task<(bool SupplierScope, bool BagStandard)>
        ReadProductUsageAsync(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var supplierScope = await dbContext.SupplierProductScopes.AnyAsync(
            item =>
                item.CompanyId == context.CompanyId &&
                item.ProductId == productId &&
                item.Status == MasterDataStatus.Active,
            cancellationToken);
        var bagStandard = await dbContext.ProductStandardBagWeights.AnyAsync(
            item =>
                item.CompanyId == context.CompanyId &&
                item.ProductId == productId &&
                item.Status == MasterDataStatus.Active,
            cancellationToken);
        return (supplierScope, bagStandard);
    }

    private async Task<ProductGroup> FindGroupAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProductGroups.SingleOrDefaultAsync(
                item => item.Id == id && item.CompanyId == context.CompanyId,
                cancellationToken) ??
            throw ProductGroupNotFound();
    }

    private async Task<Product> FindProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Products.SingleOrDefaultAsync(
                item => item.Id == id && item.CompanyId == context.CompanyId,
                cancellationToken) ??
            throw ProductNotFound();
    }

    private async Task<ProductStandardBagWeight> FindBagStandardAsync(
        Guid productId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProductStandardBagWeights.SingleOrDefaultAsync(
                item =>
                    item.Id == id &&
                    item.ProductId == productId &&
                    item.CompanyId == context.CompanyId,
                cancellationToken) ??
            throw BagStandardNotFound();
    }

    private async Task<ApplicationProblemException> ReadVersionProblemAsync(
        IQueryable<long?> query,
        long expectedVersion,
        Func<ApplicationProblemException> notFound,
        CancellationToken cancellationToken)
    {
        var version = await query.SingleOrDefaultAsync(cancellationToken);
        return version is null
            ? notFound()
            : CommercialMasterDataInfrastructure.VersionConflict(
                expectedVersion,
                version.Value);
    }

    private async Task<ApplicationProblemException>
        ReadBagStandardConcurrencyProblemAsync(
            Guid productId,
            Guid id,
            long expectedVersion,
            CancellationToken cancellationToken)
    {
        return await ReadVersionProblemAsync(
            dbContext.ProductStandardBagWeights
                .Where(item =>
                    item.Id == id &&
                    item.ProductId == productId &&
                    item.CompanyId == context.CompanyId)
                .Select(item => (long?)item.Version),
            expectedVersion,
            BagStandardNotFound,
            cancellationToken);
    }

    private void AddFacts(
        string aggregateType,
        Guid id,
        long version,
        string action,
        string eventType,
        object? auditBefore,
        object auditAfter,
        object eventPayload,
        DateTimeOffset occurredAtUtc)
    {
        dbContext.AuditEvents.Add(
            AuditEvent.Create(
                context.WorkspaceId,
                context.CompanyId,
                context.DefaultBranchId,
                context.UserId,
                context.DeviceId,
                action,
                aggregateType,
                id,
                null,
                null,
                auditBefore is null
                    ? null
                    : JsonSerializer.Serialize(auditBefore, JsonOptions),
                JsonSerializer.Serialize(auditAfter, JsonOptions),
                context.CorrelationId,
                occurredAtUtc));
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(
                context.WorkspaceId,
                eventType,
                EventVersion,
                aggregateType,
                id,
                version,
                JsonSerializer.Serialize(eventPayload, JsonOptions),
                context.CorrelationId,
                occurredAtUtc,
                OutboxEventStream.Internal));
    }

    private void AddBagStandardFacts(
        ProductStandardBagWeight standard,
        string bagTypeCode,
        string action,
        string eventType,
        object? before,
        DateTimeOffset occurredAtUtc)
    {
        var auditAfter = ToBagStandardAuditSnapshot(standard);
        var eventPayload = ToBagStandardEventPayload(standard, bagTypeCode);
        dbContext.AuditEvents.Add(
            AuditEvent.Create(
                context.WorkspaceId,
                context.CompanyId,
                context.DefaultBranchId,
                context.UserId,
                context.DeviceId,
                action,
                "Catalog.ProductStandardBagWeight",
                standard.Id,
                null,
                null,
                before is null
                    ? null
                    : JsonSerializer.Serialize(before, JsonOptions),
                JsonSerializer.Serialize(auditAfter, JsonOptions),
                context.CorrelationId,
                occurredAtUtc));
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(
                context.WorkspaceId,
                eventType,
                EventVersion,
                "Catalog.ProductStandardBagWeight",
                standard.Id,
                standard.Version,
                JsonSerializer.Serialize(eventPayload, JsonOptions),
                context.CorrelationId,
                occurredAtUtc,
                OutboxEventStream.Internal));
    }

    private static object ToProductGroupAuditSnapshot(ProductGroup group)
    {
        return new
        {
            group.Code,
            group.Name,
            group.LocalName,
            group.Description,
            status = group.Status.ToString(),
            group.Version,
        };
    }

    private static object ToProductGroupEventPayload(ProductGroup group)
    {
        return new
        {
            productGroupId = group.Id,
            group.Code,
            status = group.Status.ToString(),
            group.Version,
        };
    }

    private static object ToProductAuditSnapshot(
        Product product,
        IReadOnlyList<string> changedFields)
    {
        return new
        {
            product.Code,
            product.ProductGroupId,
            product.Name,
            product.LocalName,
            productType = product.ProductType.ToString(),
            product.IsPurchasable,
            product.ProcessingFamilyCode,
            product.Description,
            status = product.Status.ToString(),
            product.Version,
            changedFields,
        };
    }

    private static object ToProductEventPayload(Product product)
    {
        return new
        {
            productId = product.Id,
            product.ProductGroupId,
            product.Code,
            productType = product.ProductType.ToString(),
            product.IsPurchasable,
            status = product.Status.ToString(),
            product.Version,
        };
    }

    private static object ToBagStandardAuditSnapshot(
        ProductStandardBagWeight standard)
    {
        return new
        {
            standard.ProductId,
            standard.BagTypeId,
            standard.Label,
            standardContentWeightKg = standard.StandardContentWeightKg
                .ToString(
                    "0.000000",
                    System.Globalization.CultureInfo.InvariantCulture),
            standard.IsDefault,
            status = standard.Status.ToString(),
            standard.Version,
        };
    }

    private static object ToBagStandardEventPayload(
        ProductStandardBagWeight standard,
        string bagTypeCode)
    {
        return new
        {
            standardId = standard.Id,
            standard.ProductId,
            standard.BagTypeId,
            bagTypeCode,
            standard.IsDefault,
            status = standard.Status.ToString(),
            standard.Version,
        };
    }

    private void RequireContext()
    {
        if (!context.IsBound)
        {
            throw new ApplicationProblemException(
                "AUTHENTICATED_CONTEXT_INVALID",
                "The authenticated TraderPro context is invalid.",
                ApplicationErrorCategory.Authentication);
        }
    }

    private static ApplicationProblemException ProductGroupNotFound() =>
        new(
            "PRODUCT_GROUP_NOT_FOUND",
            "The product group was not found.",
            ApplicationErrorCategory.NotFound);

    private static ApplicationProblemException ProductGroupCodeExists() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_GROUP_CODE_EXISTS",
            "A product group with this code already exists.");

    private static ApplicationProblemException ProductGroupInUse() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_GROUP_IN_USE",
            "A product group with active products cannot be deactivated.");

    private static ApplicationProblemException ProductGroupInactive() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_GROUP_INACTIVE",
            "An active product requires an active same-company product group.");

    private static ApplicationProblemException ProductNotFound() =>
        new(
            "PRODUCT_NOT_FOUND",
            "The product was not found.",
            ApplicationErrorCategory.NotFound);

    private static ApplicationProblemException ProductCodeExists() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_CODE_EXISTS",
            "A product with this code already exists.");

    private static ApplicationProblemException ProductNotPurchasable() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_NOT_PURCHASABLE",
            "The product must be active and purchasable.");

    private static ApplicationProblemException ProductSupplierScopeInUse() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_SUPPLIER_SCOPE_IN_USE",
            "Active supplier scopes must be deactivated first.");

    private static ApplicationProblemException ProductBagStandardInUse() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_STANDARD_BAG_WEIGHT_IN_USE",
            "Active standard bag weights must be deactivated first.");

    private static ApplicationProblemException BagStandardNotFound() =>
        new(
            "PRODUCT_STANDARD_BAG_WEIGHT_NOT_FOUND",
            "The product standard bag weight was not found.",
            ApplicationErrorCategory.NotFound);

    private static ApplicationProblemException BagStandardExists() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_STANDARD_BAG_WEIGHT_EXISTS",
            "This product, Bag Type, and content-weight standard already exists.");

    private static ApplicationProblemException BagStandardInvalid(
        string message) =>
        new(
            "PRODUCT_STANDARD_BAG_WEIGHT_INVALID",
            message,
            ApplicationErrorCategory.Conflict);

    private static ApplicationProblemException BagStandardDefaultConflict() =>
        CommercialMasterDataInfrastructure.Conflict(
            "PRODUCT_STANDARD_BAG_WEIGHT_DEFAULT_CONFLICT",
            "The product already has an active default standard.");

    private static ApplicationProblemException? MapPersistenceProblem(
        Exception exception)
    {
        var postgres =
            CommercialMasterDataInfrastructure.PostgreSql(exception);
        return postgres?.ConstraintName switch
        {
            "ux_product_groups_workspace_company_code" =>
                ProductGroupCodeExists(),
            "ux_products_workspace_company_code" => ProductCodeExists(),
            "ck_product_group_in_use" => ProductGroupInUse(),
            "ck_product_group_active" => ProductGroupInactive(),
            "ck_product_supplier_scope_in_use" =>
                ProductSupplierScopeInUse(),
            "ck_product_standard_bag_weight_in_use" =>
                ProductBagStandardInUse(),
            "ux_product_standard_bag_weights_identity" =>
                BagStandardExists(),
            "ux_product_standard_bag_weights_active_default" =>
                BagStandardDefaultConflict(),
            "ck_product_standard_bag_weight_active_references" =>
                BagStandardInvalid(
                    "The Product and Bag Type must be active and belong to the company."),
            _ => null,
        };
    }
}
