using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderPro.Domain.Catalog;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Domain.Procurement.Suppliers;

namespace TraderPro.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration :
    IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable(
            "suppliers",
            "procurement",
            table =>
            {
                CommercialMasterConfiguration.AddMasterChecks(
                    table,
                    "suppliers");
                table.HasCheckConstraint(
                    "ck_suppliers_type",
                    "supplier_type IN (1, 2)");
                table.HasCheckConstraint(
                    "ck_suppliers_scope_mode",
                    "product_scope_mode IN (1, 2)");
                table.HasCheckConstraint(
                    "ck_suppliers_email",
                    "email IS NULL OR (email = lower(btrim(email)) AND length(email) <= 254 AND email !~ '[[:space:][:cntrl:]]' AND length(email) - length(replace(email, '@', '')) = 1 AND position('@' in email) > 1 AND position('@' in email) < length(email))");
                table.HasCheckConstraint(
                    "ck_suppliers_tax_registration",
                    "(tax_registration_number IS NULL AND normalized_tax_registration_number IS NULL) OR (tax_registration_number = btrim(tax_registration_number) AND normalized_tax_registration_number ~ '^[A-Z0-9]{2,64}$' AND normalized_tax_registration_number = upper(regexp_replace(tax_registration_number, '[ -]', '', 'g')))");
                table.HasTrigger("tr_suppliers_protect");
                table.HasTrigger("tr_suppliers_revision_contract");
                table.HasTrigger("tr_suppliers_scope_invariant");
            });
        CommercialMasterConfiguration.ConfigureKeyOwnershipCode(
            builder,
            "suppliers");
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength)
            .IsRequired();
        builder.Property(entity => entity.LocalName)
            .HasColumnName("local_name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.SupplierType)
            .HasColumnName("supplier_type")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.ProductScopeMode)
            .HasColumnName("product_scope_mode")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.ContactName)
            .HasColumnName("contact_name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.ContactNumber)
            .HasColumnName("contact_number")
            .HasMaxLength(50);
        builder.Property(entity => entity.Email)
            .HasColumnName("email")
            .HasMaxLength(254);
        builder.Property(entity => entity.AddressLine)
            .HasColumnName("address_line")
            .HasMaxLength(500);
        builder.Property(entity => entity.TaxRegistrationNumber)
            .HasColumnName("tax_registration_number")
            .HasMaxLength(64);
        builder.Property(entity => entity.NormalizedTaxRegistrationNumber)
            .HasColumnName("normalized_tax_registration_number")
            .HasMaxLength(64);
        builder.Property(entity => entity.Notes)
            .HasColumnName("notes")
            .HasMaxLength(MasterDataValueRules.MaximumNotesLength);
        CommercialMasterConfiguration.ConfigureStatusAndVersion(builder);
        CommercialMasterConfiguration.ConfigureCompanyForeignKey(builder);
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.NormalizedCode,
        })
            .IsUnique()
            .HasDatabaseName("ux_suppliers_workspace_company_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.NormalizedTaxRegistrationNumber,
        })
            .IsUnique()
            .HasFilter("normalized_tax_registration_number IS NOT NULL")
            .HasDatabaseName("ux_suppliers_workspace_company_tax");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Status,
            entity.NormalizedCode,
            entity.Id,
        })
            .HasDatabaseName("ix_suppliers_company_status_code");
    }
}

