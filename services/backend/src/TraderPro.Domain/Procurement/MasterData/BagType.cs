using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Procurement.MasterData;

public enum BagConstructionClass : short
{
    Jute = 1,
    SinglePlastic = 2,
    DoublePlastic = 3,
    Other = 4,
}

public sealed class BagType :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private BagType()
    {
    }

    private BagType(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        string? localName,
        BagConstructionClass constructionClass,
        decimal standardTareWeightKg,
        bool isReturnable,
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
            name,
            localName,
            constructionClass,
            standardTareWeightKg,
            isReturnable,
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

    public string Name { get; private set; } = string.Empty;

    public string? LocalName { get; private set; }

    public BagConstructionClass ConstructionClass { get; private set; }

    public decimal StandardTareWeightKg { get; private set; }

    public bool IsReturnable { get; private set; }

    public string? Notes { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static BagType Create(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        string? localName,
        BagConstructionClass constructionClass,
        decimal standardTareWeightKg,
        bool isReturnable,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        return new BagType(
            workspaceId,
            companyId,
            code,
            name,
            localName,
            constructionClass,
            standardTareWeightKg,
            isReturnable,
            notes,
            createdAtUtc);
    }

    public void Update(
        string name,
        string? localName,
        BagConstructionClass constructionClass,
        decimal standardTareWeightKg,
        bool isReturnable,
        string? notes,
        DateTimeOffset updatedAtUtc)
    {
        ApplyDetails(
            name,
            localName,
            constructionClass,
            standardTareWeightKg,
            isReturnable,
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
        string name,
        string? localName,
        BagConstructionClass constructionClass,
        decimal standardTareWeightKg,
        bool isReturnable,
        string? notes)
    {
        if (!Enum.IsDefined(constructionClass))
        {
            throw new MasterDataDomainException(
                "BAG_TYPE_INVALID",
                "Construction class must be Jute, SinglePlastic, DoublePlastic, or Other.",
                "constructionClass");
        }

        Name = MasterDataValueRules.RequiredText(
            name,
            MasterDataValueRules.MaximumNameLength,
            "name");
        LocalName = MasterDataValueRules.OptionalText(
            localName,
            MasterDataValueRules.MaximumNameLength,
            "localName");
        ConstructionClass = constructionClass;
        StandardTareWeightKg =
            MasterDataValueRules.ValidateTareWeight(standardTareWeightKg);
        IsReturnable = isReturnable;
        Notes = MasterDataValueRules.OptionalText(
            notes,
            MasterDataValueRules.MaximumNotesLength,
            "notes",
            "BAG_TYPE_INVALID");
    }
}
