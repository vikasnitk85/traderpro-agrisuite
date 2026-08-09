using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Procurement.Receiving;
using TraderPro.Domain.Common;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CommercialReceivingApiTests(PostgreSqlFixture fixture)
{
    private const string OwnerPassword = "commercial owner development passphrase";
    private const string OperatorPassword = "commercial operator development passphrase";
    private static readonly DateTimeOffset UtcNow = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Immutable_payload_capabilities_are_rejected_recursively_without_durable_side_effects()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-RECURSIVE-PAYLOAD");

        var operationIds = Enumerable.Range(0, 4).Select(_ => Uuid7.NewGuid()).ToArray();
        var sessionIds = Enumerable.Range(0, 4).Select(_ => Uuid7.NewGuid()).ToArray();
        string StartPayload(int index, object extra) => Payload(new
        {
            operationId = operationIds[index],
            sessionId = sessionIds[index],
            localSequence = 1,
            supplierId = scenario.SupplierId,
            supplierVersion = 1,
            companyProcurementSettingsId = scenario.SettingsId,
            procurementSettingsVersion = 1,
            destinationLocationId = scenario.LocationId,
            destinationLocationVersion = 1,
            weightProcessingPolicyId = scenario.WeightPolicyId,
            weightProcessingPolicyVersion = 1,
            vehicleSelectionMode = "Optional",
            receivingVehicleId = (Guid?)null,
            receivingVehicleVersion = (long?)null,
            externalReference = $"RECURSIVE-{index}",
            startedAtDeviceUtc = UtcNow,
            extra,
        });
        object Envelope(int index, string payload) => new
        {
            operationId = operationIds[index],
            operationType = CommercialReceivingOperationTypes.Start,
            sessionId = sessionIds[index],
            localSequence = 1,
            ownershipGeneration = (long?)null,
            expectedCloudVersion = (long?)null,
            payloadJson = payload,
            payloadHash = CommercialReceivingRequestHash.PayloadHash(payload),
            lease = (object?)null,
        };

        var topLevel = StartPayload(0, new { harmless = true });
        topLevel = topLevel[..^1] + ",\"leaseId\":\"" + Uuid7.NewGuid().ToString("D") + "\"}";
        var nestedObject = StartPayload(1, new { metadata = new { LeaseExpiresAtUtc = UtcNow } });
        var nestedArray = StartPayload(2, new { metadata = new object[] { new { harmless = 1 }, new { expectedCloudVersion = 8 } } });
        var harmless = StartPayload(3, new { metadata = new object[] { new { harmless = "preserved" } } });

        using (var response = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   new { operations = new[] { Envelope(0, topLevel), Envelope(1, nestedObject), Envelope(2, nestedArray), Envelope(3, harmless) } }))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var operations = json.RootElement.GetProperty("operations").EnumerateArray().ToArray();
            Assert.Equal(4, operations.Length);
            Assert.All(operations[..3], operation =>
            {
                Assert.Equal("Rejected", operation.GetProperty("status").GetString());
                Assert.Equal(
                    "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID",
                    operation.GetProperty("error").GetProperty("code").GetString());
            });
            Assert.True(
                operations[3].GetProperty("status").GetString() == "Accepted",
                operations[3].ToString());
        }

        await AssertCountAsync(
            database,
            "SELECT count(*) FROM sync.commercial_receiving_operation_claims WHERE operation_id = ANY(@ids)",
            1,
            ("ids", operationIds));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.idempotency_records WHERE idempotency_key = ANY(@keys)",
            1,
            ("keys", operationIds.Select(id => id.ToString("D")).ToArray()));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = ANY(@ids)",
            1,
            ("ids", sessionIds));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.audit_events WHERE aggregate_id = ANY(@ids)",
            1,
            ("ids", sessionIds));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.outbox_messages WHERE aggregate_id = ANY(@ids)",
            2,
            ("ids", sessionIds));
    }

    [Fact]
    public async Task Mixed_batch_waits_later_session_operations_without_claiming_and_retries_in_sequence()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-MIXED-BATCH");
        var started = await StartSessionAsync(client, scenario, "MIXED-BATCH");

        var sequenceThreeId = Uuid7.NewGuid();
        var sequenceTwoId = Uuid7.NewGuid();
        var sequenceThreePayload = EntryPayload(sequenceThreeId, Uuid7.NewGuid(), started.SessionId, 3, scenario.ProductId, scenario.BagTypeId, "3.005", "3.010000", "3.01");
        var sequenceTwoPayload = EntryPayload(sequenceTwoId, Uuid7.NewGuid(), started.SessionId, 2, scenario.ProductId, scenario.BagTypeId, "2.005", "2.010000", "2.01");
        var unrelatedSessionId = Uuid7.NewGuid();
        var unrelatedOperationId = Uuid7.NewGuid();

        using (var response = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   new
                   {
                       operations = new object[]
                       {
                           OperationBody(sequenceThreeId, started.SessionId, 3, 1, started.LeaseId, sequenceThreePayload),
                           OperationBody(sequenceTwoId, started.SessionId, 2, 1, started.LeaseId, sequenceTwoPayload),
                           StartOperationBody(scenario, unrelatedSessionId, unrelatedOperationId, "UNRELATED-CONTINUES"),
                       },
                   }))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var operations = json.RootElement.GetProperty("operations").EnumerateArray().ToArray();
            Assert.Equal("NeedsAttention", operations[0].GetProperty("status").GetString());
            Assert.Equal("RECEIVING_SEQUENCE_GAP", operations[0].GetProperty("error").GetProperty("code").GetString());
            Assert.Equal("NeedsAttention", operations[1].GetProperty("status").GetString());
            Assert.Equal(
                "RECEIVING_OPERATION_WAITING_FOR_PRIOR_SEQUENCE",
                operations[1].GetProperty("error").GetProperty("code").GetString());
            Assert.True(operations[1].GetProperty("error").GetProperty("retryable").GetBoolean());
            Assert.Equal("Accepted", operations[2].GetProperty("status").GetString());
        }

        await AssertCountAsync(
            database,
            "SELECT count(*) FROM sync.commercial_receiving_operation_claims WHERE operation_id = @id",
            0,
            ("id", sequenceTwoId));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.idempotency_records WHERE idempotency_key = @key",
            0,
            ("key", sequenceTwoId.ToString("D")));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_entries WHERE receiving_session_id = @id",
            0,
            ("id", started.SessionId));

        using (var response = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   new
                   {
                       operations = new[]
                       {
                           OperationBody(sequenceTwoId, started.SessionId, 2, 1, started.LeaseId, sequenceTwoPayload),
                           OperationBody(sequenceThreeId, started.SessionId, 3, 1, started.LeaseId, sequenceThreePayload),
                       },
                   }))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var operations = json.RootElement.GetProperty("operations").EnumerateArray().ToArray();
            Assert.All(operations, operation => Assert.Equal("Accepted", operation.GetProperty("status").GetString()));
        }

        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_entries WHERE receiving_session_id = @id",
            2,
            ("id", started.SessionId));
    }

    [Fact]
    public async Task Owner_and_operator_monitoring_uses_latest_ownership_update_and_actionable_lease_attention()
    {
        var testClock = new MutableClock(UtcNow);
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            testClock: testClock);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-MONITORING");
        var started = await StartSessionAsync(client, scenario, "MONITORING");

        testClock.Advance(TimeSpan.FromMinutes(5));
        using (var heartbeat = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/lease/heartbeat",
                   new { ownershipGeneration = 1, leaseId = started.LeaseId },
                   "monitoring-heartbeat"))
        {
            Assert.Equal(HttpStatusCode.OK, heartbeat.StatusCode);
        }

        async Task AssertProjectionAsync(
            string token,
            string leaseHealth,
            string? attention,
            DateTimeOffset lastCloudUpdateUtc)
        {
            using (var listResponse = await SendAsync(
                       client,
                       token,
                       HttpMethod.Get,
                       "/api/v1/procurement/receiving-sessions?limit=50"))
            using (var listJson = await ReadJsonAsync(listResponse))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var item = Assert.Single(
                    listJson.RootElement.GetProperty("items").EnumerateArray(),
                    value => value.GetProperty("sessionId").GetGuid() == started.SessionId);
                Assert.Equal(leaseHealth, item.GetProperty("leaseHealth").GetString());
                Assert.Equal(lastCloudUpdateUtc, item.GetProperty("lastCloudUpdateUtc").GetDateTimeOffset());
                if (attention is null)
                    Assert.Equal(JsonValueKind.Null, item.GetProperty("attention").ValueKind);
                else
                    Assert.Equal(attention, item.GetProperty("attention").GetString());
            }

            using var liveResponse = await SendAsync(
                client,
                token,
                HttpMethod.Get,
                $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/live-view");
            using var liveJson = await ReadJsonAsync(liveResponse);
            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
            var live = Result(liveJson);
            Assert.Equal(leaseHealth, live.GetProperty("leaseHealth").GetString());
            Assert.Equal(lastCloudUpdateUtc, live.GetProperty("lastCloudUpdateUtc").GetDateTimeOffset());
            if (attention is null)
                Assert.Equal(JsonValueKind.Null, live.GetProperty("attention").ValueKind);
            else
                Assert.Equal(attention, live.GetProperty("attention").GetString());
        }

        await AssertProjectionAsync(
            scenario.Identity.OwnerToken,
            "Healthy",
            null,
            testClock.UtcNow);
        await AssertProjectionAsync(
            scenario.Identity.OperatorToken,
            "Healthy",
            null,
            testClock.UtcNow);

        testClock.Advance(TimeSpan.FromMinutes(1));
        using (var transfer = await SendAsync(
                   client,
                   scenario.Identity.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/ownership/transfer",
                   new
                   {
                       targetDeviceId = scenario.Identity.OwnerDeviceId,
                       expectedOwnershipGeneration = 1,
                       reason = "Monitoring transfer",
                   },
                   "monitoring-transfer",
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);
        }
        var targetOperatorToken = await LoginAsync(
            client,
            scenario.Identity.WorkspaceCode,
            "operator",
            OperatorPassword,
            scenario.Identity.OwnerDeviceId,
            scenario.Identity.OwnerDeviceSecret);
        await AssertProjectionAsync(
            scenario.Identity.OwnerToken,
            "AcquisitionRequired",
            "LeaseAcquisitionRequired",
            testClock.UtcNow);
        await AssertProjectionAsync(
            targetOperatorToken,
            "AcquisitionRequired",
            "LeaseAcquisitionRequired",
            testClock.UtcNow);

        testClock.Advance(TimeSpan.FromMinutes(1));
        using (var reacquire = await SendAsync(
                   client,
                   targetOperatorToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/lease/reacquire",
                   new { ownershipGeneration = 2 },
                   "monitoring-reacquire"))
        {
            Assert.Equal(HttpStatusCode.OK, reacquire.StatusCode);
        }
        var reacquiredAt = testClock.UtcNow;
        testClock.Advance(TimeSpan.FromMinutes(61));
        var freshOwnerToken = await LoginAsync(
            client,
            scenario.Identity.WorkspaceCode,
            "owner",
            OwnerPassword,
            scenario.Identity.OwnerDeviceId,
            scenario.Identity.OwnerDeviceSecret);
        targetOperatorToken = await LoginAsync(
            client,
            scenario.Identity.WorkspaceCode,
            "operator",
            OperatorPassword,
            scenario.Identity.OwnerDeviceId,
            scenario.Identity.OwnerDeviceSecret);
        await AssertProjectionAsync(
            freshOwnerToken,
            "Expired",
            "LeaseReacquisitionRequired",
            reacquiredAt);
        await AssertProjectionAsync(
            targetOperatorToken,
            "Expired",
            "LeaseReacquisitionRequired",
            reacquiredAt);
    }

    [Fact]
    public async Task Concurrent_attention_and_success_are_serialized_by_operation_claim_and_terminal_state_is_immutable()
    {
        var testClock = new MutableClock(UtcNow);
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            testClock: testClock);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-CLAIM-RACE");
        var started = await StartSessionAsync(client, scenario, "CLAIM-RACE");
        testClock.Advance(TimeSpan.FromMinutes(61));
        var operatorToken = await LoginAsync(
            client,
            scenario.Identity.WorkspaceCode,
            "operator",
            OperatorPassword,
            scenario.Identity.OperatorDeviceId,
            scenario.Identity.OperatorDeviceSecret);

        var operationId = Uuid7.NewGuid();
        var payload = EntryPayload(
            operationId,
            Uuid7.NewGuid(),
            started.SessionId,
            2,
            scenario.ProductId,
            scenario.BagTypeId,
            "4.005",
            "4.010000",
            "4.01");

        await using var gateConnection = await database.OpenConnectionAsync();
        await using var gateTransaction = await gateConnection.BeginTransactionAsync(CancellationToken);
        await using (var gate = gateConnection.CreateCommand())
        {
            gate.Transaction = gateTransaction;
            gate.CommandText = "SELECT pg_advisory_xact_lock(738002)";
            await gate.ExecuteNonQueryAsync(CancellationToken);
        }
        await using (var installConnection = await database.OpenConnectionAsync())
        await using (var install = installConnection.CreateCommand())
        {
            install.CommandText =
                """
                CREATE OR REPLACE FUNCTION sync.gate_test_receiving_claim_attention()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    IF NEW.state = 2 THEN
                        PERFORM pg_advisory_xact_lock(738002);
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                CREATE TRIGGER tr_gate_test_receiving_claim_attention
                BEFORE UPDATE ON sync.commercial_receiving_operation_claims
                FOR EACH ROW EXECUTE FUNCTION sync.gate_test_receiving_claim_attention();
                """;
            await install.ExecuteNonQueryAsync(CancellationToken);
        }

        var firstTask = SendAsync(
            client,
            operatorToken,
            HttpMethod.Post,
            "/api/v1/mobile/commercial-sync/operations",
            Batch(OperationBody(operationId, started.SessionId, 2, 1, started.LeaseId, payload)));
        await WaitForAdvisoryWaiterAsync(database);

        Guid renewedLeaseId;
        using (var reacquired = await SendAsync(
                   client,
                   operatorToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/lease/reacquire",
                   new { ownershipGeneration = 1 },
                   "claim-race-reacquire"))
        using (var json = await ReadJsonAsync(reacquired))
        {
            Assert.Equal(HttpStatusCode.OK, reacquired.StatusCode);
            renewedLeaseId = Result(json).GetProperty("leaseId").GetGuid();
        }

        var secondTask = SendAsync(
            client,
            operatorToken,
            HttpMethod.Post,
            "/api/v1/mobile/commercial-sync/operations",
            Batch(OperationBody(operationId, started.SessionId, 2, 1, renewedLeaseId, payload)));
        await Task.Delay(150, CancellationToken);
        Assert.False(secondTask.IsCompleted);
        await gateTransaction.CommitAsync(CancellationToken);

        using (var first = await firstTask)
        using (var firstJson = await ReadJsonAsync(first))
        {
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal("NeedsAttention", Operation(firstJson).GetProperty("status").GetString());
        }
        using (var second = await secondTask)
        using (var secondJson = await ReadJsonAsync(second))
        {
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal("Accepted", Operation(secondJson).GetProperty("status").GetString());
        }

        using (var replay = await SendAsync(
                   client,
                   operatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(operationId, started.SessionId, 2, 1, renewedLeaseId, payload))))
        using (var replayJson = await ReadJsonAsync(replay))
        {
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            Assert.Equal("PreviouslyProcessed", Operation(replayJson).GetProperty("status").GetString());
        }

        await using (var cleanupConnection = await database.OpenConnectionAsync())
        await using (var cleanup = cleanupConnection.CreateCommand())
        {
            cleanup.CommandText =
                """
                DROP TRIGGER IF EXISTS tr_gate_test_receiving_claim_attention
                    ON sync.commercial_receiving_operation_claims;
                DROP FUNCTION IF EXISTS sync.gate_test_receiving_claim_attention();
                """;
            await cleanup.ExecuteNonQueryAsync(CancellationToken);
        }
        await using var directConnection = await database.OpenConnectionAsync();
        foreach (var corruption in new[]
                 {
                     "retryable = true",
                     "attention_code = 'CORRUPTED', attention_message = 'corrupted'",
                     "completed_at_utc = completed_at_utc + interval '1 second', updated_at_utc = updated_at_utc + interval '1 second'",
                     "updated_at_utc = updated_at_utc + interval '1 second'",
                 })
        {
            await AssertSqlRejectedAsync(
                directConnection,
                $"UPDATE sync.commercial_receiving_operation_claims SET {corruption} WHERE operation_id = @id",
                operationId);
        }
    }

    [Fact]
    public async Task Ownership_direct_sql_corruption_is_rejected_and_target_credential_revocation_wins_transfer_race()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-OWNERSHIP-GUARDS");
        var started = await StartSessionAsync(client, scenario, "OWNERSHIP-GUARDS");

        await using (var directConnection = await database.OpenConnectionAsync())
        {
            foreach (var corruption in new[]
                     {
                         "version = version + 1, updated_at_utc = updated_at_utc + interval '1 second'",
                         "lease_id = NULL, lease_expires_at_utc = NULL, last_heartbeat_at_utc = NULL, version = version + 1, updated_at_utc = updated_at_utc + interval '1 second'",
                         "lease_id = uuidv7(), last_heartbeat_at_utc = updated_at_utc + interval '1 second', last_reacquired_at_utc = updated_at_utc + interval '1 second', lease_expires_at_utc = updated_at_utc + interval '61 minutes', version = version + 1, updated_at_utc = updated_at_utc + interval '1 second'",
                         "ownership_generation = ownership_generation + 2, version = version + 1, updated_at_utc = updated_at_utc + interval '1 second'",
                     })
            {
                await AssertSqlRejectedAsync(
                    directConnection,
                    $"UPDATE procurement.commercial_receiving_ownerships SET {corruption} WHERE receiving_session_id = @id",
                    started.SessionId);
            }
        }

        await using var revocationConnection = await database.OpenConnectionAsync();
        await using var revocationTransaction = await revocationConnection.BeginTransactionAsync(CancellationToken);
        await using (var revoke = revocationConnection.CreateCommand())
        {
            revoke.Transaction = revocationTransaction;
            revoke.CommandText =
                """
                UPDATE platform.device_credentials
                SET revoked_at_utc = updated_at_utc + interval '1 second',
                    updated_at_utc = updated_at_utc + interval '1 second',
                    version = version + 1
                WHERE device_id = @device_id
                """;
            revoke.Parameters.AddWithValue("device_id", scenario.Identity.OwnerDeviceId);
            Assert.Equal(1, await revoke.ExecuteNonQueryAsync(CancellationToken));
        }

        var transferTask = SendAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/ownership/transfer",
            new
            {
                targetDeviceId = scenario.Identity.OwnerDeviceId,
                expectedOwnershipGeneration = 1,
                reason = "Credential revocation race",
            },
            "ownership-credential-race",
            expectedVersion: 1);
        await Task.Delay(150, CancellationToken);
        Assert.False(transferTask.IsCompleted);
        await revocationTransaction.CommitAsync(CancellationToken);

        using (var transfer = await transferTask)
        using (var json = await ReadJsonAsync(transfer))
        {
            Assert.True(
                transfer.StatusCode == HttpStatusCode.Conflict,
                json.RootElement.ToString());
            Assert.Equal("RECEIVING_OWNERSHIP_TARGET_INVALID", ErrorCode(json));
        }
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.audit_events WHERE aggregate_id = @id AND action = 'Procurement.CommercialReceiving.OwnershipTransferred'",
            0,
            ("id", started.SessionId));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id AND version = 1 AND ownership_generation = 1",
            1,
            ("id", started.SessionId));
    }

    [Fact]
    public async Task Direct_sql_cannot_insert_vehicle_facts_for_disabled_vehicle_selection()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-DISABLED-VEHICLE");
        var vehicleId = await CreateIdAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/vehicles",
            new
            {
                code = "DISABLED-TRUCK",
                registrationNumber = "KA 01 DIS 7",
                displayName = "Disabled truck",
                vehicleType = "Truck",
            },
            "disabled-vehicle",
            HttpStatusCode.Created);
        using (var settings = await SendAsync(
                   client,
                   scenario.Identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = scenario.LocationId,
                       defaultWeightProcessingPolicyId = scenario.WeightPolicyId,
                       vehicleSelectionMode = "Disabled",
                   },
                   "disabled-settings",
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
        }

        var sourceSessionId = Uuid7.NewGuid();
        var sourceOperationId = Uuid7.NewGuid();
        var sourcePayload = Payload(new
        {
            operationId = sourceOperationId,
            sessionId = sourceSessionId,
            localSequence = 1,
            supplierId = scenario.SupplierId,
            supplierVersion = 1,
            companyProcurementSettingsId = scenario.SettingsId,
            procurementSettingsVersion = 2,
            destinationLocationId = scenario.LocationId,
            destinationLocationVersion = 1,
            weightProcessingPolicyId = scenario.WeightPolicyId,
            weightProcessingPolicyVersion = 1,
            vehicleSelectionMode = "Disabled",
            receivingVehicleId = (Guid?)null,
            receivingVehicleVersion = (long?)null,
            externalReference = "DISABLED-SOURCE",
            startedAtDeviceUtc = UtcNow,
        });
        using (var start = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(new
                   {
                       operationId = sourceOperationId,
                       operationType = CommercialReceivingOperationTypes.Start,
                       sessionId = sourceSessionId,
                       localSequence = 1,
                       ownershipGeneration = (long?)null,
                       expectedCloudVersion = (long?)null,
                       payloadJson = sourcePayload,
                       payloadHash = CommercialReceivingRequestHash.PayloadHash(sourcePayload),
                       lease = (object?)null,
                   })))
        using (var json = await ReadJsonAsync(start))
        {
            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
            Assert.Equal("Accepted", Operation(json).GetProperty("status").GetString());
        }

        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO sync.commercial_receiving_operation_claims(
                workspace_id, company_id, command_scope, operation_id,
                operation_type, session_id, device_id, ownership_generation,
                request_hash, state, retryable, first_seen_at_utc,
                created_at_utc, updated_at_utc)
            SELECT source.workspace_id, source.company_id,
                   'Procurement.CommercialReceiving.MobileSyncOperation',
                   @operation_id, 'StartCommercialReceivingSession', @session_id,
                   ownership.editor_device_id, NULL, repeat('a', 64), 1, false,
                   source.updated_at_utc, source.updated_at_utc, source.updated_at_utc
            FROM procurement.commercial_receiving_sessions source
            JOIN procurement.commercial_receiving_ownerships ownership
              ON ownership.receiving_session_id = source.id
            WHERE source.id = @source_session_id;

            UPDATE procurement.commercial_receiving_reference_counters counter
            SET next_number = counter.next_number + 1,
                version = counter.version + 1
            FROM procurement.commercial_receiving_reference_reservations source
            WHERE source.session_id = @source_session_id
              AND counter.workspace_id = source.workspace_id
              AND counter.company_id = source.company_id
              AND counter.policy_id = source.policy_id
              AND counter.period_key = source.period_key;

            INSERT INTO procurement.commercial_receiving_reference_reservations(
                id, workspace_id, company_id, policy_id, period_key,
                operation_id, session_id, request_hash, policy_version,
                sequence, rendered_reference, reserved_at_utc, created_at_utc)
            SELECT @reservation_id, reservation.workspace_id,
                   reservation.company_id, reservation.policy_id,
                   reservation.period_key, @operation_id, @session_id,
                   repeat('a', 64), reservation.policy_version,
                   reservation.sequence + 1,
                   reservation.rendered_reference || '-DISABLED',
                   reservation.reserved_at_utc, reservation.created_at_utc
            FROM procurement.commercial_receiving_reference_reservations reservation
            WHERE reservation.session_id = @source_session_id;

            UPDATE procurement.commercial_receiving_reference_reservations
            SET consumed_at_utc = reserved_at_utc
            WHERE id = @reservation_id;

            INSERT INTO procurement.commercial_receiving_sessions(
                id, workspace_id, company_id, branch_id, cloud_reference,
                cloud_reference_sequence, reference_reservation_id,
                reference_policy_version_snapshot, external_reference, status,
                ownership_generation, supplier_id, supplier_version_snapshot,
                supplier_code_snapshot, supplier_name_snapshot,
                supplier_product_scope_mode_snapshot,
                company_procurement_settings_id, procurement_settings_version_snapshot,
                vehicle_selection_mode_snapshot, destination_location_id,
                destination_location_version_snapshot, destination_location_code_snapshot,
                destination_location_name_snapshot, weight_processing_policy_id,
                weight_policy_version_snapshot, weight_decimal_places_snapshot,
                weight_processing_method_snapshot, receiving_vehicle_id,
                receiving_vehicle_version_snapshot, vehicle_code_snapshot,
                vehicle_registration_snapshot, vehicle_display_name_snapshot,
                next_expected_local_sequence, entry_count, processed_total_weight_kg,
                started_at_device_utc, started_at_server_utc, submitted_at_utc,
                created_at_utc, updated_at_utc, version)
            SELECT @session_id, source.workspace_id, source.company_id,
                   source.branch_id, reservation.rendered_reference,
                   reservation.sequence, @reservation_id,
                   source.reference_policy_version_snapshot,
                   'DIRECT-DISABLED', 1, 1, source.supplier_id,
                   source.supplier_version_snapshot, source.supplier_code_snapshot,
                   source.supplier_name_snapshot,
                   source.supplier_product_scope_mode_snapshot,
                   source.company_procurement_settings_id,
                   source.procurement_settings_version_snapshot, 2,
                   source.destination_location_id,
                   source.destination_location_version_snapshot,
                   source.destination_location_code_snapshot,
                   source.destination_location_name_snapshot,
                   source.weight_processing_policy_id,
                   source.weight_policy_version_snapshot,
                   source.weight_decimal_places_snapshot,
                   source.weight_processing_method_snapshot,
                   vehicle.id, vehicle.version, vehicle.code,
                   vehicle.registration_number, vehicle.display_name,
                   2, 0, 0, source.started_at_device_utc,
                   source.started_at_server_utc, NULL,
                   source.created_at_utc, source.updated_at_utc, 1
            FROM procurement.commercial_receiving_sessions source
            JOIN procurement.commercial_receiving_reference_reservations reservation
              ON reservation.id = @reservation_id
            JOIN procurement.receiving_vehicles vehicle
              ON vehicle.id = @vehicle_id
            WHERE source.id = @source_session_id;
            """;
        command.Parameters.AddWithValue("source_session_id", sourceSessionId);
        command.Parameters.AddWithValue("operation_id", Uuid7.NewGuid());
        command.Parameters.AddWithValue("session_id", Uuid7.NewGuid());
        command.Parameters.AddWithValue("reservation_id", Uuid7.NewGuid());
        command.Parameters.AddWithValue("vehicle_id", vehicleId);
        var rejected = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(CancellationToken));
        Assert.True(
            rejected.ConstraintName == "ck_commercial_receiving_sessions_snapshots" ||
            rejected.MessageText.Contains("Disabled Commercial Receiving Vehicle", StringComparison.Ordinal),
            rejected.MessageText);
    }

    [Fact]
    public async Task Authenticated_lifecycle_transfer_cursors_and_integrity_controls_work()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: true,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var identity = await CreateIdentityAsync(client, "RECEIVING-API");

        using (var anonymous = await client.PostAsJsonAsync(
                   "/api/v1/mobile/commercial-sync/operations",
                   new { operations = Array.Empty<object>() },
                   CancellationToken))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        }

        var locationId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/operations/locations",
            new { code = "YARD-01", name = "Receiving Yard", locationType = "Yard" },
            "receiving-location",
            HttpStatusCode.Created);
        var weightPolicyId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/weight-policies",
            new { code = "STD-2", name = "Standard two decimals", decimalPlaces = 2, processingMethod = "Standard" },
            "receiving-weight-policy",
            HttpStatusCode.Created);
        var bagTypeId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/bag-types",
            new { code = "JUTE", name = "Jute", constructionClass = "Jute", standardTareWeightKg = "0.200000", isReturnable = true },
            "receiving-bag-type",
            HttpStatusCode.Created);
        var groupId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/catalog/product-groups",
            new { code = "PADDY", name = "Paddy" },
            "receiving-product-group",
            HttpStatusCode.Created);
        var productId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/catalog/products",
            new { productGroupId = groupId, code = "PADDY-A", name = "Paddy A", productType = "RawMaterial", isPurchasable = true, processingFamilyCode = "PADDY" },
            "receiving-product",
            HttpStatusCode.Created);
        var supplierId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/suppliers",
            new
            {
                code = "SUP-01",
                name = "Receiving Supplier",
                supplierType = "Business",
                productScopeMode = "Unrestricted",
                initialProductIds = Array.Empty<Guid>(),
                contactNumber = "+91 98765 43210",
                email = "private@example.test",
            },
            "receiving-supplier",
            HttpStatusCode.Created);
        Guid settingsId;
        using (var response = await SendAsync(
                   client,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationId,
                       defaultWeightProcessingPolicyId = weightPolicyId,
                       vehicleSelectionMode = "Optional",
                   },
                   "receiving-settings"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
            settingsId = Result(json).GetProperty("id").GetGuid();
            Assert.Equal(1, Result(json).GetProperty("version").GetInt64());
        }

        using (var response = await SendAsync(
                   client,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/receiving-reference-policy",
                   new { formatTemplate = "RCV-{YYYY}-{MM}-{SEQ:0000}", resetPolicy = "Monthly", startingNumber = 10 },
                   "receiving-reference-policy",
                   expectedVersion: 1))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
        }

        var sessionId = Uuid7.NewGuid();
        var startOperationId = Uuid7.NewGuid();
        var startPayload = Payload(new
        {
            operationId = startOperationId,
            sessionId,
            localSequence = 1,
            supplierId,
            supplierVersion = 1,
            companyProcurementSettingsId = settingsId,
            procurementSettingsVersion = 1,
            destinationLocationId = locationId,
            destinationLocationVersion = 1,
            weightProcessingPolicyId = weightPolicyId,
            weightProcessingPolicyVersion = 1,
            vehicleSelectionMode = "Optional",
            receivingVehicleId = (Guid?)null,
            receivingVehicleVersion = (long?)null,
            externalReference = "SUP-DELIVERY-01",
            startedAtDeviceUtc = UtcNow.AddMinutes(-5),
        });
        var startBody = Batch(new
        {
            operationId = startOperationId,
            operationType = CommercialReceivingOperationTypes.Start,
            sessionId,
            localSequence = 1,
            ownershipGeneration = (long?)null,
            expectedCloudVersion = (long?)null,
            payloadJson = startPayload,
            payloadHash = CommercialReceivingRequestHash.PayloadHash(startPayload),
            lease = (object?)null,
        });
        Guid firstLeaseId;
        using (var response = await SendAsync(client, identity.OperatorToken, HttpMethod.Post, "/api/v1/mobile/commercial-sync/operations", startBody))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
            var operation = Operation(json);
            Assert.True(operation.GetProperty("status").GetString() == "Accepted", operation.ToString());
            var cloud = operation.GetProperty("cloud");
            Assert.Equal("RCV-2026-08-0010", cloud.GetProperty("cloudReference").GetString());
            Assert.Equal(1, cloud.GetProperty("sessionVersion").GetInt64());
            Assert.Equal(1, cloud.GetProperty("ownershipGeneration").GetInt64());
            firstLeaseId = cloud.GetProperty("leaseId").GetGuid();
        }
        using (var response = await SendAsync(client, identity.OperatorToken, HttpMethod.Post, "/api/v1/mobile/commercial-sync/operations", startBody))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PreviouslyProcessed", Operation(json).GetProperty("status").GetString());
            Assert.Equal(firstLeaseId, Operation(json).GetProperty("cloud").GetProperty("leaseId").GetGuid());
        }

        using (var activeReacquire = await SendAsync(
                   client,
                   identity.OperatorToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{sessionId:D}/lease/reacquire",
                   new { ownershipGeneration = 1 },
                   "active-lease-reacquire"))
        using (var json = await ReadJsonAsync(activeReacquire))
        {
            Assert.Equal(HttpStatusCode.Conflict, activeReacquire.StatusCode);
            Assert.Equal("RECEIVING_LEASE_STILL_ACTIVE", ErrorCode(json));
        }

        var firstEntryOperationId = Uuid7.NewGuid();
        var firstEntryPayload = EntryPayload(firstEntryOperationId, Uuid7.NewGuid(), sessionId, 2, productId, bagTypeId, "12.345", "12.350000", "12.35");
        using (var response = await SendAsync(
                   client,
                   identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(firstEntryOperationId, sessionId, 2, 1, firstLeaseId, firstEntryPayload))))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(Operation(json).GetProperty("status").GetString() == "Accepted", Operation(json).ToString());
            Assert.Equal(2, Operation(json).GetProperty("cloud").GetProperty("sessionVersion").GetInt64());
            Assert.Equal("12.350000", Operation(json).GetProperty("cloud").GetProperty("processedTotalWeightKg").GetString());
        }

        string targetOperatorToken;
        using (var response = await SendAsync(
                   client,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{sessionId:D}/ownership/transfer",
                   new { targetDeviceId = identity.OwnerDeviceId, expectedOwnershipGeneration = 1, reason = "Operator device unavailable" },
                   "receiving-transfer",
                   expectedVersion: 2))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
            Assert.Equal(2, Result(json).GetProperty("ownershipGeneration").GetInt64());
            Assert.Equal(3, Result(json).GetProperty("sessionVersion").GetInt64());
            Assert.Equal(JsonValueKind.Null, Result(json).GetProperty("leaseId").ValueKind);
        }

        targetOperatorToken = await LoginAsync(
            client,
            identity.WorkspaceCode,
            "operator",
            OperatorPassword,
            identity.OwnerDeviceId,
            identity.OwnerDeviceSecret);

        using (var ownerMutation = await SendAsync(
                   client,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{sessionId:D}/lease/reacquire",
                   new { ownershipGeneration = 2 },
                   "owner-reacquire-denied"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, ownerMutation.StatusCode);
        }

        Guid secondLeaseId;
        using (var response = await SendAsync(
                   client,
                   targetOperatorToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{sessionId:D}/lease/reacquire",
                   new { ownershipGeneration = 2 },
                   "target-operator-acquire"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
            Assert.Equal(2, Result(json).GetProperty("ownershipGeneration").GetInt64());
            secondLeaseId = Result(json).GetProperty("leaseId").GetGuid();
        }

        var staleOperationId = Uuid7.NewGuid();
        var stalePayload = EntryPayload(staleOperationId, Uuid7.NewGuid(), sessionId, 3, productId, bagTypeId, "1.000", "1.000000", "1.00");
        using (var response = await SendAsync(
                   client,
                   identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(staleOperationId, sessionId, 3, 1, firstLeaseId, stalePayload))))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var operation = Operation(json);
            Assert.Equal("NeedsAttention", operation.GetProperty("status").GetString());
            Assert.Equal("RECEIVING_OWNERSHIP_DEVICE_MISMATCH", operation.GetProperty("error").GetProperty("code").GetString());
        }

        var secondEntryOperationId = Uuid7.NewGuid();
        var secondEntryPayload = EntryPayload(secondEntryOperationId, Uuid7.NewGuid(), sessionId, 3, productId, bagTypeId, "12.345", "12.350000", "12.35");
        using (var response = await SendAsync(
                   client,
                   targetOperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(secondEntryOperationId, sessionId, 3, 2, secondLeaseId, secondEntryPayload))))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Accepted", Operation(json).GetProperty("status").GetString());
            Assert.Equal(2, Operation(json).GetProperty("cloud").GetProperty("entryCount").GetInt32());
            Assert.Equal("24.700000", Operation(json).GetProperty("cloud").GetProperty("processedTotalWeightKg").GetString());
        }

        var submitOperationId = Uuid7.NewGuid();
        var submitPayload = Payload(new { operationId = submitOperationId, sessionId, localSequence = 4, submittedAtDeviceUtc = UtcNow });
        using (var response = await SendAsync(
                   client,
                   targetOperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(new
                   {
                       operationId = submitOperationId,
                       operationType = CommercialReceivingOperationTypes.Submit,
                       sessionId,
                       localSequence = 4,
                       ownershipGeneration = 2,
                       expectedCloudVersion = (long?)null,
                       payloadJson = submitPayload,
                       payloadHash = CommercialReceivingRequestHash.PayloadHash(submitPayload),
                       lease = new { leaseId = secondLeaseId },
                   })))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cloud = Operation(json).GetProperty("cloud");
            Assert.Equal("SubmittedForSettlementReview", cloud.GetProperty("sessionStatus").GetString());
            Assert.Equal(5, cloud.GetProperty("sessionVersion").GetInt64());
            Assert.Equal(JsonValueKind.Null, cloud.GetProperty("leaseId").ValueKind);
        }

        using (var response = await SendAsync(client, identity.OperatorToken, HttpMethod.Get, $"/api/v1/procurement/receiving-sessions/{sessionId:D}/live-view"))
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        using (var response = await SendAsync(client, identity.OwnerToken, HttpMethod.Get, $"/api/v1/procurement/receiving-sessions/{sessionId:D}/live-view"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = Result(json);
            Assert.Equal(2, result.GetProperty("entryCount").GetInt32());
            Assert.Equal("24.700000", result.GetProperty("processedTotalWeightKg").GetString());
            Assert.Equal("Closed", result.GetProperty("leaseHealth").GetString());
            Assert.Equal(2, result.GetProperty("recentEntries").GetArrayLength());
        }
        using (var response = await SendAsync(client, identity.OwnerToken, HttpMethod.Get, "/api/v1/procurement/receiving-sessions?limit=1"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(json.RootElement.GetProperty("items").EnumerateArray());
        }

        using (var response = await SendAsync(client, identity.OperatorToken, HttpMethod.Get, "/api/v1/mobile/commercial-sync/events?limit=100"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var events = json.RootElement.GetProperty("events").EnumerateArray().ToArray();
            Assert.Equal(3, events.Length);
            Assert.All(events, item =>
            {
                Assert.Equal("TargetDevice", item.GetProperty("audience").GetString());
                Assert.Equal(identity.OperatorDeviceId, item.GetProperty("targetDeviceId").GetGuid());
            });
        }
        using (var response = await SendAsync(client, identity.OwnerToken, HttpMethod.Get, "/api/v1/mobile/commercial-sync/events?limit=100"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var events = json.RootElement.GetProperty("events").EnumerateArray().ToArray();
            Assert.Equal(8, events.Length);
            Assert.Equal(5, events.Count(item => item.GetProperty("audience").GetString() == "OwnerBroadcast"));
            Assert.Equal(3, events.Count(item => item.GetProperty("audience").GetString() == "TargetDevice"));
        }

        using (var response = await SendAsync(client, identity.OperatorToken, HttpMethod.Get, "/api/v1/mobile/commercial-sync/masters?limit=100"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var changes = json.RootElement.GetProperty("changes").EnumerateArray().ToArray();
            Assert.Contains(changes, item => item.GetProperty("masterType").GetString() == "CompanyProcurementSettings");
            var supplier = Assert.Single(changes, item => item.GetProperty("masterType").GetString() == "Supplier");
            var payload = supplier.GetProperty("payload");
            Assert.Equal(1, payload.GetProperty("contractVersion").GetInt32());
            Assert.False(payload.TryGetProperty("contactNumber", out _));
            Assert.False(payload.TryGetProperty("email", out _));
        }

        using (var response = await SendAsync(
                   client,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/receiving-reference-policy",
                   new { formatTemplate = "RCV-{YYYY}-{MM}-{SEQ:0000}", resetPolicy = "Monthly", startingNumber = 11 },
                   "receiving-reference-after-issue",
                   expectedVersion: 2))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("RECEIVING_REFERENCE_SERIES_STARTED", ErrorCode(json));
        }

        await AssertDatabaseFactsAndGuardsAsync(database, sessionId, staleOperationId);
    }

    [Fact]
    public async Task Owner_is_read_only_and_denied_mutations_create_no_durable_side_effects()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-OWNER-READONLY");
        var started = await StartSessionAsync(client, scenario, "OWNER-READONLY");
        var deniedStartSessionId = Uuid7.NewGuid();
        var deniedStartOperationId = Uuid7.NewGuid();
        var deniedEntryOperationId = Uuid7.NewGuid();
        var deniedEntryPayload = EntryPayload(
            deniedEntryOperationId, Uuid7.NewGuid(), started.SessionId, 2,
            scenario.ProductId, scenario.BagTypeId, "1.000", "1.000000", "1.00");
        var deniedSubmitOperationId = Uuid7.NewGuid();
        var deniedSubmitPayload = Payload(new
        {
            operationId = deniedSubmitOperationId,
            sessionId = started.SessionId,
            localSequence = 2,
            submittedAtDeviceUtc = UtcNow,
        });
        var before = await ReadReceivingMutationFingerprintAsync(database, started.SessionId);

        var deniedRequests = new[]
        {
            SendAsync(client, scenario.Identity.OwnerToken, HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                Batch(StartOperationBody(scenario, deniedStartSessionId, deniedStartOperationId, "OWNER-START"))),
            SendAsync(client, scenario.Identity.OwnerToken, HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                Batch(OperationBody(deniedEntryOperationId, started.SessionId, 2, 1, started.LeaseId, deniedEntryPayload))),
            SendAsync(client, scenario.Identity.OwnerToken, HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                Batch(new
                {
                    operationId = deniedSubmitOperationId,
                    operationType = CommercialReceivingOperationTypes.Submit,
                    sessionId = started.SessionId,
                    localSequence = 2,
                    ownershipGeneration = 1,
                    expectedCloudVersion = (long?)null,
                    payloadJson = deniedSubmitPayload,
                    payloadHash = CommercialReceivingRequestHash.PayloadHash(deniedSubmitPayload),
                    lease = new { leaseId = started.LeaseId },
                })),
            SendAsync(client, scenario.Identity.OwnerToken, HttpMethod.Post,
                $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/lease/heartbeat",
                new { ownershipGeneration = 1, leaseId = started.LeaseId }, "owner-heartbeat"),
            SendAsync(client, scenario.Identity.OwnerToken, HttpMethod.Post,
                $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/lease/reacquire",
                new { ownershipGeneration = 1 }, "owner-reacquire"),
        };
        foreach (var request in deniedRequests)
        {
            using var response = await request;
            using var json = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("AUTHORIZATION_DENIED", ErrorCode(json));
        }

        Assert.Equal(before, await ReadReceivingMutationFingerprintAsync(database, started.SessionId));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id",
            0,
            ("id", deniedStartSessionId));
    }

    [Fact]
    public async Task Stale_master_claim_is_durable_and_changed_retry_must_use_a_new_operation_id()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-STALE-CLAIM");
        using (var update = await SendAsync(
                   client,
                   scenario.Identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/procurement/suppliers/{scenario.SupplierId:D}",
                   new
                   {
                       name = "Receiving Supplier Updated",
                       supplierType = "Business",
                       productScopeMode = "Unrestricted",
                   },
                   "stale-claim-supplier-update",
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        }

        var sessionId = Uuid7.NewGuid();
        var operationId = Uuid7.NewGuid();
        var staleBody = Batch(StartOperationBody(scenario, sessionId, operationId, "STALE-MASTER", supplierVersion: 1));
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var response = await SendAsync(
                client,
                scenario.Identity.OperatorToken,
                HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                staleBody);
            using var json = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("NeedsAttention", Operation(json).GetProperty("status").GetString());
            Assert.Equal("RECEIVING_SUPPLIER_VERSION_STALE", Operation(json).GetProperty("error").GetProperty("code").GetString());
        }
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM sync.commercial_receiving_operation_claims WHERE operation_id = @id AND state = 2",
            1,
            ("id", operationId));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_reservations WHERE operation_id = @id AND consumed_at_utc IS NULL",
            1,
            ("id", operationId));

        using (var changed = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(StartOperationBody(scenario, sessionId, operationId, "STALE-MASTER", supplierVersion: 2))))
        using (var json = await ReadJsonAsync(changed))
        {
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT", Operation(json).GetProperty("error").GetProperty("code").GetString());
        }

        var correctedOperationId = Uuid7.NewGuid();
        var correctedSessionId = Uuid7.NewGuid();
        using (var corrected = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(StartOperationBody(scenario, correctedSessionId, correctedOperationId, "STALE-MASTER-CORRECTED", supplierVersion: 2))))
        using (var json = await ReadJsonAsync(corrected))
        {
            Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
            Assert.Equal("Accepted", Operation(json).GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Master_sync_publishes_explicit_v1_selection_payloads_for_all_supported_types()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-MASTER-CONTRACT");

        await CreateIdAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/vehicles",
            new
            {
                code = "TRUCK",
                registrationNumber = "KA 01 AB 1234",
                displayName = "Selection truck",
                vehicleType = "Truck",
            },
            "master-contract-vehicle",
            HttpStatusCode.Created);
        await CreateIdAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            $"/api/v1/catalog/products/{scenario.ProductId:D}/bag-standards",
            new
            {
                bagTypeId = scenario.BagTypeId,
                label = "Standard 50.125",
                standardContentWeightKg = "50.125000",
                isDefault = true,
            },
            "master-contract-standard",
            HttpStatusCode.Created);
        await CreateIdAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/suppliers",
            new
            {
                code = "RESTRICTED",
                name = "Restricted Selection Supplier",
                supplierType = "Business",
                productScopeMode = "Restricted",
                initialProductIds = new[] { scenario.ProductId },
                contactName = "Protected Contact",
                contactNumber = "+91 90000 00000",
                email = "protected@example.test",
                addressLine = "Protected address",
                taxRegistrationNumber = "GST 01 AB",
                notes = "Protected notes",
            },
            "master-contract-restricted-supplier",
            HttpStatusCode.Created);

        using var response = await SendAsync(
            client,
            scenario.Identity.OperatorToken,
            HttpMethod.Get,
            "/api/v1/mobile/commercial-sync/masters?limit=100");
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var changes = json.RootElement.GetProperty("changes").EnumerateArray().ToArray();
        var expectedTypes = new[]
        {
            "CompanyProcurementSettings", "BusinessLocation", "ReceivingVehicle",
            "BagType", "WeightProcessingPolicy", "Supplier",
            "SupplierProductScope", "ProductGroup", "Product",
            "ProductStandardBagWeight",
        };
        foreach (var type in expectedTypes)
        {
            var change = changes.Last(item => item.GetProperty("masterType").GetString() == type);
            var payload = change.GetProperty("payload");
            Assert.Equal(1, payload.GetProperty("contractVersion").GetInt32());
            Assert.Equal(change.GetProperty("masterVersion").GetInt64(), payload.GetProperty("version").GetInt64());
            Assert.All(payload.EnumerateObject(), property => Assert.DoesNotContain('_', property.Name));
        }

        var supplierPayload = changes.Last(item => item.GetProperty("masterType").GetString() == "Supplier").GetProperty("payload");
        foreach (var protectedName in new[] { "contactName", "contactNumber", "email", "addressLine", "taxRegistrationNumber", "notes" })
        {
            Assert.False(supplierPayload.TryGetProperty(protectedName, out _));
        }
        var bagPayload = changes.Last(item => item.GetProperty("masterType").GetString() == "BagType").GetProperty("payload");
        Assert.Equal("0.200000", bagPayload.GetProperty("standardTareWeightKg").GetString());
        var standardPayload = changes.Last(item => item.GetProperty("masterType").GetString() == "ProductStandardBagWeight").GetProperty("payload");
        Assert.Equal("50.125000", standardPayload.GetProperty("standardContentWeightKg").GetString());
    }

    [Fact]
    public async Task Concurrent_starts_allocate_unique_references_and_batch_limit_is_atomic()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-CONCURRENT");
        var firstBody = StartOperationBody(
            scenario,
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            "CONCURRENT-A");

        using (var oversized = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   new { operations = Enumerable.Repeat(firstBody, 51).ToArray() }))
        using (var json = await ReadJsonAsync(oversized))
        {
            Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
            Assert.Equal(
                "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID",
                ErrorCode(json));
        }

        var secondBody = StartOperationBody(
            scenario,
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            "CONCURRENT-B");
        var pending = new[]
        {
            SendAsync(
                client,
                scenario.Identity.OperatorToken,
                HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                Batch(firstBody)),
            SendAsync(
                client,
                scenario.Identity.OperatorToken,
                HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                Batch(secondBody)),
        };
        var responses = await Task.WhenAll(pending);
        try
        {
            var documents = new List<JsonDocument>();
            try
            {
                foreach (var response in responses)
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    documents.Add(await ReadJsonAsync(response));
                }

                var references = documents.Select(document =>
                        Operation(document).GetProperty("cloud")
                            .GetProperty("cloudReference").GetString())
                    .ToArray();
                Assert.Equal(2, references.Distinct().Count());
                Assert.Equal(
                    new[] { "RCV-000001", "RCV-000002" },
                    references.Order(StringComparer.Ordinal).ToArray());
            }
            finally
            {
                foreach (var document in documents)
                {
                    document.Dispose();
                }
            }
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_sessions",
            2);
    }

    [Fact]
    public async Task Concurrent_Starts_for_one_Session_allocate_once_and_exact_replay_is_stable()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(
            client,
            "RECEIVING-SESSION-REFERENCE-RACE");
        var sessionId = Uuid7.NewGuid();
        var firstOperationId = Uuid7.NewGuid();
        var secondOperationId = Uuid7.NewGuid();
        var firstBody = StartOperationBody(
            scenario,
            sessionId,
            firstOperationId,
            "SESSION-RACE-A");
        var secondBody = StartOperationBody(
            scenario,
            sessionId,
            secondOperationId,
            "SESSION-RACE-B");

        var responses = await Task.WhenAll(
            SendAsync(
                client,
                scenario.Identity.OperatorToken,
                HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                Batch(firstBody)),
            SendAsync(
                client,
                scenario.Identity.OperatorToken,
                HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                Batch(secondBody)));
        object winningBody;
        try
        {
            var results = new List<(JsonDocument Document, object Body)>();
            try
            {
                for (var index = 0; index < responses.Length; index++)
                {
                    Assert.Equal(HttpStatusCode.OK, responses[index].StatusCode);
                    results.Add((
                        await ReadJsonAsync(responses[index]),
                        index == 0 ? firstBody : secondBody));
                }

                var accepted = Assert.Single(
                    results,
                    result => Operation(result.Document)
                        .GetProperty("status").GetString() == "Accepted");
                var rejected = Assert.Single(
                    results,
                    result => Operation(result.Document)
                        .GetProperty("status").GetString() == "Rejected");
                Assert.Equal(
                    "RECEIVING_REFERENCE_CONFLICT",
                    Operation(rejected.Document).GetProperty("error")
                        .GetProperty("code").GetString());
                Assert.Equal(
                    "RCV-000001",
                    Operation(accepted.Document).GetProperty("cloud")
                        .GetProperty("cloudReference").GetString());
                winningBody = accepted.Body;
            }
            finally
            {
                foreach (var result in results)
                {
                    result.Document.Dispose();
                }
            }
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_reservations WHERE session_id = @id",
            1,
            ("id", sessionId));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id AND cloud_reference = 'RCV-000001'",
            1,
            ("id", sessionId));
        await AssertCountAsync(
            database,
            "SELECT next_number FROM procurement.commercial_receiving_reference_counters",
            2);
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.idempotency_records WHERE idempotency_key IN (@first, @second) AND status = 2",
            1,
            ("first", firstOperationId.ToString("D")),
            ("second", secondOperationId.ToString("D")));

        using var replay = await SendAsync(
            client,
            scenario.Identity.OperatorToken,
            HttpMethod.Post,
            "/api/v1/mobile/commercial-sync/operations",
            Batch(winningBody));
        using var replayJson = await ReadJsonAsync(replay);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(
            "PreviouslyProcessed",
            Operation(replayJson).GetProperty("status").GetString());
        Assert.Equal(
            "RCV-000001",
            Operation(replayJson).GetProperty("cloud")
                .GetProperty("cloudReference").GetString());
    }

    [Fact]
    public async Task Direct_SQL_cannot_reserve_a_second_reference_for_one_Session()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(
            client,
            "RECEIVING-DIRECT-SESSION-RESERVATION");
        var started = await StartSessionAsync(
            client,
            scenario,
            "DIRECT-SESSION-RESERVATION");
        var operationId = Uuid7.NewGuid();
        var requestHash = new string('b', 64);

        await using var connection = await database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync(
            CancellationToken);
        await using (var claim = connection.CreateCommand())
        {
            claim.Transaction = transaction;
            claim.CommandText =
                """
                INSERT INTO sync.commercial_receiving_operation_claims(
                    workspace_id, company_id, command_scope, operation_id,
                    operation_type, session_id, device_id, ownership_generation,
                    request_hash, state, attention_code, attention_message,
                    retryable, first_seen_at_utc, completed_at_utc,
                    created_at_utc, updated_at_utc)
                SELECT workspace_id, company_id,
                       'Procurement.CommercialReceiving.MobileSyncOperation',
                       @operation_id, 'StartCommercialReceivingSession',
                       session_id, device_id, NULL, @request_hash, 1,
                       NULL, NULL, false, first_seen_at_utc, NULL,
                       created_at_utc, updated_at_utc
                FROM sync.commercial_receiving_operation_claims
                WHERE session_id = @session_id
                  AND operation_type = 'StartCommercialReceivingSession'
                LIMIT 1
                """;
            claim.Parameters.AddWithValue("operation_id", operationId);
            claim.Parameters.AddWithValue("session_id", started.SessionId);
            claim.Parameters.AddWithValue("request_hash", requestHash);
            Assert.Equal(
                1,
                await claim.ExecuteNonQueryAsync(CancellationToken));
        }

        await using (var duplicate = connection.CreateCommand())
        {
            duplicate.Transaction = transaction;
            duplicate.CommandText =
                """
                INSERT INTO procurement.commercial_receiving_reference_reservations(
                    id, workspace_id, company_id, policy_id, period_key,
                    operation_id, session_id, request_hash, policy_version,
                    sequence, rendered_reference, reserved_at_utc, created_at_utc)
                SELECT @id, workspace_id, company_id, policy_id, period_key,
                       @operation_id, session_id, @request_hash, policy_version,
                       sequence + 1000, rendered_reference || '-DUPLICATE',
                       reserved_at_utc, created_at_utc
                FROM procurement.commercial_receiving_reference_reservations
                WHERE session_id = @session_id
                """;
            duplicate.Parameters.AddWithValue("id", Uuid7.NewGuid());
            duplicate.Parameters.AddWithValue("operation_id", operationId);
            duplicate.Parameters.AddWithValue("session_id", started.SessionId);
            duplicate.Parameters.AddWithValue("request_hash", requestHash);
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => duplicate.ExecuteNonQueryAsync(CancellationToken));
            Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
            Assert.Equal(
                "ux_commercial_receiving_reference_reservations_session",
                exception.ConstraintName);
        }
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task Reserved_reference_survives_policy_update_and_new_Start_uses_new_format()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(
            client,
            "RECEIVING-REFERENCE-POLICY-SNAPSHOT");
        var originalSessionId = Uuid7.NewGuid();
        var originalOperationId = Uuid7.NewGuid();
        var originalBody = StartOperationBody(
            scenario,
            originalSessionId,
            originalOperationId,
            "POLICY-SNAPSHOT-ORIGINAL");

        await InstallCommercialOutboxFailureAsync(
            database,
            "CommercialReceivingSessionStarted");
        using (var failed = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(originalBody)))
        using (var failedJson = await ReadJsonAsync(failed))
        {
            Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
            Assert.Equal(
                "NeedsAttention",
                Operation(failedJson).GetProperty("status").GetString());
        }
        await RemoveCommercialOutboxFailureAsync(database);

        using (var update = await SendAsync(
                   client,
                   scenario.Identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/receiving-reference-policy",
                   new
                   {
                       formatTemplate = "NEW-{SEQ:0000}",
                       resetPolicy = "Never",
                       startingNumber = 1,
                   },
                   "reference-policy-after-reservation",
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        }

        using (var retry = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(originalBody)))
        using (var retryJson = await ReadJsonAsync(retry))
        {
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Equal(
                "Accepted",
                Operation(retryJson).GetProperty("status").GetString());
            Assert.Equal(
                "RCV-000001",
                Operation(retryJson).GetProperty("cloud")
                    .GetProperty("cloudReference").GetString());
        }

        var next = await StartSessionAsync(
            client,
            scenario,
            "POLICY-SNAPSHOT-NEXT");
        Assert.Equal("NEW-0002", next.CloudReference);
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_reservations WHERE policy_version = 1 AND rendered_reference = 'RCV-000001'",
            1);
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_reservations WHERE policy_version = 2 AND rendered_reference = 'NEW-0002'",
            1);
        await AssertCountAsync(
            database,
            "SELECT count(DISTINCT rendered_reference) FROM procurement.commercial_receiving_reference_reservations",
            2);
        await AssertCountAsync(
            database,
            "SELECT next_number FROM procurement.commercial_receiving_reference_counters",
            3);
    }

    [Fact]
    public async Task Structurally_invalid_Starts_are_rejected_before_reference_allocation()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(
            client,
            "RECEIVING-START-PREFLIGHT");

        var attempts = new List<(Guid OperationId, Guid SessionId, string Payload)>
        {
            (Uuid7.NewGuid(), Uuid7.NewGuid(), "{"),
        };
        void AddPayload(Func<Guid, Guid, object> payload)
        {
            var operationId = Uuid7.NewGuid();
            var sessionId = Uuid7.NewGuid();
            attempts.Add((operationId, sessionId, Payload(payload(operationId, sessionId))));
        }

        AddPayload((operationId, sessionId) => new
        {
            operationId,
            sessionId,
            localSequence = 1,
            vehicleSelectionMode = "Optional",
            startedAtDeviceUtc = UtcNow,
        });
        AddPayload((operationId, sessionId) => ValidStartPayload(
            scenario,
            operationId,
            Uuid7.NewGuid(),
            externalReference: "IDENTITY-MISMATCH"));
        AddPayload((operationId, sessionId) => ValidStartPayload(
            scenario,
            operationId,
            Guid.NewGuid(),
            externalReference: "NON-UUID7-SESSION"));
        AddPayload((operationId, sessionId) => ValidStartPayload(
            scenario,
            operationId,
            sessionId,
            startedAtDeviceUtc: DateTimeOffset.MinValue,
            externalReference: "INVALID-TIMESTAMP"));
        AddPayload((operationId, sessionId) => ValidStartPayload(
            scenario,
            operationId,
            sessionId,
            externalReference: " invalid "));
        AddPayload((operationId, sessionId) => new
        {
            operationId,
            sessionId,
            localSequence = 1,
            supplierId = scenario.SupplierId,
            supplierVersion = 0,
            companyProcurementSettingsId = scenario.SettingsId,
            procurementSettingsVersion = 0,
            destinationLocationId = scenario.LocationId,
            destinationLocationVersion = 0,
            weightProcessingPolicyId = scenario.WeightPolicyId,
            weightProcessingPolicyVersion = 0,
            vehicleSelectionMode = "Optional",
            receivingVehicleId = (Guid?)null,
            receivingVehicleVersion = (long?)null,
            externalReference = "INVALID-VERSIONS",
            startedAtDeviceUtc = UtcNow,
        });

        foreach (var attempt in attempts)
        {
            using var response = await SendAsync(
                client,
                scenario.Identity.OperatorToken,
                HttpMethod.Post,
                "/api/v1/mobile/commercial-sync/operations",
                Batch(StartOperationFromPayload(
                    attempt.OperationId,
                    attempt.SessionId,
                    attempt.Payload)));
            using var json = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var operation = Operation(json);
            Assert.Equal("Rejected", operation.GetProperty("status").GetString());
            Assert.Equal(
                "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID",
                operation.GetProperty("error").GetProperty("code").GetString());
            await AssertCountAsync(
                database,
                "SELECT count(*) FROM sync.commercial_receiving_operation_claims WHERE state = 3 AND operation_id = @id",
                1,
                ("id", attempt.OperationId));
        }

        await AssertCountAsync(
            database,
            "SELECT count(*) FROM sync.commercial_receiving_operation_claims WHERE state = 3 AND operation_id = ANY(@ids)",
            attempts.Count,
            ("ids", attempts.Select(attempt => attempt.OperationId).ToArray()));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_reservations",
            0);
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_counters",
            0);
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_sessions",
            0);
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.audit_events WHERE action = 'Procurement.CommercialReceiving.SessionStarted'",
            0);
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.outbox_messages WHERE event_type = 'CommercialReceivingSessionStarted'",
            0);
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.idempotency_records WHERE idempotency_key = ANY(@keys)",
            0,
            ("keys", attempts.Select(attempt => attempt.OperationId.ToString("D")).ToArray()));
    }

    [Fact]
    public async Task Start_locks_supplier_and_vehicle_until_the_receiving_snapshot_commits()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-START-LOCKS");
        var vehicleId = await CreateIdAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/vehicles",
            new
            {
                code = "LOCK-TRUCK",
                registrationNumber = "KA 01 LOCK 7",
                displayName = "Lock test truck",
                vehicleType = "Truck",
            },
            "receiving-start-locks-vehicle",
            HttpStatusCode.Created);

        await InstallReceivingCommitGateAsync(database, "CommercialReceivingSessionStarted");
        try
        {
            long supplierVersion = 1;
            long vehicleVersion = 1;
            foreach (var target in new[] { "Supplier", "Vehicle" })
            {
                var sessionId = Uuid7.NewGuid();
                var operationId = Uuid7.NewGuid();
                var payload = Payload(new
                {
                    operationId,
                    sessionId,
                    localSequence = 1,
                    supplierId = scenario.SupplierId,
                    supplierVersion,
                    companyProcurementSettingsId = scenario.SettingsId,
                    procurementSettingsVersion = 1,
                    destinationLocationId = scenario.LocationId,
                    destinationLocationVersion = 1,
                    weightProcessingPolicyId = scenario.WeightPolicyId,
                    weightProcessingPolicyVersion = 1,
                    vehicleSelectionMode = "Optional",
                    receivingVehicleId = vehicleId,
                    receivingVehicleVersion = vehicleVersion,
                    externalReference = $"LOCK-{target}",
                    startedAtDeviceUtc = UtcNow,
                });
                var request = Batch(new
                {
                    operationId,
                    operationType = CommercialReceivingOperationTypes.Start,
                    sessionId,
                    localSequence = 1,
                    ownershipGeneration = (long?)null,
                    expectedCloudVersion = (long?)null,
                    payloadJson = payload,
                    payloadHash = CommercialReceivingRequestHash.PayloadHash(payload),
                    lease = (object?)null,
                });
                var expectedSnapshotVersion = target == "Supplier"
                    ? supplierVersion
                    : vehicleVersion;

                await RunReceivingMasterRaceAsync(
                    database,
                    () => SendAsync(
                        client,
                        scenario.Identity.OperatorToken,
                        HttpMethod.Post,
                        "/api/v1/mobile/commercial-sync/operations",
                        request),
                    target == "Supplier" ? "procurement.suppliers" : "procurement.receiving_vehicles",
                    target == "Supplier" ? scenario.SupplierId : vehicleId,
                    target == "Supplier"
                        ? "name = name || ' committed-later'"
                        : "display_name = display_name || ' committed-later'");
                await AssertCountAsync(
                    database,
                    target == "Supplier"
                        ? "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id AND supplier_version_snapshot = @version"
                        : "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id AND receiving_vehicle_version_snapshot = @version",
                    1,
                    ("id", sessionId),
                    ("version", expectedSnapshotVersion));

                if (target == "Supplier") supplierVersion++;
                else vehicleVersion++;
            }
        }
        finally
        {
            await RemoveReceivingCommitGateAsync(database);
        }
    }

    [Fact]
    public async Task Start_lock_order_serializes_settings_weight_and_reference_policy_mutations()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(
            client,
            "RECEIVING-START-CANONICAL-LOCKS");

        await InstallReceivingCommitGateAsync(
            database,
            "CommercialReceivingSessionStarted");
        try
        {
            long settingsVersion = 1;
            long weightPolicyVersion = 1;
            var settingsSessionId = Uuid7.NewGuid();
            var settingsOperationId = Uuid7.NewGuid();
            var settingsPayload = Payload(ValidStartPayload(
                scenario,
                settingsOperationId,
                settingsSessionId,
                settingsVersion: settingsVersion,
                weightPolicyVersion: weightPolicyVersion,
                externalReference: "LOCK-SETTINGS"));
            await RunReceivingMasterRaceAsync(
                database,
                () => SendAsync(
                    client,
                    scenario.Identity.OperatorToken,
                    HttpMethod.Post,
                    "/api/v1/mobile/commercial-sync/operations",
                    Batch(StartOperationFromPayload(
                        settingsOperationId,
                        settingsSessionId,
                        settingsPayload))),
                "procurement.company_procurement_settings",
                scenario.SettingsId,
                "vehicle_selection_mode = 2");
            await AssertCountAsync(
                database,
                "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id AND procurement_settings_version_snapshot = 1",
                1,
                ("id", settingsSessionId));
            settingsVersion++;

            var weightSessionId = Uuid7.NewGuid();
            var weightOperationId = Uuid7.NewGuid();
            var weightPayload = Payload(ValidStartPayload(
                scenario,
                weightOperationId,
                weightSessionId,
                settingsVersion: settingsVersion,
                weightPolicyVersion: weightPolicyVersion,
                vehicleSelectionMode: "Disabled",
                externalReference: "LOCK-WEIGHT"));
            await RunReceivingMasterRaceAsync(
                database,
                () => SendAsync(
                    client,
                    scenario.Identity.OperatorToken,
                    HttpMethod.Post,
                    "/api/v1/mobile/commercial-sync/operations",
                    Batch(StartOperationFromPayload(
                        weightOperationId,
                        weightSessionId,
                        weightPayload))),
                "procurement.weight_processing_policies",
                scenario.WeightPolicyId,
                "name = name || ' committed-later'");
            await AssertCountAsync(
                database,
                "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id AND weight_policy_version_snapshot = 1",
                1,
                ("id", weightSessionId));
            weightPolicyVersion++;

            var referencePolicyId = await ReadGuidAsync(
                database,
                "SELECT id FROM procurement.commercial_receiving_reference_policies");
            var referenceSessionId = Uuid7.NewGuid();
            var referenceOperationId = Uuid7.NewGuid();
            var referencePayload = Payload(ValidStartPayload(
                scenario,
                referenceOperationId,
                referenceSessionId,
                settingsVersion: settingsVersion,
                weightPolicyVersion: weightPolicyVersion,
                vehicleSelectionMode: "Disabled",
                externalReference: "LOCK-REFERENCE-POLICY"));
            await RunReceivingMasterRaceAsync(
                database,
                () => SendAsync(
                    client,
                    scenario.Identity.OperatorToken,
                    HttpMethod.Post,
                    "/api/v1/mobile/commercial-sync/operations",
                    Batch(StartOperationFromPayload(
                        referenceOperationId,
                        referenceSessionId,
                        referencePayload))),
                "procurement.commercial_receiving_reference_policies",
                referencePolicyId,
                "format_template = 'LOCK-{SEQ:0000}'");
            await AssertCountAsync(
                database,
                "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id AND reference_policy_version_snapshot = 1",
                1,
                ("id", referenceSessionId));
        }
        finally
        {
            await RemoveReceivingCommitGateAsync(database);
        }
    }

    [Fact]
    public async Task Entry_locks_product_scope_bag_and_standard_until_snapshot_commit()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-ENTRY-LOCKS");
        var standardId = await CreateIdAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            $"/api/v1/catalog/products/{scenario.ProductId:D}/bag-standards",
            new
            {
                bagTypeId = scenario.BagTypeId,
                label = "Lock standard",
                standardContentWeightKg = "50.000000",
                isDefault = true,
            },
            "receiving-entry-locks-standard",
            HttpStatusCode.Created);
        var backupProductId = await CreateIdAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/catalog/products",
            new
            {
                productGroupId = scenario.GroupId,
                code = "LOCK-BACKUP",
                name = "Lock backup product",
                productType = "RawMaterial",
                isPurchasable = true,
                processingFamilyCode = "PADDY",
            },
            "receiving-entry-locks-backup-product",
            HttpStatusCode.Created);
        var restrictedSupplierId = await CreateIdAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/suppliers",
            new
            {
                code = "LOCK-RESTRICTED",
                name = "Restricted lock supplier",
                supplierType = "Business",
                productScopeMode = "Restricted",
                initialProductIds = new[] { scenario.ProductId, backupProductId },
            },
            "receiving-entry-locks-restricted-supplier",
            HttpStatusCode.Created);
        var scopeId = await ReadGuidAsync(
            database,
            "SELECT id FROM procurement.supplier_product_scopes WHERE supplier_id = @id AND product_id = @product_id",
            ("id", restrictedSupplierId),
            ("product_id", scenario.ProductId));
        var restrictedScenario = scenario with { SupplierId = restrictedSupplierId };
        var started = await StartSessionAsync(client, restrictedScenario, "ENTRY-LOCKS");

        await InstallReceivingCommitGateAsync(database, "CommercialReceivingEntryAccepted");
        try
        {
            long productVersion = 1;
            long scopeVersion = 1;
            long bagVersion = 1;
            long standardVersion = 1;
            var targets = new[]
            {
                (Name: "Product", Table: "catalog.products", Id: scenario.ProductId, Change: "name = name || ' committed-later'", Snapshot: "product_version_snapshot"),
                (Name: "Bag", Table: "procurement.bag_types", Id: scenario.BagTypeId, Change: "name = name || ' committed-later'", Snapshot: "bag_type_version_snapshot"),
                (Name: "Standard", Table: "catalog.product_standard_bag_weights", Id: standardId, Change: "label = label || ' committed-later'", Snapshot: "product_standard_bag_weight_version_snapshot"),
                (Name: "Scope", Table: "procurement.supplier_product_scopes", Id: scopeId, Change: "status = 2", Snapshot: "supplier_product_scope_version_snapshot"),
            };

            for (var index = 0; index < targets.Length; index++)
            {
                var target = targets[index];
                var operationId = Uuid7.NewGuid();
                var entryId = Uuid7.NewGuid();
                var sequence = index + 2L;
                var expectedSnapshotVersion = target.Name switch
                {
                    "Product" => productVersion,
                    "Bag" => bagVersion,
                    "Standard" => standardVersion,
                    _ => scopeVersion,
                };
                var payload = Payload(new
                {
                    operationId,
                    entryId,
                    sessionId = started.SessionId,
                    localSequence = sequence,
                    productId = scenario.ProductId,
                    productVersion,
                    supplierProductScopeId = scopeId,
                    supplierProductScopeVersion = scopeVersion,
                    bagTypeId = scenario.BagTypeId,
                    bagTypeVersion = bagVersion,
                    productStandardBagWeightId = standardId,
                    productStandardBagWeightVersion = standardVersion,
                    bagCount = 4,
                    rawWeightKg = "1.000",
                    processedWeightKg = "1.000000",
                    displayWeightKg = "1.00",
                    decimalPlaces = 2,
                    processingMethod = "Standard",
                    weightSource = "ManualScale",
                    capturedAtDeviceUtc = UtcNow,
                });
                var request = Batch(OperationBody(
                    operationId,
                    started.SessionId,
                    sequence,
                    1,
                    started.LeaseId,
                    payload));

                await RunReceivingMasterRaceAsync(
                    database,
                    () => SendAsync(
                        client,
                        scenario.Identity.OperatorToken,
                        HttpMethod.Post,
                        "/api/v1/mobile/commercial-sync/operations",
                        request),
                    target.Table,
                    target.Id,
                    target.Change);
                await AssertCountAsync(
                    database,
                    $"SELECT count(*) FROM procurement.commercial_receiving_entries WHERE operation_id = @id AND {target.Snapshot} = @version",
                    1,
                    ("id", operationId),
                    ("version", expectedSnapshotVersion));

                switch (target.Name)
                {
                    case "Product": productVersion++; break;
                    case "Bag": bagVersion++; break;
                    case "Standard": standardVersion++; break;
                    default: scopeVersion++; break;
                }
            }
        }
        finally
        {
            await RemoveReceivingCommitGateAsync(database);
        }
    }

    [Fact]
    public async Task Expired_lease_reacquires_and_concurrent_owner_transfers_have_one_winner()
    {
        var testClock = new MutableClock(UtcNow);
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            testClock: testClock);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-LEASE-RACE");
        var started = await StartSessionAsync(
            client,
            scenario,
            "LEASE-RACE");

        testClock.Advance(TimeSpan.FromMinutes(61));
        var operatorToken = await LoginAsync(
            client,
            scenario.Identity.WorkspaceCode,
            "operator",
            OperatorPassword,
            scenario.Identity.OperatorDeviceId,
            scenario.Identity.OperatorDeviceSecret);
        var ownerToken = await LoginAsync(
            client,
            scenario.Identity.WorkspaceCode,
            "owner",
            OwnerPassword,
            scenario.Identity.OwnerDeviceId,
            scenario.Identity.OwnerDeviceSecret);
        var entryOperationId = Uuid7.NewGuid();
        var payload = EntryPayload(
            entryOperationId,
            Uuid7.NewGuid(),
            started.SessionId,
            2,
            scenario.ProductId,
            scenario.BagTypeId,
            "10.005",
            "10.010000",
            "10.01");
        using (var expired = await SendAsync(
                   client,
                   operatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(
                       entryOperationId,
                       started.SessionId,
                       2,
                       1,
                       started.LeaseId,
                       payload))))
        using (var json = await ReadJsonAsync(expired))
        {
            Assert.Equal(HttpStatusCode.OK, expired.StatusCode);
            Assert.Equal(
                "NeedsAttention",
                Operation(json).GetProperty("status").GetString());
            Assert.Equal(
                "RECEIVING_LEASE_REACQUISITION_REQUIRED",
                Operation(json).GetProperty("error")
                    .GetProperty("code").GetString());
        }

        Guid renewedLeaseId;
        using (var reacquired = await SendAsync(
                   client,
                   operatorToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/lease/reacquire",
                   new { ownershipGeneration = 1 },
                   "lease-race-reacquire"))
        using (var json = await ReadJsonAsync(reacquired))
        {
            Assert.Equal(HttpStatusCode.OK, reacquired.StatusCode);
            renewedLeaseId = Result(json).GetProperty("leaseId").GetGuid();
            Assert.NotEqual(started.LeaseId, renewedLeaseId);
            Assert.Equal(
                1,
                Result(json).GetProperty("ownershipGeneration").GetInt64());
        }

        using (var accepted = await SendAsync(
                   client,
                   operatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(
                       entryOperationId,
                       started.SessionId,
                       2,
                       1,
                       renewedLeaseId,
                       payload))))
        using (var json = await ReadJsonAsync(accepted))
        {
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            Assert.Equal(
                "Accepted",
                Operation(json).GetProperty("status").GetString());
        }

        var changedPayload = EntryPayload(
            entryOperationId,
            Uuid7.NewGuid(),
            started.SessionId,
            2,
            scenario.ProductId,
            scenario.BagTypeId,
            "11.005",
            "11.010000",
            "11.01");
        using (var changed = await SendAsync(
                   client,
                   operatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(entryOperationId, started.SessionId, 2, 1, renewedLeaseId, changedPayload))))
        using (var json = await ReadJsonAsync(changed))
        {
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            Assert.Equal("Rejected", Operation(json).GetProperty("status").GetString());
            Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT", Operation(json).GetProperty("error").GetProperty("code").GetString());
        }

        var crossTypePayload = Payload(new
        {
            operationId = entryOperationId,
            sessionId = started.SessionId,
            localSequence = 3,
            submittedAtDeviceUtc = testClock.UtcNow,
        });
        using (var crossType = await SendAsync(
                   client,
                   operatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(new
                   {
                       operationId = entryOperationId,
                       operationType = CommercialReceivingOperationTypes.Submit,
                       sessionId = started.SessionId,
                       localSequence = 3,
                       ownershipGeneration = 1,
                       expectedCloudVersion = (long?)null,
                       payloadJson = crossTypePayload,
                       payloadHash = CommercialReceivingRequestHash.PayloadHash(crossTypePayload),
                       lease = new { leaseId = renewedLeaseId },
                   })))
        using (var json = await ReadJsonAsync(crossType))
        {
            Assert.Equal(HttpStatusCode.OK, crossType.StatusCode);
            Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT", Operation(json).GetProperty("error").GetProperty("code").GetString());
        }

        var otherDeviceOperatorToken = await LoginAsync(
            client,
            scenario.Identity.WorkspaceCode,
            "operator",
            OperatorPassword,
            scenario.Identity.OwnerDeviceId,
            scenario.Identity.OwnerDeviceSecret);
        using (var crossDevice = await SendAsync(
                   client,
                   otherDeviceOperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(entryOperationId, started.SessionId, 2, 1, renewedLeaseId, payload))))
        using (var json = await ReadJsonAsync(crossDevice))
        {
            Assert.Equal(HttpStatusCode.OK, crossDevice.StatusCode);
            Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT", Operation(json).GetProperty("error").GetProperty("code").GetString());
        }

        await AssertCountAsync(
            database,
            "SELECT count(*) FROM sync.commercial_receiving_operation_claims WHERE operation_id = @id AND state = 4",
            1,
            ("id", entryOperationId));

        var transfers = await Task.WhenAll(
            SendAsync(
                client,
                ownerToken,
                HttpMethod.Post,
                $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/ownership/transfer",
                new
                {
                    targetDeviceId = scenario.Identity.OwnerDeviceId,
                    expectedOwnershipGeneration = 1,
                    reason = "Concurrent transfer A",
                },
                "lease-race-transfer-a",
                expectedVersion: 2),
            SendAsync(
                client,
                ownerToken,
                HttpMethod.Post,
                $"/api/v1/procurement/receiving-sessions/{started.SessionId:D}/ownership/transfer",
                new
                {
                    targetDeviceId = scenario.Identity.OwnerDeviceId,
                    expectedOwnershipGeneration = 1,
                    reason = "Concurrent transfer B",
                },
                "lease-race-transfer-b",
                expectedVersion: 2));
        try
        {
            var transferBodies = await Task.WhenAll(
                transfers.Select(static response => response.Content.ReadAsStringAsync()));
            Assert.True(
                transfers.Count(response => response.StatusCode == HttpStatusCode.OK) == 1,
                string.Join(Environment.NewLine, transferBodies));
            Assert.True(
                transfers.Count(response => response.StatusCode == HttpStatusCode.Conflict) == 1,
                string.Join(Environment.NewLine, transferBodies));
        }
        finally
        {
            foreach (var transfer in transfers)
            {
                transfer.Dispose();
            }
        }

        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT o.ownership_generation, o.editor_device_id, s.version,
                   (SELECT count(*) FROM platform.outbox_messages
                    WHERE aggregate_id = s.id
                      AND event_type = 'CommercialReceivingOwnershipChanged')
            FROM procurement.commercial_receiving_sessions s
            JOIN procurement.commercial_receiving_ownerships o
              ON o.receiving_session_id = s.id
            WHERE s.id = @id
            """;
        command.Parameters.AddWithValue("id", started.SessionId);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        Assert.True(await reader.ReadAsync(CancellationToken));
        Assert.Equal(2, reader.GetInt64(0));
        Assert.Equal(scenario.Identity.OwnerDeviceId, reader.GetGuid(1));
        Assert.Equal(3, reader.GetInt64(2));
        Assert.Equal(3, reader.GetInt64(3));
    }

    [Fact]
    public async Task Event_pagination_and_master_bootstrap_exclude_concurrent_deltas()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-CURSORS");
        await StartSessionAsync(client, scenario, "CURSOR-A");
        await StartSessionAsync(client, scenario, "CURSOR-B");

        var eventSequences = new List<long>();
        string? eventCursor = null;
        var firstOwnerCursor = string.Empty;
        bool eventHasMore;
        do
        {
            var path = "/api/v1/mobile/commercial-sync/events?limit=1" +
                (eventCursor is null
                    ? string.Empty
                    : $"&cursor={Uri.EscapeDataString(eventCursor)}");
            using var response = await SendAsync(
                client,
                scenario.Identity.OwnerToken,
                HttpMethod.Get,
                path);
            using var json = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var root = json.RootElement;
            var events = root.GetProperty("events").EnumerateArray().ToArray();
            eventSequences.AddRange(events.Select(item =>
                item.GetProperty("sequence").GetInt64()));
            eventCursor = root.GetProperty("nextCursor").GetString();
            firstOwnerCursor = firstOwnerCursor.Length == 0
                ? eventCursor!
                : firstOwnerCursor;
            eventHasMore = root.GetProperty("hasMore").GetBoolean();
        }
        while (eventHasMore);

        Assert.Equal(2, eventSequences.Count);
        Assert.Equal(eventSequences.Order().ToArray(), eventSequences);
        Assert.Equal(2, eventSequences.Distinct().Count());
        using (var invalid = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Get,
                   "/api/v1/mobile/commercial-sync/events?limit=1&cursor=" +
                   Uri.EscapeDataString(firstOwnerCursor)))
        using (var json = await ReadJsonAsync(invalid))
        {
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Equal("COMMERCIAL_SYNC_CURSOR_INVALID", ErrorCode(json));
        }

        using var firstMasterResponse = await SendAsync(
            client,
            scenario.Identity.OwnerToken,
            HttpMethod.Get,
            "/api/v1/mobile/commercial-sync/masters?limit=1");
        using var firstMasterJson = await ReadJsonAsync(firstMasterResponse);
        Assert.Equal(HttpStatusCode.OK, firstMasterResponse.StatusCode);
        var firstMaster = firstMasterJson.RootElement;
        var highWater = firstMaster.GetProperty("bootstrapHighWaterSequence")
            .GetInt64();
        var masterCursor = firstMaster.GetProperty("nextCursor").GetString()!;
        var bootstrapChanges = firstMaster.GetProperty("changes")
            .EnumerateArray().Select(item => new
            {
                Sequence = item.GetProperty("sequence").GetInt64(),
                Type = item.GetProperty("masterType").GetString(),
                Version = item.GetProperty("masterVersion").GetInt64(),
            }).ToList();

        using (var updated = await SendAsync(
                   client,
                   scenario.Identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/catalog/products/{scenario.ProductId:D}",
                   new
                   {
                       productGroupId = scenario.GroupId,
                       name = "Cursor Product Updated",
                       productType = "RawMaterial",
                       isPurchasable = true,
                       processingFamilyCode = "CURSOR",
                   },
                   "receiving-cursor-product-update",
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        }

        bool masterHasMore;
        do
        {
            using var response = await SendAsync(
                client,
                scenario.Identity.OwnerToken,
                HttpMethod.Get,
                "/api/v1/mobile/commercial-sync/masters?limit=2&cursor=" +
                Uri.EscapeDataString(masterCursor));
            using var json = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var root = json.RootElement;
            bootstrapChanges.AddRange(root.GetProperty("changes")
                .EnumerateArray().Select(item => new
                {
                    Sequence = item.GetProperty("sequence").GetInt64(),
                    Type = item.GetProperty("masterType").GetString(),
                    Version = item.GetProperty("masterVersion").GetInt64(),
                }));
            masterCursor = root.GetProperty("nextCursor").GetString()!;
            masterHasMore = root.GetProperty("hasMore").GetBoolean();
        }
        while (masterHasMore);

        Assert.All(
            bootstrapChanges,
            change => Assert.True(change.Sequence <= highWater));
        Assert.DoesNotContain(
            bootstrapChanges,
            change => change.Type == "Product" && change.Version == 2);

        using (var delta = await SendAsync(
                   client,
                   scenario.Identity.OwnerToken,
                   HttpMethod.Get,
                   "/api/v1/mobile/commercial-sync/masters?limit=100&cursor=" +
                   Uri.EscapeDataString(masterCursor)))
        using (var json = await ReadJsonAsync(delta))
        {
            Assert.Equal(HttpStatusCode.OK, delta.StatusCode);
            Assert.Contains(
                json.RootElement.GetProperty("changes").EnumerateArray(),
                item => item.GetProperty("masterType").GetString() == "Product" &&
                        item.GetProperty("masterVersion").GetInt64() == 2);
        }
    }

    [Fact]
    public async Task Commercial_outbox_failures_roll_back_start_and_entry_facts()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-ROLLBACK");

        var rejectedSessionId = Uuid7.NewGuid();
        var rejectedStartOperationId = Uuid7.NewGuid();
        await InstallCommercialOutboxFailureAsync(
            database,
            "CommercialReceivingSessionStarted");
        using (var failedStart = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(StartOperationBody(
                       scenario,
                       rejectedSessionId,
                       rejectedStartOperationId,
                       "ROLLBACK-START"))))
        using (var json = await ReadJsonAsync(failedStart))
        {
            Assert.True(failedStart.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
            Assert.Equal("NeedsAttention", Operation(json).GetProperty("status").GetString());
        }
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_sessions WHERE id = @id",
            0,
            ("id", rejectedSessionId));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM platform.idempotency_records WHERE idempotency_key = @id",
            0,
            ("id", rejectedStartOperationId.ToString("D")));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_reservations WHERE operation_id = @id AND consumed_at_utc IS NULL",
            1,
            ("id", rejectedStartOperationId));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM sync.commercial_receiving_operation_claims WHERE operation_id = @id AND state = 2",
            1,
            ("id", rejectedStartOperationId));

        await RemoveCommercialOutboxFailureAsync(database);
        using (var retriedStart = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(StartOperationBody(
                       scenario,
                       rejectedSessionId,
                       rejectedStartOperationId,
                       "ROLLBACK-START"))))
        using (var json = await ReadJsonAsync(retriedStart))
        {
            Assert.Equal(HttpStatusCode.OK, retriedStart.StatusCode);
            Assert.Equal("Accepted", Operation(json).GetProperty("status").GetString());
            Assert.Equal("RCV-000001", Operation(json).GetProperty("cloud").GetProperty("cloudReference").GetString());
        }
        var started = await StartSessionAsync(
            client,
            scenario,
            "ROLLBACK-ENTRY");
        Assert.Equal("RCV-000002", started.CloudReference);
        var entryOperationId = Uuid7.NewGuid();
        var payload = EntryPayload(
            entryOperationId,
            Uuid7.NewGuid(),
            started.SessionId,
            2,
            scenario.ProductId,
            scenario.BagTypeId,
            "9.995",
            "10.000000",
            "10.00");
        await InstallCommercialOutboxFailureAsync(
            database,
            "CommercialReceivingEntryAccepted");
        using (var failedEntry = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(
                       entryOperationId,
                       started.SessionId,
                       2,
                       1,
                       started.LeaseId,
                       payload))))
        using (var json = await ReadJsonAsync(failedEntry))
        {
            Assert.Equal(HttpStatusCode.OK, failedEntry.StatusCode);
            Assert.Equal("NeedsAttention", Operation(json).GetProperty("status").GetString());
        }

        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT s.version, s.next_expected_local_sequence, s.entry_count,
                   s.processed_total_weight_kg,
                   (SELECT count(*) FROM procurement.commercial_receiving_entries
                    WHERE receiving_session_id = s.id),
                   (SELECT count(*) FROM platform.audit_events
                    WHERE aggregate_id = s.id
                      AND action = 'Procurement.CommercialReceiving.EntryAccepted'),
                   (SELECT count(*) FROM platform.outbox_messages
                    WHERE aggregate_id = s.id
                      AND event_type = 'CommercialReceivingEntryAccepted'),
                   (SELECT count(*) FROM platform.idempotency_records
                    WHERE idempotency_key = @operation_id)
            FROM procurement.commercial_receiving_sessions s
            WHERE s.id = @session_id
            """;
        command.Parameters.AddWithValue("session_id", started.SessionId);
        command.Parameters.AddWithValue(
            "operation_id",
            entryOperationId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);
        Assert.True(await reader.ReadAsync(CancellationToken));
        Assert.Equal(1, reader.GetInt64(0));
        Assert.Equal(2, reader.GetInt64(1));
        Assert.Equal(0, reader.GetInt32(2));
        Assert.Equal(0m, reader.GetDecimal(3));
        Assert.Equal(0, reader.GetInt64(4));
        Assert.Equal(0, reader.GetInt64(5));
        Assert.Equal(0, reader.GetInt64(6));
        Assert.Equal(0, reader.GetInt64(7));
    }

    [Fact]
    public async Task Ownership_transfer_and_submission_outbox_failures_roll_back_every_business_fact()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var scenario = await CreateScenarioAsync(client, "RECEIVING-HANDOFF-ROLLBACK");

        var transferSession = await StartSessionAsync(client, scenario, "TRANSFER-ROLLBACK");
        const string transferKey = "ownership-transfer-rollback";
        var transferBefore = await ReadRollbackFingerprintAsync(
            database, transferSession.SessionId,
            "Procurement.CommercialReceiving.OwnershipTransferred",
            "CommercialReceivingOwnershipChanged", transferKey);
        await InstallCommercialOutboxFailureAsync(database, "CommercialReceivingOwnershipChanged");
        using (var response = await SendAsync(
                   client,
                   scenario.Identity.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/receiving-sessions/{transferSession.SessionId:D}/ownership/transfer",
                   new
                   {
                       targetDeviceId = scenario.Identity.OwnerDeviceId,
                       expectedOwnershipGeneration = 1,
                       reason = "Injected rollback",
                   },
                   transferKey,
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        Assert.Equal(
            transferBefore,
            await ReadRollbackFingerprintAsync(
                database, transferSession.SessionId,
                "Procurement.CommercialReceiving.OwnershipTransferred",
                "CommercialReceivingOwnershipChanged", transferKey));
        await RemoveCommercialOutboxFailureAsync(database);

        var submitSession = await StartSessionAsync(client, scenario, "SUBMIT-ROLLBACK");
        var entryOperationId = Uuid7.NewGuid();
        var entryPayload = EntryPayload(
            entryOperationId, Uuid7.NewGuid(), submitSession.SessionId, 2,
            scenario.ProductId, scenario.BagTypeId, "5.000", "5.000000", "5.00");
        using (var entry = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(OperationBody(entryOperationId, submitSession.SessionId, 2, 1, submitSession.LeaseId, entryPayload))))
        {
            Assert.Equal(HttpStatusCode.OK, entry.StatusCode);
        }

        var submitOperationId = Uuid7.NewGuid();
        var submitPayload = Payload(new
        {
            operationId = submitOperationId,
            sessionId = submitSession.SessionId,
            localSequence = 3,
            submittedAtDeviceUtc = UtcNow,
        });
        var submitBefore = await ReadRollbackFingerprintAsync(
            database, submitSession.SessionId,
            "Procurement.CommercialReceiving.SessionSubmitted",
            "CommercialReceivingSessionSubmitted", submitOperationId.ToString("D"));
        await InstallCommercialOutboxFailureAsync(database, "CommercialReceivingSessionSubmitted");
        using (var response = await SendAsync(
                   client,
                   scenario.Identity.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/mobile/commercial-sync/operations",
                   Batch(new
                   {
                       operationId = submitOperationId,
                       operationType = CommercialReceivingOperationTypes.Submit,
                       sessionId = submitSession.SessionId,
                       localSequence = 3,
                       ownershipGeneration = 1,
                       expectedCloudVersion = (long?)null,
                       payloadJson = submitPayload,
                       payloadHash = CommercialReceivingRequestHash.PayloadHash(submitPayload),
                       lease = new { leaseId = submitSession.LeaseId },
                   })))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
            Assert.Equal("NeedsAttention", Operation(json).GetProperty("status").GetString());
        }
        Assert.Equal(
            submitBefore,
            await ReadRollbackFingerprintAsync(
                database, submitSession.SessionId,
                "Procurement.CommercialReceiving.SessionSubmitted",
                "CommercialReceivingSessionSubmitted", submitOperationId.ToString("D")));
        await AssertCountAsync(
            database,
            "SELECT count(*) FROM sync.commercial_receiving_operation_claims WHERE operation_id = @id AND state = 2",
            1,
            ("id", submitOperationId));
        await RemoveCommercialOutboxFailureAsync(database);
    }

    private static async Task<ReceivingScenario> CreateScenarioAsync(
        HttpClient client,
        string workspaceCode)
    {
        var identity = await CreateIdentityAsync(client, workspaceCode);
        var keyPrefix = workspaceCode.ToLowerInvariant();
        var locationId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/operations/locations",
            new
            {
                code = "YARD",
                name = "Receiving Yard",
                locationType = "Yard",
            },
            $"{keyPrefix}-location",
            HttpStatusCode.Created);
        var weightPolicyId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/weight-policies",
            new
            {
                code = "STD-2",
                name = "Standard two decimals",
                decimalPlaces = 2,
                processingMethod = "Standard",
            },
            $"{keyPrefix}-weight-policy",
            HttpStatusCode.Created);
        var bagTypeId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/bag-types",
            new
            {
                code = "JUTE",
                name = "Jute",
                constructionClass = "Jute",
                standardTareWeightKg = "0.200000",
                isReturnable = true,
            },
            $"{keyPrefix}-bag",
            HttpStatusCode.Created);
        var groupId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/catalog/product-groups",
            new { code = "PADDY", name = "Paddy" },
            $"{keyPrefix}-group",
            HttpStatusCode.Created);
        var productId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/catalog/products",
            new
            {
                productGroupId = groupId,
                code = "PADDY-A",
                name = "Paddy A",
                productType = "RawMaterial",
                isPurchasable = true,
                processingFamilyCode = "PADDY",
            },
            $"{keyPrefix}-product",
            HttpStatusCode.Created);
        var supplierId = await CreateIdAsync(
            client,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/suppliers",
            new
            {
                code = "SUPPLIER",
                name = "Receiving Supplier",
                supplierType = "Business",
                productScopeMode = "Unrestricted",
                initialProductIds = Array.Empty<Guid>(),
            },
            $"{keyPrefix}-supplier",
            HttpStatusCode.Created);
        Guid settingsId;
        using (var response = await SendAsync(
                   client,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationId,
                       defaultWeightProcessingPolicyId = weightPolicyId,
                       vehicleSelectionMode = "Optional",
                   },
                   $"{keyPrefix}-settings"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            settingsId = Result(json).GetProperty("id").GetGuid();
        }

        return new ReceivingScenario(
            identity,
            locationId,
            weightPolicyId,
            bagTypeId,
            groupId,
            productId,
            supplierId,
            settingsId);
    }

    private static object StartOperationBody(
        ReceivingScenario scenario,
        Guid sessionId,
        Guid operationId,
        string externalReference,
        long supplierVersion = 1)
    {
        var payload = Payload(new
        {
            operationId,
            sessionId,
            localSequence = 1,
            supplierId = scenario.SupplierId,
            supplierVersion,
            companyProcurementSettingsId = scenario.SettingsId,
            procurementSettingsVersion = 1,
            destinationLocationId = scenario.LocationId,
            destinationLocationVersion = 1,
            weightProcessingPolicyId = scenario.WeightPolicyId,
            weightProcessingPolicyVersion = 1,
            vehicleSelectionMode = "Optional",
            receivingVehicleId = (Guid?)null,
            receivingVehicleVersion = (long?)null,
            externalReference,
            startedAtDeviceUtc = UtcNow,
        });
        return new
        {
            operationId,
            operationType = CommercialReceivingOperationTypes.Start,
            sessionId,
            localSequence = 1,
            ownershipGeneration = (long?)null,
            expectedCloudVersion = (long?)null,
            payloadJson = payload,
            payloadHash = CommercialReceivingRequestHash.PayloadHash(payload),
            lease = (object?)null,
        };
    }

    private static object ValidStartPayload(
        ReceivingScenario scenario,
        Guid operationId,
        Guid sessionId,
        DateTimeOffset? startedAtDeviceUtc = null,
        string? externalReference = "VALID-START",
        long settingsVersion = 1,
        long weightPolicyVersion = 1,
        string vehicleSelectionMode = "Optional") =>
        new
        {
            operationId,
            sessionId,
            localSequence = 1,
            supplierId = scenario.SupplierId,
            supplierVersion = 1,
            companyProcurementSettingsId = scenario.SettingsId,
            procurementSettingsVersion = settingsVersion,
            destinationLocationId = scenario.LocationId,
            destinationLocationVersion = 1,
            weightProcessingPolicyId = scenario.WeightPolicyId,
            weightProcessingPolicyVersion = weightPolicyVersion,
            vehicleSelectionMode,
            receivingVehicleId = (Guid?)null,
            receivingVehicleVersion = (long?)null,
            externalReference,
            startedAtDeviceUtc = startedAtDeviceUtc ?? UtcNow,
        };

    private static object StartOperationFromPayload(
        Guid operationId,
        Guid sessionId,
        string payload) =>
        new
        {
            operationId,
            operationType = CommercialReceivingOperationTypes.Start,
            sessionId,
            localSequence = 1,
            ownershipGeneration = (long?)null,
            expectedCloudVersion = (long?)null,
            payloadJson = payload,
            payloadHash = CommercialReceivingRequestHash.PayloadHash(payload),
            lease = (object?)null,
        };

    private static async Task<StartedSession> StartSessionAsync(
        HttpClient client,
        ReceivingScenario scenario,
        string externalReference)
    {
        var sessionId = Uuid7.NewGuid();
        var operationId = Uuid7.NewGuid();
        using var response = await SendAsync(
            client,
            scenario.Identity.OperatorToken,
            HttpMethod.Post,
            "/api/v1/mobile/commercial-sync/operations",
            Batch(StartOperationBody(
                scenario,
                sessionId,
                operationId,
                externalReference)));
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var operation = Operation(json);
        Assert.True(
            operation.GetProperty("status").GetString() == "Accepted",
            operation.ToString());
        var cloud = operation.GetProperty("cloud");
        return new StartedSession(
            sessionId,
            cloud.GetProperty("leaseId").GetGuid(),
            cloud.GetProperty("cloudReference").GetString()!);
    }

    private static async Task InstallReceivingCommitGateAsync(
        IsolatedPostgreSqlDatabase database,
        string eventType)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE OR REPLACE FUNCTION platform.gate_test_receiving_commit()
            RETURNS trigger LANGUAGE plpgsql AS $function$
            BEGIN
                IF NEW.event_stream = 3 AND NEW.event_type = '{eventType}' THEN
                    PERFORM pg_advisory_xact_lock(738001);
                END IF;
                RETURN NEW;
            END;
            $function$;
            CREATE TRIGGER tr_gate_test_receiving_commit
            BEFORE INSERT ON platform.outbox_messages
            FOR EACH ROW EXECUTE FUNCTION platform.gate_test_receiving_commit();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task RemoveReceivingCommitGateAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP TRIGGER IF EXISTS tr_gate_test_receiving_commit
                ON platform.outbox_messages;
            DROP FUNCTION IF EXISTS platform.gate_test_receiving_commit();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task RunReceivingMasterRaceAsync(
        IsolatedPostgreSqlDatabase database,
        Func<Task<HttpResponseMessage>> sendReceiving,
        string masterTable,
        Guid masterId,
        string mutableAssignment)
    {
        await using var gateConnection = await database.OpenConnectionAsync();
        await using var gateTransaction = await gateConnection.BeginTransactionAsync(CancellationToken);
        await using (var gateCommand = gateConnection.CreateCommand())
        {
            gateCommand.Transaction = gateTransaction;
            gateCommand.CommandText = "SELECT pg_advisory_xact_lock(738001)";
            await gateCommand.ExecuteNonQueryAsync(CancellationToken);
        }

        var receivingTask = sendReceiving();
        await WaitForAdvisoryWaiterAsync(database);

        await using var mutationConnection = await database.OpenConnectionAsync();
        await using var mutationTransaction = await mutationConnection.BeginTransactionAsync(CancellationToken);
        await using var mutationCommand = mutationConnection.CreateCommand();
        mutationCommand.Transaction = mutationTransaction;
        mutationCommand.CommandText =
            $"UPDATE {masterTable} SET {mutableAssignment}, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id";
        mutationCommand.Parameters.AddWithValue("id", masterId);
        var mutationTask = mutationCommand.ExecuteNonQueryAsync(CancellationToken);
        await Task.Delay(150, CancellationToken);
        Assert.False(mutationTask.IsCompleted);

        await gateTransaction.CommitAsync(CancellationToken);
        using var response = await receivingTask;
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Accepted", Operation(json).GetProperty("status").GetString());
        Assert.Equal(1, await mutationTask);
        await mutationTransaction.CommitAsync(CancellationToken);
    }

    private static async Task WaitForAdvisoryWaiterAsync(
        IsolatedPostgreSqlDatabase database)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var connection = await database.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT EXISTS (SELECT 1 FROM pg_locks WHERE locktype = 'advisory' AND NOT granted)";
            if ((bool)(await command.ExecuteScalarAsync(CancellationToken))!)
            {
                return;
            }

            await Task.Delay(50, CancellationToken);
        }

        Assert.Fail("Receiving did not reach the deterministic commit gate.");
    }

    private static async Task<Guid> ReadGuidAsync(
        IsolatedPostgreSqlDatabase database,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        return (Guid)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static async Task InstallCommercialOutboxFailureAsync(
        IsolatedPostgreSqlDatabase database,
        string eventType)
    {
        Assert.Contains(
            eventType,
            new[]
            {
                "CommercialReceivingSessionStarted",
                "CommercialReceivingEntryAccepted",
                "CommercialReceivingOwnershipChanged",
                "CommercialReceivingSessionSubmitted",
            });
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE OR REPLACE FUNCTION platform.reject_test_commercial_outbox()
            RETURNS trigger LANGUAGE plpgsql AS $function$
            BEGIN
                IF NEW.event_stream = 3 AND NEW.event_type = '{eventType}' THEN
                    RAISE EXCEPTION 'Injected commercial outbox failure.'
                        USING ERRCODE = '55000';
                END IF;
                RETURN NEW;
            END;
            $function$;
            CREATE TRIGGER tr_reject_test_commercial_outbox
            BEFORE INSERT ON platform.outbox_messages
            FOR EACH ROW EXECUTE FUNCTION platform.reject_test_commercial_outbox();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task RemoveCommercialOutboxFailureAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP TRIGGER IF EXISTS tr_reject_test_commercial_outbox
                ON platform.outbox_messages;
            DROP FUNCTION IF EXISTS platform.reject_test_commercial_outbox();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task<string> ReadReceivingMutationFingerprintAsync(
        IsolatedPostgreSqlDatabase database,
        Guid sessionId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT jsonb_build_array(
                (SELECT count(*) FROM procurement.commercial_receiving_sessions),
                (SELECT count(*) FROM procurement.commercial_receiving_entries),
                (SELECT count(*) FROM platform.audit_events),
                (SELECT count(*) FROM platform.outbox_messages),
                (SELECT count(*) FROM platform.commercial_outbox_audiences),
                (SELECT count(*) FROM platform.idempotency_records),
                (SELECT count(*) FROM sync.commercial_receiving_operation_claims),
                o.version, o.ownership_generation, o.editor_device_id, o.lease_id,
                o.lease_expires_at_utc, o.last_heartbeat_at_utc,
                s.version, s.status, s.next_expected_local_sequence)
            FROM procurement.commercial_receiving_sessions s
            JOIN procurement.commercial_receiving_ownerships o
              ON o.receiving_session_id = s.id
            WHERE s.id = @session_id
            """;
        command.Parameters.AddWithValue("session_id", sessionId);
        return (string)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static async Task<string> ReadRollbackFingerprintAsync(
        IsolatedPostgreSqlDatabase database,
        Guid sessionId,
        string auditAction,
        string eventType,
        string idempotencyKey)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT jsonb_build_array(
                s.version, s.status, s.submitted_at_utc, s.next_expected_local_sequence,
                s.entry_count, s.processed_total_weight_kg, s.ownership_generation,
                o.editor_device_id, o.ownership_generation, o.lease_id,
                o.lease_expires_at_utc, o.last_heartbeat_at_utc,
                o.last_transferred_at_utc, o.last_transferred_by_user_id, o.version,
                (SELECT count(*) FROM platform.audit_events WHERE aggregate_id = s.id AND action = @audit_action),
                (SELECT count(*) FROM platform.outbox_messages WHERE aggregate_id = s.id AND event_type = @event_type),
                (SELECT count(*) FROM platform.commercial_outbox_audiences a
                 JOIN platform.outbox_messages m ON m.id = a.outbox_message_id
                 WHERE m.aggregate_id = s.id AND m.event_type = @event_type),
                (SELECT count(*) FROM platform.idempotency_records WHERE idempotency_key = @idempotency_key))
            FROM procurement.commercial_receiving_sessions s
            JOIN procurement.commercial_receiving_ownerships o ON o.receiving_session_id = s.id
            WHERE s.id = @session_id
            """;
        command.Parameters.AddWithValue("session_id", sessionId);
        command.Parameters.AddWithValue("audit_action", auditAction);
        command.Parameters.AddWithValue("event_type", eventType);
        command.Parameters.AddWithValue("idempotency_key", idempotencyKey);
        return (string)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static async Task AssertCountAsync(
        IsolatedPostgreSqlDatabase database,
        string sql,
        long expected,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        Assert.Equal(
            expected,
            (long)(await command.ExecuteScalarAsync(CancellationToken))!);
    }

    private static async Task AssertDatabaseFactsAndGuardsAsync(IsolatedPostgreSqlDatabase database, Guid sessionId, Guid staleOperationId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT
                    (SELECT count(*) FROM procurement.commercial_receiving_entries WHERE receiving_session_id = @session_id),
                    (SELECT count(*) FROM platform.audit_events WHERE aggregate_id = @session_id),
                    (SELECT count(*) FROM platform.outbox_messages WHERE aggregate_id = @session_id AND event_stream = 3),
                    (SELECT count(*) FROM platform.idempotency_records WHERE idempotency_key = @stale_key),
                    (SELECT count(*) FROM platform.outbox_messages WHERE aggregate_id = @session_id AND (event_type ILIKE '%Settlement%' OR event_type ILIKE '%Inventory%' OR event_type ILIKE '%PurchaseBill%' OR event_type ILIKE '%Finance%'))
                """;
            command.Parameters.AddWithValue("session_id", sessionId);
            command.Parameters.AddWithValue("stale_key", staleOperationId.ToString("D"));
            await using var reader = await command.ExecuteReaderAsync(CancellationToken);
            Assert.True(await reader.ReadAsync(CancellationToken));
            Assert.Equal(2L, reader.GetInt64(0));
            Assert.Equal(6L, reader.GetInt64(1));
            Assert.Equal(11L, reader.GetInt64(2));
            Assert.Equal(0L, reader.GetInt64(3));
            Assert.Equal(0L, reader.GetInt64(4));
        }

        await AssertSqlRejectedAsync(connection, "DELETE FROM procurement.commercial_receiving_entries WHERE receiving_session_id = @id", sessionId);
        await AssertSqlRejectedAsync(connection, "UPDATE procurement.commercial_receiving_sessions SET entry_count = entry_count + 1, version = version + 1 WHERE id = @id", sessionId);
        await AssertSqlRejectedAsync(connection, "UPDATE procurement.commercial_receiving_sessions SET version = version + 1, updated_at_utc = updated_at_utc + interval '1 second' WHERE id = @id", sessionId);
        await AssertSqlRejectedAsync(
            connection,
            """
            UPDATE procurement.commercial_receiving_ownerships o
            SET editor_device_id = (SELECT d.id FROM platform.devices d WHERE d.workspace_id = o.workspace_id AND d.id <> o.editor_device_id ORDER BY d.id LIMIT 1),
                ownership_generation = ownership_generation + 1,
                lease_id = NULL, lease_expires_at_utc = NULL, last_heartbeat_at_utc = NULL,
                last_transferred_at_utc = updated_at_utc + interval '1 second',
                last_transferred_by_user_id = (SELECT u.id FROM platform.users u WHERE u.workspace_id = o.workspace_id ORDER BY u.id LIMIT 1),
                version = version + 1, updated_at_utc = updated_at_utc + interval '1 second'
            WHERE receiving_session_id = @id
            """,
            sessionId);
        await AssertSqlRejectedAsync(connection, "UPDATE procurement.commercial_receiving_ownerships SET ownership_generation = ownership_generation + 2, version = version + 1, updated_at_utc = updated_at_utc + interval '1 second' WHERE receiving_session_id = @id", sessionId);
        await AssertSqlRejectedAsync(connection, "UPDATE procurement.commercial_receiving_ownerships SET last_transferred_at_utc = updated_at_utc + interval '1 second', version = version + 1, updated_at_utc = updated_at_utc + interval '2 seconds' WHERE receiving_session_id = @id", sessionId);
        await AssertSqlRejectedAsync(
            connection,
            "UPDATE platform.commercial_outbox_audiences SET target_device_id = NULL WHERE audience = 2 AND outbox_message_id IN (SELECT id FROM platform.outbox_messages WHERE aggregate_id = @id AND event_stream = 3)",
            sessionId);
        await AssertSqlRejectedAsync(
            connection,
            """
            INSERT INTO platform.commercial_outbox_audiences(outbox_message_id, workspace_id, company_id, audience, target_device_id)
            SELECT m.id, m.workspace_id, s.company_id, 1, NULL
            FROM platform.outbox_messages m
            JOIN procurement.commercial_receiving_sessions s ON s.id = @id
            WHERE m.event_stream <> 3
            ORDER BY m.sequence LIMIT 1
            """,
            sessionId);
        await AssertSqlRejectedAsync(
            connection,
            """
            INSERT INTO platform.outbox_messages(
                id, workspace_id, event_stream, event_type, event_version,
                aggregate_type, aggregate_id, aggregate_version, payload_json,
                correlation_id, occurred_at_utc, status, attempt_count)
            SELECT @id, workspace_id, 3, 'DirectCommercialWithoutAudience', 1,
                   aggregate_type, aggregate_id, aggregate_version, payload_json,
                   correlation_id, occurred_at_utc, 1, 0
            FROM platform.outbox_messages
            ORDER BY sequence LIMIT 1
            """,
            staleOperationId);
        await AssertSqlRejectedAsync(connection, "UPDATE sync.commercial_master_changes SET status = 'Inactive' WHERE sequence = (SELECT min(sequence) FROM sync.commercial_master_changes)", sessionId, useParameter: false);
    }

    private static async Task AssertSqlRejectedAsync(NpgsqlConnection connection, string sql, Guid id, bool useParameter = true)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (useParameter) command.Parameters.AddWithValue("id", id);
        await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync(CancellationToken));
    }

    private static object OperationBody(Guid operationId, Guid sessionId, long sequence, long generation, Guid leaseId, string payload) => new
    {
        operationId,
        operationType = CommercialReceivingOperationTypes.RecordEntry,
        sessionId,
        localSequence = sequence,
        ownershipGeneration = generation,
        expectedCloudVersion = (long?)null,
        payloadJson = payload,
        payloadHash = CommercialReceivingRequestHash.PayloadHash(payload),
        lease = new { leaseId },
    };

    private static string EntryPayload(Guid operationId, Guid entryId, Guid sessionId, long sequence, Guid productId, Guid bagTypeId, string raw, string processed, string display) => Payload(new
    {
        operationId,
        entryId,
        sessionId,
        localSequence = sequence,
        productId,
        productVersion = 1,
        supplierProductScopeId = (Guid?)null,
        supplierProductScopeVersion = (long?)null,
        bagTypeId,
        bagTypeVersion = 1,
        productStandardBagWeightId = (Guid?)null,
        productStandardBagWeightVersion = (long?)null,
        bagCount = 4,
        rawWeightKg = raw,
        processedWeightKg = processed,
        displayWeightKg = display,
        decimalPlaces = 2,
        processingMethod = "Standard",
        weightSource = "ManualScale",
        capturedAtDeviceUtc = UtcNow,
    });

    private static object Batch(object operation) => new { operations = new[] { operation } };
    private static string Payload(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static async Task<Guid> CreateIdAsync(HttpClient client, string token, HttpMethod method, string path, object body, string key, HttpStatusCode expected)
    {
        using var response = await SendAsync(client, token, method, path, body, key);
        using var json = await ReadJsonAsync(response);
        Assert.True(response.StatusCode == expected, json.RootElement.ToString());
        return Result(json).GetProperty("id").GetGuid();
    }

    private static async Task<CommercialIdentity> CreateIdentityAsync(HttpClient client, string workspaceCode)
    {
        using var bootstrap = await client.PostAsJsonAsync(
            "/api/v1/spikes/identity/bootstrap",
            new { workspaceCode, ownerPassword = OwnerPassword, operatorPassword = OperatorPassword },
            CancellationToken);
        Assert.Equal(HttpStatusCode.OK, bootstrap.StatusCode);
        using var setup = await ReadJsonAsync(bootstrap);
        var root = setup.RootElement;
        var ownerDeviceId = root.GetProperty("ownerDeviceId").GetGuid();
        var operatorDeviceId = root.GetProperty("operatorDeviceId").GetGuid();
        var ownerSecret = await ActivateAsync(client, workspaceCode, ownerDeviceId, root.GetProperty("ownerActivation").GetProperty("activationCode").GetString()!);
        var operatorSecret = await ActivateAsync(client, workspaceCode, operatorDeviceId, root.GetProperty("operatorActivation").GetProperty("activationCode").GetString()!);
        var ownerToken = await LoginAsync(client, workspaceCode, "owner", OwnerPassword, ownerDeviceId, ownerSecret);
        var operatorToken = await LoginAsync(client, workspaceCode, "operator", OperatorPassword, operatorDeviceId, operatorSecret);
        return new(
            workspaceCode,
            ownerDeviceId,
            operatorDeviceId,
            ownerSecret,
            operatorSecret,
            ownerToken,
            operatorToken);
    }

    private static async Task<string> ActivateAsync(HttpClient client, string workspaceCode, Guid deviceId, string activationCode)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/device-activations/redeem")
        {
            Content = JsonContent.Create(
                new { workspaceCode, activationCode, clientInstallationReference = $"receiving-{deviceId:D}", deviceLabel = "Receiving test device", platform = "Testing" }),
        };
        request.Headers.Add(
            "Idempotency-Key",
            $"receiving-activation-{deviceId:D}");
        using var response = await client.SendAsync(request, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("deviceSecret").GetString()!;
    }

    private static async Task<string> LoginAsync(HttpClient client, string workspaceCode, string login, string password, Guid deviceId, string deviceSecret)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { workspaceCode, login, password, deviceId, deviceSecret },
            CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string token, HttpMethod method, string path, object? body = null, string? idempotencyKey = null, long? expectedVersion = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (expectedVersion is not null) request.Headers.Add("X-Expected-Version", expectedVersion.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return await client.SendAsync(request, CancellationToken);
    }

    private static JsonElement Operation(JsonDocument document) => document.RootElement.GetProperty("operations")[0];
    private static JsonElement Result(JsonDocument document) => document.RootElement.GetProperty("result");
    private static string? ErrorCode(JsonDocument document) => document.RootElement.GetProperty("error").GetProperty("code").GetString();
    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(CancellationToken), cancellationToken: CancellationToken);

    private sealed record ReceivingScenario(
        CommercialIdentity Identity,
        Guid LocationId,
        Guid WeightPolicyId,
        Guid BagTypeId,
        Guid GroupId,
        Guid ProductId,
        Guid SupplierId,
        Guid SettingsId);

    private sealed record StartedSession(
        Guid SessionId,
        Guid LeaseId,
        string CloudReference);

    private sealed record CommercialIdentity(
        string WorkspaceCode,
        Guid OwnerDeviceId,
        Guid OperatorDeviceId,
        string OwnerDeviceSecret,
        string OperatorDeviceSecret,
        string OwnerToken,
        string OperatorToken);

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration)
        {
            UtcNow = UtcNow.Add(duration);
        }
    }
}
