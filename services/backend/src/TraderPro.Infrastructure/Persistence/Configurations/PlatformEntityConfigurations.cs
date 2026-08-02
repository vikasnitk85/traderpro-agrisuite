using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderPro.Domain.Platform;

namespace TraderPro.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceConfiguration :
    IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable(
            "workspaces",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_workspaces_status",
                    "status IN (1, 2, 3)");
                table.HasCheckConstraint(
                    "ck_workspaces_commercial_code",
                    """
                    workspace_code = normalized_workspace_code
                    AND normalized_workspace_code ~
                        '^[A-Z0-9]+(-[A-Z0-9]+)*$'
                    AND char_length(normalized_workspace_code)
                        BETWEEN 3 AND 64
                    """);
            });
        builder.HasKey(entity => entity.Id).HasName("pk_workspaces");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.WorkspaceCode)
            .HasColumnName("workspace_code")
            .HasMaxLength(64)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.NormalizedWorkspaceCode)
            .HasColumnName("normalized_workspace_code")
            .HasMaxLength(64)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        ConfigureVersionedTimestamps(builder);
        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasDatabaseName("ux_workspaces_code");
        builder.HasIndex(entity => entity.NormalizedWorkspaceCode)
            .IsUnique()
            .HasDatabaseName("ux_workspaces_normalized_workspace_code");
    }

    private static void ConfigureVersionedTimestamps(
        EntityTypeBuilder<Workspace> builder)
    {
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
    }
}

internal sealed class CompanyConfiguration :
    IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable(
            "companies",
            "platform",
            table => table.HasCheckConstraint(
                "ck_companies_status",
                "status IN (1, 2, 3)"));
        builder.HasKey(entity => entity.Id).HasName("pk_companies");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.Id,
        })
            .HasName("ak_companies_workspace_id_id");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.TradeName)
            .HasColumnName("trade_name")
            .HasMaxLength(200);
        builder.Property(entity => entity.TaxRegistrationNumber)
            .HasColumnName("tax_registration_number")
            .HasMaxLength(64);
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        ConfigureVersionedTimestamps(builder);
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_companies_workspaces_workspace_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.Code,
        })
            .IsUnique()
            .HasDatabaseName("ux_companies_workspace_id_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.TaxRegistrationNumber,
        })
            .IsUnique()
            .HasFilter("tax_registration_number IS NOT NULL")
            .HasDatabaseName(
                "ux_companies_workspace_id_tax_registration_number");
    }

    private static void ConfigureVersionedTimestamps(
        EntityTypeBuilder<Company> builder)
    {
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
    }
}

internal sealed class BranchConfiguration :
    IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable(
            "branches",
            "platform",
            table => table.HasCheckConstraint(
                "ck_branches_status",
                "status IN (1, 2, 3)"));
        builder.HasKey(entity => entity.Id).HasName("pk_branches");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Id,
        })
            .HasName("ak_branches_workspace_id_company_id_id");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();
        builder.Property(entity => entity.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        ConfigureVersionedTimestamps(builder);
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
                "fk_branches_companies_workspace_id_company_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Code,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_branches_workspace_id_company_id_code");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
        })
            .IsUnique()
            .HasFilter("is_default")
            .HasDatabaseName(
                "ux_branches_workspace_id_company_id_default");
    }

    private static void ConfigureVersionedTimestamps(
        EntityTypeBuilder<Branch> builder)
    {
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
    }
}

internal sealed class PlatformUserConfiguration :
    IEntityTypeConfiguration<PlatformUser>
{
    public void Configure(EntityTypeBuilder<PlatformUser> builder)
    {
        builder.ToTable(
            "users",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_users_status",
                    "status IN (1, 2)");
                table.HasCheckConstraint(
                    "ck_users_role",
                    "role IN (1, 2)");
            });
        builder.HasKey(entity => entity.Id).HasName("pk_users");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.Id,
        })
            .HasName("ak_users_workspace_id_id");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entity => entity.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.Role)
            .HasColumnName("role")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        ConfigureVersionedTimestamps(builder);
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_users_workspaces_workspace_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.Username,
        })
            .IsUnique()
            .HasDatabaseName("ux_users_workspace_id_username");
    }

    private static void ConfigureVersionedTimestamps(
        EntityTypeBuilder<PlatformUser> builder)
    {
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
    }
}

