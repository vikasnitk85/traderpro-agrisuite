using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Operations;

public enum BusinessLocationType : short
{
    Warehouse = 1,
    Yard = 2,
    Mill = 3,
    Office = 4,
    Other = 5,
}

public sealed class BusinessLocation :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private BusinessLocation()
    {
    }

    private BusinessLocation(
        Guid workspaceId,
        Guid companyId,
        Guid branchId,
        string code,
        string name,
        string? localName,
        BusinessLocationType locationType,
        string? addressLine,
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
        BranchId = MasterDataValueRules.RequiredId(
            branchId,
            nameof(branchId));
        NormalizedCode = MasterDataValueRules.NormalizeCode(code);
        Code = NormalizedCode;
        ApplyDetails(name, localName, locationType, addressLine, notes);
        Status = MasterDataStatus.Active;
        CreatedAtUtc = MasterDataValueRules.RequireUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid BranchId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string NormalizedCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? LocalName { get; private set; }

    public BusinessLocationType LocationType { get; private set; }

    public string? AddressLine { get; private set; }

    public string? Notes { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static BusinessLocation Create(
        Guid workspaceId,
        Guid companyId,
        Guid branchId,
        string code,
        string name,
        string? localName,
        BusinessLocationType locationType,
        string? addressLine,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        return new BusinessLocation(
            workspaceId,
            companyId,
            branchId,
            code,
            name,
            localName,
            locationType,
            addressLine,
            notes,
            createdAtUtc);
    }

    public void Update(
        string name,
        string? localName,
        BusinessLocationType locationType,
        string? addressLine,
        string? notes,
        DateTimeOffset updatedAtUtc)
    {
        ApplyDetails(name, localName, locationType, addressLine, notes);
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
        string name,
        string? localName,
        BusinessLocationType locationType,
        string? addressLine,
        string? notes)
    {
        if (!Enum.IsDefined(locationType))
        {
            throw new MasterDataDomainException(
                "BUSINESS_LOCATION_INVALID",
                "Location type must be Warehouse, Yard, Mill, Office, or Other.",
                "locationType");
        }

        Name = MasterDataValueRules.RequiredText(
            name,
            MasterDataValueRules.MaximumNameLength,
            "name");
        LocalName = MasterDataValueRules.OptionalText(
            localName,
            MasterDataValueRules.MaximumNameLength,
            "localName");
        LocationType = locationType;
        AddressLine = MasterDataValueRules.OptionalText(
            addressLine,
            500,
            "addressLine",
            "BUSINESS_LOCATION_INVALID");
        Notes = MasterDataValueRules.OptionalText(
            notes,
            MasterDataValueRules.MaximumNotesLength,
            "notes",
            "BUSINESS_LOCATION_INVALID");
    }
}
