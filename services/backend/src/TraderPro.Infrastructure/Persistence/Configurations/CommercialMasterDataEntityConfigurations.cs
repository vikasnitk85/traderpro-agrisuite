using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Operations;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.MasterData;

namespace TraderPro.Infrastructure.Persistence.Configurations;

internal sealed class BusinessLocationConfiguration :
    IEntityTypeConfiguration<BusinessLocation>
{
    public void Configure(EntityTypeBuilder<BusinessLocation> builder)
    {
        builder.ToTable(
            "business_locations",
            "operations",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_business_locations_status",
                    "status IN (1, 2)");
                table.HasCheckConstraint(
                    "ck_business_locations_type",
                    "location_type IN (1, 2, 3, 4, 5)");
                table.HasCheckConstraint(
                    "ck_business_locations_code",
                    """
                    code = normalized_code
                    AND normalized_code ~ '^[A-Z0-9-]{2,32}$'
                    """);
                table.HasCheckConstraint(
                    "ck_business_locations_version",
                    "version > 0");
                table.HasTrigger("tr_business_locations_protect");
            });
        ConfigureMasterKey(builder, "business_locations");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.BranchId,
            entity.Id,
        }).HasName(
            "ak_business_locations_workspace_company_branch_id");
        ConfigureOwnership(builder);
        ConfigureCode(builder);
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength)
            .IsRequired();
        builder.Property(entity => entity.LocalName)
            .HasColumnName("local_name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.LocationType)
            .HasColumnName("location_type")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.AddressLine)
            .HasColumnName("address_line")
            .HasMaxLength(500);
        builder.Property(entity => entity.Notes)
            .HasColumnName("notes")
            .HasMaxLength(MasterDataValueRules.MaximumNotesLength);
        ConfigureStatusAndVersion(builder);
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.BranchId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_business_locations_branches_workspace_company_branch");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.NormalizedCode,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_business_locations_workspace_company_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.BranchId,
            entity.Status,
            entity.NormalizedCode,
            entity.Id,
        })
            .HasDatabaseName(
                "ix_business_locations_active_destination_lookup");
    }

    private static void ConfigureOwnership(
        EntityTypeBuilder<BusinessLocation> builder)
    {
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.CompanyId)
            .HasColumnName("company_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.BranchId)
            .HasColumnName("branch_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }

    private static void ConfigureCode(
        EntityTypeBuilder<BusinessLocation> builder)
    {
        builder.Property(entity => entity.Code)
            .HasColumnName("code")
            .HasMaxLength(MasterDataValueRules.MaximumCodeLength)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.NormalizedCode)
            .HasColumnName("normalized_code")
            .HasMaxLength(MasterDataValueRules.MaximumCodeLength)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }

    private static void ConfigureStatusAndVersion(
        EntityTypeBuilder<BusinessLocation> builder)
    {
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        CommercialMasterConfiguration.ConfigureVersionedTimestamps(builder);
    }

    private static void ConfigureMasterKey(
        EntityTypeBuilder<BusinessLocation> builder,
        string tableName)
    {
        builder.HasKey(entity => entity.Id)
            .HasName($"pk_{tableName}");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
    }
}

internal sealed class ReceivingVehicleConfiguration :
    IEntityTypeConfiguration<ReceivingVehicle>
{
    public void Configure(EntityTypeBuilder<ReceivingVehicle> builder)
    {
        builder.ToTable(
            "receiving_vehicles",
            "procurement",
            table =>
            {
                CommercialMasterConfiguration.AddMasterChecks(
                    table,
                    "receiving_vehicles");
                table.HasCheckConstraint(
                    "ck_receiving_vehicles_type",
                    "vehicle_type IN (1, 2, 3, 4)");
                table.HasCheckConstraint(
                    "ck_receiving_vehicles_registration",
                    """
                    registration_number = btrim(registration_number)
                    AND normalized_registration_number ~
                        '^[A-Z0-9]{2,32}$'
                    AND normalized_registration_number = upper(
                        regexp_replace(
                            registration_number,
                            '[ -]',
                            '',
                            'g'))
                    """);
                table.HasTrigger("tr_receiving_vehicles_protect");
            });
        CommercialMasterConfiguration.ConfigureKeyOwnershipCode(
            builder,
            "receiving_vehicles");
        builder.Property(entity => entity.RegistrationNumber)
            .HasColumnName("registration_number")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.NormalizedRegistrationNumber)
            .HasColumnName("normalized_registration_number")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.VehicleType)
            .HasColumnName("vehicle_type")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.OwnerName)
            .HasColumnName("owner_name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.ContactNumber)
            .HasColumnName("contact_number")
            .HasMaxLength(50);
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
            .HasDatabaseName(
                "ux_receiving_vehicles_workspace_company_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.NormalizedRegistrationNumber,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_receiving_vehicles_workspace_company_registration");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Status,
            entity.NormalizedCode,
            entity.Id,
        })
            .HasDatabaseName(
                "ix_receiving_vehicles_company_status_code");
    }
}

