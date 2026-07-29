using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TraderPro.Application.Platform.CommandProbes;
using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CloudCommandMigrationUpgradeTests(
    PostgreSqlFixture fixture)
{
    private const string FoundationMigration =
        "20260729062805_HardenPlatformFoundationIntegrity";

    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 29, 8, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task Existing_foundation_data_upgrades_without_fake_replays()
    {
        await using var database = await fixture.CreateDatabaseAsync(
            FoundationMigration);
        var workspace = Workspace.Create(
            "upgrade-data",
            "Upgrade Data Workspace",
            UtcNow);
        await using (var context = database.CreateContext(null))
        {
            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync(CancellationToken);
        }

        var firstOutboxId = Uuid7.NewGuid();
        var secondOutboxId = Uuid7.NewGuid();
        const string legacyKey = "legacy-completed-key";
        const string probeName = "Legacy Probe";
        var legacyIdempotencyId = Uuid7.NewGuid();
        await InsertPreTaskFiveRowsAsync(
            database,
            workspace.Id,
            firstOutboxId,
            secondOutboxId,
            legacyIdempotencyId,
            legacyKey,
            CommandProbeRequestHash.ForCreate(probeName));

        await database.ApplyMigrationsAsync();

        await AssertUpgradedSchemaAndDataAsync(
            database,
            workspace.Id,
            legacyIdempotencyId);

        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/spikes/command-probes")
        {
            Content = JsonContent.Create(new { name = probeName }),
        };
        request.Headers.Add(
            "X-TraderPro-Workspace-ID",
            workspace.Id.ToString("D"));
        request.Headers.Add("Idempotency-Key", legacyKey);
        using var response = await client.SendAsync(
            request,
            CancellationToken);
        using var responseJson = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "IDEMPOTENCY_PREVIOUS_ATTEMPT_FAILED",
            responseJson.RootElement
                .GetProperty("error")
                .GetProperty("code")
                .GetString());
        Assert.False(
            responseJson.RootElement
                .GetProperty("error")
                .GetProperty("retryable")
                .GetBoolean());

        await using var verification = database.CreateContext(workspace.Id);
        Assert.Equal(
            0,
            await verification.CommandProbes.CountAsync(CancellationToken));
        Assert.Equal(
            0,
            await verification.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(
            2,
            await verification.OutboxMessages.CountAsync(CancellationToken));
        Assert.Equal(
            1,
            await verification.IdempotencyRecords.CountAsync(
                CancellationToken));
    }

    private static async Task InsertPreTaskFiveRowsAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        Guid firstOutboxId,
        Guid secondOutboxId,
        Guid legacyIdempotencyId,
        string legacyKey,
        string requestHash)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO platform.outbox_messages (
                id,
                workspace_id,
                event_type,
                event_version,
                aggregate_type,
                aggregate_id,
                aggregate_version,
                payload_json,
                correlation_id,
                occurred_at_utc,
                status,
                attempt_count)
            VALUES
                (
                    @first_outbox_id,
                    @workspace_id,
                    'LegacyEventOne',
                    1,
                    'LegacyAggregate',
                    @first_aggregate_id,
                    1,
                    CAST('{"legacy":1}' AS jsonb),
                    @first_correlation_id,
                    @occurred_at_utc,
                    1,
                    0
                ),
                (
                    @second_outbox_id,
                    @workspace_id,
                    'LegacyEventTwo',
                    1,
                    'LegacyAggregate',
                    @second_aggregate_id,
                    2,
                    CAST('{"legacy":2}' AS jsonb),
                    @second_correlation_id,
                    @occurred_at_utc,
                    1,
                    0
                );

            INSERT INTO platform.idempotency_records (
                id,
                workspace_id,
                idempotency_key,
                command_type,
                request_hash,
                status,
                result_payload_json,
                created_at_utc,
                completed_at_utc)
            VALUES (
                @idempotency_id,
                @workspace_id,
                @idempotency_key,
                @command_type,
                @request_hash,
                2,
                CAST('{"legacy":"preserved"}' AS jsonb),
                @occurred_at_utc,
                @occurred_at_utc
            );
            """;
        command.Parameters.AddWithValue("first_outbox_id", firstOutboxId);
        command.Parameters.AddWithValue("second_outbox_id", secondOutboxId);
        command.Parameters.AddWithValue("workspace_id", workspaceId);
        command.Parameters.AddWithValue(
            "first_aggregate_id",
            Uuid7.NewGuid());
        command.Parameters.AddWithValue(
            "second_aggregate_id",
            Uuid7.NewGuid());
        command.Parameters.AddWithValue(
            "first_correlation_id",
            Uuid7.NewGuid().ToString("D"));
        command.Parameters.AddWithValue(
            "second_correlation_id",
            Uuid7.NewGuid().ToString("D"));
        command.Parameters.AddWithValue("occurred_at_utc", UtcNow);
        command.Parameters.AddWithValue(
            "idempotency_id",
            legacyIdempotencyId);
        command.Parameters.AddWithValue("idempotency_key", legacyKey);
        command.Parameters.AddWithValue(
            "command_type",
            CommandProbeCommandTypes.Create);
        command.Parameters.AddWithValue("request_hash", requestHash);
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task AssertUpgradedSchemaAndDataAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        Guid legacyIdempotencyId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT
                    COUNT(*),
                    COUNT(DISTINCT sequence),
                    MIN(sequence),
                    MAX(sequence),
                    bool_and(event_stream = 1)
                FROM platform.outbox_messages
                WHERE workspace_id = @workspace_id;
                """;
            command.Parameters.AddWithValue("workspace_id", workspaceId);
            await using var reader = await command.ExecuteReaderAsync(
                CancellationToken);
            Assert.True(await reader.ReadAsync(CancellationToken));
            Assert.Equal(2, reader.GetInt64(0));
            Assert.Equal(2, reader.GetInt64(1));
            Assert.True(reader.GetInt64(2) < reader.GetInt64(3));
            Assert.True(reader.GetBoolean(4));
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT
                    status,
                    result_status_code,
                    result_payload_json ->> 'legacy'
                FROM platform.idempotency_records
                WHERE id = @id;
                """;
            command.Parameters.AddWithValue("id", legacyIdempotencyId);
            await using var reader = await command.ExecuteReaderAsync(
                CancellationToken);
            Assert.True(await reader.ReadAsync(CancellationToken));
            Assert.Equal((short)3, reader.GetInt16(0));
            Assert.True(reader.IsDBNull(1));
            Assert.Equal("preserved", reader.GetString(2));
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT
                    EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'platform'
                          AND table_name = 'outbox_messages'
                          AND column_name = 'sequence'
                          AND is_identity = 'YES'),
                    EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'platform'
                          AND indexname = 'ux_outbox_messages_sequence'),
                    EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'platform'
                          AND indexname =
                              'ix_outbox_messages_workspace_id_event_stream_sequence'),
                    EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname =
                            'ck_idempotency_records_result_status_code'),
                    EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'ck_outbox_messages_event_stream'),
                    EXISTS (
                        SELECT 1
                        FROM pg_trigger
                        WHERE tgname =
                            'tr_outbox_messages_immutable_event_facts'
                          AND NOT tgisinternal);
                """;
            await using var reader = await command.ExecuteReaderAsync(
                CancellationToken);
            Assert.True(await reader.ReadAsync(CancellationToken));
            for (var index = 0; index < reader.FieldCount; index++)
            {
                Assert.True(reader.GetBoolean(index));
            }
        }
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(
            CancellationToken);
        return await JsonDocument.ParseAsync(
            stream,
            cancellationToken: CancellationToken);
    }
}
