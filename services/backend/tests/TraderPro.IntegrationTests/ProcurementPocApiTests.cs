using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.Poc;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ProcurementPocApiTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 29, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migration_creates_procurement_tables_constraints_indexes_and_immutable_triggers()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await database.OpenConnectionAsync();

        Assert.Equal(
            3,
            await ScalarAsync<long>(
                connection,
                """
                SELECT count(*)
                FROM information_schema.tables
                WHERE table_schema = 'procurement'
                  AND table_name IN (
                    'receiving_session_pocs',
                    'receiving_entry_pocs',
                    'receiving_finalization_pocs')
                """));
        Assert.True(
            await ScalarAsync<long>(
                connection,
                """
                SELECT count(*)
                FROM pg_constraint c
                JOIN pg_namespace n ON n.oid = c.connamespace
                WHERE n.nspname = 'procurement'
                """) >= 20);
        Assert.True(
            await ScalarAsync<long>(
                connection,
                """
                SELECT count(*)
                FROM pg_indexes
                WHERE schemaname = 'procurement'
                """) >= 10);
        Assert.Equal(
            4,
            await ScalarAsync<long>(
                connection,
                """
                SELECT count(*)
                FROM pg_trigger t
                JOIN pg_class c ON c.oid = t.tgrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'procurement'
                  AND NOT t.tgisinternal
                """));
    }

    [Fact]
    public async Task Migration_upgrades_task5_database_without_losing_platform_data()
    {
        const string task5Migration =
            "20260729125244_HardenCloudCommandAndEventCursorSpike";
        await using var database = await fixture.CreateDatabaseAsync(
            task5Migration);
        var workspace = await CreateWorkspaceAsync(database, "task6-upgrade");

        await using (var context = database.CreateContext(null))
        {
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(cancellationToken: CancellationToken);
        }

        await using var verification = database.CreateContext(null);
        Assert.True(
            await verification.Workspaces.AnyAsync(
                candidate => candidate.Id == workspace.Id,
                CancellationToken));
        await using var connection = await database.OpenConnectionAsync();
        Assert.Equal(
            3,
            await ScalarAsync<long>(
                connection,
                """
                SELECT count(*)
                FROM information_schema.tables
                WHERE table_schema = 'procurement'
                """));
    }

    [Fact]
    public async Task Disabled_poc_routes_are_404_before_context_and_task5_cursor_remains_available()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(
            database,
            "disabled");
        await using (var disabledFactory = new TraderProApiFactory(
                         database.ConnectionString,
                         procurementPocEnabled: false,
                         leaseMinutes: 0))
        {
            using var disabledClient = disabledFactory.CreateClient();
            using (var operations = await disabledClient.PostAsJsonAsync(
                       "/api/v1/mobile/sync/operations",
                       new { operations = Array.Empty<object>() },
                       CancellationToken))
            using (var sessions = await disabledClient.GetAsync(
                       "/api/v1/spikes/procurement-poc/receiving-sessions",
                       CancellationToken))
            using (var bootstrap = await disabledClient.PostAsync(
                       "/api/v1/spikes/procurement-poc/bootstrap",
                       null,
                       CancellationToken))
            {
                Assert.Equal(HttpStatusCode.NotFound, operations.StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, sessions.StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, bootstrap.StatusCode);
            }

            disabledClient.DefaultRequestHeaders.Add(
                "X-TraderPro-Workspace-ID",
                workspace.Id.ToString("D"));
            using var cursor = await disabledClient.GetAsync(
                "/api/v1/mobile/sync/events?after=0&limit=10",
                CancellationToken);
            Assert.Equal(HttpStatusCode.OK, cursor.StatusCode);
        }
    }

    [Fact]
    public async Task Bootstrap_is_idempotent_and_creates_two_active_devices()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        using var client = factory.CreateClient();
        using var first = await client.PostAsync(
            "/api/v1/spikes/procurement-poc/bootstrap",
            null,
            CancellationToken);
        using var second = await client.PostAsync(
            "/api/v1/spikes/procurement-poc/bootstrap",
            null,
            CancellationToken);
        using var firstJson = await ReadJsonAsync(first);
        using var secondJson = await ReadJsonAsync(second);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(
            firstJson.RootElement.GetProperty("result").GetRawText(),
            secondJson.RootElement.GetProperty("result").GetRawText());
        var setup = ParseBootstrap(firstJson);
        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(1, await context.Companies.CountAsync(CancellationToken));
        Assert.Equal(1, await context.Branches.CountAsync(CancellationToken));
        Assert.Equal(2, await context.Devices.CountAsync(CancellationToken));
        Assert.All(
            await context.Devices.ToListAsync(CancellationToken),
            device => Assert.Equal(DeviceStatus.Active, device.Status));
    }

    [Fact]
    public async Task Two_clients_complete_ordered_backend_poc_with_safe_retries_and_no_posting_effects()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        using var mobileB = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OwnerDeviceId);
        var sessionId = Uuid7.NewGuid();
        var startOperation = Uuid7.NewGuid();

        using var started = await SendOperationAsync(
            mobileA,
            Operation(
                startOperation,
                "StartReceivingSession",
                sessionId,
                1,
                null,
                JsonSerializer.Serialize(
                    new
                    {
                        operationId = startOperation,
                        localSessionId = sessionId,
                        temporaryReference = "TMP-POC-A",
                        createdAtDeviceUtc = UtcNow,
                    },
                    JsonOptions)));
        using var startedJson = await ReadJsonAsync(started);
        var startResult = SingleOperation(startedJson);
        var leaseId = startResult.GetProperty("leaseId").GetGuid();
        Assert.Equal("Accepted", startResult.GetProperty("resultStatus").GetString());
        Assert.Equal(
            sessionId,
            startResult.GetProperty("aggregateId").GetGuid());
        Assert.Equal(
            "RS-POC-000001",
            startResult.GetProperty("cloudReference").GetString());

        using (var rejectedWrite = await SendOperationAsync(
                   mobileB,
                   RecordOperation(
                       Uuid7.NewGuid(),
                       sessionId,
                       2,
                       leaseId,
                       "50.237")))
        using (var rejectedJson = await ReadJsonAsync(rejectedWrite))
        {
            Assert.Equal(
                "RECEIVING_POC_EDITOR_DEVICE_MISMATCH",
                SingleOperation(rejectedJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        using (var monitor = await mobileB.GetAsync(
                   $"/api/v1/spikes/procurement-poc/receiving-sessions/{sessionId:D}/live-view",
                   CancellationToken))
        {
            Assert.Equal(HttpStatusCode.OK, monitor.StatusCode);
        }

        var entryOperations = new List<Guid>();
        for (var sequence = 2; sequence <= 6; sequence++)
        {
            var operationId = Uuid7.NewGuid();
            entryOperations.Add(operationId);
            using var response = await SendOperationAsync(
                mobileA,
                RecordOperation(
                    operationId,
                    sessionId,
                    sequence,
                    leaseId,
                    "50.237"));
            using var json = await ReadJsonAsync(response);
            Assert.Equal(
                "Accepted",
                SingleOperation(json).GetProperty("resultStatus").GetString());
        }

        using (var live = await mobileB.GetAsync(
                   $"/api/v1/spikes/procurement-poc/receiving-sessions/{sessionId:D}/live-view",
                   CancellationToken))
        using (var liveJson = await ReadJsonAsync(live))
        {
            var view = liveJson.RootElement.GetProperty("result");
            Assert.Equal(5, view.GetProperty("entryCount").GetInt32());
            Assert.Equal(
                "251.150000",
                view.GetProperty("processedTotalWeightKg").GetString());
            Assert.Equal(
                5,
                view.GetProperty("recentEntries").GetArrayLength());
        }

        // Simulate a lost response by replaying entry 3.
        using (var replay = await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       entryOperations[1],
                       sessionId,
                       3,
                       leaseId,
                       "50.237")))
        using (var replayJson = await ReadJsonAsync(replay))
        {
            Assert.Equal(
                "PreviouslyProcessed",
                SingleOperation(replayJson)
                    .GetProperty("resultStatus")
                    .GetString());
        }

        var submitOperation = Uuid7.NewGuid();
        using (var submitted = await SendOperationAsync(
                   mobileA,
                   Operation(
                       submitOperation,
                       "SubmitReceivingSession",
                       sessionId,
                       7,
                       null,
                       JsonSerializer.Serialize(
                           new
                           {
                               operationId = submitOperation,
                               localSessionId = sessionId,
                           },
                           JsonOptions),
                       leaseId)))
        using (var submittedJson = await ReadJsonAsync(submitted))
        {
            Assert.Equal(
                7,
                SingleOperation(submittedJson)
                    .GetProperty("cloudAggregateVersion")
                    .GetInt64());
        }

        using (var afterSubmit = await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       Uuid7.NewGuid(),
                       sessionId,
                       8,
                       leaseId,
                       "1.000")))
        using (var afterSubmitJson = await ReadJsonAsync(afterSubmit))
        {
            Assert.Equal(
                "RECEIVING_POC_STATUS_INVALID",
                SingleOperation(afterSubmitJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        using (var editorApproval = await SendVersionedCommandAsync(
                   mobileA,
                   sessionId,
                   "approve",
                   "editor-approval",
                   7))
        using (var editorApprovalJson = await ReadJsonAsync(editorApproval))
        {
            Assert.Equal(HttpStatusCode.Conflict, editorApproval.StatusCode);
            Assert.Equal(
                "RECEIVING_POC_OWNER_DEVICE_REQUIRED",
                ErrorCode(editorApprovalJson));
        }

        using (var approved = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "approve",
                   "owner-approval",
                   7))
        {
            Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        }

        using (var finalized = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "finalize",
                   "owner-finalize",
                   8))
        using (var replayedFinalize = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "finalize",
                   "owner-finalize",
                   8))
        using (var replayJson = await ReadJsonAsync(replayedFinalize))
        {
            Assert.Equal(HttpStatusCode.OK, finalized.StatusCode);
            Assert.Equal(HttpStatusCode.OK, replayedFinalize.StatusCode);
            Assert.Equal(
                "PreviouslyProcessed",
                replayJson.RootElement
                    .GetProperty("meta")
                    .GetProperty("idempotencyStatus")
                    .GetString());
        }

        using var cursor = await mobileB.GetAsync(
            "/api/v1/mobile/sync/events?after=0&limit=50",
            CancellationToken);
        using var cursorJson = await ReadJsonAsync(cursor);
        var eventTypes = cursorJson.RootElement
            .GetProperty("events")
            .EnumerateArray()
            .Select(item => item.GetProperty("eventType").GetString())
            .ToArray();
        Assert.Equal(
            new[]
            {
                "ReceivingSessionPocStarted",
                "ReceivingEntryPocAccepted",
                "ReceivingEntryPocAccepted",
                "ReceivingEntryPocAccepted",
                "ReceivingEntryPocAccepted",
                "ReceivingEntryPocAccepted",
                "ReceivingSessionPocSubmitted",
                "ReceivingSessionPocApproved",
                "ReceivingSessionPocFinalized",
            },
            eventTypes);
        var startedPayload = cursorJson.RootElement
            .GetProperty("events")[0]
            .GetProperty("payload");
        Assert.False(startedPayload.TryGetProperty("leaseId", out _));

        var firstPage = await mobileB.GetFromJsonAsync<JsonElement>(
            "/api/v1/mobile/sync/events?after=0&limit=3",
            JsonOptions,
            CancellationToken);
        var firstEvents = firstPage.GetProperty("events");
        Assert.Equal(3, firstEvents.GetArrayLength());
        Assert.True(firstPage.GetProperty("hasMore").GetBoolean());
        var nextCursor = firstPage.GetProperty("nextCursor").GetInt64();
        var resumed = await mobileB.GetFromJsonAsync<JsonElement>(
            $"/api/v1/mobile/sync/events?after={nextCursor}&limit=50",
            JsonOptions,
            CancellationToken);
        Assert.Equal(
            6,
            resumed.GetProperty("events").GetArrayLength());

        await using var context = database.CreateContext(setup.WorkspaceId);
        var session = await context.ReceivingSessionPocs.SingleAsync(
            CancellationToken);
        Assert.Equal(ReceivingPocStatus.Finalized, session.Status);
        Assert.Equal(5, session.EntryCount);
        Assert.Equal(251.150000m, session.ProcessedTotalWeightKg);
        Assert.Equal(9, session.Version);
        Assert.Equal(
            5,
            await context.ReceivingEntryPocs.CountAsync(CancellationToken));
        var finalization = await context.ReceivingFinalizationPocs.SingleAsync(
            CancellationToken);
        Assert.Equal(5, finalization.FinalEntryCount);
        Assert.Equal(251.150000m, finalization.FinalProcessedTotalWeightKg);
        Assert.Equal(9, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(9, await context.OutboxMessages.CountAsync(CancellationToken));
        Assert.All(
            await context.OutboxMessages.ToListAsync(CancellationToken),
            message =>
            {
                Assert.Equal(OutboxMessageStatus.Pending, message.Status);
                Assert.Equal(0, message.AttemptCount);
            });
        Assert.Equal(
            new[]
            {
                "Procurement.Poc.ReceivingEntry.Accepted",
                "Procurement.Poc.ReceivingSession.Approved",
                "Procurement.Poc.ReceivingSession.Finalized",
                "Procurement.Poc.ReceivingSession.Started",
                "Procurement.Poc.ReceivingSession.Submitted",
            },
            (await context.AuditEvents
                .Select(item => item.Action)
                .Distinct()
                .OrderBy(item => item)
                .ToArrayAsync(CancellationToken)));
        Assert.Equal(
            eventTypes,
            (await context.OutboxMessages
                .OrderBy(item => item.Sequence)
                .Select(item => item.EventType)
                .ToArrayAsync(CancellationToken)));
    }

    [Fact]
    public async Task Mobile_idempotency_is_device_bound_and_uses_one_operation_scope()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        using var mobileB = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OwnerDeviceId);
        var sessionId = Uuid7.NewGuid();
        var startOperationId = Uuid7.NewGuid();
        var startPayload = JsonSerializer.Serialize(
            new
            {
                operationId = startOperationId,
                localSessionId = sessionId,
                temporaryReference = "TMP-DEVICE-BOUND",
                createdAtDeviceUtc = UtcNow,
            },
            JsonOptions);
        var startOperation = Operation(
            startOperationId,
            "StartReceivingSession",
            sessionId,
            1,
            null,
            startPayload);

        using (var unexpectedLease = await SendOperationAsync(
                   mobileA,
                   startOperation with { LeaseId = Uuid7.NewGuid() }))
        using (var unexpectedLeaseJson = await ReadJsonAsync(unexpectedLease))
        {
            Assert.Equal(
                "RECEIVING_POC_LEASE_UNEXPECTED",
                SingleOperation(unexpectedLeaseJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        Guid leaseId;
        using (var started = await SendOperationAsync(mobileA, startOperation))
        using (var startedJson = await ReadJsonAsync(started))
        {
            leaseId = SingleOperation(startedJson)
                .GetProperty("leaseId")
                .GetGuid();
        }

        using (var sameDeviceReplay = await SendOperationAsync(
                   mobileA,
                   startOperation))
        using (var sameDeviceReplayJson = await ReadJsonAsync(sameDeviceReplay))
        {
            Assert.Equal(
                "PreviouslyProcessed",
                SingleOperation(sameDeviceReplayJson)
                    .GetProperty("resultStatus")
                    .GetString());
            Assert.Equal(
                leaseId,
                SingleOperation(sameDeviceReplayJson)
                    .GetProperty("leaseId")
                    .GetGuid());
        }

        using (var otherDeviceReplay = await SendOperationAsync(
                   mobileB,
                   startOperation))
        using (var otherDeviceReplayJson = await ReadJsonAsync(otherDeviceReplay))
        {
            var result = SingleOperation(otherDeviceReplayJson);
            Assert.Equal(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                result.GetProperty("errorCode").GetString());
            Assert.Equal(JsonValueKind.Null, result.GetProperty("leaseId").ValueKind);
        }

        var typeReuse = RecordOperation(
            startOperationId,
            sessionId,
            2,
            leaseId,
            "1.000");
        using (var reused = await SendOperationAsync(mobileA, typeReuse))
        using (var reusedJson = await ReadJsonAsync(reused))
        {
            Assert.Equal(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                SingleOperation(reusedJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        var cloudMismatch = RecordOperation(
            Uuid7.NewGuid(),
            sessionId,
            2,
            leaseId,
            "1.000",
            Uuid7.NewGuid());
        using (var mismatch = await SendOperationAsync(mobileA, cloudMismatch))
        using (var mismatchJson = await ReadJsonAsync(mismatch))
        {
            Assert.Equal(
                "SYNC_OPERATION_BATCH_INVALID",
                SingleOperation(mismatchJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        var entryOperationId = Uuid7.NewGuid();
        var entryOperation = RecordOperation(
            entryOperationId,
            sessionId,
            2,
            leaseId,
            "50.237");
        using (var payload = JsonDocument.Parse(entryOperation.PayloadJson))
        {
            var root = payload.RootElement;
            Assert.False(root.TryGetProperty("leaseId", out _));
            Assert.Equal(entryOperationId, root.GetProperty("operationId").GetGuid());
            Assert.Equal(sessionId, root.GetProperty("localSessionId").GetGuid());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("cloudSessionId").ValueKind);
            Assert.Equal(2, root.GetProperty("localSequence").GetInt64());
        }

        using (var missingLease = await SendOperationAsync(
                   mobileA,
                   entryOperation with { LeaseId = null }))
        using (var missingLeaseJson = await ReadJsonAsync(missingLease))
        {
            Assert.Equal(
                "RECEIVING_POC_LEASE_REQUIRED",
                SingleOperation(missingLeaseJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        using (var accepted = await SendOperationAsync(mobileA, entryOperation))
        using (var acceptedJson = await ReadJsonAsync(accepted))
        {
            Assert.Equal(
                "Accepted",
                SingleOperation(acceptedJson)
                    .GetProperty("resultStatus")
                    .GetString());
        }

        using (var sameDeviceReplay = await SendOperationAsync(
                   mobileA,
                   entryOperation))
        using (var sameDeviceReplayJson = await ReadJsonAsync(sameDeviceReplay))
        {
            Assert.Equal(
                "PreviouslyProcessed",
                SingleOperation(sameDeviceReplayJson)
                    .GetProperty("resultStatus")
                    .GetString());
        }

        using (var otherDeviceReplay = await SendOperationAsync(
                   mobileB,
                   entryOperation))
        using (var otherDeviceReplayJson = await ReadJsonAsync(otherDeviceReplay))
        {
            Assert.Equal(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                SingleOperation(otherDeviceReplayJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        var submitOperationId = Uuid7.NewGuid();
        using (var submitted = await SendOperationAsync(
                   mobileA,
                   Operation(
                       submitOperationId,
                       "SubmitReceivingSession",
                       sessionId,
                       3,
                       null,
                       JsonSerializer.Serialize(
                           new
                           {
                               operationId = submitOperationId,
                               localSessionId = sessionId,
                           },
                           JsonOptions),
                       leaseId)))
        {
            Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        }

        using (var staleApproval = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "approve",
                   "device-bound-approve-stale",
                   2))
        using (var staleApprovalJson = await ReadJsonAsync(staleApproval))
        {
            Assert.Equal(HttpStatusCode.Conflict, staleApproval.StatusCode);
            Assert.Equal(
                "RECEIVING_POC_VERSION_CONFLICT",
                ErrorCode(staleApprovalJson));
        }

        using (var approved = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "approve",
                   "device-bound-approve",
                   3))
        using (var approvedReplay = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "approve",
                   "device-bound-approve",
                   3))
        using (var approvedReplayJson = await ReadJsonAsync(approvedReplay))
        {
            Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
            Assert.Equal(
                "PreviouslyProcessed",
                approvedReplayJson.RootElement
                    .GetProperty("meta")
                    .GetProperty("idempotencyStatus")
                    .GetString());
        }

        using (var approvalReplayByA = await SendVersionedCommandAsync(
                   mobileA,
                   sessionId,
                   "approve",
                   "device-bound-approve",
                   3))
        using (var approvalReplayByAJson = await ReadJsonAsync(
                   approvalReplayByA))
        {
            Assert.Equal(HttpStatusCode.Conflict, approvalReplayByA.StatusCode);
            Assert.Equal(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                ErrorCode(approvalReplayByAJson));
        }

        using (var finalized = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "finalize",
                   "device-bound-finalize",
                   4))
        using (var finalizedReplay = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "finalize",
                   "device-bound-finalize",
                   4))
        using (var finalizedReplayJson = await ReadJsonAsync(finalizedReplay))
        {
            Assert.Equal(HttpStatusCode.OK, finalized.StatusCode);
            Assert.Equal(
                "PreviouslyProcessed",
                finalizedReplayJson.RootElement
                    .GetProperty("meta")
                    .GetProperty("idempotencyStatus")
                    .GetString());
        }

        using (var finalizationReplayByA = await SendVersionedCommandAsync(
                   mobileA,
                   sessionId,
                   "finalize",
                   "device-bound-finalize",
                   4))
        using (var finalizationReplayByAJson = await ReadJsonAsync(
                   finalizationReplayByA))
        {
            Assert.Equal(
                HttpStatusCode.Conflict,
                finalizationReplayByA.StatusCode);
            Assert.Equal(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                ErrorCode(finalizationReplayByAJson));
        }

        using (var differentFinalization = await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "finalize",
                   "device-bound-finalize-different",
                   5))
        using (var differentFinalizationJson = await ReadJsonAsync(
                   differentFinalization))
        {
            Assert.Equal(
                HttpStatusCode.Conflict,
                differentFinalization.StatusCode);
            Assert.Equal(
                "RECEIVING_POC_ALREADY_FINALIZED",
                ErrorCode(differentFinalizationJson));
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(
            3,
            await context.IdempotencyRecords.CountAsync(
                item =>
                    item.CommandType ==
                    "Procurement.Poc.MobileSyncOperation",
                CancellationToken));
        Assert.Equal(
            1,
            await context.ReceivingEntryPocs.CountAsync(CancellationToken));
        Assert.Equal(
            1,
            await context.ReceivingFinalizationPocs.CountAsync(
                CancellationToken));
    }

    [Fact]
    public async Task Mixed_aggregate_batch_commits_earlier_work_blocks_only_failed_aggregate()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        var sessionAId = Uuid7.NewGuid();
        var sessionBId = Uuid7.NewGuid();
        var sessionA = await StartAsync(mobileA, sessionAId);
        var sessionB = await StartAsync(mobileA, sessionBId);
        var acceptedA = RecordOperation(
            Uuid7.NewGuid(),
            sessionAId,
            2,
            sessionA.LeaseId,
            "10.129");
        var malformedA = Operation(
            Uuid7.NewGuid(),
            "RecordReceivingEntry",
            sessionAId,
            3,
            null,
            "{",
            sessionA.LeaseId);
        var blockedA = RecordOperation(
            Uuid7.NewGuid(),
            sessionAId,
            4,
            sessionA.LeaseId,
            "1.000");
        var acceptedB = RecordOperation(
            Uuid7.NewGuid(),
            sessionBId,
            2,
            sessionB.LeaseId,
            "20.239");

        using var response = await SendOperationsAsync(
            mobileA,
            acceptedA,
            malformedA,
            blockedA,
            acceptedB);
        using var json = await ReadJsonAsync(response);
        var results = json.RootElement
            .GetProperty("result")
            .GetProperty("operations");

        Assert.Equal("Accepted", results[0].GetProperty("resultStatus").GetString());
        Assert.Equal(
            "SYNC_OPERATION_BATCH_INVALID",
            results[1].GetProperty("errorCode").GetString());
        Assert.Equal(
            "NeedsAttention",
            results[2].GetProperty("resultStatus").GetString());
        Assert.Equal(
            "Accepted",
            results[3].GetProperty("resultStatus").GetString());

        await using var context = database.CreateContext(setup.WorkspaceId);
        var storedA = await context.ReceivingSessionPocs.SingleAsync(
            item => item.Id == sessionAId,
            CancellationToken);
        var storedB = await context.ReceivingSessionPocs.SingleAsync(
            item => item.Id == sessionBId,
            CancellationToken);
        Assert.Equal(1, storedA.EntryCount);
        Assert.Equal(10.120000m, storedA.ProcessedTotalWeightKg);
        Assert.Equal(1, storedB.EntryCount);
        Assert.Equal(20.230000m, storedB.ProcessedTotalWeightKg);
        Assert.Equal(
            2,
            await context.ReceivingEntryPocs.CountAsync(CancellationToken));
        Assert.Equal(4, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(4, await context.OutboxMessages.CountAsync(CancellationToken));
        Assert.Equal(
            4,
            await context.IdempotencyRecords.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task Total_capacity_failure_is_stable_and_rolls_back_every_command_effect()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        var sessionId = Uuid7.NewGuid();
        var start = await StartAsync(mobileA, sessionId);
        using (var accepted = await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       Uuid7.NewGuid(),
                       sessionId,
                       2,
                       start.LeaseId,
                       "99999999999999.000")))
        {
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }

        var rejectedOperationId = Uuid7.NewGuid();
        using (var rejected = await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       rejectedOperationId,
                       sessionId,
                       3,
                       start.LeaseId,
                       "1.000")))
        using (var rejectedJson = await ReadJsonAsync(rejected))
        {
            Assert.Equal(
                "RECEIVING_POC_TOTAL_WEIGHT_EXCEEDED",
                SingleOperation(rejectedJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        var session = await context.ReceivingSessionPocs.SingleAsync(
            CancellationToken);
        Assert.Equal(1, session.EntryCount);
        Assert.Equal(2, session.Version);
        Assert.Equal(
            99999999999999.000000m,
            session.ProcessedTotalWeightKg);
        Assert.Equal(
            1,
            await context.ReceivingEntryPocs.CountAsync(CancellationToken));
        Assert.Equal(2, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(2, await context.OutboxMessages.CountAsync(CancellationToken));
        Assert.Equal(
            2,
            await context.IdempotencyRecords.CountAsync(CancellationToken));
        Assert.False(
            await context.IdempotencyRecords.AnyAsync(
                item =>
                    item.IdempotencyKey ==
                    rejectedOperationId.ToString("D"),
                CancellationToken));
    }

    [Fact]
    public async Task PostgreSql_rejects_session_identity_updates_invalid_state_and_delete()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        var sessionId = Uuid7.NewGuid();
        _ = await StartAsync(mobileA, sessionId);

        var editorFailure = await ExecuteRejectedSqlAsync(
            database,
            """
            UPDATE procurement.receiving_session_pocs
            SET editor_device_id = @editor
            WHERE id = @id
            """,
            ("editor", setup.OwnerDeviceId),
            ("id", sessionId));
        Assert.Equal("55000", editorFailure.SqlState);

        var workspaceFailure = await ExecuteRejectedSqlAsync(
            database,
            """
            UPDATE procurement.receiving_session_pocs
            SET workspace_id = @workspace
            WHERE id = @id
            """,
            ("workspace", Uuid7.NewGuid()),
            ("id", sessionId));
        Assert.Equal("55000", workspaceFailure.SqlState);

        var stateFailure = await ExecuteRejectedSqlAsync(
            database,
            """
            UPDATE procurement.receiving_session_pocs
            SET status = 2
            WHERE id = @id
            """,
            ("id", sessionId));
        Assert.Equal(PostgresErrorCodes.CheckViolation, stateFailure.SqlState);
        Assert.Equal(
            "ck_receiving_session_pocs_state_shape",
            stateFailure.ConstraintName);

        var deleteFailure = await ExecuteRejectedSqlAsync(
            database,
            """
            DELETE FROM procurement.receiving_session_pocs
            WHERE id = @id
            """,
            ("id", sessionId));
        Assert.Equal("55000", deleteFailure.SqlState);

        await using var context = database.CreateContext(setup.WorkspaceId);
        var entityType = context.Model.FindEntityType(
            typeof(ReceivingSessionPoc));
        Assert.NotNull(entityType);
        Assert.Equal(
            Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw,
            entityType.FindProperty(nameof(ReceivingSessionPoc.WorkspaceId))!
                .GetAfterSaveBehavior());
        Assert.Equal(
            Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw,
            entityType.FindProperty(nameof(ReceivingSessionPoc.EditorDeviceId))!
                .GetAfterSaveBehavior());
        Assert.True(
            await context.ReceivingSessionPocs.AnyAsync(
                item => item.Id == sessionId,
                CancellationToken));
    }

    [Fact]
    public async Task Payload_sequence_weight_and_workspace_failures_write_nothing()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        var sessionId = Uuid7.NewGuid();
        var start = await StartAsync(mobileA, sessionId);
        var leaseId = start.LeaseId;

        var acceptedOperation = Uuid7.NewGuid();
        using (var accepted = await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       acceptedOperation,
                       sessionId,
                       2,
                       leaseId,
                       "10.129")))
        {
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }

        using (var conflict = await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       acceptedOperation,
                       sessionId,
                       2,
                       leaseId,
                       "11.129")))
        using (var conflictJson = await ReadJsonAsync(conflict))
        {
            Assert.Equal(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                SingleOperation(conflictJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        using (var lower = await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       Uuid7.NewGuid(),
                       sessionId,
                       2,
                       leaseId,
                       "1.000")))
        using (var lowerJson = await ReadJsonAsync(lower))
        {
            Assert.Equal(
                "RECEIVING_POC_SEQUENCE_CONFLICT",
                SingleOperation(lowerJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        using (var gap = await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       Uuid7.NewGuid(),
                       sessionId,
                       4,
                       leaseId,
                       "1.000")))
        using (var gapJson = await ReadJsonAsync(gap))
        {
            Assert.Equal(
                "RECEIVING_POC_SEQUENCE_GAP",
                SingleOperation(gapJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        var mismatchPayload = JsonSerializer.Serialize(
            new
            {
                productReference = "H Aman",
                bagTypeReference = "Plastic",
                bagCount = 1,
                rawWeightKg = "50.237",
                processedWeightKg = "50.240000",
                displayWeightKg = "50.24",
                decimalPlaces = 2,
                processingMethod = "Floor",
                weightSource = "TestScale",
                capturedAtDeviceUtc = UtcNow,
            },
            JsonOptions);
        using (var mismatch = await SendOperationAsync(
                   mobileA,
                   Operation(
                       Uuid7.NewGuid(),
                       "RecordReceivingEntry",
                       sessionId,
                       3,
                       null,
                       mismatchPayload,
                       leaseId)))
        using (var mismatchJson = await ReadJsonAsync(mismatch))
        {
            Assert.Equal(
                "RECEIVING_POC_WEIGHT_PROCESSING_MISMATCH",
                SingleOperation(mismatchJson)
                    .GetProperty("errorCode")
                    .GetString());
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        var session = await context.ReceivingSessionPocs.SingleAsync(
            CancellationToken);
        Assert.Equal(1, session.EntryCount);
        Assert.Equal(10.120000m, session.ProcessedTotalWeightKg);
        Assert.Equal(2, session.Version);
        Assert.Equal(2, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(2, await context.OutboxMessages.CountAsync(CancellationToken));
        Assert.Equal(
            2,
            await context.IdempotencyRecords.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task Lease_expiry_heartbeat_and_device_separation_use_server_clock()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        BootstrapSetup setup;
        Guid sessionId = Uuid7.NewGuid();
        StartResult start;
        await using (var initialFactory = PocFactory(database, UtcNow))
        {
            setup = await BootstrapAsync(initialFactory);
            using var mobileA = CreateDeviceClient(
                initialFactory,
                setup.WorkspaceId,
                setup.OperatorDeviceId);
            start = await StartAsync(mobileA, sessionId);
        }

        await using (var beforeExpiryFactory = PocFactory(
                         database,
                         UtcNow.AddMinutes(4)))
        {
            using var mobileB = CreateDeviceClient(
                beforeExpiryFactory,
                setup.WorkspaceId,
                setup.OwnerDeviceId);
            using var mismatch = await SendHeartbeatAsync(
                mobileB,
                sessionId,
                start.LeaseId,
                "wrong-device-heartbeat");
            using var mismatchJson = await ReadJsonAsync(mismatch);
            Assert.Equal(
                "RECEIVING_POC_EDITOR_DEVICE_MISMATCH",
                ErrorCode(mismatchJson));

            using var mobileA = CreateDeviceClient(
                beforeExpiryFactory,
                setup.WorkspaceId,
                setup.OperatorDeviceId);
            using var renewed = await SendHeartbeatAsync(
                mobileA,
                sessionId,
                start.LeaseId,
                "renew-heartbeat");
            Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        }

        await using (var renewedFactory = PocFactory(
                         database,
                         UtcNow.AddMinutes(6)))
        {
            using var mobileA = CreateDeviceClient(
                renewedFactory,
                setup.WorkspaceId,
                setup.OperatorDeviceId);
            using var accepted = await SendOperationAsync(
                mobileA,
                RecordOperation(
                    Uuid7.NewGuid(),
                    sessionId,
                    2,
                    start.LeaseId,
                    "1.000"));
            using var acceptedJson = await ReadJsonAsync(accepted);
            Assert.Equal(
                "Accepted",
                SingleOperation(acceptedJson)
                    .GetProperty("resultStatus")
                    .GetString());
        }

        var expiredSessionId = Uuid7.NewGuid();
        StartResult expiredStart;
        await using (var initialFactory = PocFactory(database, UtcNow))
        {
            using var mobileA = CreateDeviceClient(
                initialFactory,
                setup.WorkspaceId,
                setup.OperatorDeviceId);
            expiredStart = await StartAsync(mobileA, expiredSessionId);
        }

        await using (var expiredFactory = PocFactory(
                         database,
                         UtcNow.AddMinutes(6)))
        {
            using var mobileA = CreateDeviceClient(
                expiredFactory,
                setup.WorkspaceId,
                setup.OperatorDeviceId);
            using var expired = await SendOperationAsync(
                mobileA,
                RecordOperation(
                    Uuid7.NewGuid(),
                    expiredSessionId,
                    2,
                    expiredStart.LeaseId,
                    "1.000"));
            using var expiredJson = await ReadJsonAsync(expired);
            Assert.Equal(
                "RECEIVING_POC_LEASE_EXPIRED",
                SingleOperation(expiredJson)
                    .GetProperty("errorCode")
                    .GetString());
        }
    }

    [Fact]
    public async Task Entry_outbox_failure_rolls_back_entry_session_audit_idempotency_and_cursor()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        var sessionId = Uuid7.NewGuid();
        var start = await StartAsync(mobileA, sessionId);
        await CreateRejectingPocOutboxTriggerAsync(database);

        var operationId = Uuid7.NewGuid();
        try
        {
            using var response = await SendOperationAsync(
                mobileA,
                RecordOperation(
                    operationId,
                    sessionId,
                    2,
                    start.LeaseId,
                    "2.000"));
            using var responseJson = await ReadJsonAsync(response);
            Assert.Equal(
                "TEMPORARY_COMMAND_FAILURE",
                SingleOperation(responseJson)
                    .GetProperty("errorCode")
                    .GetString());
        }
        finally
        {
            await DropRejectingPocOutboxTriggerAsync(database);
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        var session = await context.ReceivingSessionPocs.SingleAsync(
            CancellationToken);
        Assert.Equal(0, session.EntryCount);
        Assert.Equal(1, session.Version);
        Assert.Empty(await context.ReceivingEntryPocs.ToListAsync(CancellationToken));
        Assert.Equal(1, await context.AuditEvents.CountAsync(CancellationToken));
        Assert.Equal(1, await context.OutboxMessages.CountAsync(CancellationToken));
        Assert.False(
            await context.IdempotencyRecords.AnyAsync(
                record =>
                    record.IdempotencyKey == operationId.ToString("D"),
                CancellationToken));
    }

    [Fact]
    public async Task Concurrent_identical_finalization_creates_one_completion()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        using var mobileB = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OwnerDeviceId);
        var sessionId = Uuid7.NewGuid();
        var start = await StartAsync(mobileA, sessionId);
        using (await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       Uuid7.NewGuid(),
                       sessionId,
                       2,
                       start.LeaseId,
                       "2.000")))
        {
        }

        using (await SendOperationAsync(
                   mobileA,
                   Operation(
                       Uuid7.NewGuid(),
                       "SubmitReceivingSession",
                       sessionId,
                       3,
                       null,
                       JsonSerializer.Serialize(
                           new { },
                           JsonOptions),
                       start.LeaseId)))
        {
        }

        using (await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "approve",
                   "approve-concurrent",
                   3))
        {
        }

        var firstTask = SendVersionedCommandAsync(
            mobileB,
            sessionId,
            "finalize",
            "same-finalization-key",
            4);
        var secondTask = SendVersionedCommandAsync(
            mobileB,
            sessionId,
            "finalize",
            "same-finalization-key",
            4);
        var responses = await Task.WhenAll(firstTask, secondTask);
        try
        {
            Assert.All(
                responses,
                response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
            var statuses = new List<string?>();
            foreach (var response in responses)
            {
                using var json = await ReadJsonAsync(response);
                statuses.Add(
                    json.RootElement
                        .GetProperty("meta")
                        .GetProperty("idempotencyStatus")
                        .GetString());
            }

            Assert.Equal(
                new[] { "PreviouslyProcessed", "Processed" },
                statuses.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(
            1,
            await context.ReceivingFinalizationPocs.CountAsync(CancellationToken));
        Assert.Equal(
            1,
            await context.AuditEvents.CountAsync(
                item =>
                    item.Action ==
                    "Procurement.Poc.ReceivingSession.Finalized",
                CancellationToken));
        Assert.Equal(
            1,
            await context.OutboxMessages.CountAsync(
                item => item.EventType == "ReceivingSessionPocFinalized",
                CancellationToken));
    }

    [Fact]
    public async Task Finalization_outbox_failure_rolls_back_state_record_audit_and_idempotency()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        using var mobileB = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OwnerDeviceId);
        var sessionId = Uuid7.NewGuid();
        var start = await StartAsync(mobileA, sessionId);
        using (await SendOperationAsync(
                   mobileA,
                   RecordOperation(
                       Uuid7.NewGuid(),
                       sessionId,
                       2,
                       start.LeaseId,
                       "2.000")))
        {
        }

        using (await SendOperationAsync(
                   mobileA,
                   Operation(
                       Uuid7.NewGuid(),
                       "SubmitReceivingSession",
                       sessionId,
                       3,
                       null,
                       JsonSerializer.Serialize(
                           new { },
                           JsonOptions),
                       start.LeaseId)))
        {
        }

        using (await SendVersionedCommandAsync(
                   mobileB,
                   sessionId,
                   "approve",
                   "approve-rollback",
                   3))
        {
        }

        await CreateRejectingPocOutboxTriggerAsync(database);
        try
        {
            using var response = await SendVersionedCommandAsync(
                mobileB,
                sessionId,
                "finalize",
                "finalize-rollback",
                4);
            using var json = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("TEMPORARY_COMMAND_FAILURE", ErrorCode(json));
        }
        finally
        {
            await DropRejectingPocOutboxTriggerAsync(database);
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        var session = await context.ReceivingSessionPocs.SingleAsync(
            CancellationToken);
        Assert.Equal(ReceivingPocStatus.Approved, session.Status);
        Assert.Equal(4, session.Version);
        Assert.Empty(
            await context.ReceivingFinalizationPocs.ToListAsync(
                CancellationToken));
        Assert.Equal(
            0,
            await context.AuditEvents.CountAsync(
                item =>
                    item.Action ==
                    "Procurement.Poc.ReceivingSession.Finalized",
                CancellationToken));
        Assert.Equal(
            0,
            await context.OutboxMessages.CountAsync(
                item => item.EventType == "ReceivingSessionPocFinalized",
                CancellationToken));
        Assert.False(
            await context.IdempotencyRecords.AnyAsync(
                item => item.IdempotencyKey == "finalize-rollback",
                CancellationToken));
    }

    [Fact]
    public async Task Device_and_workspace_context_fail_closed_without_cross_request_leakage()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = PocFactory(database);
        var setup = await BootstrapAsync(factory);
        using var valid = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OwnerDeviceId);
        using (var list = await valid.GetAsync(
                   "/api/v1/spikes/procurement-poc/receiving-sessions",
                   CancellationToken))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        }

        using var missing = factory.CreateClient();
        using (var response = await missing.GetAsync(
                   "/api/v1/spikes/procurement-poc/receiving-sessions",
                   CancellationToken))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("DEVICE_CONTEXT_REQUIRED", ErrorCode(json));
        }

        var other = await CreateWorkspaceAndDeviceAsync(database, "other");
        using var mismatch = factory.CreateClient();
        mismatch.DefaultRequestHeaders.Add(
            "X-TraderPro-Workspace-ID",
            other.WorkspaceId.ToString("D"));
        mismatch.DefaultRequestHeaders.Add(
            "X-TraderPro-Device-ID",
            setup.OwnerDeviceId.ToString("D"));
        using (var response = await mismatch.GetAsync(
                   "/api/v1/spikes/procurement-poc/receiving-sessions",
                   CancellationToken))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("DEVICE_WORKSPACE_MISMATCH", ErrorCode(json));
        }

        using var mobileA = CreateDeviceClient(
            factory,
            setup.WorkspaceId,
            setup.OperatorDeviceId);
        var sessionId = Uuid7.NewGuid();
        _ = await StartAsync(mobileA, sessionId);
        using var otherWorkspace = CreateDeviceClient(
            factory,
            other.WorkspaceId,
            other.DeviceId);
        using (var response = await otherWorkspace.GetAsync(
                   $"/api/v1/spikes/procurement-poc/receiving-sessions/{sessionId:D}/live-view",
                   CancellationToken))
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    private static TraderProApiFactory PocFactory(
        IsolatedPostgreSqlDatabase database,
        DateTimeOffset? utcNow = null)
    {
        return new TraderProApiFactory(
            database.ConnectionString,
            procurementPocEnabled: true,
            utcNow: utcNow);
    }

    private static HttpClient CreateDeviceClient(
        TraderProApiFactory factory,
        Guid workspaceId,
        Guid deviceId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-TraderPro-Workspace-ID",
            workspaceId.ToString("D"));
        client.DefaultRequestHeaders.Add(
            "X-TraderPro-Device-ID",
            deviceId.ToString("D"));
        return client;
    }

    private static async Task<BootstrapSetup> BootstrapAsync(
        TraderProApiFactory factory)
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            "/api/v1/spikes/procurement-poc/bootstrap",
            null,
            CancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                await response.Content.ReadAsStringAsync(CancellationToken));
        }
        using var json = await ReadJsonAsync(response);
        return ParseBootstrap(json);
    }

    private static BootstrapSetup ParseBootstrap(JsonDocument json)
    {
        var result = json.RootElement.GetProperty("result");
        return new BootstrapSetup(
            result.GetProperty("workspaceId").GetGuid(),
            result.GetProperty("operatorDeviceId").GetGuid(),
            result.GetProperty("ownerDeviceId").GetGuid());
    }

    private static async Task<StartResult> StartAsync(
        HttpClient client,
        Guid sessionId)
    {
        var operationId = Uuid7.NewGuid();
        using var response = await SendOperationAsync(
            client,
            Operation(
                operationId,
                "StartReceivingSession",
                sessionId,
                1,
                null,
                JsonSerializer.Serialize(
                    new
                    {
                        operationId,
                        localSessionId = sessionId,
                        temporaryReference = $"TMP-{sessionId:N}",
                        createdAtDeviceUtc = UtcNow,
                    },
                    JsonOptions)));
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                await response.Content.ReadAsStringAsync(CancellationToken));
        }
        using var json = await ReadJsonAsync(response);
        var result = SingleOperation(json);
        Assert.Equal("Accepted", result.GetProperty("resultStatus").GetString());
        return new StartResult(result.GetProperty("leaseId").GetGuid());
    }

    private static MobileOperation RecordOperation(
        Guid operationId,
        Guid sessionId,
        long sequence,
        Guid leaseId,
        string rawWeight,
        Guid? cloudSessionId = null)
    {
        var processed = decimal.Parse(
            rawWeight,
            System.Globalization.CultureInfo.InvariantCulture);
        var floored = Math.Floor(processed * 100m) / 100m;
        var payload = JsonSerializer.Serialize(
            new
            {
                operationId,
                localSessionId = sessionId,
                cloudSessionId,
                localSequence = sequence,
                productReference = "H Aman",
                bagTypeReference = "Plastic",
                bagCount = 1,
                rawWeightKg = rawWeight,
                processedWeightKg = floored.ToString(
                    "0.000000",
                    System.Globalization.CultureInfo.InvariantCulture),
                displayWeightKg = floored.ToString(
                    "0.00",
                    System.Globalization.CultureInfo.InvariantCulture),
                decimalPlaces = 2,
                processingMethod = "Floor",
                weightSource = "TestScale",
                capturedAtDeviceUtc = UtcNow,
            },
            JsonOptions);
        return Operation(
            operationId,
            "RecordReceivingEntry",
            sessionId,
            sequence,
            null,
            payload,
            leaseId);
    }

    private static MobileOperation Operation(
        Guid operationId,
        string operationType,
        Guid aggregateId,
        long localSequence,
        long? expectedCloudVersion,
        string payloadJson,
        Guid? leaseId = null)
    {
        return new MobileOperation(
            operationId,
            operationType,
            aggregateId,
            localSequence,
            expectedCloudVersion,
            payloadJson,
            Convert.ToHexStringLower(
                SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson))),
            leaseId);
    }

    private static Task<HttpResponseMessage> SendOperationAsync(
        HttpClient client,
        MobileOperation operation)
    {
        return SendOperationsAsync(client, operation);
    }

    private static Task<HttpResponseMessage> SendOperationsAsync(
        HttpClient client,
        params MobileOperation[] operations)
    {
        return client.PostAsJsonAsync(
            "/api/v1/mobile/sync/operations",
            new
            {
                operations,
            },
            JsonOptions,
            CancellationToken);
    }

    private static Task<HttpResponseMessage> SendVersionedCommandAsync(
        HttpClient client,
        Guid sessionId,
        string command,
        string idempotencyKey,
        long expectedVersion)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/spikes/procurement-poc/receiving-sessions/{sessionId:D}/{command}");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.Add(
            "X-Expected-Version",
            expectedVersion.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        return client.SendAsync(request, CancellationToken);
    }

    private static async Task<PostgresException> ExecuteRejectedSqlAsync(
        IsolatedPostgreSqlDatabase database,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(
                parameter.Name,
                parameter.Value);
        }

        return await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(CancellationToken));
    }

    private static Task<HttpResponseMessage> SendHeartbeatAsync(
        HttpClient client,
        Guid sessionId,
        Guid leaseId,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/spikes/procurement-poc/receiving-sessions/{sessionId:D}/heartbeat")
        {
            Content = JsonContent.Create(
                new { leaseId },
                options: JsonOptions),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request, CancellationToken);
    }

    private static JsonElement SingleOperation(JsonDocument json)
    {
        return json.RootElement
            .GetProperty("result")
            .GetProperty("operations")[0];
    }

    private static string? ErrorCode(JsonDocument json)
    {
        return json.RootElement
            .GetProperty("error")
            .GetProperty("code")
            .GetString();
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync(
            CancellationToken);
        return await JsonDocument.ParseAsync(
            stream,
            cancellationToken: CancellationToken);
    }

    private static async Task<Workspace> CreateWorkspaceAsync(
        IsolatedPostgreSqlDatabase database,
        string suffix)
    {
        await using var context = database.CreateContext(null, UtcNow);
        var workspace = Workspace.Create(
            $"procurement-poc-{suffix}-{Guid.NewGuid():N}",
            "Procurement POC migration workspace",
            UtcNow);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync(CancellationToken);
        return workspace;
    }

    private static async Task<(Guid WorkspaceId, Guid DeviceId)>
        CreateWorkspaceAndDeviceAsync(
            IsolatedPostgreSqlDatabase database,
            string suffix)
    {
        var workspace = await CreateWorkspaceAsync(database, suffix);
        await using var context = database.CreateContext(workspace.Id, UtcNow);
        var device = Device.Create(
            workspace.Id,
            $"device-{suffix}",
            $"Device {suffix}",
            "Testing",
            UtcNow);
        context.Devices.Add(device);
        await context.SaveChangesAsync(CancellationToken);
        return (workspace.Id, device.Id);
    }

    private static async Task CreateRejectingPocOutboxTriggerAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE FUNCTION platform.reject_procurement_poc_outbox()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                IF NEW.event_type LIKE 'Receiving%Poc%' THEN
                    RAISE EXCEPTION 'Injected procurement POC outbox failure';
                END IF;
                RETURN NEW;
            END;
            $function$;

            CREATE TRIGGER tr_reject_procurement_poc_outbox
            BEFORE INSERT ON platform.outbox_messages
            FOR EACH ROW
            EXECUTE FUNCTION platform.reject_procurement_poc_outbox();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task DropRejectingPocOutboxTriggerAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP TRIGGER IF EXISTS tr_reject_procurement_poc_outbox
                ON platform.outbox_messages;
            DROP FUNCTION IF EXISTS platform.reject_procurement_poc_outbox();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private sealed record MobileOperation(
        Guid OperationId,
        string OperationType,
        Guid AggregateId,
        long LocalSequence,
        long? ExpectedCloudVersion,
        string PayloadJson,
        string PayloadHash,
        Guid? LeaseId);

    private sealed record BootstrapSetup(
        Guid WorkspaceId,
        Guid OperatorDeviceId,
        Guid OwnerDeviceId);

    private sealed record StartResult(Guid LeaseId);
}
