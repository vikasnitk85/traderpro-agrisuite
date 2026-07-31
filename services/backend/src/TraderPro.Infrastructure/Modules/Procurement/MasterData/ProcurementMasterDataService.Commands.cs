using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Application.Procurement.MasterData;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Operations;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Infrastructure.Modules.Shared;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Procurement.MasterData;

internal sealed partial class ProcurementMasterDataService(
    TraderProDbContext dbContext,
    IAuthenticatedTraderProContext context,
    IClock clock,
    PostgreSqlIdempotentCommandExecutor idempotency) :
    IProcurementMasterDataService
{
    private const int EventVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<IdempotentCommandResult<ReceivingVehicleResult>>
        CreateVehicleAsync(
            CreateReceivingVehicleCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var normalizedCode =
            CommercialMasterDataInfrastructure.NormalizeCode(command.Code);
        var vehicleType =
            CommercialMasterDataInputRules.ParseVehicleType(
                command.VehicleType);
        var canonicalRegistration = CommercialMasterDataInputRules
            .CanonicalRegistrationDisplay(command.RegistrationNumber);
        string normalizedRegistration;
        try
        {
            normalizedRegistration =
                MasterDataValueRules.NormalizeRegistration(
                    canonicalRegistration);
        }
        catch (MasterDataDomainException exception)
        {
            throw MasterDataProblem.FromDomain(exception);
        }

        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateVehicle,
            context,
            ("code", normalizedCode),
            ("registrationNumber", canonicalRegistration),
            ("normalizedRegistrationNumber", normalizedRegistration),
            ("displayName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.DisplayName)),
            ("vehicleType", vehicleType),
            ("ownerName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.OwnerName)),
            ("contactNumber", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.ContactNumber)),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.CreateVehicle,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            201,
            async () =>
            {
                var duplicate = await dbContext.ReceivingVehicles
                    .Where(item => item.CompanyId == context.CompanyId)
                    .Where(item =>
                        item.NormalizedCode == normalizedCode ||
                        item.NormalizedRegistrationNumber ==
                            normalizedRegistration)
                    .Select(item => new
                    {
                        item.NormalizedCode,
                        item.NormalizedRegistrationNumber,
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                if (duplicate?.NormalizedCode == normalizedCode)
                {
                    throw VehicleCodeExists();
                }

                if (duplicate is not null)
                {
                    throw VehicleRegistrationExists();
                }

                var now = clock.UtcNow;
                var vehicle = CommercialMasterDataInfrastructure.Domain(
                    () => ReceivingVehicle.Create(
                        context.WorkspaceId,
                        context.CompanyId,
                        normalizedCode,
                        canonicalRegistration,
                        command.DisplayName,
                        vehicleType,
                        command.OwnerName,
                        command.ContactNumber,
                        command.Notes,
                        now));
                dbContext.ReceivingVehicles.Add(vehicle);
                var result = ToResult(vehicle);
                AddMasterFacts(
                    "Procurement.ReceivingVehicle",
                    vehicle.Id,
                    vehicle.Status,
                    vehicle.Version,
                    "Procurement.ReceivingVehicle.Created",
                    "Procurement.ReceivingVehicleCreated",
                    null,
                    result,
                    now);
                return result;
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ReceivingVehicleResult>>
        UpdateVehicleAsync(
            UpdateReceivingVehicleCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var vehicleType =
            CommercialMasterDataInputRules.ParseVehicleType(
                command.VehicleType);
        var canonicalRegistration = CommercialMasterDataInputRules
            .CanonicalRegistrationDisplay(command.RegistrationNumber);
        string normalizedRegistration;
        try
        {
            normalizedRegistration =
                MasterDataValueRules.NormalizeRegistration(
                    canonicalRegistration);
        }
        catch (MasterDataDomainException exception)
        {
            throw MasterDataProblem.FromDomain(exception);
        }

        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateVehicle,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion),
            ("registrationNumber", canonicalRegistration),
            ("normalizedRegistrationNumber", normalizedRegistration),
            ("displayName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.DisplayName)),
            ("vehicleType", vehicleType),
            ("ownerName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.OwnerName)),
            ("contactNumber", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.ContactNumber)),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return ExecuteVehicleMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.UpdateVehicle,
            idempotencyKey,
            hash,
            "Procurement.ReceivingVehicle.Updated",
            "Procurement.ReceivingVehicleUpdated",
            async (vehicle, now) =>
            {
                if (await dbContext.ReceivingVehicles.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.Id != vehicle.Id &&
                            item.NormalizedRegistrationNumber ==
                                normalizedRegistration,
                        cancellationToken))
                {
                    throw VehicleRegistrationExists();
                }

                CommercialMasterDataInfrastructure.Domain(
                    () => vehicle.Update(
                        canonicalRegistration,
                        command.DisplayName,
                        vehicleType,
                        command.OwnerName,
                        command.ContactNumber,
                        command.Notes,
                        now));
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ReceivingVehicleResult>>
        SetVehicleStatusAsync(
            MasterStatusCommand command,
            bool activate,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var commandType = activate
            ? CommercialMasterDataCommandTypes.ReactivateVehicle
            : CommercialMasterDataCommandTypes.DeactivateVehicle;
        var hash = CommercialMasterDataRequestHash.Compute(
            commandType,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return ExecuteVehicleMutationAsync(
            command.Id,
            command.ExpectedVersion,
            commandType,
            idempotencyKey,
            hash,
            activate
                ? "Procurement.ReceivingVehicle.Reactivated"
                : "Procurement.ReceivingVehicle.Deactivated",
            activate
                ? "Procurement.ReceivingVehicleReactivated"
                : "Procurement.ReceivingVehicleDeactivated",
            (vehicle, now) =>
            {
                CommercialMasterDataInfrastructure.Domain(
                    () =>
                    {
                        if (activate)
                        {
                            vehicle.Reactivate(now);
                        }
                        else
                        {
                            vehicle.Deactivate(now);
                        }
                    });
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<BagTypeResult>> CreateBagTypeAsync(
        CreateBagTypeCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var normalizedCode =
            CommercialMasterDataInfrastructure.NormalizeCode(command.Code);
        var constructionClass =
            CommercialMasterDataInputRules.ParseConstructionClass(
                command.ConstructionClass);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateBagType,
            context,
            ("code", normalizedCode),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("constructionClass", constructionClass),
            ("standardTareWeightKg", command.StandardTareWeightKg),
            ("isReturnable", command.IsReturnable),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.CreateBagType,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            201,
            async () =>
            {
                if (await dbContext.BagTypes.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.NormalizedCode == normalizedCode,
                        cancellationToken))
                {
                    throw BagTypeCodeExists();
                }

                var now = clock.UtcNow;
                var bagType = CommercialMasterDataInfrastructure.Domain(
                    () => BagType.Create(
                        context.WorkspaceId,
                        context.CompanyId,
                        normalizedCode,
                        command.Name,
                        command.LocalName,
                        constructionClass,
                        command.StandardTareWeightKg,
                        command.IsReturnable,
                        command.Notes,
                        now));
                dbContext.BagTypes.Add(bagType);
                var result = ToResult(bagType);
                AddMasterFacts(
                    "Procurement.BagType",
                    bagType.Id,
                    bagType.Status,
                    bagType.Version,
                    "Procurement.BagType.Created",
                    "Procurement.BagTypeCreated",
                    null,
                    result,
                    now);
                return result;
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<BagTypeResult>> UpdateBagTypeAsync(
        UpdateBagTypeCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var constructionClass =
            CommercialMasterDataInputRules.ParseConstructionClass(
                command.ConstructionClass);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateBagType,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("constructionClass", constructionClass),
            ("standardTareWeightKg", command.StandardTareWeightKg),
            ("isReturnable", command.IsReturnable),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return ExecuteBagTypeMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.UpdateBagType,
            idempotencyKey,
            hash,
            "Procurement.BagType.Updated",
            "Procurement.BagTypeUpdated",
            (bagType, now) =>
            {
                CommercialMasterDataInfrastructure.Domain(
                    () => bagType.Update(
                        command.Name,
                        command.LocalName,
                        constructionClass,
                        command.StandardTareWeightKg,
                        command.IsReturnable,
                        command.Notes,
                        now));
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<BagTypeResult>>
        SetBagTypeStatusAsync(
            MasterStatusCommand command,
            bool activate,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var commandType = activate
            ? CommercialMasterDataCommandTypes.ReactivateBagType
            : CommercialMasterDataCommandTypes.DeactivateBagType;
        var hash = CommercialMasterDataRequestHash.Compute(
            commandType,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return ExecuteBagTypeMutationAsync(
            command.Id,
            command.ExpectedVersion,
            commandType,
            idempotencyKey,
            hash,
            activate
                ? "Procurement.BagType.Reactivated"
                : "Procurement.BagType.Deactivated",
            activate
                ? "Procurement.BagTypeReactivated"
                : "Procurement.BagTypeDeactivated",
            (bagType, now) =>
            {
                CommercialMasterDataInfrastructure.Domain(
                    () =>
                    {
                        if (activate)
                        {
                            bagType.Reactivate(now);
                        }
                        else
                        {
                            bagType.Deactivate(now);
                        }
                    });
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<WeightProcessingPolicyResult>>
        CreateWeightPolicyAsync(
            CreateWeightProcessingPolicyCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var normalizedCode =
            CommercialMasterDataInfrastructure.NormalizeCode(command.Code);
        var processingMethod =
            CommercialMasterDataInputRules.ParseProcessingMethod(
                command.ProcessingMethod);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateWeightPolicy,
            context,
            ("code", normalizedCode),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("decimalPlaces", command.DecimalPlaces),
            ("processingMethod", processingMethod),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.CreateWeightPolicy,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            201,
            async () =>
            {
                if (await dbContext.WeightProcessingPolicies.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.NormalizedCode == normalizedCode,
                        cancellationToken))
                {
                    throw WeightPolicyCodeExists();
                }

                var now = clock.UtcNow;
                var policy = CommercialMasterDataInfrastructure.Domain(
                    () => WeightProcessingPolicy.Create(
                        context.WorkspaceId,
                        context.CompanyId,
                        normalizedCode,
                        command.Name,
                        command.DecimalPlaces,
                        processingMethod,
                        command.Notes,
                        now));
                dbContext.WeightProcessingPolicies.Add(policy);
                var result = ToResult(policy);
                AddMasterFacts(
                    "Procurement.WeightProcessingPolicy",
                    policy.Id,
                    policy.Status,
                    policy.Version,
                    "Procurement.WeightProcessingPolicy.Created",
                    "Procurement.WeightProcessingPolicyCreated",
                    null,
                    result,
                    now);
                return result;
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<WeightProcessingPolicyResult>>
        UpdateWeightPolicyAsync(
            UpdateWeightProcessingPolicyCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var processingMethod =
            CommercialMasterDataInputRules.ParseProcessingMethod(
                command.ProcessingMethod);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateWeightPolicy,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("decimalPlaces", command.DecimalPlaces),
            ("processingMethod", processingMethod),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return ExecuteWeightPolicyMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.UpdateWeightPolicy,
            idempotencyKey,
            hash,
            "Procurement.WeightProcessingPolicy.Updated",
            "Procurement.WeightProcessingPolicyUpdated",
            (policy, now) =>
            {
                CommercialMasterDataInfrastructure.Domain(
                    () => policy.Update(
                        command.Name,
                        command.DecimalPlaces,
                        processingMethod,
                        command.Notes,
                        now));
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<WeightProcessingPolicyResult>>
        SetWeightPolicyStatusAsync(
            MasterStatusCommand command,
            bool activate,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var commandType = activate
            ? CommercialMasterDataCommandTypes.ReactivateWeightPolicy
            : CommercialMasterDataCommandTypes.DeactivateWeightPolicy;
        var hash = CommercialMasterDataRequestHash.Compute(
            commandType,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return ExecuteWeightPolicyMutationAsync(
            command.Id,
            command.ExpectedVersion,
            commandType,
            idempotencyKey,
            hash,
            activate
                ? "Procurement.WeightProcessingPolicy.Reactivated"
                : "Procurement.WeightProcessingPolicy.Deactivated",
            activate
                ? "Procurement.WeightProcessingPolicyReactivated"
                : "Procurement.WeightProcessingPolicyDeactivated",
            async (policy, now) =>
            {
                if (!activate)
                {
                    await PostgreSqlProcurementDefaultsLock.AcquireAsync(
                        dbContext,
                        context.WorkspaceId,
                        context.CompanyId,
                        cancellationToken);
                }

                if (!activate &&
                    await dbContext.CompanyProcurementSettings.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.DefaultWeightProcessingPolicyId == policy.Id,
                        cancellationToken))
                {
                    throw DefaultPolicyInUse();
                }

                CommercialMasterDataInfrastructure.Domain(
                    () =>
                    {
                        if (activate)
                        {
                            policy.Reactivate(now);
                        }
                        else
                        {
                            policy.Deactivate(now);
                        }
                    });
            },
            cancellationToken);
    }

    public async Task<CompanyProcurementSettingsResult> GetSettingsAsync(
        CancellationToken cancellationToken)
    {
        RequireContext();
        var result = await dbContext.CompanyProcurementSettings
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == context.CompanyId &&
                item.DefaultBranchId == context.DefaultBranchId)
            .Select(item => new CompanyProcurementSettingsResult(
                item.Id,
                item.DefaultBranchId,
                item.DefaultDestinationLocationId,
                item.DefaultWeightProcessingPolicyId,
                item.VehicleSelectionMode.ToString(),
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.Version))
            .SingleOrDefaultAsync(cancellationToken);
        return result ??
            throw new ApplicationProblemException(
                "PROCUREMENT_SETTINGS_NOT_CONFIGURED",
                "Company procurement settings have not been configured.",
                ApplicationErrorCategory.NotFound);
    }

    public async Task<IdempotentCommandResult<CompanyProcurementSettingsResult>>
        ConfigureSettingsAsync(
            ConfigureCompanyProcurementSettingsCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        if (command.ExpectedVersion is <= 0)
        {
            throw new ApplicationProblemException(
                "MASTER_EXPECTED_VERSION_REQUIRED",
                "X-Expected-Version must be a positive integer when updating configured settings.",
                ApplicationErrorCategory.Validation);
        }

        var vehicleSelectionMode =
            CommercialMasterDataInputRules.ParseVehicleSelectionMode(
                command.VehicleSelectionMode);
        var hash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.ConfigureCompanySettings,
            context,
            ("expectedVersion", command.ExpectedVersion),
            ("defaultDestinationLocationId",
                command.DefaultDestinationLocationId),
            ("defaultWeightProcessingPolicyId",
                command.DefaultWeightProcessingPolicyId),
            ("vehicleSelectionMode", vehicleSelectionMode));
        return await idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.ConfigureCompanySettings,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            hash,
            context.CorrelationId,
            200,
            async () =>
            {
                await PostgreSqlProcurementDefaultsLock.AcquireAsync(
                    dbContext,
                    context.WorkspaceId,
                    context.CompanyId,
                    cancellationToken);
                await ValidateSettingsReferencesAsync(
                    command,
                    cancellationToken);
                var settings = await dbContext.CompanyProcurementSettings
                    .SingleOrDefaultAsync(
                        item => item.CompanyId == context.CompanyId,
                        cancellationToken);
                var now = clock.UtcNow;
                CompanyProcurementSettingsResult? before = null;
                string action;
                string eventType;
                long version;
                if (settings is null)
                {
                    if (command.ExpectedVersion is not null)
                    {
                        throw CommercialMasterDataInfrastructure
                            .VersionConflict(
                                command.ExpectedVersion.Value,
                                0);
                    }

                    settings = CommercialMasterDataInfrastructure.Domain(
                        () => CompanyProcurementSettings.Create(
                            context.WorkspaceId,
                            context.CompanyId,
                            context.DefaultBranchId,
                            command.DefaultDestinationLocationId,
                            command.DefaultWeightProcessingPolicyId,
                            vehicleSelectionMode,
                            now));
                    dbContext.CompanyProcurementSettings.Add(settings);
                    action =
                        "Procurement.CompanySettings.Configured";
                    eventType =
                        "Procurement.CompanySettingsConfigured";
                    version = settings.Version;
                }
                else
                {
                    if (command.ExpectedVersion is null)
                    {
                        throw new ApplicationProblemException(
                            "MASTER_EXPECTED_VERSION_REQUIRED",
                            "X-Expected-Version is required to update procurement settings.",
                            ApplicationErrorCategory.Validation);
                    }

                    if (settings.Version != command.ExpectedVersion.Value)
                    {
                        throw CommercialMasterDataInfrastructure
                            .VersionConflict(
                                command.ExpectedVersion.Value,
                                settings.Version);
                    }

                    before = ToResult(settings);
                    CommercialMasterDataInfrastructure.Domain(
                        () => settings.Update(
                            command.DefaultDestinationLocationId,
                            command.DefaultWeightProcessingPolicyId,
                            vehicleSelectionMode,
                            now));
                    action = "Procurement.CompanySettings.Updated";
                    eventType = "Procurement.CompanySettingsUpdated";
                    version = settings.Version;
                }

                var result = ToResult(settings);
                AddSettingsFacts(
                    settings,
                    version,
                    action,
                    eventType,
                    before,
                    result,
                    now);
                return result;
            },
            IsReplayable,
            command.ExpectedVersion is null
                ? null
                : () => ReadSettingsConcurrencyProblemAsync(
                    command.ExpectedVersion!.Value,
                    cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private async Task ValidateSettingsReferencesAsync(
        ConfigureCompanyProcurementSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var locationActive = await dbContext.BusinessLocations.AnyAsync(
            item =>
                item.Id == command.DefaultDestinationLocationId &&
                item.CompanyId == context.CompanyId &&
                item.BranchId == context.DefaultBranchId &&
                item.Status == MasterDataStatus.Active,
            cancellationToken);
        var policyActive = await dbContext.WeightProcessingPolicies.AnyAsync(
            item =>
                item.Id == command.DefaultWeightProcessingPolicyId &&
                item.CompanyId == context.CompanyId &&
                item.Status == MasterDataStatus.Active,
            cancellationToken);
        if (!locationActive || !policyActive)
        {
            throw new ApplicationProblemException(
                "PROCUREMENT_SETTINGS_INVALID",
                "Defaults must reference an active same-company location in the default branch and an active same-company weight policy.",
                ApplicationErrorCategory.Validation);
        }
    }

    private Task<IdempotentCommandResult<ReceivingVehicleResult>>
        ExecuteVehicleMutationAsync(
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string requestHash,
            string auditAction,
            string eventType,
            Func<ReceivingVehicle, DateTimeOffset, Task> mutate,
            CancellationToken cancellationToken)
    {
        CommercialMasterDataInputRules.RequireExpectedVersion(expectedVersion);
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            commandType,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            requestHash,
            context.CorrelationId,
            200,
            async () =>
            {
                var entity = await dbContext.ReceivingVehicles
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == id &&
                            item.CompanyId == context.CompanyId,
                        cancellationToken) ??
                    throw VehicleNotFound();
                if (entity.Version != expectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        expectedVersion,
                        entity.Version);
                }

                var now = clock.UtcNow;
                var before = ToResult(entity);
                await mutate(entity, now);
                var after = ToResult(entity);
                AddMasterFacts(
                    "Procurement.ReceivingVehicle",
                    entity.Id,
                    entity.Status,
                    entity.Version,
                    auditAction,
                    eventType,
                    before,
                    after,
                    now);
                return after;
            },
            IsReplayable,
            () => ReadVersionProblemAsync(
                dbContext.ReceivingVehicles
                    .Where(item =>
                        item.Id == id &&
                        item.CompanyId == context.CompanyId)
                    .Select(item => (long?)item.Version),
                expectedVersion,
                VehicleNotFound,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private Task<IdempotentCommandResult<BagTypeResult>>
        ExecuteBagTypeMutationAsync(
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string requestHash,
            string auditAction,
            string eventType,
            Func<BagType, DateTimeOffset, Task> mutate,
            CancellationToken cancellationToken)
    {
        CommercialMasterDataInputRules.RequireExpectedVersion(expectedVersion);
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            commandType,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            requestHash,
            context.CorrelationId,
            200,
            async () =>
            {
                var entity = await dbContext.BagTypes.SingleOrDefaultAsync(
                        item =>
                            item.Id == id &&
                            item.CompanyId == context.CompanyId,
                        cancellationToken) ??
                    throw BagTypeNotFound();
                if (entity.Version != expectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        expectedVersion,
                        entity.Version);
                }

                var now = clock.UtcNow;
                var before = ToResult(entity);
                await mutate(entity, now);
                var after = ToResult(entity);
                AddMasterFacts(
                    "Procurement.BagType",
                    entity.Id,
                    entity.Status,
                    entity.Version,
                    auditAction,
                    eventType,
                    before,
                    after,
                    now);
                return after;
            },
            IsReplayable,
            () => ReadVersionProblemAsync(
                dbContext.BagTypes
                    .Where(item =>
                        item.Id == id &&
                        item.CompanyId == context.CompanyId)
                    .Select(item => (long?)item.Version),
                expectedVersion,
                BagTypeNotFound,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private Task<IdempotentCommandResult<WeightProcessingPolicyResult>>
        ExecuteWeightPolicyMutationAsync(
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string requestHash,
            string auditAction,
            string eventType,
            Func<WeightProcessingPolicy, DateTimeOffset, Task> mutate,
            CancellationToken cancellationToken)
    {
        CommercialMasterDataInputRules.RequireExpectedVersion(expectedVersion);
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            commandType,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            requestHash,
            context.CorrelationId,
            200,
            async () =>
            {
                var entity = await dbContext.WeightProcessingPolicies
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == id &&
                            item.CompanyId == context.CompanyId,
                        cancellationToken) ??
                    throw WeightPolicyNotFound();
                if (entity.Version != expectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        expectedVersion,
                        entity.Version);
                }

                var now = clock.UtcNow;
                var before = ToResult(entity);
                await mutate(entity, now);
                var after = ToResult(entity);
                AddMasterFacts(
                    "Procurement.WeightProcessingPolicy",
                    entity.Id,
                    entity.Status,
                    entity.Version,
                    auditAction,
                    eventType,
                    before,
                    after,
                    now);
                return after;
            },
            IsReplayable,
            () => ReadVersionProblemAsync(
                dbContext.WeightProcessingPolicies
                    .Where(item =>
                        item.Id == id &&
                        item.CompanyId == context.CompanyId)
                    .Select(item => (long?)item.Version),
                expectedVersion,
                WeightPolicyNotFound,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private async Task<ApplicationProblemException> ReadVersionProblemAsync(
        IQueryable<long?> query,
        long expectedVersion,
        Func<ApplicationProblemException> notFound,
        CancellationToken cancellationToken)
    {
        var version = await query.SingleOrDefaultAsync(
            cancellationToken);
        return version is null
            ? notFound()
            : CommercialMasterDataInfrastructure.VersionConflict(
                expectedVersion,
                version.Value);
    }

    private async Task<ApplicationProblemException>
        ReadSettingsConcurrencyProblemAsync(
            long expectedVersion,
            CancellationToken cancellationToken)
    {
        var version = await dbContext.CompanyProcurementSettings
            .AsNoTracking()
            .Where(item => item.CompanyId == context.CompanyId)
            .Select(item => (long?)item.Version)
            .SingleOrDefaultAsync(cancellationToken);
        return CommercialMasterDataInfrastructure.VersionConflict(
            expectedVersion,
            version ?? 0);
    }

    private void AddMasterFacts<TResult>(
        string aggregateType,
        Guid aggregateId,
        MasterDataStatus status,
        long version,
        string action,
        string eventType,
        TResult? before,
        TResult after,
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
                aggregateId,
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
                aggregateType,
                aggregateId,
                version,
                JsonSerializer.Serialize(
                    new
                    {
                        id = aggregateId,
                        status = status.ToString(),
                        version,
                    },
                    JsonOptions),
                context.CorrelationId,
                occurredAtUtc,
                OutboxEventStream.Internal));
    }

    private void AddSettingsFacts(
        CompanyProcurementSettings settings,
        long version,
        string action,
        string eventType,
        CompanyProcurementSettingsResult? before,
        CompanyProcurementSettingsResult after,
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
                "Procurement.CompanyProcurementSettings",
                settings.Id,
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
                "Procurement.CompanyProcurementSettings",
                settings.Id,
                version,
                JsonSerializer.Serialize(
                    new
                    {
                        settingsId = settings.Id,
                        settings.DefaultBranchId,
                        version,
                    },
                    JsonOptions),
                context.CorrelationId,
                occurredAtUtc,
                OutboxEventStream.Internal));
    }
}
