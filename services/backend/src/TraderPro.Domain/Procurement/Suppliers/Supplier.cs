using System.Globalization;
using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Procurement.Suppliers;

public enum SupplierType : short
{
    Individual = 1,
    Business = 2,
}

public enum SupplierProductScopeMode : short
{
    Unrestricted = 1,
    Restricted = 2,
}

public sealed class Supplier :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private Supplier()
    {
    }

    private Supplier(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        string? localName,
        SupplierType supplierType,
        SupplierProductScopeMode productScopeMode,
        bool hasInitialActiveScope,
        string? contactName,
        string? contactNumber,
        string? email,
        string? addressLine,
        string? taxRegistrationNumber,
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
            supplierType,
            productScopeMode,
            hasInitialActiveScope,
            contactName,
            contactNumber,
            email,
            addressLine,
            taxRegistrationNumber,
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

    public SupplierType SupplierType { get; private set; }

    public SupplierProductScopeMode ProductScopeMode { get; private set; }

    public string? ContactName { get; private set; }

    public string? ContactNumber { get; private set; }

    public string? Email { get; private set; }

    public string? AddressLine { get; private set; }

    public string? TaxRegistrationNumber { get; private set; }

    public string? NormalizedTaxRegistrationNumber { get; private set; }

    public string? Notes { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static Supplier Create(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        string? localName,
        SupplierType supplierType,
        SupplierProductScopeMode productScopeMode,
        bool hasInitialActiveScope,
        string? contactName,
        string? contactNumber,
        string? email,
        string? addressLine,
        string? taxRegistrationNumber,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        return new Supplier(
            workspaceId,
            companyId,
            code,
            name,
            localName,
            supplierType,
            productScopeMode,
            hasInitialActiveScope,
            contactName,
            contactNumber,
            email,
            addressLine,
            taxRegistrationNumber,
            notes,
            createdAtUtc);
    }

    public void Update(
        string name,
        string? localName,
        SupplierType supplierType,
        SupplierProductScopeMode productScopeMode,
        bool hasActiveScope,
        string? contactName,
        string? contactNumber,
        string? email,
        string? addressLine,
        string? taxRegistrationNumber,
        string? notes,
        DateTimeOffset updatedAtUtc)
    {
        ApplyDetails(
            name,
            localName,
            supplierType,
            productScopeMode,
            hasActiveScope,
            contactName,
            contactNumber,
            email,
            addressLine,
            taxRegistrationNumber,
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

    private void ApplyDetails(
        string name,
        string? localName,
        SupplierType supplierType,
        SupplierProductScopeMode productScopeMode,
        bool hasActiveScope,
        string? contactName,
        string? contactNumber,
        string? email,
        string? addressLine,
        string? taxRegistrationNumber,
        string? notes)
    {
        if (!Enum.IsDefined(supplierType))
        {
            throw Invalid(
                "Supplier type must be Individual or Business.",
                "supplierType");
        }

        if (!Enum.IsDefined(productScopeMode))
        {
            throw Invalid(
                "Product scope mode must be Unrestricted or Restricted.",
                "productScopeMode");
        }

        if (productScopeMode is SupplierProductScopeMode.Restricted &&
            !hasActiveScope)
        {
            throw new MasterDataDomainException(
                "SUPPLIER_PRODUCT_SCOPE_REQUIRED",
                "A Restricted supplier requires at least one active product scope.",
                "initialProductIds");
        }

        Name = MasterDataValueRules.RequiredText(
            name,
            MasterDataValueRules.MaximumNameLength,
            "name",
            "SUPPLIER_INVALID");
        LocalName = MasterDataValueRules.OptionalText(
            localName,
            MasterDataValueRules.MaximumNameLength,
            "localName",
            "SUPPLIER_INVALID");
        SupplierType = supplierType;
        ProductScopeMode = productScopeMode;
        ContactName = MasterDataValueRules.OptionalText(
            contactName,
            MasterDataValueRules.MaximumNameLength,
            "contactName",
            "SUPPLIER_INVALID");
        ContactNumber = CanonicalizeContactNumber(contactNumber);
        Email = CanonicalizeEmail(email);
        AddressLine = MasterDataValueRules.OptionalText(
            addressLine,
            500,
            "addressLine",
            "SUPPLIER_INVALID");
        (TaxRegistrationNumber, NormalizedTaxRegistrationNumber) =
            CanonicalizeTaxRegistration(taxRegistrationNumber);
        Notes = MasterDataValueRules.OptionalText(
            notes,
            MasterDataValueRules.MaximumNotesLength,
            "notes",
            "SUPPLIER_INVALID");
    }

    private void AdvanceRevision(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = MasterDataValueRules.RequireRevisionTimestamp(
            UpdatedAtUtc,
            updatedAtUtc);
        Version = MasterDataValueRules.NextVersion(Version);
    }

    public static string? CanonicalizeContactNumber(string? value)
    {
        var canonical = MasterDataValueRules.OptionalText(
            value,
            50,
            "contactNumber",
            "SUPPLIER_INVALID");
        if (canonical is null)
        {
            return null;
        }

        if (!canonical.Any(char.IsAsciiDigit) ||
            canonical.Any(character =>
                !char.IsAsciiDigit(character) &&
                character is not (' ' or '+' or '-' or '(' or ')' or '.')))
        {
            throw Invalid(
                "Contact number contains unsupported characters.",
                "contactNumber");
        }

        return canonical;
    }

    public static string? CanonicalizeEmail(string? value)
    {
        var canonical = MasterDataValueRules.OptionalText(
            value,
            254,
            "email",
            "SUPPLIER_INVALID");
        if (canonical is null)
        {
            return null;
        }

        var at = canonical.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 ||
            at != canonical.LastIndexOf('@') ||
            at == canonical.Length - 1 ||
            canonical.Any(character =>
                char.IsWhiteSpace(character) ||
                char.IsControl(character)))
        {
            throw Invalid("Email address is invalid.", "email");
        }

        return canonical.ToLower(CultureInfo.InvariantCulture);
    }

    public static (string? Display, string? Normalized)
        CanonicalizeTaxRegistration(string? value)
    {
        var canonical = MasterDataValueRules.OptionalText(
            value,
            64,
            "taxRegistrationNumber",
            "SUPPLIER_INVALID");
        if (canonical is null)
        {
            return (null, null);
        }

        var normalized = new string(
            canonical
                .Where(character => character is not (' ' or '-'))
                .Select(character =>
                    char.ToUpper(character, CultureInfo.InvariantCulture))
                .ToArray());
        if (normalized.Length is < 2 or > 64 ||
            normalized.Any(character =>
                character is not (>= 'A' and <= 'Z') &&
                character is not (>= '0' and <= '9')))
        {
            throw Invalid(
                "Tax registration must normalize to letters and digits.",
                "taxRegistrationNumber");
        }

        return (canonical, normalized);
    }

    private static MasterDataDomainException Invalid(
        string message,
        string field)
    {
        return new MasterDataDomainException(
            "SUPPLIER_INVALID",
            message,
            field);
    }
}
