using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(TraderProDbContext))]
    [Migration("20260809090000_CrashSafeDeviceActivation")]
    public partial class CrashSafeDeviceActivation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "redemption_idempotency_key_hash",
                schema: "platform",
                table: "device_activation_codes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "redemption_request_hash",
                schema: "platform",
                table: "device_activation_codes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "replay_protected_result",
                schema: "platform",
                table: "device_activation_codes",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "replay_allowed_until_utc",
                schema: "platform",
                table: "device_activation_codes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_device_activation_codes_replay_state",
                schema: "platform",
                table: "device_activation_codes",
                sql: """
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

            migrationBuilder.CreateIndex(
                name: "ux_device_activation_codes_workspace_redemption_key",
                schema: "platform",
                table: "device_activation_codes",
                columns: new[]
                {
                    "workspace_id",
                    "redemption_idempotency_key_hash",
                },
                unique: true,
                filter: "redemption_idempotency_key_hash IS NOT NULL");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION
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
                    IF OLD.redemption_idempotency_key_hash IS NULL THEN
                        IF OLD.used_at_utc IS NULL AND
                           NEW.used_at_utc IS NOT NULL THEN
                            IF NEW.redemption_idempotency_key_hash IS NULL
                               OR NEW.redemption_request_hash IS NULL
                               OR NEW.replay_protected_result IS NULL
                               OR NEW.replay_allowed_until_utc IS NULL THEN
                                RAISE EXCEPTION
                                    'Activation replay material is required at consumption'
                                    USING ERRCODE = '55000';
                            END IF;
                        ELSIF NEW.redemption_idempotency_key_hash IS NOT NULL
                           OR NEW.redemption_request_hash IS NOT NULL
                           OR NEW.replay_protected_result IS NOT NULL
                           OR NEW.replay_allowed_until_utc IS NOT NULL THEN
                            RAISE EXCEPTION
                                'Activation replay material cannot be added later'
                                USING ERRCODE = '55000';
                        END IF;
                    ELSE
                        IF NEW.redemption_idempotency_key_hash IS DISTINCT FROM
                               OLD.redemption_idempotency_key_hash
                           OR NEW.redemption_request_hash IS DISTINCT FROM
                               OLD.redemption_request_hash
                           OR NEW.replay_allowed_until_utc IS DISTINCT FROM
                               OLD.replay_allowed_until_utc
                           OR (OLD.replay_protected_result IS NULL AND
                               NEW.replay_protected_result IS NOT NULL)
                           OR (OLD.replay_protected_result IS NOT NULL AND
                               NEW.replay_protected_result IS NOT NULL AND
                               NEW.replay_protected_result IS DISTINCT FROM
                                   OLD.replay_protected_result) THEN
                            RAISE EXCEPTION
                                'Activation replay transition is invalid'
                                USING ERRCODE = '55000';
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Crash-safe Device activation is a forward-only security migration. Restore the database from backup for rollback.");
        }
    }
}
