using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionIdentityAndDeviceSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_workspace_code",
                schema: "platform",
                table: "workspaces",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "workspace_code",
                schema: "platform",
                table: "workspaces",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "role",
                schema: "platform",
                table: "users",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2);

            migrationBuilder.Sql(
                """
                UPDATE platform.workspaces
                SET workspace_code =
                        'TP-' || upper(replace(id::text, '-', '')),
                    normalized_workspace_code =
                        'TP-' || upper(replace(id::text, '-', ''));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_workspace_code",
                schema: "platform",
                table: "workspaces",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "workspace_code",
                schema: "platform",
                table: "workspaces",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "device_activation_codes",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    issued_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_activation_codes", x => x.id);
                    table.CheckConstraint("ck_device_activation_codes_expiry", "expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_device_activation_codes_one_time_state", "used_at_utc IS NULL OR revoked_at_utc IS NULL");
                    table.CheckConstraint("ck_device_activation_codes_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_device_activation_codes_devices_workspace_id_device_id",
                        columns: x => new { x.workspace_id, x.device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_device_activation_codes_users_workspace_id_issued_by",
                        columns: x => new { x.workspace_id, x.issued_by_user_id },
                        principalSchema: "platform",
                        principalTable: "users",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_credentials",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_secret_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    secret_version = table.Column<int>(type: "integer", nullable: false),
                    activated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    client_installation_reference_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_credentials", x => x.id);
                    table.CheckConstraint("ck_device_credentials_secret_version", "secret_version > 0");
                    table.CheckConstraint("ck_device_credentials_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_device_credentials_devices_workspace_id_device_id",
                        columns: x => new { x.workspace_id, x.device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token_families",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_credential_version = table.Column<int>(type: "integer", nullable: false),
                    device_secret_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token_families", x => x.id);
                    table.UniqueConstraint("ak_refresh_token_families_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_refresh_token_families_expiry", "expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_refresh_token_families_revocation", "(revoked_at_utc IS NULL AND revocation_reason IS NULL)\nOR (revoked_at_utc IS NOT NULL AND revocation_reason IS NOT NULL)");
                    table.CheckConstraint("ck_refresh_token_families_versions", "user_credential_version > 0\nAND device_secret_version > 0\nAND version > 0");
                    table.ForeignKey(
                        name: "fk_refresh_token_families_devices_workspace_id_device_id",
                        columns: x => new { x.workspace_id, x.device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refresh_token_families_users_workspace_id_user_id",
                        columns: x => new { x.workspace_id, x.user_id },
                        principalSchema: "platform",
                        principalTable: "users",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_credentials",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    password_changed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    failed_sign_in_count = table.Column<int>(type: "integer", nullable: false),
                    lockout_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    credential_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_credentials", x => x.id);
                    table.CheckConstraint("ck_user_credentials_credential_version", "credential_version > 0");
                    table.CheckConstraint("ck_user_credentials_failed_sign_in_count", "failed_sign_in_count >= 0");
                    table.CheckConstraint("ck_user_credentials_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_user_credentials_users_workspace_id_user_id",
                        columns: x => new { x.workspace_id, x.user_id },
                        principalSchema: "platform",
                        principalTable: "users",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rotated_to_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    replay_protected_token = table.Column<string>(type: "text", nullable: true),
                    replay_allowed_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.UniqueConstraint("ak_refresh_tokens_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_refresh_tokens_expiry", "expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_refresh_tokens_rotation_state", "(\n    consumed_at_utc IS NULL\n    AND rotated_to_token_id IS NULL\n    AND replay_protected_token IS NULL\n    AND replay_allowed_until_utc IS NULL\n)\nOR\n(\n    consumed_at_utc IS NOT NULL\n    AND rotated_to_token_id IS NOT NULL\n    AND replay_protected_token IS NOT NULL\n    AND replay_allowed_until_utc > consumed_at_utc\n    AND replay_allowed_until_utc <= expires_at_utc\n)");
                    table.CheckConstraint("ck_refresh_tokens_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_refresh_tokens_families_workspace_id_family_id",
                        columns: x => new { x.workspace_id, x.family_id },
                        principalSchema: "platform",
                        principalTable: "refresh_token_families",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_rotated_to_workspace_id_token_id",
                        columns: x => new { x.workspace_id, x.rotated_to_token_id },
                        principalSchema: "platform",
                        principalTable: "refresh_tokens",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_workspaces_normalized_workspace_code",
                schema: "platform",
                table: "workspaces",
                column: "normalized_workspace_code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_workspaces_commercial_code",
                schema: "platform",
                table: "workspaces",
                sql: "workspace_code = normalized_workspace_code\nAND normalized_workspace_code ~\n    '^[A-Z0-9]+(-[A-Z0-9]+)*$'\nAND char_length(normalized_workspace_code)\n    BETWEEN 3 AND 64");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_role",
                schema: "platform",
                table: "users",
                sql: "role IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "ix_device_activation_codes_workspace_device_expiry",
                schema: "platform",
                table: "device_activation_codes",
                columns: new[] { "workspace_id", "device_id", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_device_activation_codes_workspace_id_issued_by_user_id",
                schema: "platform",
                table: "device_activation_codes",
                columns: new[] { "workspace_id", "issued_by_user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_device_activation_codes_code_hash",
                schema: "platform",
                table: "device_activation_codes",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_device_credentials_workspace_id_device_id",
                schema: "platform",
                table: "device_credentials",
                columns: new[] { "workspace_id", "device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_families_workspace_device_active",
                schema: "platform",
                table: "refresh_token_families",
                columns: new[] { "workspace_id", "device_id", "revoked_at_utc", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_families_workspace_user_active",
                schema: "platform",
                table: "refresh_token_families",
                columns: new[] { "workspace_id", "user_id", "revoked_at_utc", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_workspace_family_created",
                schema: "platform",
                table: "refresh_tokens",
                columns: new[] { "workspace_id", "family_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_workspace_id_rotated_to_token_id",
                schema: "platform",
                table: "refresh_tokens",
                columns: new[] { "workspace_id", "rotated_to_token_id" });

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                schema: "platform",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_credentials_workspace_id_normalized_login",
                schema: "platform",
                table: "user_credentials",
                columns: new[] { "workspace_id", "normalized_login" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_credentials_workspace_id_user_id",
                schema: "platform",
                table: "user_credentials",
                columns: new[] { "workspace_id", "user_id" },
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION platform.enforce_workspace_code_immutable()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW.workspace_code IS DISTINCT FROM OLD.workspace_code
                       OR NEW.normalized_workspace_code IS DISTINCT FROM
                          OLD.normalized_workspace_code THEN
                        RAISE EXCEPTION
                            'Commercial workspace code is immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER workspaces_commercial_code_immutable
                BEFORE UPDATE ON platform.workspaces
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_workspace_code_immutable();

                CREATE FUNCTION platform.reject_identity_record_delete()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION
                        'Identity and session records cannot be physically deleted'
                        USING ERRCODE = '55000';
                END;
                $function$;

                CREATE FUNCTION platform.enforce_user_credential_transition()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.user_id IS DISTINCT FROM OLD.user_id
                       OR NEW.normalized_login IS DISTINCT FROM
                          OLD.normalized_login
                       OR NEW.created_at_utc IS DISTINCT FROM
                          OLD.created_at_utc THEN
                        RAISE EXCEPTION
                            'User credential ownership is immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW.version <> OLD.version + 1 THEN
                        RAISE EXCEPTION
                            'User credential version must advance once'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW.password_hash IS DISTINCT FROM OLD.password_hash THEN
                        IF NEW.credential_version <>
                               OLD.credential_version + 1
                           OR NEW.password_changed_at_utc <=
                              OLD.password_changed_at_utc THEN
                            RAISE EXCEPTION
                                'Password changes must advance credential version'
                                USING ERRCODE = '55000';
                        END IF;
                    ELSIF NEW.credential_version <>
                          OLD.credential_version THEN
                        RAISE EXCEPTION
                            'Credential version changed without password'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE FUNCTION platform.enforce_device_credential_transition()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.device_id IS DISTINCT FROM OLD.device_id
                       OR NEW.created_at_utc IS DISTINCT FROM
                          OLD.created_at_utc THEN
                        RAISE EXCEPTION
                            'Device credential ownership is immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW.version <> OLD.version + 1 THEN
                        RAISE EXCEPTION
                            'Device credential version must advance once'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW.device_secret_hash IS DISTINCT FROM
                       OLD.device_secret_hash THEN
                        IF NEW.secret_version <> OLD.secret_version + 1
                           OR NEW.activated_at_utc <
                              OLD.activated_at_utc THEN
                            RAISE EXCEPTION
                                'Device reactivation must rotate the secret'
                                USING ERRCODE = '55000';
                        END IF;
                    ELSIF NEW.secret_version <> OLD.secret_version THEN
                        RAISE EXCEPTION
                            'Secret version changed without secret rotation'
                            USING ERRCODE = '55000';
                    END IF;
                    IF OLD.revoked_at_utc IS NOT NULL
                       AND NEW.revoked_at_utc IS NULL
                       AND NEW.device_secret_hash IS NOT DISTINCT FROM
                           OLD.device_secret_hash THEN
                        RAISE EXCEPTION
                            'Revocation can clear only through reactivation'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE FUNCTION
                    platform.enforce_device_activation_code_transition()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.device_id IS DISTINCT FROM OLD.device_id
                       OR NEW.code_hash IS DISTINCT FROM OLD.code_hash
                       OR NEW.expires_at_utc IS DISTINCT FROM
                          OLD.expires_at_utc
                       OR NEW.issued_by_user_id IS DISTINCT FROM
                          OLD.issued_by_user_id
                       OR NEW.created_at_utc IS DISTINCT FROM
                          OLD.created_at_utc THEN
                        RAISE EXCEPTION
                            'Activation code facts are immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW.version <> OLD.version + 1
                       OR (OLD.used_at_utc IS NOT NULL AND
                           NEW.used_at_utc IS DISTINCT FROM OLD.used_at_utc)
                       OR (OLD.revoked_at_utc IS NOT NULL AND
                           NEW.revoked_at_utc IS DISTINCT FROM
                               OLD.revoked_at_utc) THEN
                        RAISE EXCEPTION
                            'Activation code state transition is invalid'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE FUNCTION
                    platform.enforce_refresh_token_family_transition()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.user_id IS DISTINCT FROM OLD.user_id
                       OR NEW.device_id IS DISTINCT FROM OLD.device_id
                       OR NEW.user_credential_version IS DISTINCT FROM
                          OLD.user_credential_version
                       OR NEW.device_secret_version IS DISTINCT FROM
                          OLD.device_secret_version
                       OR NEW.created_at_utc IS DISTINCT FROM
                          OLD.created_at_utc
                       OR NEW.expires_at_utc IS DISTINCT FROM
                          OLD.expires_at_utc THEN
                        RAISE EXCEPTION
                            'Refresh-token family ownership is immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW.version <> OLD.version + 1
                       OR (OLD.revoked_at_utc IS NOT NULL AND
                           (NEW.revoked_at_utc IS DISTINCT FROM
                                OLD.revoked_at_utc
                            OR NEW.revocation_reason IS DISTINCT FROM
                                OLD.revocation_reason)) THEN
                        RAISE EXCEPTION
                            'Refresh-token family transition is invalid'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE FUNCTION platform.enforce_refresh_token_transition()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.family_id IS DISTINCT FROM OLD.family_id
                       OR NEW.token_hash IS DISTINCT FROM OLD.token_hash
                       OR NEW.created_at_utc IS DISTINCT FROM
                          OLD.created_at_utc
                       OR NEW.expires_at_utc IS DISTINCT FROM
                          OLD.expires_at_utc THEN
                        RAISE EXCEPTION
                            'Refresh-token ownership is immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW.version <> OLD.version + 1
                       OR (OLD.consumed_at_utc IS NOT NULL AND
                           (NEW.consumed_at_utc IS DISTINCT FROM
                                OLD.consumed_at_utc
                            OR NEW.rotated_to_token_id IS DISTINCT FROM
                                OLD.rotated_to_token_id
                            OR NEW.replay_protected_token IS DISTINCT FROM
                                OLD.replay_protected_token
                            OR NEW.replay_allowed_until_utc IS DISTINCT FROM
                                OLD.replay_allowed_until_utc))
                       OR (OLD.revoked_at_utc IS NOT NULL AND
                           NEW.revoked_at_utc IS DISTINCT FROM
                               OLD.revoked_at_utc) THEN
                        RAISE EXCEPTION
                            'Refresh-token state transition is invalid'
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER user_credentials_controlled_update
                BEFORE UPDATE ON platform.user_credentials
                FOR EACH ROW EXECUTE FUNCTION
                    platform.enforce_user_credential_transition();
                CREATE TRIGGER device_credentials_controlled_update
                BEFORE UPDATE ON platform.device_credentials
                FOR EACH ROW EXECUTE FUNCTION
                    platform.enforce_device_credential_transition();
                CREATE TRIGGER device_activation_codes_controlled_update
                BEFORE UPDATE ON platform.device_activation_codes
                FOR EACH ROW EXECUTE FUNCTION
                    platform.enforce_device_activation_code_transition();
                CREATE TRIGGER refresh_token_families_controlled_update
                BEFORE UPDATE ON platform.refresh_token_families
                FOR EACH ROW EXECUTE FUNCTION
                    platform.enforce_refresh_token_family_transition();
                CREATE TRIGGER refresh_tokens_controlled_update
                BEFORE UPDATE ON platform.refresh_tokens
                FOR EACH ROW EXECUTE FUNCTION
                    platform.enforce_refresh_token_transition();

                CREATE TRIGGER user_credentials_no_delete
                BEFORE DELETE ON platform.user_credentials
                FOR EACH ROW EXECUTE FUNCTION
                    platform.reject_identity_record_delete();
                CREATE TRIGGER device_credentials_no_delete
                BEFORE DELETE ON platform.device_credentials
                FOR EACH ROW EXECUTE FUNCTION
                    platform.reject_identity_record_delete();
                CREATE TRIGGER device_activation_codes_no_delete
                BEFORE DELETE ON platform.device_activation_codes
                FOR EACH ROW EXECUTE FUNCTION
                    platform.reject_identity_record_delete();
                CREATE TRIGGER refresh_token_families_no_delete
                BEFORE DELETE ON platform.refresh_token_families
                FOR EACH ROW EXECUTE FUNCTION
                    platform.reject_identity_record_delete();
                CREATE TRIGGER refresh_tokens_no_delete
                BEFORE DELETE ON platform.refresh_tokens
                FOR EACH ROW EXECUTE FUNCTION
                    platform.reject_identity_record_delete();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS workspaces_commercial_code_immutable
                    ON platform.workspaces;
                DROP FUNCTION IF EXISTS
                    platform.enforce_workspace_code_immutable();
                DROP FUNCTION IF EXISTS
                    platform.enforce_user_credential_transition() CASCADE;
                DROP FUNCTION IF EXISTS
                    platform.enforce_device_credential_transition() CASCADE;
                DROP FUNCTION IF EXISTS
                    platform.enforce_device_activation_code_transition()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    platform.enforce_refresh_token_family_transition()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    platform.enforce_refresh_token_transition() CASCADE;
                DROP FUNCTION IF EXISTS
                    platform.reject_identity_record_delete() CASCADE;
                """);

            migrationBuilder.DropTable(
                name: "device_activation_codes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "device_credentials",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "user_credentials",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "refresh_token_families",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "ux_workspaces_normalized_workspace_code",
                schema: "platform",
                table: "workspaces");

            migrationBuilder.DropCheckConstraint(
                name: "ck_workspaces_commercial_code",
                schema: "platform",
                table: "workspaces");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_role",
                schema: "platform",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_workspace_code",
                schema: "platform",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "workspace_code",
                schema: "platform",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "platform",
                table: "users");
        }
    }
}