internal sealed class ProductGroupConfiguration :
    IEntityTypeConfiguration<ProductGroup>
{
    public void Configure(EntityTypeBuilder<ProductGroup> builder)
    {
        builder.ToTable(
            "product_groups",
            "catalog",
            table =>
            {
                CommercialMasterConfiguration.AddMasterChecks(
                    table,
                    "product_groups");
                table.HasTrigger("tr_product_groups_protect");
                table.HasTrigger("tr_product_groups_revision_contract");
                table.HasTrigger("tr_product_groups_in_use");
            });
        CommercialMasterConfiguration.ConfigureKeyOwnershipCode(
            builder,
            "product_groups");
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength)
            .IsRequired();
        builder.Property(entity => entity.LocalName)
            .HasColumnName("local_name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);
        CommercialMasterConfiguration.ConfigureStatusAndVersion(builder);
        CommercialMasterConfiguration.ConfigureCompanyForeignKey(builder);
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.NormalizedCode,
        })
            .IsUnique()
            .HasDatabaseName("ux_product_groups_workspace_company_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Status,
            entity.NormalizedCode,
            entity.Id,
        })
            .HasDatabaseName("ix_product_groups_company_status_code");
    }
}

internal sealed class ProductConfiguration :
    IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(
            "products",
            "catalog",
            table =>
            {
                CommercialMasterConfiguration.AddMasterChecks(
                    table,
                    "products");
                table.HasCheckConstraint(
                    "ck_products_type",
                    "product_type IN (1, 2, 3, 4, 5)");
                table.HasCheckConstraint(
                    "ck_products_processing_family",
                    "(processing_family_code IS NULL AND normalized_processing_family_code IS NULL) OR (processing_family_code = btrim(processing_family_code) AND normalized_processing_family_code ~ '^[A-Z0-9-]{2,32}$' AND normalized_processing_family_code = upper(processing_family_code))");
                table.HasTrigger("tr_products_protect");
                table.HasTrigger("tr_products_revision_contract");
                table.HasTrigger("tr_products_validate_group");
                table.HasTrigger("tr_products_in_use");
            });
        CommercialMasterConfiguration.ConfigureKeyOwnershipCode(
            builder,
            "products");
        builder.Property(entity => entity.ProductGroupId)
            .HasColumnName("product_group_id")
            .IsRequired();
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength)
            .IsRequired();
        builder.Property(entity => entity.LocalName)
            .HasColumnName("local_name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.ProductType)
            .HasColumnName("product_type")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.IsPurchasable)
            .HasColumnName("is_purchasable")
            .IsRequired();
        builder.Property(entity => entity.ProcessingFamilyCode)
            .HasColumnName("processing_family_code")
            .HasMaxLength(MasterDataValueRules.MaximumCodeLength);
        builder.Property(entity => entity.NormalizedProcessingFamilyCode)
            .HasColumnName("normalized_processing_family_code")
            .HasMaxLength(MasterDataValueRules.MaximumCodeLength);
        builder.Property(entity => entity.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);
        builder.Property(entity => entity.Notes)
            .HasColumnName("notes")
            .HasMaxLength(MasterDataValueRules.MaximumNotesLength);
        CommercialMasterConfiguration.ConfigureStatusAndVersion(builder);
        CommercialMasterConfiguration.ConfigureCompanyForeignKey(builder);
        builder.HasOne<ProductGroup>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.ProductGroupId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_products_product_group");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.NormalizedCode,
        })
            .IsUnique()
            .HasDatabaseName("ux_products_workspace_company_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Status,
            entity.IsPurchasable,
            entity.NormalizedCode,
            entity.Id,
        })
            .HasDatabaseName("ix_products_receiving_selection");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.ProductGroupId,
            entity.Status,
        })
            .HasDatabaseName("ix_products_group_status");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.NormalizedProcessingFamilyCode,
            entity.Status,
        })
            .HasDatabaseName("ix_products_processing_family");
    }
}

