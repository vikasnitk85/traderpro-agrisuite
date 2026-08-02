using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderPro.Domain.Catalog;
using TraderPro.Domain.Operations;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Domain.Procurement.Receiving;
using TraderPro.Domain.Procurement.Suppliers;

namespace TraderPro.Infrastructure.Persistence.Configurations;

internal sealed class CommercialReceivingReferencePolicyConfiguration :
    IEntityTypeConfiguration<CommercialReceivingReferencePolicy>
{
    public void Configure(EntityTypeBuilder<CommercialReceivingReferencePolicy> b)
    {
        b.ToTable("commercial_receiving_reference_policies", "procurement", t =>
        {
            t.HasCheckConstraint("ck_commercial_receiving_reference_policies_document_type", "document_type = 'CommercialReceiving'");
            t.HasCheckConstraint("ck_commercial_receiving_reference_policies_reset_policy", "reset_policy IN (1, 2, 3)");
            t.HasCheckConstraint("ck_commercial_receiving_reference_policies_starting_number", "starting_number > 0");
            t.HasCheckConstraint("ck_commercial_receiving_reference_policies_version", "version > 0");
            t.HasCheckConstraint("ck_commercial_receiving_reference_policies_template", "char_length(format_template) BETWEEN 1 AND 100 AND format_template = btrim(format_template) AND regexp_count(format_template, '\\{SEQ:0{1,12}\\}') = 1 AND regexp_replace(format_template, '\\{SEQ:0{1,12}\\}|\\{YYYY\\}|\\{MM\\}', '', 'g') !~ '[{}[:cntrl:]]' AND (reset_policy <> 2 OR strpos(format_template, '{YYYY}') > 0) AND (reset_policy <> 3 OR (strpos(format_template, '{YYYY}') > 0 AND strpos(format_template, '{MM}') > 0))");
        });
        b.HasKey(x => x.Id).HasName("pk_commercial_receiving_reference_policies");
        MapId(b.Property(x => x.Id), "id");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        b.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        b.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(50).IsRequired();
        b.Property(x => x.FormatTemplate).HasColumnName("format_template").HasMaxLength(100).IsRequired();
        b.Property(x => x.ResetPolicy).HasColumnName("reset_policy").HasConversion<short>().IsRequired();
        b.Property(x => x.StartingNumber).HasColumnName("starting_number").IsRequired();
        Timestamps(b.Property(x => x.CreatedAtUtc), b.Property(x => x.UpdatedAtUtc));
        Version(b.Property(x => x.Version));
        b.HasAlternateKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).HasName("ak_commercial_receiving_reference_policies_scope_id");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.DocumentType }).IsUnique().HasDatabaseName("ux_commercial_receiving_reference_policies_company_document");
        b.HasOne<Company>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId }).HasPrincipalKey(x => new { x.WorkspaceId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_reference_policies_company");
    }

    internal static void MapId(PropertyBuilder<Guid> property, string name) => property.HasColumnName(name).ValueGeneratedNever();
    internal static void Timestamps(PropertyBuilder<DateTimeOffset> created, PropertyBuilder<DateTimeOffset> updated)
    {
        created.HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        updated.HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();
    }
    internal static void Version(PropertyBuilder<long> property) => property.HasColumnName("version").IsConcurrencyToken().IsRequired();
}

internal sealed class CommercialReceivingReferenceCounterConfiguration :
    IEntityTypeConfiguration<CommercialReceivingReferenceCounter>
{
    public void Configure(EntityTypeBuilder<CommercialReceivingReferenceCounter> b)
    {
        b.ToTable("commercial_receiving_reference_counters", "procurement", t =>
        {
            t.HasCheckConstraint("ck_commercial_receiving_reference_counters_next", "next_number > 0");
            t.HasCheckConstraint("ck_commercial_receiving_reference_counters_version", "version > 0");
        });
        b.HasKey(x => new { x.WorkspaceId, x.CompanyId, x.PolicyId, x.PeriodKey }).HasName("pk_commercial_receiving_reference_counters");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.PolicyId).HasColumnName("policy_id");
        b.Property(x => x.PeriodKey).HasColumnName("period_key").HasMaxLength(20);
        b.Property(x => x.NextNumber).HasColumnName("next_number");
        CommercialReceivingReferencePolicyConfiguration.Version(b.Property(x => x.Version));
        b.HasOne<CommercialReceivingReferencePolicy>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.PolicyId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_reference_counters_policy");
    }
}