internal sealed class DeviceConfiguration :
    IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable(
            "devices",
            "platform",
            table => table.HasCheckConstraint(
                "ck_devices_status",
                "status IN (1, 2)"));
        builder.HasKey(entity => entity.Id).HasName("pk_devices");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.Id,
        })
            .HasName("ak_devices_workspace_id_id");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.Platform)
            .HasColumnName("platform")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.LastSeenAtUtc)
            .HasColumnName("last_seen_at_utc")
            .HasColumnType("timestamp with time zone");
        ConfigureVersionedTimestamps(builder);
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_devices_workspaces_workspace_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.InstallationId,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_devices_workspace_id_installation_id");
    }

    private static void ConfigureVersionedTimestamps(
        EntityTypeBuilder<Device> builder)
    {
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
    }
}

internal sealed class IdempotencyRecordConfiguration :
    IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable(
            "idempotency_records",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_idempotency_records_status",
                    "status IN (1, 2, 3)");
                table.HasCheckConstraint(
                    "ck_idempotency_records_result_status_code",
                    """
                    (status = 2
                        AND result_payload_json IS NOT NULL
                        AND result_status_code BETWEEN 100 AND 599)
                    OR (status <> 2 AND result_status_code IS NULL)
                    """);
            });
        builder.HasKey(entity => entity.Id)
            .HasName("pk_idempotency_records");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.CommandType)
            .HasColumnName("command_type")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.RequestHash)
            .HasColumnName("request_hash")
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.ResultPayloadJson)
            .HasColumnName("result_payload_json")
            .HasColumnType("jsonb");
        builder.Property(entity => entity.ResultStatusCode)
            .HasColumnName("result_status_code");
        builder.Property(entity => entity.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.CompletedAtUtc)
            .HasColumnName("completed_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_idempotency_records_workspaces_workspace_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CommandType,
            entity.IdempotencyKey,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_idempotency_records_workspace_command_key");
    }
}

internal sealed class OutboxMessageConfiguration :
    IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(
            "outbox_messages",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_outbox_messages_status",
                    "status IN (1, 2, 3, 4)");
                table.HasCheckConstraint(
                    "ck_outbox_messages_event_stream",
                    "event_stream IN (1, 2, 3)");
                table.HasCheckConstraint(
                    "ck_outbox_messages_event_version",
                    "event_version > 0");
                table.HasCheckConstraint(
                    "ck_outbox_messages_aggregate_version",
                    "aggregate_version > 0");
                table.HasCheckConstraint(
                    "ck_outbox_messages_attempt_count",
                    "attempt_count >= 0");
            });
        builder.HasKey(entity => entity.Id)
            .HasName("pk_outbox_messages");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.Sequence)
            .HasColumnName("sequence")
            .UseIdentityAlwaysColumn()
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.EventStream)
            .HasColumnName("event_stream")
            .HasConversion<short>()
            .HasDefaultValue(OutboxEventStream.Internal)
            .IsRequired();
        builder.Property(entity => entity.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.EventVersion)
            .HasColumnName("event_version")
            .IsRequired();
        builder.Property(entity => entity.AggregateType)
            .HasColumnName("aggregate_type")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();
        builder.Property(entity => entity.AggregateVersion)
            .HasColumnName("aggregate_version")
            .IsRequired();
        builder.Property(entity => entity.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(entity => entity.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entity => entity.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        ProtectAfterSave(builder.Property(entity => entity.Sequence));
        ProtectAfterSave(builder.Property(entity => entity.WorkspaceId));
        ProtectAfterSave(builder.Property(entity => entity.EventStream));
        ProtectAfterSave(builder.Property(entity => entity.EventType));
        ProtectAfterSave(builder.Property(entity => entity.EventVersion));
        ProtectAfterSave(builder.Property(entity => entity.AggregateType));
        ProtectAfterSave(builder.Property(entity => entity.AggregateId));
        ProtectAfterSave(builder.Property(entity => entity.AggregateVersion));
        ProtectAfterSave(builder.Property(entity => entity.PayloadJson));
        ProtectAfterSave(builder.Property(entity => entity.CorrelationId));
        ProtectAfterSave(builder.Property(entity => entity.OccurredAtUtc));
        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();
        builder.Property(entity => entity.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.ProcessedAtUtc)
            .HasColumnName("processed_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(4000);
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_outbox_messages_workspaces_workspace_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.Status,
            entity.NextAttemptAtUtc,
        })
            .HasDatabaseName(
                "ix_outbox_messages_workspace_status_next_attempt");
        builder.HasIndex(entity => entity.Sequence)
            .IsUnique()
            .HasDatabaseName("ux_outbox_messages_sequence");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.EventStream,
            entity.Sequence,
        })
            .HasDatabaseName(
                "ix_outbox_messages_workspace_id_event_stream_sequence");
    }

    private static void ProtectAfterSave<TProperty>(
        PropertyBuilder<TProperty> property)
    {
        property.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}

