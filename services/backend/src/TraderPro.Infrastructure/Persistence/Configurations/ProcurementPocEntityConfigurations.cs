using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.Poc;

namespace TraderPro.Infrastructure.Persistence.Configurations;

internal sealed class ReceivingSessionPocConfiguration :
    IEntityTypeConfiguration<ReceivingSessionPoc>
{
    public void Configure(EntityTypeBuilder<ReceivingSessionPoc> builder)
    {
        builder.ToTable(
            "receiving_session_pocs",
            "procurement",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_receiving_session_pocs_status",
                    "status IN (1, 2, 3, 4)");
                table.HasCheckConstraint(
                    "ck_receiving_session_pocs_reference_sequence",
                    "cloud_reference_sequence > 0");
                table.HasCheckConstraint(
                    "ck_receiving_session_pocs_next_sequence",
                    "next_expected_local_sequence > 0");
                table.HasCheckConstraint(
                    "ck_receiving_session_pocs_entry_count",
                    "entry_count >= 0");
                table.HasCheckConstraint(
                    "ck_receiving_session_pocs_total",
                    "processed_total_weight_kg >= 0");
                table.HasCheckConstraint(
                    "ck_receiving_session_pocs_version",
                    "version > 0");
                table.HasCheckConstraint(
                    "ck_receiving_session_pocs_state_shape",
                    """
                    (status = 1
                        AND lease_id IS NOT NULL
                        AND lease_expires_at_utc IS NOT NULL
                        AND last_lease_heartbeat_at_utc IS NOT NULL
                        AND submitted_at_utc IS NULL
                        AND approved_at_utc IS NULL
                        AND approved_by_device_id IS NULL)
                    OR (status = 2
                        AND lease_id IS NULL
                        AND lease_expires_at_utc IS NULL
                        AND last_lease_heartbeat_at_utc IS NULL
                        AND submitted_at_utc IS NOT NULL
                        AND approved_at_utc IS NULL
                        AND approved_by_device_id IS NULL)
                    OR (status IN (3, 4)
                        AND lease_id IS NULL
                        AND lease_expires_at_utc IS NULL
                        AND last_lease_heartbeat_at_utc IS NULL
                        AND submitted_at_utc IS NOT NULL
                        AND approved_at_utc IS NOT NULL
                        AND approved_by_device_id IS NOT NULL)
                    """);
                table.HasTrigger(
                    "tr_receiving_session_pocs_identity_immutable");
                table.HasTrigger(
                    "tr_receiving_session_pocs_delete_immutable");
            });
        builder.HasKey(entity => entity.Id)
            .HasName("pk_receiving_session_pocs");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.Id,
        })
            .HasName("ak_receiving_session_pocs_workspace_id_id");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.CloudReferenceSequence)
            .HasColumnName("cloud_reference_sequence")
            .UseIdentityAlwaysColumn()
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Ignore(entity => entity.CloudReference);
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.EditorDeviceId)
            .HasColumnName("editor_device_id")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.LeaseId)
            .HasColumnName("lease_id");
        builder.Property(entity => entity.LeaseExpiresAtUtc)
            .HasColumnName("lease_expires_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.LastLeaseHeartbeatAtUtc)
            .HasColumnName("last_lease_heartbeat_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.NextExpectedLocalSequence)
            .HasColumnName("next_expected_local_sequence")
            .IsRequired();
        builder.Property(entity => entity.EntryCount)
            .HasColumnName("entry_count")
            .IsRequired();
        builder.Property(entity => entity.ProcessedTotalWeightKg)
            .HasColumnName("processed_total_weight_kg")
            .HasPrecision(20, 6)
            .IsRequired();
        builder.Property(entity => entity.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.ApprovedAtUtc)
            .HasColumnName("approved_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.ApprovedByDeviceId)
            .HasColumnName("approved_by_device_id");
        builder.Property(entity => entity.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_receiving_session_pocs_workspaces_workspace_id");
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.EditorDeviceId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_receiving_session_pocs_devices_workspace_editor");
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.ApprovedByDeviceId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_receiving_session_pocs_devices_workspace_approver");
        builder.HasIndex(entity => entity.CloudReferenceSequence)
            .IsUnique()
            .HasDatabaseName(
                "ux_receiving_session_pocs_cloud_reference_sequence");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.Status,
            entity.CloudReferenceSequence,
        })
            .HasDatabaseName(
                "ix_receiving_session_pocs_workspace_status_reference");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.UpdatedAtUtc,
        })
            .HasDatabaseName(
                "ix_receiving_session_pocs_workspace_updated_at");
    }
}