internal sealed class ProductStandardBagWeightConfiguration :
    IEntityTypeConfiguration<ProductStandardBagWeight>
{
    public void Configure(
        EntityTypeBuilder<ProductStandardBagWeight> builder)
    {
        builder.ToTable(
            "product_standard_bag_weights",
            "catalog",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_product_standard_bag_weights_status",
                    "status IN (1, 2)");
                table.HasCheckConstraint(
                    "ck_product_standard_bag_weights_value",
                    "standard_content_weight_kg > 0");
                table.HasCheckConstraint(
                    "ck_product_standard_bag_weights_default",
                    "NOT is_default OR status = 1");
                table.HasCheckConstraint(
                    "ck_product_standard_bag_weights_version",
                    "version > 0");
                table.HasTrigger("tr_product_standard_bag_weights_protect");
                table.HasTrigger(
                    "tr_product_standard_bag_weights_revision_contract");
                table.HasTrigger(
                    "tr_product_standard_bag_weights_validate");
            });
        ConfigureAssociationKey(builder, "product_standard_bag_weights");
        builder.Property(entity => entity.ProductId)
            .HasColumnName("product_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.BagTypeId)
            .HasColumnName("bag_type_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.Label)
            .HasColumnName("label")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.StandardContentWeightKg)
            .HasColumnName("standard_content_weight_kg")
            .HasPrecision(20, 6)
            .IsRequired();
        builder.Property(entity => entity.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();
        CommercialMasterConfiguration.ConfigureStatusAndVersion(builder);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.ProductId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_standard_bag_weights_product");
        builder.HasOne<BagType>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.BagTypeId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_standard_bag_weights_bag_type");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.ProductId,
            entity.BagTypeId,
            entity.StandardContentWeightKg,
        })
            .IsUnique()
            .HasDatabaseName("ux_product_standard_bag_weights_identity");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.ProductId,
        })
            .IsUnique()
            .HasFilter("status = 1 AND is_default")
            .HasDatabaseName("ux_product_standard_bag_weights_active_default");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.ProductId,
            entity.Status,
            entity.Id,
        })
            .HasDatabaseName("ix_product_standard_bag_weights_product_status");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.BagTypeId,
            entity.Status,
        })
            .HasDatabaseName("ix_product_standard_bag_weights_bag_status");
    }

    private static void ConfigureAssociationKey<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        string tableName)
        where TEntity : class
    {
        builder.HasKey("Id").HasName($"pk_{tableName}");
        builder.Property<Guid>("Id")
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.HasAlternateKey("WorkspaceId", "CompanyId", "Id")
            .HasName($"ak_{tableName}_workspace_company_id");
        builder.Property<Guid>("WorkspaceId")
            .HasColumnName("workspace_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property<Guid>("CompanyId")
            .HasColumnName("company_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}

internal sealed class SupplierProductScopeConfiguration :
    IEntityTypeConfiguration<SupplierProductScope>
{
    public void Configure(EntityTypeBuilder<SupplierProductScope> builder)
    {
        builder.ToTable(
            "supplier_product_scopes",
            "procurement",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_supplier_product_scopes_status",
                    "status IN (1, 2)");
                table.HasCheckConstraint(
                    "ck_supplier_product_scopes_version",
                    "version > 0");
                table.HasTrigger("tr_supplier_product_scopes_protect");
                table.HasTrigger(
                    "tr_supplier_product_scopes_revision_contract");
                table.HasTrigger("tr_supplier_product_scopes_validate");
                table.HasTrigger("tr_supplier_product_scopes_invariant");
            });
        builder.HasKey(entity => entity.Id)
            .HasName("pk_supplier_product_scopes");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Id,
        }).HasName("ak_supplier_product_scopes_workspace_company_id");
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.CompanyId)
            .HasColumnName("company_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.SupplierId)
            .HasColumnName("supplier_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.ProductId)
            .HasColumnName("product_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        CommercialMasterConfiguration.ConfigureStatusAndVersion(builder);
        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.SupplierId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_supplier_product_scopes_supplier");
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.ProductId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_supplier_product_scopes_product");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.SupplierId,
            entity.ProductId,
        })
            .IsUnique()
            .HasDatabaseName("ux_supplier_product_scopes_identity");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.SupplierId,
            entity.Status,
            entity.Id,
        })
            .HasDatabaseName("ix_supplier_product_scopes_supplier_status");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.ProductId,
            entity.Status,
        })
            .HasDatabaseName("ix_supplier_product_scopes_product_status");
    }
}
