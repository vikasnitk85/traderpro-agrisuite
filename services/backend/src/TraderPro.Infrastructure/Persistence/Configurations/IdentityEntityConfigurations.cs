using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Platform.Identity;

namespace TraderPro.Infrastructure.Persistence.Configurations;

internal sealed class UserCredentialConfiguration :
    IEntityTypeConfiguration<UserCredential>
{
    public void Configure(EntityTypeBuilder<UserCredential> builder)
    {
        builder.ToTable(
            "user_credentials",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_user_credentials_credential_version",
                    "credential_version > 0");
                table.HasCheckConstraint(
                    "ck_user_credentials_failed_sign_in_count",
                    "failed_sign_in_count >= 0");
                table.HasCheckConstraint(
                    "ck_user_credentials_version",
                    "version > 0");
            });
        IdentityConfiguration.ConfigureKeyAndWorkspace(builder);
        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        builder.Property(entity => entity.NormalizedLogin)
            .HasColumnName("normalized_login")
            .HasMaxLength(LoginNormalizer.MaximumLength)
            .IsRequired();
        builder.Property(entity => entity.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(entity => entity.PasswordChangedAtUtc)
            .HasColumnName("password_changed_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.FailedSignInCount)
            .HasColumnName("failed_sign_in_count")
            .IsRequired();
        builder.Property(entity => entity.LockoutEndUtc)
            .HasColumnName("lockout_end_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.CredentialVersion)
            .HasColumnName("credential_version")
            .IsRequired();
        IdentityConfiguration.ConfigureVersionedTimestamps(builder);
        IdentityConfiguration.ProtectAfterSave(
            builder.Property(entity => entity.WorkspaceId));
        IdentityConfiguration.ProtectAfterSave(
            builder.Property(entity => entity.UserId));
        IdentityConfiguration.ProtectAfterSave(
            builder.Property(entity => entity.NormalizedLogin));
        IdentityConfiguration.ProtectAfterSave(
            builder.Property(entity => entity.CreatedAtUtc));
        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.UserId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_user_credentials_users_workspace_id_user_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.UserId,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_user_credentials_workspace_id_user_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.NormalizedLogin,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_user_credentials_workspace_id_normalized_login");
    }
}

internal sealed class DeviceCredentialConfiguration :
    IEntityTypeConfiguration<DeviceCredential>
{
    public void Configure(EntityTypeBuilder<DeviceCredential> builder)
    {
        builder.ToTable(
            "device_credentials",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_device_credentials_secret_version",
                    "secret_version > 0");
                table.HasCheckConstraint(
                    "ck_device_credentials_version",
                    "version > 0");
            });
        IdentityConfiguration.ConfigureKeyAndWorkspace(builder);
        builder.Property(entity => entity.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();
        builder.Property(entity => entity.DeviceSecretHash)
            .HasColumnName("device_secret_hash")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.SecretVersion)
            .HasColumnName("secret_version")
            .IsRequired();
        builder.Property(entity => entity.ActivatedAtUtc)
            .HasColumnName("activated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.LastUsedAtUtc)
            .HasColumnName("last_used_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.RevokedAtUtc)
            .HasColumnName("revoked_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.ClientInstallationReferenceHash)
            .HasColumnName("client_installation_reference_hash")
            .HasMaxLength(64);
        IdentityConfiguration.ConfigureVersionedTimestamps(builder);
        IdentityConfiguration.ProtectAfterSave(
            builder.Property(entity => entity.WorkspaceId));
        IdentityConfiguration.ProtectAfterSave(
            builder.Property(entity => entity.DeviceId));
        IdentityConfiguration.ProtectAfterSave(
            builder.Property(entity => entity.CreatedAtUtc));
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.DeviceId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_device_credentials_devices_workspace_id_device_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.DeviceId,
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_device_credentials_workspace_id_device_id");
    }
}

internal sealed class DeviceActivationCodeConfiguration :
    IEntityTypeConfiguration<DeviceActivationCode>
{
    public void Configure(EntityTypeBuilder<DeviceActivationCode> builder)
    {
        builder.ToTable(
            "device_activation_codes",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_device_activation_codes_expiry",
                    "expires_at_utc > created_at_utc");
                table.HasCheckConstraint(
                    "ck_device_activation_codes_one_time_state",
                    "used_at_utc IS NULL OR revoked_at_utc IS NULL");
                table.HasCheckConstraint(
                    "ck_device_activation_codes_terminal_timing",
                    """
                    (used_at_utc IS NULL OR (
                        used_at_utc >= created_at_utc
                        AND used_at_utc <= expires_at_utc
                    ))
                    AND (revoked_at_utc IS NULL OR
                        revoked_at_utc >= created_at_utc)
                    """);
                table.HasCheckConstraint(
                    "ck_device_activation_codes_replay_state",
                    """
                    (
                        redemption_idempotency_key_hash IS NULL
                        AND redemption_request_hash IS NULL
                        AND replay_protected_result IS NULL
                        AND replay_allowed_until_utc IS NULL
                    )
                    OR
                    (
                        used_at_utc IS NOT NULL
                        AND redemption_idempotency_key_hash IS NOT NULL
                        AND redemption_request_hash IS NOT NULL
                        AND replay_allowed_until_utc IS NOT NULL
                        AND redemption_idempotency_key_hash ~ '^[0-9a-f]{64}$'
                        AND redemption_request_hash ~ '^[0-9a-f]{64}$'
                        AND replay_allowed_until_utc > used_at_utc
                        AND replay_allowed_until_utc <=
                            used_at_utc + interval '1 day'
                    )
                    """);
                table.HasCheckConstraint(
                    "ck_device_activation_codes_version",
                    "version > 0");
            });
        IdentityConfiguration.ConfigureKeyAndWorkspace(builder);
        builder.Property(entity => entity.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();
        builder.Property(entity => entity.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.UsedAtUtc)
            .HasColumnName("used_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.RevokedAtUtc)
            .HasColumnName("revoked_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.RedemptionIdempotencyKeyHash)
            .HasColumnName("redemption_idempotency_key_hash")
            .HasMaxLength(64);
        builder.Property(entity => entity.RedemptionRequestHash)
            .HasColumnName("redemption_request_hash")
            .HasMaxLength(64);
        builder.Property(entity => entity.ReplayProtectedResult)
            .HasColumnName("replay_protected_result")
            .HasMaxLength(4096);
        builder.Property(entity => entity.ReplayAllowedUntilUtc)
            .HasColumnName("replay_allowed_until_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.IssuedByUserId)
            .HasColumnName("issued_by_user_id")
            .IsRequired();
        IdentityConfiguration.ConfigureCreatedVersion(builder);
        foreach (var property in builder.Metadata.GetProperties()
                     .Where(property => property.Name is not
                         nameof(DeviceActivationCode.UsedAtUtc) and not
                         nameof(DeviceActivationCode.RevokedAtUtc) and not
                         nameof(DeviceActivationCode.RedemptionIdempotencyKeyHash) and not
                         nameof(DeviceActivationCode.RedemptionRequestHash) and not
                         nameof(DeviceActivationCode.ReplayProtectedResult) and not
                         nameof(DeviceActivationCode.ReplayAllowedUntilUtc) and not
                         nameof(DeviceActivationCode.Version)))
        {
            property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        }

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.DeviceId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_device_activation_codes_devices_workspace_id_device_id");
        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.IssuedByUserId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_device_activation_codes_users_workspace_id_issued_by");
        builder.HasIndex(entity => entity.CodeHash)
            .IsUnique()
            .HasDatabaseName("ux_device_activation_codes_code_hash");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.RedemptionIdempotencyKeyHash,
        })
            .IsUnique()
            .HasFilter("redemption_idempotency_key_hash IS NOT NULL")
            .HasDatabaseName(
                "ux_device_activation_codes_workspace_redemption_key");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.DeviceId,
            entity.ExpiresAtUtc,
        })
            .HasDatabaseName(
                "ix_device_activation_codes_workspace_device_expiry");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.DeviceId,
        })
            .IsUnique()
            .HasFilter(
                "used_at_utc IS NULL AND revoked_at_utc IS NULL")
            .HasDatabaseName(
                "ux_device_activation_codes_workspace_device_active");
    }
}