internal sealed class CommercialReceivingSessionConfiguration :
    IEntityTypeConfiguration<CommercialReceivingSession>
{
    public void Configure(EntityTypeBuilder<CommercialReceivingSession> b)
    {
        b.ToTable("commercial_receiving_sessions", "procurement", t =>
        {
            t.HasCheckConstraint("ck_commercial_receiving_sessions_status", "status IN (1, 2)");
            t.HasCheckConstraint("ck_commercial_receiving_sessions_shape", "version > 0 AND ownership_generation > 0 AND next_expected_local_sequence >= 2 AND entry_count >= 0 AND processed_total_weight_kg >= 0 AND cloud_reference_sequence > 0 AND reference_policy_version_snapshot > 0 AND ((status = 1 AND submitted_at_utc IS NULL) OR (status = 2 AND submitted_at_utc IS NOT NULL AND entry_count > 0 AND processed_total_weight_kg > 0))");
            t.HasCheckConstraint("ck_commercial_receiving_sessions_snapshots", "supplier_version_snapshot > 0 AND supplier_product_scope_mode_snapshot IN (1, 2) AND procurement_settings_version_snapshot > 0 AND vehicle_selection_mode_snapshot IN (1, 2) AND destination_location_version_snapshot > 0 AND weight_policy_version_snapshot > 0 AND weight_decimal_places_snapshot BETWEEN 1 AND 3 AND weight_processing_method_snapshot IN (0, 1, 2) AND ((vehicle_selection_mode_snapshot = 2 AND receiving_vehicle_id IS NULL AND receiving_vehicle_version_snapshot IS NULL AND vehicle_code_snapshot IS NULL AND vehicle_registration_snapshot IS NULL AND vehicle_display_name_snapshot IS NULL) OR (vehicle_selection_mode_snapshot = 1 AND ((receiving_vehicle_id IS NULL AND receiving_vehicle_version_snapshot IS NULL AND vehicle_code_snapshot IS NULL AND vehicle_registration_snapshot IS NULL AND vehicle_display_name_snapshot IS NULL) OR (receiving_vehicle_id IS NOT NULL AND receiving_vehicle_version_snapshot > 0 AND vehicle_code_snapshot IS NOT NULL AND vehicle_registration_snapshot IS NOT NULL))))");
            t.HasCheckConstraint("ck_commercial_receiving_sessions_vehicle_shape", "(receiving_vehicle_id IS NULL AND receiving_vehicle_version_snapshot IS NULL AND vehicle_code_snapshot IS NULL AND vehicle_registration_snapshot IS NULL) OR (receiving_vehicle_id IS NOT NULL AND receiving_vehicle_version_snapshot > 0 AND vehicle_code_snapshot IS NOT NULL AND vehicle_registration_snapshot IS NOT NULL)");
        });
        b.HasKey(x => x.Id).HasName("pk_commercial_receiving_sessions");
        CommercialReceivingReferencePolicyConfiguration.MapId(b.Property(x => x.Id), "id");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.BranchId).HasColumnName("branch_id");
        b.Property(x => x.CloudReference).HasColumnName("cloud_reference").HasMaxLength(100);
        b.Property(x => x.CloudReferenceSequence).HasColumnName("cloud_reference_sequence");
        b.Property(x => x.ReferenceReservationId).HasColumnName("reference_reservation_id");
        b.Property(x => x.ReferencePolicyVersionSnapshot).HasColumnName("reference_policy_version_snapshot");
        b.Property(x => x.ExternalReference).HasColumnName("external_reference").HasMaxLength(100);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<short>();
        b.Property(x => x.OwnershipGeneration).HasColumnName("ownership_generation");
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.SupplierVersionSnapshot).HasColumnName("supplier_version_snapshot");
        b.Property(x => x.SupplierCodeSnapshot).HasColumnName("supplier_code_snapshot").HasMaxLength(32);
        b.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(200);
        b.Property(x => x.SupplierProductScopeModeSnapshot).HasColumnName("supplier_product_scope_mode_snapshot").HasConversion<short>();
        b.Property(x => x.CompanyProcurementSettingsId).HasColumnName("company_procurement_settings_id");
        b.Property(x => x.ProcurementSettingsVersionSnapshot).HasColumnName("procurement_settings_version_snapshot");
        b.Property(x => x.VehicleSelectionModeSnapshot).HasColumnName("vehicle_selection_mode_snapshot").HasConversion<short>();
        b.Property(x => x.DestinationLocationId).HasColumnName("destination_location_id");
        b.Property(x => x.DestinationLocationVersionSnapshot).HasColumnName("destination_location_version_snapshot");
        b.Property(x => x.DestinationLocationCodeSnapshot).HasColumnName("destination_location_code_snapshot").HasMaxLength(32);
        b.Property(x => x.DestinationLocationNameSnapshot).HasColumnName("destination_location_name_snapshot").HasMaxLength(200);
        b.Property(x => x.WeightProcessingPolicyId).HasColumnName("weight_processing_policy_id");
        b.Property(x => x.WeightPolicyVersionSnapshot).HasColumnName("weight_policy_version_snapshot");
        b.Property(x => x.WeightDecimalPlacesSnapshot).HasColumnName("weight_decimal_places_snapshot");
        b.Property(x => x.WeightProcessingMethodSnapshot).HasColumnName("weight_processing_method_snapshot").HasConversion<short>();
        b.Property(x => x.ReceivingVehicleId).HasColumnName("receiving_vehicle_id");
        b.Property(x => x.ReceivingVehicleVersionSnapshot).HasColumnName("receiving_vehicle_version_snapshot");
        b.Property(x => x.VehicleCodeSnapshot).HasColumnName("vehicle_code_snapshot").HasMaxLength(32);
        b.Property(x => x.VehicleRegistrationSnapshot).HasColumnName("vehicle_registration_snapshot").HasMaxLength(50);
        b.Property(x => x.VehicleDisplayNameSnapshot).HasColumnName("vehicle_display_name_snapshot").HasMaxLength(200);
        b.Property(x => x.NextExpectedLocalSequence).HasColumnName("next_expected_local_sequence");
        b.Property(x => x.EntryCount).HasColumnName("entry_count");
        b.Property(x => x.ProcessedTotalWeightKg).HasColumnName("processed_total_weight_kg").HasPrecision(20, 6);
        b.Property(x => x.StartedAtDeviceUtc).HasColumnName("started_at_device_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.StartedAtServerUtc).HasColumnName("started_at_server_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.SubmittedAtUtc).HasColumnName("submitted_at_utc").HasColumnType("timestamp with time zone");
        CommercialReceivingReferencePolicyConfiguration.Timestamps(b.Property(x => x.CreatedAtUtc), b.Property(x => x.UpdatedAtUtc));
        CommercialReceivingReferencePolicyConfiguration.Version(b.Property(x => x.Version));
        b.HasAlternateKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).HasName("ak_commercial_receiving_sessions_scope_id");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.CloudReference }).IsUnique().HasDatabaseName("ux_commercial_receiving_sessions_company_reference");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.Status, x.UpdatedAtUtc, x.CloudReference }).HasDatabaseName("ix_commercial_receiving_sessions_company_list");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.BranchId, x.DestinationLocationId }).HasDatabaseName("ix_cr_sessions_destination");
        b.HasOne<Branch>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.BranchId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_sessions_branch");
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.SupplierId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_sessions_supplier");
        b.HasOne<CompanyProcurementSettings>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.CompanyProcurementSettingsId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_sessions_settings");
        b.HasOne<BusinessLocation>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.BranchId, x.DestinationLocationId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.BranchId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_sessions_destination");
        b.HasOne<WeightProcessingPolicy>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.WeightProcessingPolicyId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_sessions_weight_policy");
        b.HasOne<ReceivingVehicle>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.ReceivingVehicleId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_sessions_vehicle");
        b.HasOne<CommercialReceivingReferenceReservation>().WithOne().HasForeignKey<CommercialReceivingSession>(x => new { x.WorkspaceId, x.CompanyId, x.ReferenceReservationId }).HasPrincipalKey<CommercialReceivingReferenceReservation>(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_sessions_reference_reservation");
    }
}

