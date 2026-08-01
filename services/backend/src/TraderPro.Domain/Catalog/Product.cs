using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Catalog;

public enum ProductType : short
{
    RawMaterial = 1,
    FinishedGood = 2,
    ByProduct = 3,
    Consumable = 4,
    Other = 5,
}

public sealed class Product :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private Product()
    {
    }

    private Product(
        Guid workspaceId,
        Guid companyId,
        Guid productGroupId,
        string code,
        string name,
        string? localName,
        ProductType productType,
        bool isPurchasable,
        string? processingFamilyCode,
        string? description,
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
        ProductGroupId = MasterDataValueRules.RequiredId(
            productGroupId,
            nameof(productGroupId));
        NormalizedCode = MasterDataValueRules.NormalizeCode(code);
        Code = NormalizedCode;
        ApplyDetails(
            productGroupId,
            name,
            localName,
            productType,
            isPurchasable,
            processingFamilyCode,
            description,
            notes);
        Status = MasterDataStatus.Active;
        CreatedAtUtc = MasterDataValueRules.RequireUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid ProductGroupId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string NormalizedCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? LocalName { get; private set; }

    public ProductType ProductType { get; private set; }

    public bool IsPurchasable { get; private set; }

    public string? ProcessingFamilyCode { get; private set; }

    public string? NormalizedProcessingFamilyCode { get; private set; }

    public string? Description { get; private set; }

    public string? Notes { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static Product Create(
        Guid workspaceId,
        Guid companyId,
        Guid productGroupId,
        string code,
        string name,
        string? localName,
        ProductType productType,
        bool isPurchasable,
        string? processingFamilyCode,
        string? description,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        return new Product(
            workspaceId,
            companyId,
            productGroupId,
            code,
            name,
            localName,
            productType,
            isPurchasable,
            processingFamilyCode,
            description,
            notes,
            createdAtUtc);
    }

    public void Update(
        Guid productGroupId,
        string name,
        string? localName,
        ProductType productType,
        bool isPurchasable,
        string? processingFamilyCode,
        string? description,
        string? notes,
        bool hasActiveSupplierScope,
        bool hasActiveBagStandard,
        DateTimeOffset updatedAtUtc)
    {
        if (!isPurchasable)
        {
            RequireAssociationsReleased(
                hasActiveSupplierScope,
                hasActiveBagStandard);
        }

        ApplyDetails(
            productGroupId,
            name,
            localName,
            productType,
            isPurchasable,
            processingFamilyCode,
            description,
            notes);
        AdvanceRevision(updatedAtUtc);
    }

    public void Deactivate(
        bool hasActiveSupplierScope,
        bool hasActiveBagStandard,
        DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireInactive(Status);
        RequireAssociationsReleased(
            hasActiveSupplierScope,
            hasActiveBagStandard);
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
        Guid productGroupId,
        string name,
        string? localName,
        ProductType productType,
        bool isPurchasable,
        string? processingFamilyCode,
        string? description,
        string? notes)
    {
        ProductGroupId = MasterDataValueRules.RequiredId(
            productGroupId,
            "productGroupId");
        if (!Enum.IsDefined(productType))
        {
            throw new MasterDataDomainException(
                "PRODUCT_INVALID",
                "Product type must be RawMaterial, FinishedGood, ByProduct, Consumable, or Other.",
                "productType");
        }

        Name = MasterDataValueRules.RequiredText(
            name,
            MasterDataValueRules.MaximumNameLength,
            "name",
            "PRODUCT_INVALID");
        LocalName = MasterDataValueRules.OptionalText(
            localName,
            MasterDataValueRules.MaximumNameLength,
            "localName",
            "PRODUCT_INVALID");
        ProductType = productType;
        IsPurchasable = isPurchasable;
        (ProcessingFamilyCode, NormalizedProcessingFamilyCode) =
            CanonicalizeProcessingFamily(processingFamilyCode);
        Description = MasterDataValueRules.OptionalText(
            description,
            1000,
            "description",
            "PRODUCT_INVALID");
        Notes = MasterDataValueRules.OptionalText(
            notes,
            MasterDataValueRules.MaximumNotesLength,
            "notes",
            "PRODUCT_INVALID");
    }

    public static (string? Display, string? Normalized)
        CanonicalizeProcessingFamily(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        try
        {
            return (
                value,
                MasterDataValueRules.NormalizeCode(
                    value,
                    "processingFamilyCode"));
        }
        catch (MasterDataDomainException)
        {
            throw new MasterDataDomainException(
                "PRODUCT_PROCESSING_FAMILY_INVALID",
                "Processing family must use the 2 to 32 character master-code format.",
                "processingFamilyCode");
        }
    }

    private static void RequireAssociationsReleased(
        bool hasActiveSupplierScope,
        bool hasActiveBagStandard)
    {
        if (hasActiveSupplierScope)
        {
            throw new MasterDataDomainException(
                "PRODUCT_SUPPLIER_SCOPE_IN_USE",
                "Active supplier product scopes must be deactivated first.",
                "status");
        }

        if (hasActiveBagStandard)
        {
            throw new MasterDataDomainException(
                "PRODUCT_STANDARD_BAG_WEIGHT_IN_USE",
                "Active standard bag weights must be deactivated first.",
                "status");
        }
    }

    private void AdvanceRevision(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = MasterDataValueRules.RequireRevisionTimestamp(
            UpdatedAtUtc,
            updatedAtUtc);
        Version = MasterDataValueRules.NextVersion(Version);
    }
}
