using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Procurement.MasterData;

public enum VehicleSelectionMode : short
{
    Optional = 1,
    Disabled = 2,
}

public sealed class CompanyProcurementSettings :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private CompanyProcurementSettings()
    {
    }

    private CompanyProcurementSettings(
        Guid workspaceId,
        Guid companyId,
        Guid defaultBranchId,
        Guid defaultDestinationLocationId,
        Guid defaultWeightProcessingPolicyId,
        VehicleSelectionMode vehicleSelectionMode,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = MasterDataValueRules.RequiredId(
            workspaceId,
            nameof(workspaceId));
        CompanyId = MasterDataValueRules.RequiredId(
            companyId,
            nameof(companyId));
        DefaultBranchId = MasterDataValueRules.RequiredId(
            defaultBranchId,
            nameof(defaultBranchId));
        Apply(
            defaultDestinationLocationId,
            defaultWeightProcessingPolicyId,
            vehicleSelectionMode);
        CreatedAtUtc = MasterDataValueRules.RequireUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid DefaultBranchId { get; private set; }

    public Guid DefaultDestinationLocationId { get; private set; }

    public Guid DefaultWeightProcessingPolicyId { get; private set; }

    public VehicleSelectionMode VehicleSelectionMode { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static CompanyProcurementSettings Create(
        Guid workspaceId,
        Guid companyId,
        Guid defaultBranchId,
        Guid defaultDestinationLocationId,
        Guid defaultWeightProcessingPolicyId,
        VehicleSelectionMode vehicleSelectionMode,
        DateTimeOffset createdAtUtc)
    {
        return new CompanyProcurementSettings(
            workspaceId,
            companyId,
            defaultBranchId,
            defaultDestinationLocationId,
            defaultWeightProcessingPolicyId,
            vehicleSelectionMode,
            createdAtUtc);
    }

    public void Update(
        Guid defaultDestinationLocationId,
        Guid defaultWeightProcessingPolicyId,
        VehicleSelectionMode vehicleSelectionMode,
        DateTimeOffset updatedAtUtc)
    {
        Apply(
            defaultDestinationLocationId,
            defaultWeightProcessingPolicyId,
            vehicleSelectionMode);
        UpdatedAtUtc = MasterDataValueRules.RequireRevisionTimestamp(
            UpdatedAtUtc,
            updatedAtUtc);
        Version = MasterDataValueRules.NextVersion(Version);
    }

    private void Apply(
        Guid defaultDestinationLocationId,
        Guid defaultWeightProcessingPolicyId,
        VehicleSelectionMode vehicleSelectionMode)
    {
        if (!Enum.IsDefined(vehicleSelectionMode))
        {
            throw new MasterDataDomainException(
                "PROCUREMENT_SETTINGS_INVALID",
                "Vehicle selection mode must be Optional or Disabled.",
                "vehicleSelectionMode");
        }

        DefaultDestinationLocationId = MasterDataValueRules.RequiredId(
            defaultDestinationLocationId,
            "defaultDestinationLocationId");
        DefaultWeightProcessingPolicyId = MasterDataValueRules.RequiredId(
            defaultWeightProcessingPolicyId,
            "defaultWeightProcessingPolicyId");
        VehicleSelectionMode = vehicleSelectionMode;
    }
}