internal sealed class CommercialReceivingOwnershipConfiguration : IEntityTypeConfiguration<CommercialReceivingOwnership>
{
    public void Configure(EntityTypeBuilder<CommercialReceivingOwnership> b)
    {
        b.ToTable("commercial_receiving_ownerships", "procurement", t =>
        {
            t.HasCheckConstraint("ck_commercial_receiving_ownerships_generation", "ownership_generation > 0 AND version > 0");
            t.HasCheckConstraint("ck_commercial_receiving_ownerships_lease_shape", "(lease_id IS NULL AND lease_expires_at_utc IS NULL AND last_heartbeat_at_utc IS NULL) OR (lease_id IS NOT NULL AND lease_expires_at_utc IS NOT NULL AND last_heartbeat_at_utc IS NOT NULL AND lease_expires_at_utc > last_heartbeat_at_utc)");
        });
        b.HasKey(x => x.Id).HasName("pk_commercial_receiving_ownerships");
        CommercialReceivingReferencePolicyConfiguration.MapId(b.Property(x => x.Id), "id");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id"); b.Property(x => x.CompanyId).HasColumnName("company_id"); b.Property(x => x.ReceivingSessionId).HasColumnName("receiving_session_id"); b.Property(x => x.EditorDeviceId).HasColumnName("editor_device_id"); b.Property(x => x.OwnershipGeneration).HasColumnName("ownership_generation"); b.Property(x => x.LeaseId).HasColumnName("lease_id");
        b.Property(x => x.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc").HasColumnType("timestamp with time zone"); b.Property(x => x.LastHeartbeatAtUtc).HasColumnName("last_heartbeat_at_utc").HasColumnType("timestamp with time zone"); b.Property(x => x.LastReacquiredAtUtc).HasColumnName("last_reacquired_at_utc").HasColumnType("timestamp with time zone"); b.Property(x => x.LastTransferredAtUtc).HasColumnName("last_transferred_at_utc").HasColumnType("timestamp with time zone"); b.Property(x => x.LastTransferredByUserId).HasColumnName("last_transferred_by_user_id");
        CommercialReceivingReferencePolicyConfiguration.Timestamps(b.Property(x => x.CreatedAtUtc), b.Property(x => x.UpdatedAtUtc)); CommercialReceivingReferencePolicyConfiguration.Version(b.Property(x => x.Version));
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.ReceivingSessionId }).IsUnique().HasDatabaseName("ux_commercial_receiving_ownerships_session");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.EditorDeviceId, x.ReceivingSessionId }).HasDatabaseName("ix_commercial_receiving_ownerships_editor");
        b.HasOne<CommercialReceivingSession>().WithOne().HasForeignKey<CommercialReceivingOwnership>(x => new { x.WorkspaceId, x.CompanyId, x.ReceivingSessionId }).HasPrincipalKey<CommercialReceivingSession>(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_ownerships_session");
        b.HasOne<Device>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.EditorDeviceId }).HasPrincipalKey(x => new { x.WorkspaceId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_ownerships_editor_device");
    }
}

