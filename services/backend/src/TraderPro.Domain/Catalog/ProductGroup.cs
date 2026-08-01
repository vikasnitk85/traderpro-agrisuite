using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Catalog;

public sealed class ProductGroup :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private ProductGroup()
    {
    }

    private ProductGroup(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        string? localName,
        string? description,
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
        ApplyDetails(name, localName, description);
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

    public string? Description { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static ProductGroup Create(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        string? localName,
        string? description,
        DateTimeOffset createdAtUtc)
    {
        return new ProductGroup(
            workspaceId,
            companyId,
            code,
            name,
            localName,
            description,
            createdAtUtc);
    }

    public void Update(
        string name,
        string? localName,
        string? description,
        DateTimeOffset updatedAtUtc)
    {
        ApplyDetails(name, localName, description);
        AdvanceRevision(updatedAtUtc);
    }

    public void Deactivate(
        bool hasActiveProducts,
        DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireInactive(Status);
        if (hasActiveProducts)
        {
            throw new MasterDataDomainException(
                "PRODUCT_GROUP_IN_USE",
                "A product group with active products cannot be deactivated.",
                "status");
        }

        Status = MasterDataStatus.Inactive;
        AdvanceRevision(updatedAtUtc);
    }

    public void Reactivate(DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireActive(Status);
        Status = MasterDataStatus.Active;
        AdvanceRevision(updatedAtUtc);
    }

    private void ApplyDetails(
        string name,
        string? localName,
        string? description)
    {
        Name = MasterDataValueRules.RequiredText(
            name,
            MasterDataValueRules.MaximumNameLength,
            "name",
            "PRODUCT_GROUP_INVALID");
        LocalName = MasterDataValueRules.OptionalText(
            localName,
            MasterDataValueRules.MaximumNameLength,
            "localName",
            "PRODUCT_GROUP_INVALID");
        Description = MasterDataValueRules.OptionalText(
            description,
            1000,
            "description",
            "PRODUCT_GROUP_INVALID");
    }

    private void AdvanceRevision(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = MasterDataValueRules.RequireRevisionTimestamp(
            UpdatedAtUtc,
            updatedAtUtc);
        Version = MasterDataValueRules.NextVersion(Version);
    }
}
