using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Catalog;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Platform.Identity;
using TraderPro.Domain.Procurement.Suppliers;

namespace TraderPro.UnitTests;

public sealed class CommercialSupplierProductCatalogTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Supplier_normalizes_identity_contact_email_tax_and_optional_text()
    {
        var supplier = Supplier.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "sup-01",
            "  Aman Grower  ",
            "   ",
            SupplierType.Individual,
            SupplierProductScopeMode.Unrestricted,
            false,
            " Contact Person ",
            "+91 (987) 654-3210",
            " OWNER@EXAMPLE.COM ",
            " Market Road ",
            " gst-12 ab 34 ",
            "   ",
            UtcNow);

        Assert.Equal("SUP-01", supplier.Code);
        Assert.Equal("Aman Grower", supplier.Name);
        Assert.Null(supplier.LocalName);
        Assert.Equal("owner@example.com", supplier.Email);
        Assert.Equal("gst-12 ab 34", supplier.TaxRegistrationNumber);
        Assert.Equal("GST12AB34", supplier.NormalizedTaxRegistrationNumber);
        Assert.Null(supplier.Notes);
        Assert.Equal(1, supplier.Version);
    }

    [Fact]
    public void Restricted_supplier_requires_scope_and_revisions_advance_once()
    {
        var exception = Assert.Throws<MasterDataDomainException>(
            () => Supplier.Create(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "SUP-01",
                "Restricted",
                null,
                SupplierType.Business,
                SupplierProductScopeMode.Restricted,
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                UtcNow));
        Assert.Equal("SUPPLIER_PRODUCT_SCOPE_REQUIRED", exception.Code);

        var supplier = Supplier.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "SUP-02",
            "Restricted",
            null,
            SupplierType.Business,
            SupplierProductScopeMode.Restricted,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            UtcNow);
        supplier.Update(
            "Restricted Business",
            null,
            SupplierType.Business,
            SupplierProductScopeMode.Unrestricted,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            UtcNow.AddMinutes(1));
        supplier.Deactivate(UtcNow.AddMinutes(2));
        supplier.Reactivate(UtcNow.AddMinutes(3));

        Assert.Equal(4, supplier.Version);
        Assert.Equal(UtcNow.AddMinutes(3), supplier.UpdatedAtUtc);
    }

    [Fact]
    public void Product_group_and_product_enforce_in_use_boundaries()
    {
        var group = ProductGroup.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PADDY",
            "Paddy",
            null,
            null,
            UtcNow);
        var groupException = Assert.Throws<MasterDataDomainException>(
            () => group.Deactivate(true, UtcNow.AddMinutes(1)));
        Assert.Equal("PRODUCT_GROUP_IN_USE", groupException.Code);
        Assert.Equal(1, group.Version);

        var product = Product.Create(
            group.WorkspaceId,
            group.CompanyId,
            group.Id,
            "H-AMAN",
            "H Aman",
            null,
            ProductType.RawMaterial,
            true,
            "Aman",
            null,
            null,
            UtcNow);
        Assert.Equal("Aman", product.ProcessingFamilyCode);
        Assert.Equal("AMAN", product.NormalizedProcessingFamilyCode);
        var scopeException = Assert.Throws<MasterDataDomainException>(
            () => product.Deactivate(
                true,
                false,
                UtcNow.AddMinutes(1)));
        Assert.Equal("PRODUCT_SUPPLIER_SCOPE_IN_USE", scopeException.Code);
        var standardException = Assert.Throws<MasterDataDomainException>(
            () => product.Deactivate(
                false,
                true,
                UtcNow.AddMinutes(1)));
        Assert.Equal(
            "PRODUCT_STANDARD_BAG_WEIGHT_IN_USE",
            standardException.Code);
        Assert.Equal(1, product.Version);
    }

    [Theory]
    [InlineData(ProductType.RawMaterial)]
    [InlineData(ProductType.FinishedGood)]
    [InlineData(ProductType.ByProduct)]
    [InlineData(ProductType.Consumable)]
    [InlineData(ProductType.Other)]
    public void Every_product_type_is_controlled(ProductType type)
    {
        var product = Product.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PR-01",
            "Product",
            null,
            type,
            false,
            null,
            null,
            null,
            UtcNow);

        Assert.Equal(type, product.ProductType);
        Assert.False(product.IsPurchasable);
    }

    [Fact]
    public void Standard_bag_weight_is_exact_positive_and_default_is_explicit()
    {
        var standard = ProductStandardBagWeight.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            " 50 kg content ",
            50.125000m,
            false,
            UtcNow);
        standard.SetDefault(UtcNow.AddMinutes(1));
        standard.Deactivate(UtcNow.AddMinutes(2));
        standard.Reactivate(UtcNow.AddMinutes(3));

        Assert.Equal(50.125000m, standard.StandardContentWeightKg);
        Assert.False(standard.IsDefault);
        Assert.Equal(MasterDataStatus.Active, standard.Status);
        Assert.Equal(4, standard.Version);

        var exception = Assert.Throws<MasterDataDomainException>(
            () => ProductStandardBagWeight.Create(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                null,
                0m,
                false,
                UtcNow));
        Assert.Equal(
            "PRODUCT_STANDARD_BAG_WEIGHT_VALUE_INVALID",
            exception.Code);
    }

    [Fact]
    public void Restricted_supplier_scope_cannot_deactivate_final_active_row()
    {
        var scope = SupplierProductScope.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            UtcNow);
        var exception = Assert.Throws<MasterDataDomainException>(
            () => scope.Deactivate(
                supplierIsRestricted: true,
                activeScopeCount: 1,
                UtcNow.AddMinutes(1)));
        Assert.Equal("SUPPLIER_PRODUCT_SCOPE_REQUIRED", exception.Code);
        Assert.Equal(1, scope.Version);

        scope.Deactivate(
            supplierIsRestricted: false,
            activeScopeCount: 1,
            UtcNow.AddMinutes(1));
        scope.Reactivate(UtcNow.AddMinutes(2));
        Assert.Equal(3, scope.Version);
    }

    [Fact]
    public void Canonical_sets_optional_text_and_decimal_hashes_are_stable()
    {
        var firstId = Guid.Parse("0198b552-c4a0-7000-8000-000000000001");
        var secondId = Guid.Parse("0198b552-c4a0-7000-8000-000000000002");
        Assert.Equal(
            [firstId, secondId],
            CommercialMasterDataInputRules.CanonicalInitialProductIds(
                [secondId, firstId]));
        var duplicate = Assert.Throws<ApplicationProblemException>(
            () => CommercialMasterDataInputRules.CanonicalInitialProductIds(
                [firstId, firstId]));
        Assert.Equal("SUPPLIER_PRODUCT_SCOPE_INVALID", duplicate.Code);

        var context = new TestContext();
        var first = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateProductStandardBagWeight,
            context,
            ("label", CommercialMasterDataInputRules.CanonicalOptionalText(
                "   ")),
            ("weight", 50.1m));
        var identical = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateProductStandardBagWeight,
            context,
            ("label", (string?)null),
            ("weight", 50.100000m));
        var changed = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateProductStandardBagWeight,
            context,
            ("label", (string?)null),
            ("weight", 50.2m));

        Assert.Equal(first, identical);
        Assert.NotEqual(first, changed);
    }

    private sealed record TestContext : IAuthenticatedTraderProContext
    {
        public bool IsBound => true;

        public Guid WorkspaceId { get; init; } = Guid.CreateVersion7();

        public Guid UserId { get; init; } = Guid.CreateVersion7();

        public Guid DeviceId { get; init; } = Guid.CreateVersion7();

        public Guid CompanyId { get; init; } = Guid.CreateVersion7();

        public Guid DefaultBranchId { get; init; } = Guid.CreateVersion7();

        public TraderProRole Role => TraderProRole.Owner;

        public bool IsOwner => true;

        public bool IsOperatorOrOwner => true;

        public Guid TokenFamilyId { get; } = Guid.CreateVersion7();

        public string CorrelationId { get; } =
            Guid.CreateVersion7().ToString("D");
    }
}