internal sealed class RefreshTokenFamilyConfiguration :
    IEntityTypeConfiguration<RefreshTokenFamily>
{
    public void Configure(EntityTypeBuilder<RefreshTokenFamily> builder)
    {
        builder.ToTable(
            "refresh_token_families",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_refresh_token_families_versions",
                    """
                    user_credential_version > 0
                    AND device_secret_version > 0
                    AND version > 0
                    """);
                table.HasCheckConstraint(
                    "ck_refresh_token_families_expiry",
                    "expires_at_utc > created_at_utc");
                table.HasCheckConstraint(
                    "ck_refresh_token_families_revocation",
                    """
                    (revoked_at_utc IS NULL AND revocation_reason IS NULL)
                    OR (revoked_at_utc IS NOT NULL AND revocation_reason IS NOT NULL)
                    """);
            });
        IdentityConfiguration.ConfigureKeyAndWorkspace(builder);
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.Id,
        })
            .HasName("ak_refresh_token_families_workspace_id_id");
        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        builder.Property(entity => entity.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();
        builder.Property(entity => entity.UserCredentialVersion)
            .HasColumnName("user_credential_version")
            .IsRequired();
        builder.Property(entity => entity.DeviceSecretVersion)
            .HasColumnName("device_secret_version")
            .IsRequired();
        builder.Property(entity => entity.LastUsedAtUtc)
            .HasColumnName("last_used_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.RevokedAtUtc)
            .HasColumnName("revoked_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasMaxLength(200);
        IdentityConfiguration.ConfigureCreatedVersion(builder);
        foreach (var propertyName in new[]
                 {
                     nameof(RefreshTokenFamily.WorkspaceId),
                     nameof(RefreshTokenFamily.UserId),
                     nameof(RefreshTokenFamily.DeviceId),
                     nameof(RefreshTokenFamily.UserCredentialVersion),
                     nameof(RefreshTokenFamily.DeviceSecretVersion),
                     nameof(RefreshTokenFamily.CreatedAtUtc),
                     nameof(RefreshTokenFamily.ExpiresAtUtc),
                 })
        {
            builder.Property(propertyName).Metadata.SetAfterSaveBehavior(
                PropertySaveBehavior.Throw);
        }

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.UserId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_refresh_token_families_users_workspace_id_user_id");
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.DeviceId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_refresh_token_families_devices_workspace_id_device_id");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.UserId,
            entity.RevokedAtUtc,
            entity.ExpiresAtUtc,
        })
            .HasDatabaseName(
                "ix_refresh_token_families_workspace_user_active");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.DeviceId,
            entity.RevokedAtUtc,
            entity.ExpiresAtUtc,
        })
            .HasDatabaseName(
                "ix_refresh_token_families_workspace_device_active");
    }
}

