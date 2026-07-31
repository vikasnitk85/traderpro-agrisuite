using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Infrastructure.Modules.Shared;

namespace TraderPro.Infrastructure.Modules.Procurement.MasterData;

internal sealed partial class ProcurementMasterDataService
{
    public async Task<ReceivingVehicleResult> GetVehicleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var entity = await dbContext.ReceivingVehicles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == id &&
                    item.CompanyId == context.CompanyId,
                cancellationToken);
        return entity is null ? throw VehicleNotFound() : ToResult(entity);
    }

    public async Task<MasterPage<ReceivingVehicleResult>> ListVehiclesAsync(
        MasterListQuery query,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(query);
        var cursorScope = Scope(
            CommercialMasterKinds.ReceivingVehicle,
            query);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            query.Cursor,
            cursorScope);
        var rows = dbContext.ReceivingVehicles
            .AsNoTracking()
            .Where(item => item.CompanyId == context.CompanyId);
        rows = CommercialMasterDataInfrastructure.ApplyStatus(
            rows,
            query.Status,
            item => item.Status);
        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
            rows = rows.Where(item =>
                EF.Functions.ILike(item.Code, pattern, "\\") ||
                EF.Functions.ILike(
                    item.RegistrationNumber,
                    pattern,
                    "\\") ||
                EF.Functions.ILike(
                    item.NormalizedRegistrationNumber,
                    pattern,
                    "\\") ||
                (item.DisplayName != null &&
                    EF.Functions.ILike(item.DisplayName, pattern, "\\")));
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
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        return Page<ReceivingVehicle, ReceivingVehicleResult>(
            page,
            query.Limit,
            cursorScope,
            item => item.NormalizedCode,
            item => ToResult(item));
    }

    public async Task<BagTypeResult> GetBagTypeAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var entity = await dbContext.BagTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == id &&
                    item.CompanyId == context.CompanyId,
                cancellationToken);
        return entity is null ? throw BagTypeNotFound() : ToResult(entity);
    }

    public async Task<MasterPage<BagTypeResult>> ListBagTypesAsync(
        MasterListQuery query,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(query);
        var cursorScope = Scope(CommercialMasterKinds.BagType, query);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            query.Cursor,
            cursorScope);
        var rows = dbContext.BagTypes
            .AsNoTracking()
            .Where(item => item.CompanyId == context.CompanyId);
        rows = CommercialMasterDataInfrastructure.ApplyStatus(
            rows,
            query.Status,
            item => item.Status);
        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
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
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        return Page<BagType, BagTypeResult>(
            page,
            query.Limit,
            cursorScope,
            item => item.NormalizedCode,
            item => ToResult(item));
    }

    public async Task<WeightProcessingPolicyResult> GetWeightPolicyAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var entity = await dbContext.WeightProcessingPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == id &&
                    item.CompanyId == context.CompanyId,
                cancellationToken);
        return entity is null ? throw WeightPolicyNotFound() : ToResult(entity);
    }

    public async Task<MasterPage<WeightProcessingPolicyResult>>
        ListWeightPoliciesAsync(
            MasterListQuery query,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(query);
        var cursorScope = Scope(
            CommercialMasterKinds.WeightProcessingPolicy,
            query);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            query.Cursor,
            cursorScope);
        var rows = dbContext.WeightProcessingPolicies
            .AsNoTracking()
            .Where(item => item.CompanyId == context.CompanyId);
        rows = CommercialMasterDataInfrastructure.ApplyStatus(
            rows,
            query.Status,
            item => item.Status);
        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
            rows = rows.Where(item =>
                EF.Functions.ILike(item.Code, pattern, "\\") ||
                EF.Functions.ILike(item.Name, pattern, "\\"));
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
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        return Page<WeightProcessingPolicy, WeightProcessingPolicyResult>(
            page,
            query.Limit,
            cursorScope,
            item => item.NormalizedCode,
            item => ToResult(item));
    }

    private static MasterPage<TResult> Page<TEntity, TResult>(
        IReadOnlyList<TEntity> page,
        int limit,
        CommercialMasterCursorScope cursorScope,
        Func<TEntity, string> code,
        Func<TEntity, TResult> convert)
        where TResult : class
    {
        var hasMore = page.Count > limit;
        var selected = page.Take(limit).ToArray();
        var items = selected.Select(convert).ToArray();
        var id = selected.Length == 0
            ? Guid.Empty
            : items[^1] switch
            {
                ReceivingVehicleResult result => result.Id,
                BagTypeResult result => result.Id,
                WeightProcessingPolicyResult result => result.Id,
                _ => Guid.Empty,
            };
        return new MasterPage<TResult>(
            items,
            hasMore
                ? CommercialMasterDataInfrastructure.EncodeCursor(
                    cursorScope,
                    code(selected[^1]),
                    id)
                : null,
            hasMore);
    }

    private CommercialMasterCursorScope Scope(
        string masterKind,
        MasterListQuery query)
    {
        return new CommercialMasterCursorScope(
            masterKind,
            context.WorkspaceId,
            context.CompanyId,
            null,
            query.Status,
            query.Search);
    }

    private static ReceivingVehicleResult ToResult(
        ReceivingVehicle entity)
    {
        return new ReceivingVehicleResult(
            entity.Id,
            entity.Code,
            entity.RegistrationNumber,
            entity.DisplayName,
            entity.VehicleType.ToString(),
            entity.OwnerName,
            entity.ContactNumber,
            entity.Notes,
            entity.Status.ToString(),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.Version);
    }

    private static BagTypeResult ToResult(
        BagType entity)
    {
        return new BagTypeResult(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.LocalName,
            entity.ConstructionClass.ToString(),
            entity.StandardTareWeightKg.ToString(
                "0.000000",
                CultureInfo.InvariantCulture),
            entity.IsReturnable,
            entity.Notes,
            entity.Status.ToString(),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.Version);
    }

    private static WeightProcessingPolicyResult ToResult(
        WeightProcessingPolicy entity)
    {
        return new WeightProcessingPolicyResult(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.DecimalPlaces,
            entity.ProcessingMethod.ToString(),
            entity.Notes,
            entity.Status.ToString(),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.Version);
    }

    private static CompanyProcurementSettingsResult ToResult(
        CompanyProcurementSettings entity)
    {
        return new CompanyProcurementSettingsResult(
            entity.Id,
            entity.DefaultBranchId,
            entity.DefaultDestinationLocationId,
            entity.DefaultWeightProcessingPolicyId,
            entity.VehicleSelectionMode.ToString(),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.Version);
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

    private static bool IsReplayable(ReceivingVehicleResult result)
    {
        return result.Id != Guid.Empty &&
            result.Code.Length is >= 2 and <= 32 &&
            result.Version > 0;
    }

    private static bool IsReplayable(BagTypeResult result)
    {
        return result.Id != Guid.Empty &&
            result.Code.Length is >= 2 and <= 32 &&
            result.Version > 0;
    }

    private static bool IsReplayable(WeightProcessingPolicyResult result)
    {
        return result.Id != Guid.Empty &&
            result.Code.Length is >= 2 and <= 32 &&
            result.Version > 0;
    }

    private static bool IsReplayable(
        CompanyProcurementSettingsResult result)
    {
        return result.Id != Guid.Empty &&
            result.DefaultBranchId != Guid.Empty &&
            result.DefaultDestinationLocationId != Guid.Empty &&
            result.DefaultWeightProcessingPolicyId != Guid.Empty &&
            result.Version > 0;
    }

    private static ApplicationProblemException VehicleNotFound()
    {
        return new ApplicationProblemException(
            "RECEIVING_VEHICLE_NOT_FOUND",
            "The receiving vehicle was not found.",
            ApplicationErrorCategory.NotFound);
    }

    private static ApplicationProblemException BagTypeNotFound()
    {
        return new ApplicationProblemException(
            "BAG_TYPE_NOT_FOUND",
            "The bag type was not found.",
            ApplicationErrorCategory.NotFound);
    }

    private static ApplicationProblemException WeightPolicyNotFound()
    {
        return new ApplicationProblemException(
            "WEIGHT_POLICY_NOT_FOUND",
            "The weight processing policy was not found.",
            ApplicationErrorCategory.NotFound);
    }

    private static ApplicationProblemException VehicleCodeExists()
    {
        return CommercialMasterDataInfrastructure.Conflict(
            "RECEIVING_VEHICLE_CODE_EXISTS",
            "A receiving vehicle with this code already exists.");
    }

    private static ApplicationProblemException VehicleRegistrationExists()
    {
        return CommercialMasterDataInfrastructure.Conflict(
            "RECEIVING_VEHICLE_REGISTRATION_EXISTS",
            "A receiving vehicle with this registration already exists.");
    }

    private static ApplicationProblemException BagTypeCodeExists()
    {
        return CommercialMasterDataInfrastructure.Conflict(
            "BAG_TYPE_CODE_EXISTS",
            "A bag type with this code already exists.");
    }

    private static ApplicationProblemException WeightPolicyCodeExists()
    {
        return CommercialMasterDataInfrastructure.Conflict(
            "WEIGHT_POLICY_CODE_EXISTS",
            "A weight policy with this code already exists.");
    }

    private static ApplicationProblemException DefaultPolicyInUse()
    {
        return CommercialMasterDataInfrastructure.Conflict(
            "PROCUREMENT_DEFAULT_WEIGHT_POLICY_IN_USE",
            "The current default weight policy must be changed before this policy can be deactivated.");
    }

    private static ApplicationProblemException SettingsInvalid()
    {
        return new ApplicationProblemException(
            "PROCUREMENT_SETTINGS_INVALID",
            "Procurement settings contain an invalid company, branch, location, or weight-policy reference.",
            ApplicationErrorCategory.Validation);
    }

    private static ApplicationProblemException? MapPersistenceProblem(
        Exception exception)
    {
        var postgres =
            CommercialMasterDataInfrastructure.PostgreSql(exception);
        return postgres?.ConstraintName switch
        {
            "ux_receiving_vehicles_workspace_company_code" =>
                VehicleCodeExists(),
            "ux_receiving_vehicles_workspace_company_registration" =>
                VehicleRegistrationExists(),
            "ux_bag_types_workspace_company_code" =>
                BagTypeCodeExists(),
            "ux_weight_processing_policies_workspace_company_code" =>
                WeightPolicyCodeExists(),
            "ck_procurement_default_weight_policy_active" =>
                DefaultPolicyInUse(),
            "fk_company_procurement_settings_company" or
            "fk_company_procurement_settings_default_branch" or
            "fk_company_procurement_settings_default_destination" or
            "fk_company_procurement_settings_default_weight_policy" or
            "ck_company_procurement_settings_active_defaults" =>
                SettingsInvalid(),
            "ux_company_procurement_settings_workspace_company" =>
                CommercialMasterDataInfrastructure.Conflict(
                    "MASTER_VERSION_CONFLICT",
                    "Procurement settings were configured concurrently."),
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