internal sealed class CommercialReceivingEntryConfiguration : IEntityTypeConfiguration<CommercialReceivingEntry>
{
    public void Configure(EntityTypeBuilder<CommercialReceivingEntry> b)
    {
        b.ToTable("commercial_receiving_entries", "procurement", t =>
        {
            t.HasCheckConstraint("ck_commercial_receiving_entries_physical", "local_sequence > 1 AND bag_count > 0 AND processed_weight_kg > 0 AND decimal_places_snapshot BETWEEN 1 AND 3");
            t.HasCheckConstraint("ck_commercial_receiving_entries_snapshots", "product_version_snapshot > 0 AND product_type_snapshot IN (1, 2, 3, 4, 5) AND supplier_scope_mode_snapshot IN (1, 2) AND ((supplier_scope_mode_snapshot = 1 AND supplier_product_scope_id IS NULL AND supplier_product_scope_version_snapshot IS NULL AND supplier_scope_validation_result_snapshot = 'Unrestricted') OR (supplier_scope_mode_snapshot = 2 AND supplier_product_scope_id IS NOT NULL AND supplier_product_scope_version_snapshot > 0 AND supplier_scope_validation_result_snapshot = 'RestrictedScopeValidated')) AND bag_type_version_snapshot > 0 AND bag_construction_class_snapshot IN (1, 2, 3, 4) AND bag_tare_weight_kg_snapshot >= 0 AND ((product_standard_bag_weight_id IS NULL AND product_standard_bag_weight_version_snapshot IS NULL AND standard_bag_weight_label_snapshot IS NULL AND standard_content_weight_kg_snapshot IS NULL) OR (product_standard_bag_weight_id IS NOT NULL AND product_standard_bag_weight_version_snapshot > 0 AND standard_content_weight_kg_snapshot > 0)) AND processing_method_snapshot IN (0, 1, 2) AND char_length(weight_source) BETWEEN 1 AND 32");
        });
        b.HasKey(x => x.Id).HasName("pk_commercial_receiving_entries"); CommercialReceivingReferencePolicyConfiguration.MapId(b.Property(x => x.Id), "id");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id"); b.Property(x => x.CompanyId).HasColumnName("company_id"); b.Property(x => x.ReceivingSessionId).HasColumnName("receiving_session_id"); CommercialReceivingReferencePolicyConfiguration.MapId(b.Property(x => x.OperationId), "operation_id"); b.Property(x => x.LocalSequence).HasColumnName("local_sequence");
        b.Property(x => x.ProductId).HasColumnName("product_id"); b.Property(x => x.ProductVersionSnapshot).HasColumnName("product_version_snapshot"); b.Property(x => x.ProductCodeSnapshot).HasColumnName("product_code_snapshot").HasMaxLength(32); b.Property(x => x.ProductNameSnapshot).HasColumnName("product_name_snapshot").HasMaxLength(200); b.Property(x => x.ProductTypeSnapshot).HasColumnName("product_type_snapshot").HasConversion<short>(); b.Property(x => x.ProcessingFamilyCodeSnapshot).HasColumnName("processing_family_code_snapshot").HasMaxLength(32);
        b.Property(x => x.SupplierProductScopeId).HasColumnName("supplier_product_scope_id"); b.Property(x => x.SupplierProductScopeVersionSnapshot).HasColumnName("supplier_product_scope_version_snapshot"); b.Property(x => x.SupplierScopeModeSnapshot).HasColumnName("supplier_scope_mode_snapshot").HasConversion<short>(); b.Property(x => x.SupplierScopeValidationResultSnapshot).HasColumnName("supplier_scope_validation_result_snapshot").HasMaxLength(40);
        b.Property(x => x.BagTypeId).HasColumnName("bag_type_id"); b.Property(x => x.BagTypeVersionSnapshot).HasColumnName("bag_type_version_snapshot"); b.Property(x => x.BagTypeCodeSnapshot).HasColumnName("bag_type_code_snapshot").HasMaxLength(32); b.Property(x => x.BagTypeNameSnapshot).HasColumnName("bag_type_name_snapshot").HasMaxLength(200); b.Property(x => x.BagConstructionClassSnapshot).HasColumnName("bag_construction_class_snapshot").HasConversion<short>(); b.Property(x => x.BagTareWeightKgSnapshot).HasColumnName("bag_tare_weight_kg_snapshot").HasPrecision(20, 6); b.Property(x => x.BagReturnableSnapshot).HasColumnName("bag_returnable_snapshot");
        b.Property(x => x.ProductStandardBagWeightId).HasColumnName("product_standard_bag_weight_id"); b.Property(x => x.ProductStandardBagWeightVersionSnapshot).HasColumnName("product_standard_bag_weight_version_snapshot"); b.Property(x => x.StandardBagWeightLabelSnapshot).HasColumnName("standard_bag_weight_label_snapshot").HasMaxLength(100); b.Property(x => x.StandardContentWeightKgSnapshot).HasColumnName("standard_content_weight_kg_snapshot").HasPrecision(20, 6);
        b.Property(x => x.BagCount).HasColumnName("bag_count"); b.Property(x => x.RawWeightKg).HasColumnName("raw_weight_kg").HasMaxLength(32); b.Property(x => x.ProcessedWeightKg).HasColumnName("processed_weight_kg").HasPrecision(20, 6); b.Property(x => x.DisplayWeightKg).HasColumnName("display_weight_kg").HasMaxLength(32); b.Property(x => x.DecimalPlacesSnapshot).HasColumnName("decimal_places_snapshot"); b.Property(x => x.ProcessingMethodSnapshot).HasColumnName("processing_method_snapshot").HasConversion<short>(); b.Property(x => x.WeightSource).HasColumnName("weight_source").HasMaxLength(32); b.Property(x => x.CapturedAtDeviceUtc).HasColumnName("captured_at_device_utc").HasColumnType("timestamp with time zone"); b.Property(x => x.AcceptedAtServerUtc).HasColumnName("accepted_at_server_utc").HasColumnType("timestamp with time zone"); b.Ignore(x => x.CreatedAtUtc);
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.OperationId }).IsUnique().HasDatabaseName("ux_commercial_receiving_entries_company_operation"); b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.ReceivingSessionId, x.LocalSequence }).IsUnique().HasDatabaseName("ux_commercial_receiving_entries_session_sequence");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.ProductStandardBagWeightId }).HasDatabaseName("ix_cr_entries_standard_weight");
        b.HasOne<CommercialReceivingSession>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.ReceivingSessionId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_entries_session");
        b.HasOne<Product>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.ProductId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_entries_product");
        b.HasOne<SupplierProductScope>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.SupplierProductScopeId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_entries_supplier_scope");
        b.HasOne<BagType>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.BagTypeId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_entries_bag_type");
        b.HasOne<ProductStandardBagWeight>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.ProductStandardBagWeightId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_entries_standard_weight");
        foreach (var property in b.Metadata.GetProperties()) property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}

