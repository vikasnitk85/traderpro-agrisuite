using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Platform.CommandProbes;
using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CloudCommandSpikeApiTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 29, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_and_identical_retry_commit_one_result_audit_event_and_cursor_event()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "create-retry");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();

        using var first = await SendCreateAsync(
            client,
            workspace.Id,
            "create-retry-key",
            "Probe A");
        using var replay = await SendCreateAsync(
            client,
            workspace.Id,
            "create-retry-key",
            "Probe A");
        using var firstJson = await ReadJsonAsync(first);
        using var replayJson = await ReadJsonAsync(replay);
        var probeId = firstJson.RootElement
            .GetProperty("result")
            .GetProperty("id")
            .GetGuid();

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(
            "Processed",
            firstJson.RootElement
                .GetProperty("meta")
                .GetProperty("idempotencyStatus")
                .GetString());
        Assert.Equal(
            "PreviouslyProcessed",
            replayJson.RootElement
                .GetProperty("meta")
                .GetProperty("idempotencyStatus")
                .GetString());
        Assert.Equal(
            firstJson.RootElement.GetProperty("result").GetRawText(),
            replayJson.RootElement.GetProperty("result").GetRawText());

        await using (var context = database.CreateContext(workspace.Id))
        {
            Assert.Equal(1, await context.CommandProbes.CountAsync(CancellationToken));
            Assert.Equal(1, await context.AuditEvents.CountAsync(CancellationToken));
            Assert.Equal(1, await context.OutboxMessages.CountAsync(CancellationToken));
            var record = await context.IdempotencyRecords.SingleAsync(
                CancellationToken);
            Assert.Equal(IdempotencyRecordStatus.Completed, record.Status);
            Assert.Equal(201, record.ResultStatusCode);
        }

        using var cursor = await SendCursorAsync(
            client,
            workspace.Id,
            after: 0,
            limit: 50);
        using var cursorJson = await ReadJsonAsync(cursor);
        var events = cursorJson.RootElement.GetProperty("events");
        Assert.Single(events.EnumerateArray());
        Assert.Equal(
            "CommandProbeCreated",
            events[0].GetProperty("eventType").GetString());
        Assert.Equal(
            probeId,
            events[0].GetProperty("aggregateId").GetGuid());
        Assert.Equal(
            probeId,
            events[0].GetProperty("payload").GetProperty("probeId").GetGuid());
    }

    [Fact]
    public async Task Reusing_idempotency_key_with_changed_payload_returns_stable_conflict()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "payload-conflict");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();

        using var first = await SendCreateAsync(
            client,
            workspace.Id,
            "shared-key",
            "Probe A");
        using var conflict = await SendCreateAsync(
            client,
            workspace.Id,
            "shared-key",
            "Probe B");
        using var conflictJson = await ReadJsonAsync(conflict);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(
            "IDEMPOTENCY_PAYLOAD_CONFLICT",
            ErrorCode(conflictJson));

        await using var context = database.CreateContext(workspace.Id);
        Assert.Equal(1, await context.CommandProbes.CountAsync(CancellationToken));
        Assert.Equal(1, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(1, await context.OutboxMessages.CountAsync(CancellationToken));
        Assert.Equal(1, await context.IdempotencyRecords.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task Lost_increment_response_retry_returns_original_without_second_change()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "increment-retry");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();
        using var created = await SendCreateAsync(
            client,
            workspace.Id,
            "create-key",
            "Increment Probe");
        using var createdJson = await ReadJsonAsync(created);
        var probeId = ResultId(createdJson);

        using (await SendIncrementAsync(
                   client,
                   workspace.Id,
                   probeId,
                   "increment-key",
                   expectedVersion: 1,
                   delta: 3))
        {
            // Deliberately ignore the first committed response.
        }

        using var replay = await SendIncrementAsync(
            client,
            workspace.Id,
            probeId,
            "increment-key",
            expectedVersion: 1,
            delta: 3);
        using var replayJson = await ReadJsonAsync(replay);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(
            "PreviouslyProcessed",
            replayJson.RootElement
                .GetProperty("meta")
                .GetProperty("idempotencyStatus")
                .GetString());
        Assert.Equal(
            3,
            replayJson.RootElement
                .GetProperty("result")
                .GetProperty("counter")
                .GetInt64());
        Assert.Equal(
            2,
            replayJson.RootElement
                .GetProperty("result")
                .GetProperty("version")
                .GetInt64());

        await using var context = database.CreateContext(workspace.Id);
        var probe = await context.CommandProbes.SingleAsync(CancellationToken);
        Assert.Equal(3, probe.Counter);
        Assert.Equal(2, probe.Version);
        Assert.Equal(2, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(2, await context.OutboxMessages.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task Concurrent_updates_with_same_expected_version_allow_exactly_one()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "version-race");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();
        using var created = await SendCreateAsync(
            client,
            workspace.Id,
            "version-create",
            "Version Probe");
        using var createdJson = await ReadJsonAsync(created);
        var probeId = ResultId(createdJson);

        var firstTask = SendIncrementAsync(
            client,
            workspace.Id,
            probeId,
            "version-key-a",
            expectedVersion: 1,
            delta: 1);
        var secondTask = SendIncrementAsync(
            client,
            workspace.Id,
            probeId,
            "version-key-b",
            expectedVersion: 1,
            delta: 1);
        var responses = await Task.WhenAll(firstTask, secondTask);
        try
        {
            Assert.Equal(
                1,
                responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            var conflict = Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Conflict);
            using var conflictJson = await ReadJsonAsync(conflict);
            Assert.Equal(
                "COMMAND_PROBE_VERSION_CONFLICT",
                ErrorCode(conflictJson));
            Assert.Equal(
                1,
                conflictJson.RootElement
                    .GetProperty("error")
                    .GetProperty("details")
                    .GetProperty("expectedVersion")
                    .GetInt64());
            Assert.Equal(
                2,
                conflictJson.RootElement
                    .GetProperty("error")
                    .GetProperty("details")
                    .GetProperty("currentVersion")
                    .GetInt64());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        await using var context = database.CreateContext(workspace.Id);
        var probe = await context.CommandProbes.SingleAsync(CancellationToken);
        Assert.Equal(1, probe.Counter);
        Assert.Equal(2, probe.Version);
        Assert.Equal(2, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(2, await context.OutboxMessages.CountAsync(CancellationToken));
        Assert.Equal(2, await context.IdempotencyRecords.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task Concurrent_identical_requests_use_database_backed_single_execution()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "same-key-race");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();

        var firstTask = SendCreateAsync(
            client,
            workspace.Id,
            "concurrent-same-key",
            "Concurrent Probe");
        var secondTask = SendCreateAsync(
            client,
            workspace.Id,
            "concurrent-same-key",
            "Concurrent Probe");
        var responses = await Task.WhenAll(firstTask, secondTask);
        try
        {
            Assert.All(
                responses,
                response => Assert.Equal(
                    HttpStatusCode.Created,
                    response.StatusCode));
            using var firstJson = await ReadJsonAsync(responses[0]);
            using var secondJson = await ReadJsonAsync(responses[1]);
            Assert.Equal(ResultId(firstJson), ResultId(secondJson));
            Assert.Equal(
                new[] { "PreviouslyProcessed", "Processed" },
                new[]
                    {
                        IdempotencyStatus(firstJson),
                        IdempotencyStatus(secondJson),
                    }
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        await using var context = database.CreateContext(workspace.Id);
        Assert.Equal(1, await context.CommandProbes.CountAsync(CancellationToken));
        Assert.Equal(1, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(1, await context.OutboxMessages.CountAsync(CancellationToken));
        Assert.Equal(1, await context.IdempotencyRecords.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task Outbox_failure_rolls_back_probe_audit_idempotency_and_cursor_event()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "rollback");
        await CreateRejectingOutboxTriggerAsync(database);
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();

        try
        {
            using var response = await SendCreateAsync(
                client,
                workspace.Id,
                "rollback-key",
                "Rollback Probe");
            using var responseJson = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("TEMPORARY_COMMAND_FAILURE", ErrorCode(responseJson));
        }
        finally
        {
            await DropRejectingOutboxTriggerAsync(database);
        }

        await using (var context = database.CreateContext(workspace.Id))
        {
            Assert.Equal(0, await context.CommandProbes.CountAsync(CancellationToken));
            Assert.Equal(0, await context.AuditEvents.CountAsync(CancellationToken));
            Assert.Equal(0, await context.OutboxMessages.CountAsync(CancellationToken));
            Assert.Equal(0, await context.IdempotencyRecords.CountAsync(CancellationToken));
        }

        using var cursor = await SendCursorAsync(client, workspace.Id, 0, 50);
        using var cursorJson = await ReadJsonAsync(cursor);
        Assert.Empty(
            cursorJson.RootElement
                .GetProperty("events")
                .EnumerateArray());
        Assert.Equal(
            0,
            cursorJson.RootElement.GetProperty("nextCursor").GetInt64());
    }

    [Fact]
    public async Task Cursor_is_workspace_isolated_ordered_resumable_and_accepts_gaps()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspaceA = await CreateWorkspaceAsync(database, "cursor-a");
        var workspaceB = await CreateWorkspaceAsync(database, "cursor-b");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();

        using var a1 = await SendCreateAsync(
            client,
            workspaceA.Id,
            "cursor-a-1",
            "A1");
        using var b1 = await SendCreateAsync(
            client,
            workspaceB.Id,
            "cursor-b-1",
            "B1");
        using var a2 = await SendCreateAsync(
            client,
            workspaceA.Id,
            "cursor-a-2",
            "A2");
        Assert.Equal(HttpStatusCode.Created, a1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, b1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, a2.StatusCode);

        using var page1 = await SendCursorAsync(
            client,
            workspaceA.Id,
            after: 0,
            limit: 1);
        using var page1Json = await ReadJsonAsync(page1);
        Assert.True(page1Json.RootElement.GetProperty("hasMore").GetBoolean());
        var firstSequence = page1Json.RootElement
            .GetProperty("events")[0]
            .GetProperty("sequence")
            .GetInt64();

        using var page2 = await SendCursorAsync(
            client,
            workspaceA.Id,
            after: firstSequence,
            limit: 50);
        using var page2Json = await ReadJsonAsync(page2);
        var secondSequence = page2Json.RootElement
            .GetProperty("events")[0]
            .GetProperty("sequence")
            .GetInt64();
        Assert.True(secondSequence > firstSequence);
        Assert.True(secondSequence - firstSequence > 1);
        Assert.False(page2Json.RootElement.GetProperty("hasMore").GetBoolean());
        Assert.Equal(
            secondSequence,
            page2Json.RootElement.GetProperty("nextCursor").GetInt64());

        using var bPage = await SendCursorAsync(
            client,
            workspaceB.Id,
            after: 0,
            limit: 50);
        using var bJson = await ReadJsonAsync(bPage);
        Assert.Single(
            bJson.RootElement.GetProperty("events").EnumerateArray());

        using var empty = await SendCursorAsync(
            client,
            workspaceA.Id,
            after: secondSequence,
            limit: 50);
        using var emptyJson = await ReadJsonAsync(empty);
        Assert.Empty(
            emptyJson.RootElement.GetProperty("events").EnumerateArray());
        Assert.Equal(
            secondSequence,
            emptyJson.RootElement.GetProperty("nextCursor").GetInt64());
    }

    [Fact]
    public async Task Cursor_returns_only_mobile_sync_events()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "cursor-stream");
        var internalEventId = Uuid7.NewGuid();
        await using (var context = database.CreateContext(workspace.Id))
        {
            context.OutboxMessages.Add(
                OutboxMessage.Create(
                    workspace.Id,
                    "InternalOnlyEvent",
                    1,
                    "InternalAggregate",
                    internalEventId,
                    1,
                    """{"internal":true}""",
                    Uuid7.NewGuid().ToString("D"),
                    UtcNow));
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();
        using var created = await SendCreateAsync(
            client,
            workspace.Id,
            "cursor-stream-key",
            "Mobile Probe");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var cursor = await SendCursorAsync(
            client,
            workspace.Id,
            0,
            50);
        using var cursorJson = await ReadJsonAsync(cursor);
        var events = cursorJson.RootElement
            .GetProperty("events")
            .EnumerateArray()
            .ToArray();
        var returned = Assert.Single(events);
        Assert.Equal(
            "CommandProbeCreated",
            returned.GetProperty("eventType").GetString());
        Assert.NotEqual(
            internalEventId,
            returned.GetProperty("aggregateId").GetGuid());

        await using var verification = database.CreateContext(workspace.Id);
        Assert.Equal(
            1,
            await verification.OutboxMessages.CountAsync(
                message =>
                    message.EventStream == OutboxEventStream.Internal,
                CancellationToken));
        Assert.Equal(
            1,
            await verification.OutboxMessages.CountAsync(
                message =>
                    message.EventStream == OutboxEventStream.MobileSync,
                CancellationToken));
    }

    [Fact]
    public async Task Workspace_context_fails_closed_and_cross_workspace_ids_are_hidden()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspaceA = await CreateWorkspaceAsync(database, "workspace-a");
        var workspaceB = await CreateWorkspaceAsync(database, "workspace-b");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();
        using var created = await SendCreateAsync(
            client,
            workspaceB.Id,
            "workspace-create",
            "B Probe");
        using var createdJson = await ReadJsonAsync(created);
        var probeId = ResultId(createdJson);

        using var read = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/spikes/command-probes/{probeId:D}");
        read.Headers.Add(
            "X-TraderPro-Workspace-ID",
            workspaceA.Id.ToString("D"));
        using var hiddenRead = await client.SendAsync(read, CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hiddenRead.StatusCode);

        using var hiddenIncrement = await SendIncrementAsync(
            client,
            workspaceA.Id,
            probeId,
            "cross-increment",
            1,
            1);
        using var hiddenJson = await ReadJsonAsync(hiddenIncrement);
        Assert.Equal(HttpStatusCode.NotFound, hiddenIncrement.StatusCode);
        Assert.Equal("COMMAND_PROBE_NOT_FOUND", ErrorCode(hiddenJson));

        using var bound = await SendCursorAsync(
            client,
            workspaceA.Id,
            0,
            50);
        Assert.Equal(HttpStatusCode.OK, bound.StatusCode);

        using var missing = await client.GetAsync(
            "/api/v1/mobile/sync/events/",
            CancellationToken);
        using var missingJson = await ReadJsonAsync(missing);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("WORKSPACE_CONTEXT_REQUIRED", ErrorCode(missingJson));

        using var malformedRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/mobile/sync/events");
        malformedRequest.Headers.Add(
            "X-TraderPro-Workspace-ID",
            "not-a-uuid");
        using var malformed = await client.SendAsync(
            malformedRequest,
            CancellationToken);
        using var malformedJson = await ReadJsonAsync(malformed);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal("WORKSPACE_CONTEXT_INVALID", ErrorCode(malformedJson));

        using var unknown = await SendCursorAsync(
            client,
            Uuid7.NewGuid(),
            0,
            50);
        using var unknownJson = await ReadJsonAsync(unknown);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("WORKSPACE_NOT_FOUND", ErrorCode(unknownJson));
    }

    [Fact]
    public async Task Readers_require_workspace_context_when_invoked_below_http()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var probeReader =
            scope.ServiceProvider.GetRequiredService<ICommandProbeReader>();
        var cursorReader =
            scope.ServiceProvider.GetRequiredService<ICloudEventCursorReader>();

        var probeError = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => probeReader.FindAsync(Uuid7.NewGuid(), CancellationToken));
        var cursorError = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => cursorReader.ReadEventsAsync(0, 50, CancellationToken));

        Assert.Equal("WORKSPACE_CONTEXT_REQUIRED", probeError.Code);
        Assert.Equal("WORKSPACE_CONTEXT_REQUIRED", cursorError.Code);
        Assert.False(probeError.Retryable);
        Assert.False(cursorError.Retryable);
    }

    [Fact]
    public async Task Invalid_command_bodies_use_the_standard_validation_error()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "invalid-body");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();

        await AssertInvalidBodyAsync(client, workspace.Id, content: null);
        await AssertInvalidBodyAsync(
            client,
            workspace.Id,
            new StringContent(
                "{",
                Encoding.UTF8,
                "application/json"));
        await AssertInvalidBodyAsync(
            client,
            workspace.Id,
            new StringContent(
                """{"name":42}""",
                Encoding.UTF8,
                "application/json"));

        await using var verification = database.CreateContext(workspace.Id);
        Assert.Equal(
            0,
            await verification.CommandProbes.CountAsync(CancellationToken));
        Assert.Equal(
            0,
            await verification.IdempotencyRecords.CountAsync(
                CancellationToken));
        Assert.Equal(
            0,
            await verification.OutboxMessages.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task Spike_endpoints_are_not_mapped_when_disabled()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "disabled");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false);
        using var client = factory.CreateClient();

        using var response = await SendCreateAsync(
            client,
            workspace.Id,
            "disabled-key",
            "Disabled Probe");
        using var cursor = await SendCursorAsync(
            client,
            workspace.Id,
            0,
            50);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cursor.StatusCode);
    }

    [Fact]
    public async Task Correlation_is_preserved_or_generated_and_committed_everywhere()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "correlation");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();
        var supplied = Uuid7.NewGuid().ToString("D");

        using var suppliedResponse = await SendCreateAsync(
            client,
            workspace.Id,
            "correlation-supplied",
            "Supplied Correlation",
            supplied);
        using var suppliedJson = await ReadJsonAsync(suppliedResponse);
        Assert.Equal(
            supplied,
            suppliedJson.RootElement
                .GetProperty("meta")
                .GetProperty("correlationId")
                .GetString());
        Assert.Equal(
            supplied,
            suppliedResponse.Headers.GetValues("X-Correlation-ID").Single());

        using var generatedResponse = await SendCreateAsync(
            client,
            workspace.Id,
            "correlation-generated",
            "Generated Correlation");
        using var generatedJson = await ReadJsonAsync(generatedResponse);
        var generated = generatedJson.RootElement
            .GetProperty("meta")
            .GetProperty("correlationId")
            .GetString();
        Assert.NotNull(generated);
        Assert.True(Guid.TryParseExact(generated, "D", out var generatedId));
        Assert.Equal(7, generatedId.Version);

        await using var context = database.CreateContext(workspace.Id);
        Assert.Equal(
            1,
            await context.AuditEvents.CountAsync(
                audit => audit.CorrelationId == supplied,
                CancellationToken));
        Assert.Equal(
            1,
            await context.OutboxMessages.CountAsync(
                message => message.CorrelationId == supplied,
                CancellationToken));
        Assert.Equal(
            1,
            await context.AuditEvents.CountAsync(
                audit => audit.CorrelationId == generated,
                CancellationToken));
        Assert.Equal(
            1,
            await context.OutboxMessages.CountAsync(
                message => message.CorrelationId == generated,
                CancellationToken));
    }

    [Fact]
    public async Task Outbox_sequence_is_database_generated_and_read_only_after_save()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "sequence");
        await using var factory = new TraderProApiFactory(
            database.ConnectionString);
        using var client = factory.CreateClient();
        using var created = await SendCreateAsync(
            client,
            workspace.Id,
            "sequence-key",
            "Sequence Probe");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        await using var context = database.CreateContext(workspace.Id);
        var message = await context.OutboxMessages.SingleAsync(
            CancellationToken);
        Assert.True(message.Sequence > 0);
        var originalSequence = message.Sequence;
        context.Entry(message)
            .Property(candidate => candidate.Sequence)
            .CurrentValue = message.Sequence + 100;
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync(CancellationToken));

        await using var verification = database.CreateContext(workspace.Id);
        Assert.Equal(
            originalSequence,
            await verification.OutboxMessages
                .Select(candidate => candidate.Sequence)
                .SingleAsync(CancellationToken));
    }

    private static async Task<Workspace> CreateWorkspaceAsync(
        IsolatedPostgreSqlDatabase database,
        string code)
    {
        var workspace = Workspace.Create(
            code,
            $"{code} Workspace",
            UtcNow);
        await using var context = database.CreateContext(null);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync(CancellationToken);
        return workspace;
    }

    private static async Task<HttpResponseMessage> SendCreateAsync(
        HttpClient client,
        Guid workspaceId,
        string key,
        string name,
        string? correlationId = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/spikes/command-probes")
        {
            Content = JsonContent.Create(new { name }),
        };
        SetSpikeHeaders(request, workspaceId, key, correlationId);
        return await client.SendAsync(request, CancellationToken);
    }

    private static async Task<HttpResponseMessage> SendIncrementAsync(
        HttpClient client,
        Guid workspaceId,
        Guid probeId,
        string key,
        long expectedVersion,
        int delta)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/spikes/command-probes/{probeId:D}/increment")
        {
            Content = JsonContent.Create(new { delta }),
        };
        SetSpikeHeaders(request, workspaceId, key, null);
        request.Headers.Add(
            "X-Expected-Version",
            expectedVersion.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        return await client.SendAsync(request, CancellationToken);
    }

    private static async Task AssertInvalidBodyAsync(
        HttpClient client,
        Guid workspaceId,
        HttpContent? content)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/spikes/command-probes")
        {
            Content = content,
        };
        SetSpikeHeaders(request, workspaceId, "invalid-body-key", null);
        using var response = await client.SendAsync(request, CancellationToken);
        var raw = await response.Content.ReadAsStringAsync(CancellationToken);
        using var json = JsonDocument.Parse(raw);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("REQUEST_BODY_INVALID", ErrorCode(json));
        Assert.Equal(
            "Validation",
            json.RootElement
                .GetProperty("error")
                .GetProperty("category")
                .GetString());
        Assert.False(
            json.RootElement
                .GetProperty("error")
                .GetProperty("retryable")
                .GetBoolean());
        Assert.DoesNotContain("JsonException", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("stack", raw, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpResponseMessage> SendCursorAsync(
        HttpClient client,
        Guid workspaceId,
        long after,
        int limit)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/mobile/sync/events?after={after}&limit={limit}");
        request.Headers.Add(
            "X-TraderPro-Workspace-ID",
            workspaceId.ToString("D"));
        return await client.SendAsync(request, CancellationToken);
    }

    private static void SetSpikeHeaders(
        HttpRequestMessage request,
        Guid workspaceId,
        string key,
        string? correlationId)
    {
        request.Headers.Add(
            "X-TraderPro-Workspace-ID",
            workspaceId.ToString("D"));
        request.Headers.Add("Idempotency-Key", key);
        if (correlationId is not null)
        {
            request.Headers.Add("X-Correlation-ID", correlationId);
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

    private static Guid ResultId(JsonDocument document)
    {
        return document.RootElement
            .GetProperty("result")
            .GetProperty("id")
            .GetGuid();
    }

    private static string? IdempotencyStatus(JsonDocument document)
    {
        return document.RootElement
            .GetProperty("meta")
            .GetProperty("idempotencyStatus")
            .GetString();
    }

    private static string? ErrorCode(JsonDocument document)
    {
        return document.RootElement
            .GetProperty("error")
            .GetProperty("code")
            .GetString();
    }

    private static async Task CreateRejectingOutboxTriggerAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE OR REPLACE FUNCTION platform.reject_test_outbox_insert()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'injected outbox failure'
                    USING ERRCODE = '55000';
            END;
            $$;

            CREATE TRIGGER reject_test_outbox_insert
            BEFORE INSERT ON platform.outbox_messages
            FOR EACH ROW
            EXECUTE FUNCTION platform.reject_test_outbox_insert();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task DropRejectingOutboxTriggerAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP TRIGGER IF EXISTS reject_test_outbox_insert
                ON platform.outbox_messages;
            DROP FUNCTION IF EXISTS platform.reject_test_outbox_insert();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }
}