internal sealed class BagTypeConfiguration :
    IEntityTypeConfiguration<BagType>
{
    public void Configure(EntityTypeBuilder<BagType> builder)
    {
        builder.ToTable(
            "bag_types",
            "procurement",
            table =>
            {
                CommercialMasterConfiguration.AddMasterChecks(
                    table,
                    "bag_types");
                table.HasCheckConstraint(
                    "ck_bag_types_construction_class",
                    "construction_class IN (1, 2, 3, 4)");
                table.HasCheckConstraint(
                    "ck_bag_types_tare_weight",
                    "standard_tare_weight_kg >= 0");
                table.HasTrigger("tr_bag_types_protect");
            });
        CommercialMasterConfiguration.ConfigureKeyOwnershipCode(
            builder,
            "bag_types");
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength)
            .IsRequired();
        builder.Property(entity => entity.LocalName)
            .HasColumnName("local_name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength);
        builder.Property(entity => entity.ConstructionClass)
            .HasColumnName("construction_class")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.StandardTareWeightKg)
            .HasColumnName("standard_tare_weight_kg")
            .HasPrecision(20, 6)
            .IsRequired();
        builder.Property(entity => entity.IsReturnable)
            .HasColumnName("is_returnable")
            .IsRequired();
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
            .HasDatabaseName("ux_bag_types_workspace_company_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Status,
            entity.NormalizedCode,
            entity.Id,
        })
            .HasDatabaseName("ix_bag_types_company_status_code");
    }
}

internal sealed class WeightProcessingPolicyConfiguration :
    IEntityTypeConfiguration<WeightProcessingPolicy>
{
    public void Configure(EntityTypeBuilder<WeightProcessingPolicy> builder)
    {
        builder.ToTable(
            "weight_processing_policies",
            "procurement",
            table =>
            {
                CommercialMasterConfiguration.AddMasterChecks(
                    table,
                    "weight_processing_policies");
                table.HasCheckConstraint(
                    "ck_weight_processing_policies_decimal_places",
                    "decimal_places IN (1, 2, 3)");
                table.HasCheckConstraint(
                    "ck_weight_processing_policies_method",
                    "processing_method IN ('Standard', 'Floor', 'Ceiling')");
                table.HasTrigger(
                    "tr_weight_processing_policies_protect");
            });
        CommercialMasterConfiguration.ConfigureKeyOwnershipCode(
            builder,
            "weight_processing_policies");
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(MasterDataValueRules.MaximumNameLength)
            .IsRequired();
        builder.Property(entity => entity.DecimalPlaces)
            .HasColumnName("decimal_places")
            .IsRequired();
        builder.Property(entity => entity.ProcessingMethod)
            .HasColumnName("processing_method")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
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
            .HasDatabaseName(
                "ux_weight_processing_policies_workspace_company_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Status,
            entity.NormalizedCode,
            entity.Id,
        })
            .HasDatabaseName(
                "ix_weight_processing_policies_company_status_code");
    }
}

internal sealed class CompanyProcurementSettingsConfiguration :
    IEntityTypeConfiguration<CompanyProcurementSettings>
{
    public void Configure(
        EntityTypeBuilder<CompanyProcurementSettings> builder)
    {
        builder.ToTable(
            "company_procurement_settings",
            "procurement",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_company_procurement_settings_vehicle_mode",
                    "vehicle_selection_mode IN (1, 2)");
                table.HasCheckConstraint(
                    "ck_company_procurement_settings_version",
                    "version > 0");
                table.HasTrigger(
                    "tr_company_procurement_settings_protect");
            });
        builder.HasKey(entity => entity.Id)
            .HasName("pk_company_procurement_settings");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Id,
        }).HasName("ak_company_procurement_settings_workspace_company_id");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.CompanyId)
            .HasColumnName("company_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.DefaultBranchId)
            .HasColumnName("default_branch_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.DefaultDestinationLocationId)
            .HasColumnName("default_destination_location_id")
            .IsRequired();
        builder.Property(entity => entity.DefaultWeightProcessingPolicyId)
            .HasColumnName("default_weight_processing_policy_id")
            .IsRequired();
        builder.Property(entity => entity.VehicleSelectionMode)
            .HasColumnName("vehicle_selection_mode")
            .HasConversion<short>()
            .IsRequired();
        CommercialMasterConfiguration.ConfigureVersionedTimestamps(builder);
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_company_procurement_settings_company");
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.DefaultBranchId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_company_procurement_settings_default_branch");
        builder.HasOne<BusinessLocation>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.DefaultBranchId,
                entity.DefaultDestinationLocationId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.BranchId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_company_procurement_settings_default_destination");
        builder.HasOne<WeightProcessingPolicy>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.DefaultWeightProcessingPolicyId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.CompanyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_company_procurement_settings_default_weight_policy");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_company_procurement_settings_workspace_company");
    }
}

internal static class CommercialMasterConfiguration
{
    public static void AddMasterChecks(
        TableBuilder table,
        string tableName)
    {
        table.HasCheckConstraint(
            $"ck_{tableName}_status",
            "status IN (1, 2)");
        table.HasCheckConstraint(
            $"ck_{tableName}_code",
            """
            code = normalized_code
            AND normalized_code ~ '^[A-Z0-9-]{2,32}$'
            """);
        table.HasCheckConstraint(
            $"ck_{tableName}_version",
            "version > 0");
    }

    public static void ConfigureKeyOwnershipCode<TEntity>(
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
        builder.Property<string>("Code")
            .HasColumnName("code")
            .HasMaxLength(MasterDataValueRules.MaximumCodeLength)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property<string>("NormalizedCode")
            .HasColumnName("normalized_code")
            .HasMaxLength(MasterDataValueRules.MaximumCodeLength)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }

    public static void ConfigureStatusAndVersion<TEntity>(
        EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<MasterDataStatus>("Status")
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        ConfigureVersionedTimestamps(builder);
    }

    public static void ConfigureVersionedTimestamps<TEntity>(
        EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<DateTimeOffset>("CreatedAtUtc")
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property<DateTimeOffset>("UpdatedAtUtc")
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property<long>("Version")
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();
    }

    public static void ConfigureCompanyForeignKey<TEntity>(
        EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey("WorkspaceId", "CompanyId")
            .HasPrincipalKey(
                nameof(Company.WorkspaceId),
                nameof(Company.Id))
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName($"fk_{builder.Metadata.GetTableName()}_company");
    }
}