internal sealed class CommercialReceivingOperationClaimConfiguration :
    IEntityTypeConfiguration<CommercialReceivingOperationClaim>
{
    public void Configure(EntityTypeBuilder<CommercialReceivingOperationClaim> b)
    {
        b.ToTable("commercial_receiving_operation_claims", "sync", t =>
        {
            t.HasCheckConstraint("ck_commercial_receiving_operation_claims_scope", "command_scope = 'Procurement.CommercialReceiving.MobileSyncOperation'");
            t.HasCheckConstraint("ck_commercial_receiving_operation_claims_state", "state IN (1, 2, 3, 4)");
            t.HasCheckConstraint("ck_commercial_receiving_operation_claims_shape", "char_length(request_hash) = 64 AND request_hash ~ '^[0-9a-f]{64}$' AND (ownership_generation IS NULL OR ownership_generation > 0) AND ((state IN (1, 4) AND attention_code IS NULL AND attention_message IS NULL) OR (state IN (2, 3) AND attention_code IS NOT NULL AND attention_message IS NOT NULL)) AND ((state = 4 AND completed_at_utc IS NOT NULL) OR (state <> 4 AND completed_at_utc IS NULL)) AND (state NOT IN (3, 4) OR retryable = false)");
            t.HasCheckConstraint("ck_commercial_receiving_operation_claims_uuidv7", "substring(operation_id::text, 15, 1) = '7' AND substring(session_id::text, 15, 1) = '7'");
        });
        b.HasKey(x => new { x.WorkspaceId, x.CompanyId, x.CommandScopeValue, x.OperationId }).HasName("pk_commercial_receiving_operation_claims");
        b.HasAlternateKey(x => new { x.WorkspaceId, x.CompanyId, x.OperationId }).HasName("ak_commercial_receiving_operation_claims_company_operation");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CommandScopeValue).HasColumnName("command_scope").HasMaxLength(100);
        b.Property(x => x.OperationId).HasColumnName("operation_id").ValueGeneratedNever();
        b.Property(x => x.OperationType).HasColumnName("operation_type").HasMaxLength(100);
        b.Property(x => x.SessionId).HasColumnName("session_id");
        b.Property(x => x.DeviceId).HasColumnName("device_id");
        b.Property(x => x.OwnershipGeneration).HasColumnName("ownership_generation");
        b.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
        b.Property(x => x.State).HasColumnName("state").HasConversion<short>();
        b.Property(x => x.AttentionCode).HasColumnName("attention_code").HasMaxLength(200);
        b.Property(x => x.AttentionMessage).HasColumnName("attention_message").HasMaxLength(1000);
        b.Property(x => x.Retryable).HasColumnName("retryable");
        b.Property(x => x.FirstSeenAtUtc).HasColumnName("first_seen_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc").HasColumnType("timestamp with time zone");
        CommercialReceivingReferencePolicyConfiguration.Timestamps(b.Property(x => x.CreatedAtUtc), b.Property(x => x.UpdatedAtUtc));
        b.HasOne<Company>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId }).HasPrincipalKey(x => new { x.WorkspaceId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_operation_claims_company");
        b.HasOne<Device>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.DeviceId }).HasPrincipalKey(x => new { x.WorkspaceId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_operation_claims_device");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.State, x.UpdatedAtUtc }).HasDatabaseName("ix_commercial_receiving_operation_claims_attention");
    }
}