internal sealed class CommercialOutboxAudienceConfiguration :
    IEntityTypeConfiguration<CommercialOutboxAudience>
{
    public void Configure(EntityTypeBuilder<CommercialOutboxAudience> builder)
    {
        builder.ToTable(
            "commercial_outbox_audiences",
            "platform",
            table => table.HasCheckConstraint(
                "ck_commercial_outbox_audiences_shape",
                "audience IN (1, 2) AND ((audience = 1 AND target_device_id IS NULL) OR (audience = 2 AND target_device_id IS NOT NULL))"));
        builder.HasKey(entity => entity.OutboxMessageId)
            .HasName("pk_commercial_outbox_audiences");
        builder.Property(entity => entity.OutboxMessageId)
            .HasColumnName("outbox_message_id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();
        builder.Property(entity => entity.Audience)
            .HasColumnName("audience")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(entity => entity.TargetDeviceId)
            .HasColumnName("target_device_id");
        builder.HasOne<OutboxMessage>()
            .WithOne()
            .HasForeignKey<CommercialOutboxAudience>(entity => entity.OutboxMessageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commercial_outbox_audiences_message");
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => new { entity.WorkspaceId, entity.CompanyId })
            .HasPrincipalKey(entity => new { entity.WorkspaceId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commercial_outbox_audiences_company");
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new { entity.WorkspaceId, entity.TargetDeviceId })
            .HasPrincipalKey(entity => new { entity.WorkspaceId, entity.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commercial_outbox_audiences_target_device");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.CompanyId,
            entity.Audience,
            entity.TargetDeviceId,
            entity.OutboxMessageId,
        }).HasDatabaseName("ix_commercial_outbox_audiences_cursor");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.TargetDeviceId,
        }).HasDatabaseName(
            "ix_commercial_outbox_audiences_workspace_target_device");
        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        }
    }
}

internal sealed class CommandProbeConfiguration :
    IEntityTypeConfiguration<CommandProbe>
{
    public void Configure(EntityTypeBuilder<CommandProbe> builder)
    {
        builder.ToTable(
            "command_probes",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_command_probes_counter",
                    "counter >= 0");
                table.HasCheckConstraint(
                    "ck_command_probes_version",
                    "version > 0");
            });
        builder.HasKey(entity => entity.Id)
            .HasName("pk_command_probes");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.Id,
        })
            .HasName("ak_command_probes_workspace_id_id");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(CommandProbe.MaximumNameLength)
            .IsRequired();
        builder.Property(entity => entity.Counter)
            .HasColumnName("counter")
            .IsRequired();
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
                "fk_command_probes_workspaces_workspace_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.Name,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_command_probes_workspace_id_name");
    }
}

internal sealed class AuditEventConfiguration :
    IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable(
            "audit_events",
            "platform",
            table => table.HasCheckConstraint(
                "ck_audit_events_branch_requires_company",
                "branch_id IS NULL OR company_id IS NOT NULL"));
        builder.HasKey(entity => entity.Id).HasName("pk_audit_events");
        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(entity => entity.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();
        builder.Property(entity => entity.CompanyId)
            .HasColumnName("company_id");
        builder.Property(entity => entity.BranchId)
            .HasColumnName("branch_id");
        builder.Property(entity => entity.ActorUserId)
            .HasColumnName("actor_user_id");
        builder.Property(entity => entity.ActorDeviceId)
            .HasColumnName("actor_device_id");
        builder.Property(entity => entity.Action)
            .HasColumnName("action")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.AggregateType)
            .HasColumnName("aggregate_type")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(entity => entity.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();
        builder.Property(entity => entity.PermissionKey)
            .HasColumnName("permission_key")
            .HasMaxLength(200);
        builder.Property(entity => entity.Reason)
            .HasColumnName("reason")
            .HasMaxLength(2000);
        builder.Property(entity => entity.BeforeSnapshotJson)
            .HasColumnName("before_snapshot_json")
            .HasColumnType("jsonb");
        builder.Property(entity => entity.AfterSnapshotJson)
            .HasColumnName("after_snapshot_json")
            .HasColumnType("jsonb");
        builder.Property(entity => entity.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entity => entity.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_audit_events_workspaces_workspace_id");
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
                "fk_audit_events_companies_workspace_id_company_id");
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
                "fk_audit_events_branches_workspace_company_branch");
        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.ActorUserId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_audit_events_users_workspace_id_actor_user_id");
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.ActorDeviceId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_audit_events_devices_workspace_id_actor_device_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.OccurredAtUtc,
        })
            .HasDatabaseName(
                "ix_audit_events_workspace_id_occurred_at_utc");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.AggregateType,
            entity.AggregateId,
            entity.OccurredAtUtc,
        })
            .HasDatabaseName(
                "ix_audit_events_workspace_aggregate_history");
    }
}