internal sealed class RefreshTokenConfiguration :
    IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(
            "refresh_tokens",
            "platform",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_refresh_tokens_expiry",
                    "expires_at_utc > created_at_utc");
                table.HasCheckConstraint(
                    "ck_refresh_tokens_version",
                    "version > 0");
                table.HasCheckConstraint(
                    "ck_refresh_tokens_rotation_state",
                    """
                    (
                        consumed_at_utc IS NULL
                        AND rotated_to_token_id IS NULL
                        AND replay_protected_token IS NULL
                        AND replay_allowed_until_utc IS NULL
                    )
                    OR
                    (
                        consumed_at_utc IS NOT NULL
                        AND rotated_to_token_id IS NOT NULL
                        AND consumed_at_utc >= created_at_utc
                        AND consumed_at_utc <= expires_at_utc
                        AND (
                            (
                                replay_protected_token IS NOT NULL
                                AND replay_allowed_until_utc >
                                    consumed_at_utc
                                AND replay_allowed_until_utc <=
                                    expires_at_utc
                            )
                            OR
                            (
                                replay_protected_token IS NULL
                                AND replay_allowed_until_utc IS NULL
                            )
                        )
                    )
                    """);
                table.HasCheckConstraint(
                    "ck_refresh_tokens_no_self_rotation",
                    "rotated_to_token_id IS NULL OR rotated_to_token_id <> id");
            });
        IdentityConfiguration.ConfigureKeyAndWorkspace(builder);
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.Id,
        })
            .HasName("ak_refresh_tokens_workspace_id_id");
        builder.HasAlternateKey(entity => new
        {
            entity.WorkspaceId,
            entity.FamilyId,
            entity.Id,
        })
            .HasName(
                "ak_refresh_tokens_workspace_family_id");
        builder.Property(entity => entity.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();
        builder.Property(entity => entity.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entity => entity.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(entity => entity.ConsumedAtUtc)
            .HasColumnName("consumed_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.RevokedAtUtc)
            .HasColumnName("revoked_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.RotatedToTokenId)
            .HasColumnName("rotated_to_token_id");
        builder.Property(entity => entity.ReplayProtectedToken)
            .HasColumnName("replay_protected_token")
            .HasColumnType("text");
        builder.Property(entity => entity.ReplayAllowedUntilUtc)
            .HasColumnName("replay_allowed_until_utc")
            .HasColumnType("timestamp with time zone");
        IdentityConfiguration.ConfigureCreatedVersion(builder);
        foreach (var propertyName in new[]
                 {
                     nameof(RefreshToken.WorkspaceId),
                     nameof(RefreshToken.FamilyId),
                     nameof(RefreshToken.TokenHash),
                     nameof(RefreshToken.CreatedAtUtc),
                     nameof(RefreshToken.ExpiresAtUtc),
                 })
        {
            builder.Property(propertyName).Metadata.SetAfterSaveBehavior(
                PropertySaveBehavior.Throw);
        }

        builder.HasOne<RefreshTokenFamily>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.FamilyId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_refresh_tokens_families_workspace_id_family_id");
        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(entity => new
            {
                entity.WorkspaceId,
                entity.FamilyId,
                entity.RotatedToTokenId,
            })
            .HasPrincipalKey(entity => new
            {
                entity.WorkspaceId,
                entity.FamilyId,
                entity.Id,
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_refresh_tokens_rotated_to_workspace_family_token");
        builder.HasIndex(entity => entity.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_tokens_token_hash");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.FamilyId,
            entity.CreatedAtUtc,
        })
            .HasDatabaseName(
                "ix_refresh_tokens_workspace_family_created");
        builder.HasIndex(entity => new
        {
            entity.WorkspaceId,
            entity.FamilyId,
            entity.RotatedToTokenId,
        })
            .IsUnique()
            .HasFilter("rotated_to_token_id IS NOT NULL")
            .HasDatabaseName(
                "ux_refresh_tokens_workspace_family_replacement");
    }
}

internal static class IdentityConfiguration
{
    public static void ConfigureKeyAndWorkspace<TEntity>(
        EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.HasKey("Id").HasName(
            $"pk_{builder.Metadata.GetTableName()}");
        builder.Property<Guid>("Id")
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property<Guid>("WorkspaceId")
            .HasColumnName("workspace_id")
            .IsRequired();
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

    public static void ConfigureCreatedVersion<TEntity>(
        EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<DateTimeOffset>("CreatedAtUtc")
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property<long>("Version")
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();
    }

    public static void ProtectAfterSave<TProperty>(
        PropertyBuilder<TProperty> property)
    {
        property.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
