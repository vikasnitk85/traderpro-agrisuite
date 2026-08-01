using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Catalog;

public sealed class ProductStandardBagWeight :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private const decimal MaximumWeight = 99999999999999.999999m;

    private ProductStandardBagWeight()
    {
    }

    private ProductStandardBagWeight(
        Guid workspaceId,
        Guid companyId,
        Guid productId,
        Guid bagTypeId,
        string? label,
        decimal standardContentWeightKg,
        bool isDefault,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = MasterDataValueRules.RequiredId(
            workspaceId,
            nameof(workspaceId));
        CompanyId = MasterDataValueRules.RequiredId(
            companyId,
            nameof(companyId));
        ProductId = MasterDataValueRules.RequiredId(
            productId,
            nameof(productId));
        BagTypeId = MasterDataValueRules.RequiredId(
            bagTypeId,
            nameof(bagTypeId));
        ApplyDetails(label, standardContentWeightKg);
        IsDefault = isDefault;
        Status = MasterDataStatus.Active;
        CreatedAtUtc = MasterDataValueRules.RequireUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid BagTypeId { get; private set; }

    public string? Label { get; private set; }

    public decimal StandardContentWeightKg { get; private set; }

    public bool IsDefault { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static ProductStandardBagWeight Create(
        Guid workspaceId,
        Guid companyId,
        Guid productId,
        Guid bagTypeId,
        string? label,
        decimal standardContentWeightKg,
        bool isDefault,
        DateTimeOffset createdAtUtc)
    {
        return new ProductStandardBagWeight(
            workspaceId,
            companyId,
            productId,
            bagTypeId,
            label,
            standardContentWeightKg,
            isDefault,
            createdAtUtc);
    }

    public void Update(
        string? label,
        decimal standardContentWeightKg,
        DateTimeOffset updatedAtUtc)
    {
        ApplyDetails(label, standardContentWeightKg);
        AdvanceRevision(updatedAtUtc);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireInactive(Status);
        Status = MasterDataStatus.Inactive;
        IsDefault = false;
        AdvanceRevision(updatedAtUtc);
    }

    public void Reactivate(DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireActive(Status);
        Status = MasterDataStatus.Active;
        IsDefault = false;
        AdvanceRevision(updatedAtUtc);
    }

    public void SetDefault(DateTimeOffset updatedAtUtc)
    {
        if (Status is not MasterDataStatus.Active || IsDefault)
        {
            throw new MasterDataDomainException(
                "PRODUCT_STANDARD_BAG_WEIGHT_DEFAULT_CONFLICT",
                "Only an active non-default standard can become the default.",
                "isDefault");
        }

        IsDefault = true;
        AdvanceRevision(updatedAtUtc);
    }

    public void ClearDefault(DateTimeOffset updatedAtUtc)
    {
        if (!IsDefault)
        {
            throw new MasterDataDomainException(
                "PRODUCT_STANDARD_BAG_WEIGHT_DEFAULT_CONFLICT",
                "The standard is not the current default.",
                "isDefault");
        }

        IsDefault = false;
        AdvanceRevision(updatedAtUtc);
    }

    private void ApplyDetails(
        string? label,
        decimal standardContentWeightKg)
    {
        Label = MasterDataValueRules.OptionalText(
            label,
            MasterDataValueRules.MaximumNameLength,
            "label",
            "PRODUCT_STANDARD_BAG_WEIGHT_INVALID");
        StandardContentWeightKg = ValidateWeight(standardContentWeightKg);
    }

    private static decimal ValidateWeight(decimal value)
    {
        var scale = (decimal.GetBits(value)[3] >> 16) & 0x7F;
        if (value <= 0m || value > MaximumWeight || scale > 6)
        {
            throw new MasterDataDomainException(
                "PRODUCT_STANDARD_BAG_WEIGHT_VALUE_INVALID",
                "Standard content weight must be a positive decimal with at most 14 integer and 6 fractional digits.",
                "standardContentWeightKg");
        }

        return value;
    }

    private void AdvanceRevision(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = MasterDataValueRules.RequireRevisionTimestamp(
            UpdatedAtUtc,
            updatedAtUtc);
        Version = MasterDataValueRules.NextVersion(Version);
    }
}
