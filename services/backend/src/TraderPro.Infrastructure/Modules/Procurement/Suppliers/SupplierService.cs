using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Application.Procurement.Suppliers;
using TraderPro.Domain.Catalog;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.Suppliers;
using TraderPro.Infrastructure.Modules.Shared;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Procurement.Suppliers;

internal sealed class SupplierService(
    TraderProDbContext dbContext,
    IAuthenticatedTraderProContext context,
    IClock clock,
    PostgreSqlIdempotentCommandExecutor idempotency) : ISupplierService
{
    private const int EventVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<IdempotentCommandResult<SupplierResult>> CreateAsync(
        CreateSupplierCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var productIds = CommercialMasterDataInputRules
            .CanonicalInitialProductIds(command.InitialProductIds);
        var normalizedCode =
            CommercialMasterDataInfrastructure.NormalizeCode(command.Code);
        var supplierType = CommercialMasterDataInputRules.ParseSupplierType(
            command.SupplierType);
        var scopeMode = CommercialMasterDataInputRules
            .ParseSupplierProductScopeMode(
                command.ProductScopeMode,
                useDefault: true);
        var supplier = CommercialMasterDataInfrastructure.Domain(
            () => Supplier.Create(
                context.WorkspaceId,
                context.CompanyId,
                normalizedCode,
                command.Name,
                command.LocalName,
                supplierType,
                scopeMode,
                productIds.Length > 0,
                command.ContactName,
                command.ContactNumber,
                command.Email,
                command.AddressLine,
                command.TaxRegistrationNumber,
                command.Notes,
                clock.UtcNow));
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateSupplier,
            context,
            ("code", supplier.Code),
            ("name", supplier.Name),
            ("localName", supplier.LocalName),
            ("supplierType", supplier.SupplierType),
            ("productScopeMode", supplier.ProductScopeMode),
            ("contactName", supplier.ContactName),
            ("contactNumber", supplier.ContactNumber),
            ("email", supplier.Email),
            ("addressLine", supplier.AddressLine),
            ("taxRegistrationNumber", supplier.TaxRegistrationNumber),
            ("normalizedTaxRegistrationNumber",
                supplier.NormalizedTaxRegistrationNumber),
            ("notes", supplier.Notes),
            ("initialProductIds", string.Join(",", productIds.Select(
                id => id.ToString("D")))));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.CreateSupplier,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            201,
            async () =>
            {
                await PostgreSqlCommercialCatalogLocks
                    .AcquireSupplierScopeAsync(
                        dbContext,
                        context.WorkspaceId,
                        context.CompanyId,
                        supplier.Id,
                        cancellationToken);
                await EnsureSupplierUniqueAsync(
                    supplier.NormalizedCode,
                    supplier.NormalizedTaxRegistrationNumber,
                    null,
                    cancellationToken);
                var products = await ValidateProductsAsync(
                    productIds,
                    cancellationToken);
                dbContext.Suppliers.Add(supplier);
                AddSupplierFacts(
                    supplier,
                    "Procurement.Supplier.Created",
                    "Procurement.SupplierCreated",
                    null,
                    supplier.CreatedAtUtc);
                foreach (var product in products.OrderBy(item => item.Id))
                {
                    var scope = SupplierProductScope.Create(
                        context.WorkspaceId,
                        context.CompanyId,
                        supplier.Id,
                        product.Id,
                        supplier.CreatedAtUtc);
                    dbContext.SupplierProductScopes.Add(scope);
                    AddScopeFacts(
                        scope,
                        product,
                        "Procurement.SupplierProductScope.Created",
                        "Procurement.SupplierProductScopeAdded",
                        null,
                        supplier.CreatedAtUtc);
                }
                return ToResult(supplier);
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<SupplierResult>> UpdateAsync(
        UpdateSupplierCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        CommercialMasterDataInputRules.RequireExpectedVersion(
            command.ExpectedVersion);
        var supplierType = CommercialMasterDataInputRules.ParseSupplierType(
            command.SupplierType);
        var scopeMode = CommercialMasterDataInputRules
            .ParseSupplierProductScopeMode(command.ProductScopeMode);
        var canonicalContact = CommercialMasterDataInfrastructure.Domain(
            () => Supplier.CanonicalizeContactNumber(command.ContactNumber));
        var canonicalEmail = CommercialMasterDataInfrastructure.Domain(
            () => Supplier.CanonicalizeEmail(command.Email));
        var canonicalTax = CommercialMasterDataInfrastructure.Domain(
            () => Supplier.CanonicalizeTaxRegistration(
                command.TaxRegistrationNumber));
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateSupplier,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("supplierType", supplierType),
            ("productScopeMode", scopeMode),
            ("contactName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.ContactName)),
            ("contactNumber", canonicalContact),
            ("email", canonicalEmail),
            ("addressLine", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.AddressLine)),
            ("taxRegistrationNumber", canonicalTax.Display),
            ("normalizedTaxRegistrationNumber", canonicalTax.Normalized),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return ExecuteSupplierMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.UpdateSupplier,
            idempotencyKey,
            hash,
            "Procurement.Supplier.Updated",
            "Procurement.SupplierUpdated",
            async (supplier, now) =>
            {
                await PostgreSqlCommercialCatalogLocks
                    .AcquireSupplierScopeAsync(
                        dbContext,
                        context.WorkspaceId,
                        context.CompanyId,
                        supplier.Id,
                        cancellationToken);
                await EnsureSupplierUniqueAsync(
                    supplier.NormalizedCode,
                    canonicalTax.Normalized,
                    supplier.Id,
                    cancellationToken);
                var hasActiveScope = await dbContext.SupplierProductScopes
                    .AnyAsync(
                        item =>
                            item.SupplierId == supplier.Id &&
                            item.CompanyId == context.CompanyId &&
                            item.Status == MasterDataStatus.Active,
                        cancellationToken);
                CommercialMasterDataInfrastructure.Domain(
                    () => supplier.Update(
                        command.Name,
                        command.LocalName,
                        supplierType,
                        scopeMode,
                        hasActiveScope,
                        command.ContactName,
                        command.ContactNumber,
                        command.Email,
                        command.AddressLine,
                        command.TaxRegistrationNumber,
                        command.Notes,
                        now));
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<SupplierResult>> SetStatusAsync(
        MasterStatusCommand command,
        bool activate,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var commandType = activate
            ? CommercialMasterDataCommandTypes.ReactivateSupplier
            : CommercialMasterDataCommandTypes.DeactivateSupplier;
        var hash = CommercialMasterDataRequestHash.Compute(
            commandType,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return ExecuteSupplierMutationAsync(
            command.Id,
            command.ExpectedVersion,
            commandType,
            idempotencyKey,
            hash,
            activate
                ? "Procurement.Supplier.Reactivated"
                : "Procurement.Supplier.Deactivated",
            activate
                ? "Procurement.SupplierReactivated"
                : "Procurement.SupplierDeactivated",
            (supplier, now) =>
            {
                CommercialMasterDataInfrastructure.Domain(
                    () =>
                    {
                        if (activate)
                        {
                            supplier.Reactivate(now);
                        }
                        else
                        {
                            supplier.Deactivate(now);
                        }
                    });
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    public async Task<SupplierResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var supplier = await dbContext.Suppliers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id && item.CompanyId == context.CompanyId,
                cancellationToken);
        return supplier is null ? throw SupplierNotFound() : ToResult(supplier);
    }

    public async Task<MasterPage<SupplierResult>> ListAsync(
        SupplierListQuery query,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(query);
        var master = query.Master;
        SupplierType? supplierType = query.SupplierType is null
            ? null
            : CommercialMasterDataInputRules.ParseSupplierType(
                query.SupplierType);
        SupplierProductScopeMode? productScopeMode =
            query.ProductScopeMode is null
            ? null
            : CommercialMasterDataInputRules.ParseSupplierProductScopeMode(
                query.ProductScopeMode);
        var filterScope =
            $"supplierType={supplierType?.ToString() ?? "*"};scopeMode={productScopeMode?.ToString() ?? "*"}";
        var scope = new CommercialMasterCursorScope(
            CommercialMasterKinds.Supplier,
            context.WorkspaceId,
            context.CompanyId,
            null,
            master.Status,
            master.Search,
            filterScope);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            master.Cursor,
            scope);
        var rows = dbContext.Suppliers
            .AsNoTracking()
            .Where(item => item.CompanyId == context.CompanyId);
        rows = CommercialMasterDataInfrastructure.ApplyStatus(
            rows,
            master.Status,
            item => item.Status);
        if (supplierType is not null)
        {
            rows = rows.Where(item =>
                item.SupplierType == supplierType.Value);
        }

        if (productScopeMode is not null)
        {
            rows = rows.Where(item =>
                item.ProductScopeMode == productScopeMode.Value);
        }

        if (master.Search is not null)
        {
            var pattern = $"%{EscapeLike(master.Search)}%";
            rows = rows.Where(item =>
                EF.Functions.ILike(item.Code, pattern, "\\") ||
                EF.Functions.ILike(item.Name, pattern, "\\") ||
                (item.LocalName != null &&
                    EF.Functions.ILike(item.LocalName, pattern, "\\")));
        }

        if (cursor is not null)
        {
            rows = rows.Where(item =>
                string.Compare(item.NormalizedCode, cursor.Value.Code) > 0 ||
                (item.NormalizedCode == cursor.Value.Code &&
                    item.Id.CompareTo(cursor.Value.Id) > 0));
        }

        var page = await rows
            .OrderBy(item => item.NormalizedCode)
            .ThenBy(item => item.Id)
            .Take(master.Limit + 1)
            .ToListAsync(cancellationToken);
        var hasMore = page.Count > master.Limit;
        var selected = page.Take(master.Limit).ToArray();
        return new MasterPage<SupplierResult>(
            selected.Select(ToResult).ToArray(),
            hasMore
                ? CommercialMasterDataInfrastructure.EncodeCursor(
                    scope,
                    selected[^1].NormalizedCode,
                    selected[^1].Id)
                : null,
            hasMore);
    }

    public Task<IdempotentCommandResult<SupplierProductScopeResult>>
        AddScopeAsync(
            AddSupplierProductScopeCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.AddSupplierProductScope,
            context,
            ("supplierId", command.SupplierId),
            ("productId", command.ProductId));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.AddSupplierProductScope,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            201,
            async () =>
            {
                await PostgreSqlCommercialCatalogLocks
                    .AcquireSupplierScopeAsync(
                        dbContext,
                        context.WorkspaceId,
                        context.CompanyId,
                        command.SupplierId,
                        cancellationToken);
                var supplier = await FindSupplierAsync(
                    command.SupplierId,
                    cancellationToken);
                if (supplier.Status is not MasterDataStatus.Active)
                {
                    throw ScopeInvalid(
                        "Only an active supplier can receive a product scope.");
                }

                var product = await FindActivePurchasableProductAsync(
                    command.ProductId,
                    cancellationToken);
                if (await dbContext.SupplierProductScopes.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.SupplierId == supplier.Id &&
                            item.ProductId == product.Id,
                        cancellationToken))
                {
                    throw ScopeExists();
                }

                var now = clock.UtcNow;
                var scope = SupplierProductScope.Create(
                    context.WorkspaceId,
                    context.CompanyId,
                    supplier.Id,
                    product.Id,
                    now);
                dbContext.SupplierProductScopes.Add(scope);
                AddScopeFacts(
                    scope,
                    product,
                    "Procurement.SupplierProductScope.Created",
                    "Procurement.SupplierProductScopeAdded",
                    null,
                    now);
                return ToResult(scope, product);
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<SupplierProductScopeResult>>
        SetScopeStatusAsync(
            SupplierProductScopeStatusCommand command,
            bool activate,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        CommercialMasterDataInputRules.RequireExpectedVersion(
            command.ExpectedVersion);
        var commandType = activate
            ? CommercialMasterDataCommandTypes.ReactivateSupplierProductScope
            : CommercialMasterDataCommandTypes.DeactivateSupplierProductScope;
        var hash = CommercialMasterDataRequestHash.Compute(
            commandType,
            context,
            ("supplierId", command.SupplierId),
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
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
                await PostgreSqlCommercialCatalogLocks
                    .AcquireSupplierScopeAsync(
                        dbContext,
                        context.WorkspaceId,
                        context.CompanyId,
                        command.SupplierId,
                        cancellationToken);
                var supplier = await FindSupplierAsync(
                    command.SupplierId,
                    cancellationToken);
                var scope = await dbContext.SupplierProductScopes
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == command.Id &&
                            item.SupplierId == command.SupplierId &&
                            item.CompanyId == context.CompanyId,
                        cancellationToken) ??
                    throw ScopeNotFound();
                if (scope.Version != command.ExpectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        command.ExpectedVersion,
                        scope.Version);
                }

                Product product;
                if (activate)
                {
                    if (supplier.Status is not MasterDataStatus.Active)
                    {
                        throw ScopeInvalid(
                            "Only an active supplier can reactivate a product scope.");
                    }

                    product = await FindActivePurchasableProductAsync(
                        scope.ProductId,
                        cancellationToken);
                }
                else
                {
                    product = await dbContext.Products.SingleOrDefaultAsync(
                            item =>
                                item.Id == scope.ProductId &&
                                item.CompanyId == context.CompanyId,
                            cancellationToken) ??
                        throw ScopeInvalid("The scoped product is unavailable.");
                }

                var before = ToSafeScopeSnapshot(scope, product);
                var now = clock.UtcNow;
                if (activate)
                {
                    CommercialMasterDataInfrastructure.Domain(
                        () => scope.Reactivate(now));
                }
                else
                {
                    var activeCount = await dbContext.SupplierProductScopes
                        .CountAsync(
                            item =>
                                item.SupplierId == supplier.Id &&
                                item.CompanyId == context.CompanyId &&
                                item.Status == MasterDataStatus.Active,
                            cancellationToken);
                    CommercialMasterDataInfrastructure.Domain(
                        () => scope.Deactivate(
                            supplier.ProductScopeMode is
                                SupplierProductScopeMode.Restricted,
                            activeCount,
                            now));
                }

                AddScopeFacts(
                    scope,
                    product,
                    activate
                        ? "Procurement.SupplierProductScope.Reactivated"
                        : "Procurement.SupplierProductScope.Deactivated",
                    activate
                        ? "Procurement.SupplierProductScopeReactivated"
                        : "Procurement.SupplierProductScopeDeactivated",
                    before,
                    now);
                return ToResult(scope, product);
            },
            IsReplayable,
            () => ReadScopeConcurrencyProblemAsync(
                command,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public async Task<MasterPage<SupplierProductScopeResult>> ListScopesAsync(
        Guid supplierId,
        MasterListQuery query,
        CancellationToken cancellationToken)
    {
        RequireContext();
        _ = await dbContext.Suppliers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == supplierId &&
                    item.CompanyId == context.CompanyId,
                cancellationToken) ??
            throw SupplierNotFound();
        var filterScope = $"supplierId={supplierId:D}";
        var scope = new CommercialMasterCursorScope(
            CommercialMasterKinds.SupplierProductScope,
            context.WorkspaceId,
            context.CompanyId,
            null,
            query.Status,
            query.Search,
            filterScope);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            query.Cursor,
            scope);
        var rows =
            from association in dbContext.SupplierProductScopes.AsNoTracking()
            join product in dbContext.Products.AsNoTracking()
                on association.ProductId equals product.Id
            where association.CompanyId == context.CompanyId &&
                association.SupplierId == supplierId
            select new { Association = association, Product = product };
        rows = query.Status switch
        {
            MasterStatusFilter.Active => rows.Where(item =>
                item.Association.Status == MasterDataStatus.Active),
            MasterStatusFilter.Inactive => rows.Where(item =>
                item.Association.Status == MasterDataStatus.Inactive),
            _ => rows,
        };
        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
            rows = rows.Where(item =>
                EF.Functions.ILike(item.Product.Code, pattern, "\\") ||
                EF.Functions.ILike(item.Product.Name, pattern, "\\"));
        }

        if (cursor is not null)
        {
            rows = rows.Where(item =>
                string.Compare(
                    item.Product.NormalizedCode,
                    cursor.Value.Code) > 0 ||
                (item.Product.NormalizedCode == cursor.Value.Code &&
                    item.Association.Id.CompareTo(cursor.Value.Id) > 0));
        }

        var page = await rows
            .OrderBy(item => item.Product.NormalizedCode)
            .ThenBy(item => item.Association.Id)
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        var hasMore = page.Count > query.Limit;
        var selected = page.Take(query.Limit).ToArray();
        return new MasterPage<SupplierProductScopeResult>(
            selected.Select(item => ToResult(
                item.Association,
                item.Product)).ToArray(),
            hasMore
                ? CommercialMasterDataInfrastructure.EncodeCursor(
                    scope,
                    selected[^1].Product.NormalizedCode,
                    selected[^1].Association.Id)
                : null,
            hasMore);
    }

    private Task<IdempotentCommandResult<SupplierResult>>
        ExecuteSupplierMutationAsync(
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string hash,
            string action,
            string eventType,
            Func<Supplier, DateTimeOffset, Task> mutate,
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
                var supplier = await FindSupplierAsync(id, cancellationToken);
                if (supplier.Version != expectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        expectedVersion,
                        supplier.Version);
                }

                var before = ToSafeSupplierAuditSnapshot(supplier, []);
                var protectedFieldsBefore =
                    SupplierProtectedFields.From(supplier);
                var now = clock.UtcNow;
                await mutate(supplier, now);
                AddSupplierFacts(
                    supplier,
                    action,
                    eventType,
                    before,
                    now,
                    ChangedProtectedFields(
                        protectedFieldsBefore,
                        supplier));
                return ToResult(supplier);
            },
            IsReplayable,
            () => ReadSupplierConcurrencyProblemAsync(
                id,
                expectedVersion,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private async Task EnsureSupplierUniqueAsync(
        string normalizedCode,
        string? normalizedTax,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var duplicate = await dbContext.Suppliers
            .Where(item =>
                item.CompanyId == context.CompanyId &&
                (!exceptId.HasValue || item.Id != exceptId.Value))
            .Where(item =>
                item.NormalizedCode == normalizedCode ||
                (normalizedTax != null &&
                    item.NormalizedTaxRegistrationNumber == normalizedTax))
            .Select(item => new
            {
                item.NormalizedCode,
                item.NormalizedTaxRegistrationNumber,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (duplicate?.NormalizedCode == normalizedCode)
        {
            throw SupplierCodeExists();
        }

        if (duplicate is not null)
        {
            throw SupplierTaxExists();
        }
    }

    private async Task<Product[]> ValidateProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var products = await dbContext.Products
            .Where(item =>
                item.CompanyId == context.CompanyId &&
                productIds.Contains(item.Id) &&
                item.Status == MasterDataStatus.Active &&
                item.IsPurchasable)
            .ToArrayAsync(cancellationToken);
        if (products.Length != productIds.Count)
        {
            throw ScopeInvalid(
                "Initial scopes require active purchasable same-company products.");
        }

        return products;
    }

    private async Task<Product> FindActivePurchasableProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Products.SingleOrDefaultAsync(
                item =>
                    item.Id == productId &&
                    item.CompanyId == context.CompanyId &&
                    item.Status == MasterDataStatus.Active &&
                    item.IsPurchasable,
                cancellationToken) ??
            throw new ApplicationProblemException(
                "PRODUCT_NOT_PURCHASABLE",
                "The product must be active and purchasable.",
                ApplicationErrorCategory.Conflict);
    }

    private async Task<Supplier> FindSupplierAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Suppliers.SingleOrDefaultAsync(
                item => item.Id == id && item.CompanyId == context.CompanyId,
                cancellationToken) ??
            throw SupplierNotFound();
    }

    private async Task<ApplicationProblemException>
        ReadSupplierConcurrencyProblemAsync(
            Guid id,
            long expectedVersion,
            CancellationToken cancellationToken)
    {
        var version = await dbContext.Suppliers
            .AsNoTracking()
            .Where(item => item.Id == id && item.CompanyId == context.CompanyId)
            .Select(item => (long?)item.Version)
            .SingleOrDefaultAsync(cancellationToken);
        return version is null
            ? SupplierNotFound()
            : CommercialMasterDataInfrastructure.VersionConflict(
                expectedVersion,
                version.Value);
    }

    private async Task<ApplicationProblemException>
        ReadScopeConcurrencyProblemAsync(
            SupplierProductScopeStatusCommand command,
            CancellationToken cancellationToken)
    {
        var version = await dbContext.SupplierProductScopes
            .AsNoTracking()
            .Where(item =>
                item.Id == command.Id &&
                item.SupplierId == command.SupplierId &&
                item.CompanyId == context.CompanyId)
            .Select(item => (long?)item.Version)
            .SingleOrDefaultAsync(cancellationToken);
        return version is null
            ? ScopeNotFound()
            : CommercialMasterDataInfrastructure.VersionConflict(
                command.ExpectedVersion,
                version.Value);
    }

    private void AddSupplierFacts(
        Supplier supplier,
        string action,
        string eventType,
        object? before,
        DateTimeOffset occurredAtUtc,
        IReadOnlyList<string>? changedFields = null)
    {
        var auditAfter = ToSafeSupplierAuditSnapshot(
            supplier,
            changedFields ?? []);
        var eventPayload = ToSupplierEventPayload(supplier);
        dbContext.AuditEvents.Add(
            AuditEvent.Create(
                context.WorkspaceId,
                context.CompanyId,
                context.DefaultBranchId,
                context.UserId,
                context.DeviceId,
                action,
                "Procurement.Supplier",
                supplier.Id,
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
                "Procurement.Supplier",
                supplier.Id,
                supplier.Version,
                JsonSerializer.Serialize(eventPayload, JsonOptions),
                context.CorrelationId,
                occurredAtUtc,
                OutboxEventStream.Internal));
    }

    private void AddScopeFacts(
        SupplierProductScope scope,
        Product product,
        string action,
        string eventType,
        object? before,
        DateTimeOffset occurredAtUtc)
    {
        var after = ToSafeScopeSnapshot(scope, product);
        dbContext.AuditEvents.Add(
            AuditEvent.Create(
                context.WorkspaceId,
                context.CompanyId,
                context.DefaultBranchId,
                context.UserId,
                context.DeviceId,
                action,
                "Procurement.SupplierProductScope",
                scope.Id,
                null,
                null,
                before is null
                    ? null
                    : JsonSerializer.Serialize(before, JsonOptions),
                JsonSerializer.Serialize(after, JsonOptions),
                context.CorrelationId,
                occurredAtUtc));
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(
                context.WorkspaceId,
                eventType,
                EventVersion,
                "Procurement.SupplierProductScope",
                scope.Id,
                scope.Version,
                JsonSerializer.Serialize(after, JsonOptions),
                context.CorrelationId,
                occurredAtUtc,
                OutboxEventStream.Internal));
    }

    private static SupplierResult ToResult(Supplier supplier)
    {
        return new SupplierResult(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.LocalName,
            supplier.SupplierType.ToString(),
            supplier.ProductScopeMode.ToString(),
            supplier.ContactName,
            supplier.ContactNumber,
            supplier.Email,
            supplier.AddressLine,
            supplier.TaxRegistrationNumber,
            supplier.NormalizedTaxRegistrationNumber,
            supplier.Notes,
            supplier.Status.ToString(),
            supplier.CreatedAtUtc,
            supplier.UpdatedAtUtc,
            supplier.Version);
    }

    private static SupplierProductScopeResult ToResult(
        SupplierProductScope scope,
        Product product)
    {
        return new SupplierProductScopeResult(
            scope.Id,
            scope.SupplierId,
            scope.ProductId,
            product.Code,
            product.Name,
            scope.Status.ToString(),
            scope.CreatedAtUtc,
            scope.UpdatedAtUtc,
            scope.Version);
    }

    private static object ToSafeSupplierAuditSnapshot(
        Supplier supplier,
        IReadOnlyList<string> changedFields)
    {
        return new
        {
            supplierId = supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.LocalName,
            supplierType = supplier.SupplierType.ToString(),
            productScopeMode = supplier.ProductScopeMode.ToString(),
            status = supplier.Status.ToString(),
            supplier.Version,
            changedFields,
        };
    }

    private static object ToSupplierEventPayload(Supplier supplier)
    {
        return new
        {
            supplierId = supplier.Id,
            supplier.Code,
            supplierType = supplier.SupplierType.ToString(),
            productScopeMode = supplier.ProductScopeMode.ToString(),
            status = supplier.Status.ToString(),
            supplier.Version,
        };
    }

    private static object ToSafeScopeSnapshot(
        SupplierProductScope scope,
        Product product)
    {
        return new
        {
            scopeId = scope.Id,
            scope.SupplierId,
            scope.ProductId,
            productCode = product.Code,
            status = scope.Status.ToString(),
            scope.Version,
        };
    }

    private static string[] ChangedProtectedFields(
        SupplierProtectedFields before,
        Supplier after)
    {
        var changed = new List<string>(6);
        if (!string.Equals(
                before.ContactName,
                after.ContactName,
                StringComparison.Ordinal))
        {
            changed.Add("contactName");
        }

        if (!string.Equals(
                before.ContactNumber,
                after.ContactNumber,
                StringComparison.Ordinal))
        {
            changed.Add("contactNumber");
        }

        if (!string.Equals(before.Email, after.Email, StringComparison.Ordinal))
        {
            changed.Add("email");
        }

        if (!string.Equals(
                before.AddressLine,
                after.AddressLine,
                StringComparison.Ordinal))
        {
            changed.Add("addressLine");
        }

        if (!string.Equals(
                before.TaxRegistrationNumber,
                after.TaxRegistrationNumber,
                StringComparison.Ordinal) ||
            !string.Equals(
                before.NormalizedTaxRegistrationNumber,
                after.NormalizedTaxRegistrationNumber,
                StringComparison.Ordinal))
        {
            changed.Add("taxRegistrationNumber");
        }

        if (!string.Equals(before.Notes, after.Notes, StringComparison.Ordinal))
        {
            changed.Add("notes");
        }

        return [.. changed];
    }

    private sealed record SupplierProtectedFields(
        string? ContactName,
        string? ContactNumber,
        string? Email,
        string? AddressLine,
        string? TaxRegistrationNumber,
        string? NormalizedTaxRegistrationNumber,
        string? Notes)
    {
        public static SupplierProtectedFields From(Supplier supplier) =>
            new(
                supplier.ContactName,
                supplier.ContactNumber,
                supplier.Email,
                supplier.AddressLine,
                supplier.TaxRegistrationNumber,
                supplier.NormalizedTaxRegistrationNumber,
                supplier.Notes);
    }

    private static bool IsReplayable(SupplierResult result)
    {
        return result.Id != Guid.Empty &&
            result.Code.Length is >= 2 and <= 32 &&
            result.Version > 0;
    }

    private static bool IsReplayable(SupplierProductScopeResult result)
    {
        return result.Id != Guid.Empty &&
            result.SupplierId != Guid.Empty &&
            result.ProductId != Guid.Empty &&
            result.Version > 0;
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

    private static ApplicationProblemException SupplierNotFound() =>
        new(
            "SUPPLIER_NOT_FOUND",
            "The supplier was not found.",
            ApplicationErrorCategory.NotFound);

    private static ApplicationProblemException ScopeNotFound() =>
        new(
            "SUPPLIER_PRODUCT_SCOPE_NOT_FOUND",
            "The supplier product scope was not found.",
            ApplicationErrorCategory.NotFound);

    private static ApplicationProblemException SupplierCodeExists() =>
        CommercialMasterDataInfrastructure.Conflict(
            "SUPPLIER_CODE_EXISTS",
            "A supplier with this code already exists.");

    private static ApplicationProblemException SupplierTaxExists() =>
        CommercialMasterDataInfrastructure.Conflict(
            "SUPPLIER_TAX_REGISTRATION_EXISTS",
            "A supplier with this tax registration already exists.");

    private static ApplicationProblemException ScopeExists() =>
        CommercialMasterDataInfrastructure.Conflict(
            "SUPPLIER_PRODUCT_SCOPE_EXISTS",
            "This supplier product scope already exists.");

    private static ApplicationProblemException ScopeInvalid(string message) =>
        new(
            "SUPPLIER_PRODUCT_SCOPE_INVALID",
            message,
            ApplicationErrorCategory.Conflict);

    private static ApplicationProblemException ScopeRequired() =>
        CommercialMasterDataInfrastructure.Conflict(
            "SUPPLIER_PRODUCT_SCOPE_REQUIRED",
            "A Restricted supplier must retain at least one active product scope.");

    private static ApplicationProblemException? MapPersistenceProblem(
        Exception exception)
    {
        var postgres =
            CommercialMasterDataInfrastructure.PostgreSql(exception);
        return postgres?.ConstraintName switch
        {
            "ux_suppliers_workspace_company_code" => SupplierCodeExists(),
            "ux_suppliers_workspace_company_tax" => SupplierTaxExists(),
            "ux_supplier_product_scopes_identity" => ScopeExists(),
            "ck_supplier_product_scope_required" => ScopeRequired(),
            "ck_supplier_product_scope_active_references" =>
                ScopeInvalid(
                    "Scopes require an active supplier and active purchasable product."),
            _ => null,
        };
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