internal sealed class ReceivingEntryPocConfiguration :
    IEntityTypeConfiguration<ReceivingEntryPoc>
{
    public void Configure(EntityTypeBuilder<ReceivingEntryPoc> builder)
    {
        builder.ToTable(
            "receiving_entry_pocs",
            "procurement",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_receiving_entry_pocs_local_sequence",
                    "local_sequence > 0");
                table.HasCheckConstraint(
                    "ck_receiving_entry_pocs_bag_count",
                    "bag_count > 0");
                table.HasCheckConstraint(
                    "ck_receiving_entry_pocs_processed_weight",
                    "processed_weight_kg >= 0");
                table.HasCheckConstraint(
                    "ck_receiving_entry_pocs_display_weight",
                    "display_weight_kg >= 0");
                table.HasCheckConstraint(
                    "ck_receiving_entry_pocs_decimal_places",
                    "decimal_places IN (1, 2, 3)");
                table.HasCheckConstraint(
                    "ck_receiving_entry_pocs_processing_method",
                    "processing_method IN ('Standard', 'Floor', 'Ceiling')");
                table.HasCheckConstraint(
                    "ck_receiving_entry_pocs_weight_source",
                    "weight_source IN ('ManualSpike', 'TestScale')");
            });
        builder.HasKey(entity => entity.Id)
            .HasName("pk_receiving_entry_pocs");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.Id,
        })
            .HasName("ak_receiving_entry_pocs_workspace_id_id");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.ReceivingSessionId)
            .HasColumnName("receiving_session_id")
            .IsRequired();
        builder.Property(entity => entity.OperationId)
            .HasColumnName("operation_id")
            .IsRequired();
        builder.Property(entity => entity.LocalSequence)
            .HasColumnName("local_sequence")
            .IsRequired();
        builder.Property(entity => entity.ProductReference)
            .HasColumnName("product_reference")
            .HasMaxLength(ReceivingEntryPoc.MaximumProductReferenceLength)
            .IsRequired();
        builder.Property(entity => entity.BagTypeReference)
            .HasColumnName("bag_type_reference")
            .HasMaxLength(ReceivingEntryPoc.MaximumBagTypeReferenceLength)
            .IsRequired();
        builder.Property(entity => entity.BagCount)
            .HasColumnName("bag_count")
            .IsRequired();
        builder.Property(entity => entity.RawWeightKg)
            .HasColumnName("raw_weight_kg")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(entity => entity.ProcessedWeightKg)
            .HasColumnName("processed_weight_kg")
            .HasPrecision(20, 6)
            .IsRequired();
        builder.Property(entity => entity.DisplayWeightKg)
            .HasColumnName("display_weight_kg")
            .HasPrecision(20, 3)
            .IsRequired();
        builder.Property(entity => entity.DecimalPlaces)
            .HasColumnName("decimal_places")
            .IsRequired();
        builder.Property(entity => entity.ProcessingMethod)
            .HasColumnName("processing_method")
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(entity => entity.WeightSource)
            .HasColumnName("weight_source")
            .HasMaxLength(ReceivingEntryPoc.MaximumWeightSourceLength)
            .IsRequired();
        builder.Property(entity => entity.CapturedAtDeviceUtc)
            .HasColumnName("captured_at_device_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.AcceptedAtServerUtc)
            .HasColumnName("accepted_at_server_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        ProtectAllProperties(builder);
        builder.HasOne<ReceivingSessionPoc>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.ReceivingSessionId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_receiving_entry_pocs_sessions_workspace_session");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.OperationId,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_receiving_entry_pocs_workspace_operation");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.ReceivingSessionId,
            entity.LocalSequence,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_receiving_entry_pocs_workspace_session_sequence");
    }

    private static void ProtectAllProperties(
        EntityTypeBuilder<ReceivingEntryPoc> builder)
    {
        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        }
    }
}

internal sealed class ReceivingFinalizationPocConfiguration :
    IEntityTypeConfiguration<ReceivingFinalizationPoc>
{
    public void Configure(EntityTypeBuilder<ReceivingFinalizationPoc> builder)
    {
        builder.ToTable(
            "receiving_finalization_pocs",
            "procurement",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_receiving_finalization_pocs_entry_count",
                    "final_entry_count > 0");
                table.HasCheckConstraint(
                    "ck_receiving_finalization_pocs_total",
                    "final_processed_total_weight_kg >= 0");
                table.HasCheckConstraint(
                    "ck_receiving_finalization_pocs_version",
                    "version > 0");
            });
        builder.HasKey(entity => entity.Id)
            .HasName("pk_receiving_finalization_pocs");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.ReceivingSessionId)
            .HasColumnName("receiving_session_id")
            .IsRequired();
        builder.Property(entity => entity.FinalEntryCount)
            .HasColumnName("final_entry_count")
            .IsRequired();
        builder.Property(entity => entity.FinalProcessedTotalWeightKg)
            .HasColumnName("final_processed_total_weight_kg")
            .HasPrecision(20, 6)
            .IsRequired();
        builder.Property(entity => entity.ApprovedByDeviceId)
            .HasColumnName("approved_by_device_id")
            .IsRequired();
        builder.Property(entity => entity.FinalizedByDeviceId)
            .HasColumnName("finalized_by_device_id")
            .IsRequired();
        builder.Property(entity => entity.FinalizedAtUtc)
            .HasColumnName("finalized_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.Version)
            .HasColumnName("version")
            .IsRequired();
        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        }

        builder.HasOne<ReceivingSessionPoc>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.ReceivingSessionId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_receiving_finalization_pocs_sessions_workspace_session");
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.ApprovedByDeviceId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_receiving_finalization_pocs_devices_workspace_approver");
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.FinalizedByDeviceId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_receiving_finalization_pocs_devices_workspace_finalizer");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.ReceivingSessionId,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_receiving_finalization_pocs_workspace_session");
    }
}
