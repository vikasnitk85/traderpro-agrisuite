using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenProductionIdentitySessionSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_refresh_tokens_rotated_to_workspace_id_token_id",
                schema: "platform",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_workspace_id_rotated_to_token_id",
                schema: "platform",
                table: "refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_refresh_tokens_rotation_state",
                schema: "platform",
                table: "refresh_tokens");

            migrationBuilder.Sql(
                """
                WITH ranked_active_codes AS (
                    SELECT
                        id,
                        row_number() OVER (
                            PARTITION BY workspace_id, device_id
                            ORDER BY
                                (expires_at_utc > CURRENT_TIMESTAMP) DESC,
                                created_at_utc DESC,
                                id DESC
                        ) AS active_rank,
                        expires_at_utc > CURRENT_TIMESTAMP AS is_valid
                    FROM platform.device_activation_codes
                    WHERE used_at_utc IS NULL
                      AND revoked_at_utc IS NULL
                )
                UPDATE platform.device_activation_codes AS code
                SET revoked_at_utc = greatest(
                        code.created_at_utc,
                        least(code.expires_at_utc, CURRENT_TIMESTAMP)),
                    version = code.version + 1
                FROM ranked_active_codes AS ranked
                WHERE code.id = ranked.id
                  AND (
                      NOT ranked.is_valid
                      OR ranked.active_rank > 1
                  );

                DO $validation$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM platform.refresh_tokens AS predecessor
                        JOIN platform.refresh_tokens AS replacement
                          ON replacement.id =
                                predecessor.rotated_to_token_id
                         AND replacement.workspace_id =
                                predecessor.workspace_id
                        WHERE predecessor.rotated_to_token_id IS NOT NULL
                          AND (
                              replacement.family_id <>
                                predecessor.family_id
                              OR replacement.created_at_utc <
                                predecessor.consumed_at_utc
                              OR predecessor.id =
                                predecessor.rotated_to_token_id
                          )
                    ) THEN
                        RAISE EXCEPTION
                            'Existing refresh-token chains are not valid for session hardening'
                            USING ERRCODE = '55000';
                    END IF;
                END;
                $validation$;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_refresh_tokens_workspace_family_id",
                schema: "platform",
                table: "refresh_tokens",
                columns: new[] { "workspace_id", "family_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_workspace_family_replacement",
                schema: "platform",
                table: "refresh_tokens",
                columns: new[] { "workspace_id", "family_id", "rotated_to_token_id" },
                unique: true,
                filter: "rotated_to_token_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_refresh_tokens_no_self_rotation",
                schema: "platform",
                table: "refresh_tokens",
                sql: "rotated_to_token_id IS NULL OR rotated_to_token_id <> id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_refresh_tokens_rotation_state",
                schema: "platform",
                table: "refresh_tokens",
                sql: "(\n    consumed_at_utc IS NULL\n    AND rotated_to_token_id IS NULL\n    AND replay_protected_token IS NULL\n    AND replay_allowed_until_utc IS NULL\n)\nOR\n(\n    consumed_at_utc IS NOT NULL\n    AND rotated_to_token_id IS NOT NULL\n    AND consumed_at_utc >= created_at_utc\n    AND consumed_at_utc <= expires_at_utc\n    AND (\n        (\n            replay_protected_token IS NOT NULL\n            AND replay_allowed_until_utc >\n                consumed_at_utc\n            AND replay_allowed_until_utc <=\n                expires_at_utc\n        )\n        OR\n        (\n            replay_protected_token IS NULL\n            AND replay_allowed_until_utc IS NULL\n        )\n    )\n)");

            migrationBuilder.CreateIndex(
                name: "ux_device_activation_codes_workspace_device_active",
                schema: "platform",
                table: "device_activation_codes",
                columns: new[] { "workspace_id", "device_id" },
                unique: true,
                filter: "used_at_utc IS NULL AND revoked_at_utc IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_device_activation_codes_terminal_timing",
                schema: "platform",
                table: "device_activation_codes",
                sql: "(used_at_utc IS NULL OR (\n    used_at_utc >= created_at_utc\n    AND used_at_utc <= expires_at_utc\n))\nAND (revoked_at_utc IS NULL OR\n    revoked_at_utc >= created_at_utc)");

            migrationBuilder.AddForeignKey(
                name: "fk_refresh_tokens_rotated_to_workspace_family_token",
                schema: "platform",
                table: "refresh_tokens",
                columns: new[] { "workspace_id", "family_id", "rotated_to_token_id" },
                principalSchema: "platform",
                principalTable: "refresh_tokens",
                principalColumns: new[] { "workspace_id", "family_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION
                    platform.enforce_refresh_token_transition()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    replacement_created_at_utc
                        timestamp with time zone;
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        IF NEW.consumed_at_utc IS NOT NULL
                           AND (
                               NEW.replay_protected_token IS NULL
                               OR NEW.replay_allowed_until_utc IS NULL
                           ) THEN
                            RAISE EXCEPTION
                                'Inserted consumed tokens require protected replay material'
                                USING ERRCODE = '55000';
                        END IF;
                        IF NEW.rotated_to_token_id IS NOT NULL THEN
                            SELECT replacement.created_at_utc
                            INTO replacement_created_at_utc
                            FROM platform.refresh_tokens AS replacement
                            WHERE replacement.workspace_id =
                                    NEW.workspace_id
                              AND replacement.family_id = NEW.family_id
                              AND replacement.id =
                                    NEW.rotated_to_token_id;
                            IF replacement_created_at_utc IS NULL
                               OR replacement_created_at_utc <
                                  NEW.consumed_at_utc THEN
                                RAISE EXCEPTION
                                    'Refresh-token replacement timing is invalid'
                                    USING ERRCODE = '55000';
                            END IF;
                        END IF;
                        RETURN NEW;
                    END IF;

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
                    IF NEW.version <> OLD.version + 1 THEN
                        RAISE EXCEPTION
                            'Refresh-token version must advance once'
                            USING ERRCODE = '55000';
                    END IF;
                    IF OLD.consumed_at_utc IS NOT NULL
                       AND (
                           NEW.consumed_at_utc IS DISTINCT FROM
                               OLD.consumed_at_utc
                           OR NEW.rotated_to_token_id IS DISTINCT FROM
                               OLD.rotated_to_token_id
                       ) THEN
                        RAISE EXCEPTION
                            'Refresh-token rotation facts are immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF OLD.revoked_at_utc IS NOT NULL
                       AND NEW.revoked_at_utc IS DISTINCT FROM
                           OLD.revoked_at_utc THEN
                        RAISE EXCEPTION
                            'Refresh-token revocation is immutable'
                            USING ERRCODE = '55000';
                    END IF;
                    IF OLD.consumed_at_utc IS NOT NULL
                       AND OLD.replay_protected_token IS NULL THEN
                        IF NEW.replay_protected_token IS NOT NULL
                           OR NEW.replay_allowed_until_utc IS NOT NULL THEN
                            RAISE EXCEPTION
                                'Cleared refresh replay material cannot be restored'
                                USING ERRCODE = '55000';
                        END IF;
                    ELSIF OLD.replay_protected_token IS NOT NULL
                       AND (
                        NEW.replay_protected_token IS DISTINCT FROM
                            OLD.replay_protected_token
                        OR NEW.replay_allowed_until_utc IS DISTINCT FROM
                            OLD.replay_allowed_until_utc
                    ) AND NOT (
                        NEW.replay_protected_token IS NULL
                        AND NEW.replay_allowed_until_utc IS NULL
                        AND NEW.consumed_at_utc IS NOT NULL
                        AND NEW.rotated_to_token_id IS NOT NULL
                    ) THEN
                        RAISE EXCEPTION
                            'Refresh replay material transition is invalid'
                            USING ERRCODE = '55000';
                    END IF;
                    IF OLD.consumed_at_utc IS NULL
                       AND NEW.consumed_at_utc IS NOT NULL
                       AND (
                           NEW.replay_protected_token IS NULL
                           OR NEW.replay_allowed_until_utc IS NULL
                       ) THEN
                        RAISE EXCEPTION
                            'Initial rotation requires protected replay material'
                            USING ERRCODE = '55000';
                    END IF;
                    IF NEW.rotated_to_token_id IS NOT NULL THEN
                        IF NEW.rotated_to_token_id = NEW.id THEN
                            RAISE EXCEPTION
                                'A refresh token cannot rotate to itself'
                                USING ERRCODE = '55000';
                        END IF;
                        SELECT replacement.created_at_utc
                        INTO replacement_created_at_utc
                        FROM platform.refresh_tokens AS replacement
                        WHERE replacement.workspace_id = NEW.workspace_id
                          AND replacement.family_id = NEW.family_id
                          AND replacement.id =
                                NEW.rotated_to_token_id;
                        IF replacement_created_at_utc IS NULL
                           OR replacement_created_at_utc <
                              NEW.consumed_at_utc THEN
                            RAISE EXCEPTION
                                'Refresh-token replacement timing is invalid'
                                USING ERRCODE = '55000';
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                DROP TRIGGER IF EXISTS refresh_tokens_controlled_update
                    ON platform.refresh_tokens;
                CREATE TRIGGER refresh_tokens_controlled_update
                BEFORE INSERT OR UPDATE ON platform.refresh_tokens
                FOR EACH ROW EXECUTE FUNCTION
                    platform.enforce_refresh_token_transition();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_refresh_tokens_rotated_to_workspace_family_token",
                schema: "platform",
                table: "refresh_tokens");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_refresh_tokens_workspace_family_id",
                schema: "platform",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ux_refresh_tokens_workspace_family_replacement",
                schema: "platform",
                table: "refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_refresh_tokens_no_self_rotation",
                schema: "platform",
                table: "refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_refresh_tokens_rotation_state",
                schema: "platform",
                table: "refresh_tokens");

            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS refresh_tokens_controlled_update
                    ON platform.refresh_tokens;
                UPDATE platform.refresh_tokens
                SET replay_protected_token =
                        'unavailable-after-session-security-downgrade',
                    replay_allowed_until_utc = expires_at_utc,
                    version = version + 1
                WHERE consumed_at_utc IS NOT NULL
                  AND replay_protected_token IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "ux_device_activation_codes_workspace_device_active",
                schema: "platform",
                table: "device_activation_codes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_device_activation_codes_terminal_timing",
                schema: "platform",
                table: "device_activation_codes");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_workspace_id_rotated_to_token_id",
                schema: "platform",
                table: "refresh_tokens",
                columns: new[] { "workspace_id", "rotated_to_token_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_refresh_tokens_rotation_state",
                schema: "platform",
                table: "refresh_tokens",
                sql: "(\n    consumed_at_utc IS NULL\n    AND rotated_to_token_id IS NULL\n    AND replay_protected_token IS NULL\n    AND replay_allowed_until_utc IS NULL\n)\nOR\n(\n    consumed_at_utc IS NOT NULL\n    AND rotated_to_token_id IS NOT NULL\n    AND replay_protected_token IS NOT NULL\n    AND replay_allowed_until_utc > consumed_at_utc\n    AND replay_allowed_until_utc <= expires_at_utc\n)");

            migrationBuilder.AddForeignKey(
                name: "fk_refresh_tokens_rotated_to_workspace_id_token_id",
                schema: "platform",
                table: "refresh_tokens",
                columns: new[] { "workspace_id", "rotated_to_token_id" },
                principalSchema: "platform",
                principalTable: "refresh_tokens",
                principalColumns: new[] { "workspace_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION
                    platform.enforce_refresh_token_transition()
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
                CREATE TRIGGER refresh_tokens_controlled_update
                BEFORE UPDATE ON platform.refresh_tokens
                FOR EACH ROW EXECUTE FUNCTION
                    platform.enforce_refresh_token_transition();
                """);
        }
    }
}