internal sealed class CommercialReceivingReferenceReservationConfiguration :
    IEntityTypeConfiguration<CommercialReceivingReferenceReservation>
{
    public void Configure(EntityTypeBuilder<CommercialReceivingReferenceReservation> b)
    {
        b.ToTable("commercial_receiving_reference_reservations", "procurement", t =>
        {
            t.HasCheckConstraint("ck_commercial_receiving_reference_reservations_shape", "sequence > 0 AND policy_version > 0 AND char_length(request_hash) = 64 AND request_hash ~ '^[0-9a-f]{64}$'");
            t.HasCheckConstraint("ck_commercial_receiving_reference_reservations_uuidv7", "substring(id::text, 15, 1) = '7' AND substring(operation_id::text, 15, 1) = '7' AND substring(session_id::text, 15, 1) = '7'");
        });
        b.HasKey(x => x.Id).HasName("pk_commercial_receiving_reference_reservations");
        CommercialReceivingReferencePolicyConfiguration.MapId(b.Property(x => x.Id), "id");
        b.HasAlternateKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).HasName("ak_commercial_receiving_reference_reservations_scope_id");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.PolicyId).HasColumnName("policy_id");
        b.Property(x => x.PeriodKey).HasColumnName("period_key").HasMaxLength(20);
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.SessionId).HasColumnName("session_id");
        b.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
        b.Property(x => x.PolicyVersion).HasColumnName("policy_version");
        b.Property(x => x.Sequence).HasColumnName("sequence");
        b.Property(x => x.RenderedReference).HasColumnName("rendered_reference").HasMaxLength(100);
        b.Property(x => x.ReservedAtUtc).HasColumnName("reserved_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.ConsumedAtUtc).HasColumnName("consumed_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        b.HasOne<CommercialReceivingReferencePolicy>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.PolicyId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_reference_reservations_policy");
        b.HasOne<CommercialReceivingOperationClaim>().WithMany().HasForeignKey(x => new { x.WorkspaceId, x.CompanyId, x.OperationId }).HasPrincipalKey(x => new { x.WorkspaceId, x.CompanyId, x.OperationId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_commercial_receiving_reference_reservations_claim");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.OperationId }).IsUnique().HasDatabaseName("ux_commercial_receiving_reference_reservations_operation");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.SessionId }).IsUnique().HasDatabaseName("ux_commercial_receiving_reference_reservations_session");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.PolicyId, x.PeriodKey, x.Sequence }).IsUnique().HasDatabaseName("ux_commercial_receiving_reference_reservations_sequence");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.RenderedReference }).IsUnique().HasDatabaseName("ux_commercial_receiving_reference_reservations_reference");
    }
}

internal sealed class CommercialMasterChangeConfiguration : IEntityTypeConfiguration<CommercialMasterChange>
{
    public void Configure(EntityTypeBuilder<CommercialMasterChange> b)
    {
        b.ToTable("commercial_master_changes", "sync", t =>
        {
            t.HasCheckConstraint("ck_commercial_master_changes_shape", "master_version > 0 AND status IN ('Active', 'Inactive')");
            t.HasCheckConstraint("ck_commercial_master_changes_type", "master_type IN ('CompanyProcurementSettings', 'BusinessLocation', 'ReceivingVehicle', 'BagType', 'WeightProcessingPolicy', 'Supplier', 'SupplierProductScope', 'ProductGroup', 'Product', 'ProductStandardBagWeight')");
        });
        b.HasKey(x => x.Sequence).HasName("pk_commercial_master_changes"); b.Property(x => x.Sequence).HasColumnName("sequence").UseIdentityAlwaysColumn().ValueGeneratedOnAdd(); b.Property(x => x.WorkspaceId).HasColumnName("workspace_id"); b.Property(x => x.CompanyId).HasColumnName("company_id"); b.Property(x => x.MasterType).HasColumnName("master_type").HasMaxLength(100); b.Property(x => x.MasterId).HasColumnName("master_id"); b.Property(x => x.MasterVersion).HasColumnName("master_version"); b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20); b.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb"); b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.Sequence }).HasDatabaseName("ix_commercial_master_changes_company_sequence"); b.HasIndex(x => new { x.WorkspaceId, x.CompanyId, x.MasterType, x.MasterId, x.MasterVersion }).IsUnique().HasDatabaseName("ux_commercial_master_changes_version");
        foreach (var property in b.Metadata.GetProperties()) property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
