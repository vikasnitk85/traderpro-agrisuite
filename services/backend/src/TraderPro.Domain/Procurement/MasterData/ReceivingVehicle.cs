using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Procurement.MasterData;

public enum ReceivingVehicleType : short
{
    Truck = 1,
    Tractor = 2,
    Van = 3,
    Other = 4,
}

public sealed class ReceivingVehicle :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private ReceivingVehicle()
    {
    }

    private ReceivingVehicle(
        Guid workspaceId,
        Guid companyId,
        string code,
        string registrationNumber,
        string? displayName,
        ReceivingVehicleType vehicleType,
        string? ownerName,
        string? contactNumber,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = MasterDataValueRules.RequiredId(
            workspaceId,
            nameof(workspaceId));
        CompanyId = MasterDataValueRules.RequiredId(
            companyId,
            nameof(companyId));
        NormalizedCode = MasterDataValueRules.NormalizeCode(code);
        Code = NormalizedCode;
        ApplyDetails(
            registrationNumber,
            displayName,
            vehicleType,
            ownerName,
            contactNumber,
            notes);
        Status = MasterDataStatus.Active;
        CreatedAtUtc = MasterDataValueRules.RequireUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string NormalizedCode { get; private set; } = string.Empty;

    public string RegistrationNumber { get; private set; } = string.Empty;

    public string NormalizedRegistrationNumber { get; private set; } =
        string.Empty;

    public string? DisplayName { get; private set; }

    public ReceivingVehicleType VehicleType { get; private set; }

    public string? OwnerName { get; private set; }

    public string? ContactNumber { get; private set; }

    public string? Notes { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static ReceivingVehicle Create(
        Guid workspaceId,
        Guid companyId,
        string code,
        string registrationNumber,
        string? displayName,
        ReceivingVehicleType vehicleType,
        string? ownerName,
        string? contactNumber,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        return new ReceivingVehicle(
            workspaceId,
            companyId,
            code,
            registrationNumber,
            displayName,
            vehicleType,
            ownerName,
            contactNumber,
            notes,
            createdAtUtc);
    }

    public void Update(
        string registrationNumber,
        string? displayName,
        ReceivingVehicleType vehicleType,
        string? ownerName,
        string? contactNumber,
        string? notes,
        DateTimeOffset updatedAtUtc)
    {
        ApplyDetails(
            registrationNumber,
            displayName,
            vehicleType,
            ownerName,
            contactNumber,
            notes);
        AdvanceRevision(updatedAtUtc);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireInactive(Status);
        Status = MasterDataStatus.Inactive;
        AdvanceRevision(updatedAtUtc);
    }

    public void Reactivate(DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireActive(Status);
        Status = MasterDataStatus.Active;
        AdvanceRevision(updatedAtUtc);
    }

    private void AdvanceRevision(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = MasterDataValueRules.RequireRevisionTimestamp(
            UpdatedAtUtc,
            updatedAtUtc);
        Version = MasterDataValueRules.NextVersion(Version);
    }

    private void ApplyDetails(
        string registrationNumber,
        string? displayName,
        ReceivingVehicleType vehicleType,
        string? ownerName,
        string? contactNumber,
        string? notes)
    {
        if (!Enum.IsDefined(vehicleType))
        {
            throw new MasterDataDomainException(
                "RECEIVING_VEHICLE_INVALID",
                "Vehicle type must be Truck, Tractor, Van, or Other.",
                "vehicleType");
        }

        RegistrationNumber = MasterDataValueRules.RequiredText(
            registrationNumber,
            64,
            "registrationNumber",
            "RECEIVING_VEHICLE_INVALID");
        NormalizedRegistrationNumber =
            MasterDataValueRules.NormalizeRegistration(registrationNumber);
        DisplayName = MasterDataValueRules.OptionalText(
            displayName,
            MasterDataValueRules.MaximumNameLength,
            "displayName",
            "RECEIVING_VEHICLE_INVALID");
        VehicleType = vehicleType;
        OwnerName = MasterDataValueRules.OptionalText(
            ownerName,
            MasterDataValueRules.MaximumNameLength,
            "ownerName",
            "RECEIVING_VEHICLE_INVALID");
        ContactNumber = MasterDataValueRules.OptionalText(
            contactNumber,
            50,
            "contactNumber",
            "RECEIVING_VEHICLE_INVALID");
        Notes = MasterDataValueRules.OptionalText(
            notes,
            MasterDataValueRules.MaximumNotesLength,
            "notes",
            "RECEIVING_VEHICLE_INVALID");
    }
}
