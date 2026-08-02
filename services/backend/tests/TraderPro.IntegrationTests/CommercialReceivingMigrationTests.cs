using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TraderPro.Application.Procurement.Receiving;
using TraderPro.Domain.Common;
using TraderPro.Infrastructure.Persistence.Migrations;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CommercialReceivingMigrationTests(PostgreSqlFixture fixture)
{
    private const string OwnerPassword = "commercial owner development passphrase";
    private const string OperatorPassword = "commercial operator development passphrase";
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);
    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;
    [Fact]
    public async Task Fresh_migration_creates_production_receiving_and_sync_controls()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await database.OpenConnectionAsync();

        var tables = new[]
        {
            "procurement.commercial_receiving_reference_policies",
            "procurement.commercial_receiving_reference_counters",
            "procurement.commercial_receiving_sessions",
            "procurement.commercial_receiving_ownerships",
            "procurement.commercial_receiving_entries",
            "platform.commercial_outbox_audiences",
            "sync.commercial_master_changes",
        };
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass(@table) IS NOT NULL";
            command.Parameters.AddWithValue("table", table);
            Assert.True((bool)(await command.ExecuteScalarAsync(
                TestContext.Current.CancellationToken))!);
        }

        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='tr_commercial_outbox_audiences_immutable' AND NOT tgisinternal)"));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='tr_commercial_receiving_entries_immutable' AND NOT tgisinternal)"));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='tr_products_commercial_sync' AND NOT tgisinternal)"));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_commercial_receiving_sessions_uuidv7')"));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname='ux_commercial_receiving_reference_reservations_session')"));
    }

    [Fact]
    public async Task Migration_preserves_POC_tables_and_adds_no_receiving_rows()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await database.OpenConnectionAsync();

        Assert.True(await ExistsAsync(
            connection,
            "SELECT to_regclass('procurement.receiving_session_pocs') IS NOT NULL"));
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT count(*) FROM procurement.commercial_receiving_sessions";
        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync(
            TestContext.Current.CancellationToken))!);
    }

    [Fact]
    public async Task Existing_Task_7A_7B_and_POC_data_upgrades_without_contract_loss()
    {
        const string previousMigration =
            "20260801030919_HardenCommercialSupplierProductCatalogContracts";
        await using var database = await fixture.CreateDatabaseAsync(previousMigration);
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            procurementPocEnabled: true,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();

        var identity = await CreateUpgradeIdentityAsync(client, "RECEIVING-UPGRADE");
        var locationId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/operations/locations",
            new { code = "UP-YARD", name = "Upgrade yard", locationType = "Yard" },
            "upgrade-location", HttpStatusCode.Created);
        var policyId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/procurement/weight-policies",
            new { code = "UP-WEIGHT", name = "Upgrade weight", decimalPlaces = 2, processingMethod = "Standard" },
            "upgrade-weight-policy", HttpStatusCode.Created);
        var vehicleId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/procurement/vehicles",
            new { code = "UP-TRUCK", registrationNumber = "KA 01 UP 7", displayName = "Upgrade truck", vehicleType = "Truck" },
            "upgrade-vehicle", HttpStatusCode.Created);
        var inactiveVehicleId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/procurement/vehicles",
            new { code = "UP-OLD", registrationNumber = "KA 01 OLD 7", displayName = "Inactive truck", vehicleType = "Truck" },
            "upgrade-inactive-vehicle", HttpStatusCode.Created);
        await SendUpgradeAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            $"/api/v1/procurement/vehicles/{inactiveVehicleId:D}/deactivate",
            null, "upgrade-inactive-vehicle-deactivate", 1, HttpStatusCode.OK);
        var bagId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/procurement/bag-types",
            new { code = "UP-BAG", name = "Upgrade bag", constructionClass = "Jute", standardTareWeightKg = "0.200000", isReturnable = true },
            "upgrade-bag", HttpStatusCode.Created);
        var groupId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/catalog/product-groups",
            new { code = "UP-GROUP", name = "Upgrade group" },
            "upgrade-group", HttpStatusCode.Created);
        var inactiveGroupId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/catalog/product-groups",
            new { code = "UP-OLD-GROUP", name = "Inactive group" },
            "upgrade-inactive-group", HttpStatusCode.Created);
        await SendUpgradeAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            $"/api/v1/catalog/product-groups/{inactiveGroupId:D}/deactivate",
            null, "upgrade-inactive-group-deactivate", 1, HttpStatusCode.OK);
        var productId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/catalog/products",
            new
            {
                productGroupId = groupId,
                code = "UP-PRODUCT",
                name = "Upgrade product",
                productType = "RawMaterial",
                isPurchasable = true,
                processingFamilyCode = "PADDY",
            },
            "upgrade-product", HttpStatusCode.Created);
        await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            $"/api/v1/catalog/products/{productId:D}/bag-standards",
            new { bagTypeId = bagId, label = "Upgrade 50", standardContentWeightKg = "50.125000", isDefault = true },
            "upgrade-standard", HttpStatusCode.Created);
        var supplierId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/procurement/suppliers",
            new
            {
                code = "UP-SUPPLIER",
                name = "Upgrade supplier",
                supplierType = "Business",
                productScopeMode = "Restricted",
                initialProductIds = new[] { productId },
                contactName = "Protected Contact",
                contactNumber = "+91 90000 00000",
                email = "protected.upgrade@example.test",
                addressLine = "Protected address",
                taxRegistrationNumber = "GST-UPGRADE-01",
                notes = "Protected notes",
            },
            "upgrade-supplier", HttpStatusCode.Created);
        await SendUpgradeAsync(
            client, identity.OwnerToken, HttpMethod.Put,
            "/api/v1/procurement/settings",
            new
            {
                defaultDestinationLocationId = locationId,
                defaultWeightProcessingPolicyId = policyId,
                vehicleSelectionMode = "Optional",
            },
            "upgrade-settings", null, HttpStatusCode.OK);


        Guid workspaceId;
        Guid companyId;
        Guid branchId;
        var pocSessionId = Uuid7.NewGuid();
        var oldPocOutboxId = Uuid7.NewGuid();
        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT w.id, c.id, b.id
                FROM platform.workspaces w
                JOIN platform.companies c ON c.workspace_id = w.id
                JOIN platform.branches b ON b.workspace_id = w.id AND b.company_id = c.id
                WHERE w.normalized_workspace_code = 'RECEIVING-UPGRADE'
                """;
            await using var reader = await command.ExecuteReaderAsync(CancellationToken);
            Assert.True(await reader.ReadAsync(CancellationToken));
            workspaceId = reader.GetGuid(0);
            companyId = reader.GetGuid(1);
            branchId = reader.GetGuid(2);
        }
        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                INSERT INTO procurement.receiving_session_pocs(
                    id, workspace_id, status, editor_device_id, lease_id,
                    lease_expires_at_utc, last_lease_heartbeat_at_utc,
                    next_expected_local_sequence, entry_count,
                    processed_total_weight_kg, created_at_utc, updated_at_utc, version)
                VALUES (@session_id, @workspace_id, 1, @device_id, @lease_id,
                        @lease_expires, @now, 2, 0, 0, @now, @now, 1);

                INSERT INTO platform.outbox_messages(
                    id, workspace_id, event_stream, event_type, event_version,
                    aggregate_type, aggregate_id, aggregate_version, payload_json,
                    correlation_id, occurred_at_utc, status, attempt_count)
                VALUES (@outbox_id, @workspace_id, 2, 'UpgradePocMobileSync', 1,
                        'Procurement.ReceivingSessionPoc', @session_id, 1,
                        '{"existing":true}', @correlation_id, @now, 1, 0);
                """;
            command.Parameters.AddWithValue("session_id", pocSessionId);
            command.Parameters.AddWithValue("workspace_id", workspaceId);
            command.Parameters.AddWithValue("device_id", identity.OperatorDeviceId);
            command.Parameters.AddWithValue("lease_id", Uuid7.NewGuid());
            command.Parameters.AddWithValue("lease_expires", UtcNow.AddHours(1));
            command.Parameters.AddWithValue("now", UtcNow);
            command.Parameters.AddWithValue("outbox_id", oldPocOutboxId);
            command.Parameters.AddWithValue("correlation_id", Uuid7.NewGuid().ToString("D"));
            await command.ExecuteNonQueryAsync(CancellationToken);
        }

        var before = await ReadUpgradeFingerprintAsync(
            database, workspaceId, companyId, branchId, supplierId,
            productId, vehicleId, pocSessionId, oldPocOutboxId);
        await database.ApplyMigrationsAsync();

        Assert.Equal(
            before,
            await ReadUpgradeFingerprintAsync(
                database, workspaceId, companyId, branchId, supplierId,
                productId, vehicleId, pocSessionId, oldPocOutboxId));
        await AssertScalarAsync(database,
            "SELECT count(*) FROM platform.__ef_migrations_history WHERE \"MigrationId\" IN ('20260801182716_AddCommercialReceivingBackend', '20260802090000_HardenCommercialReceivingBackendContracts')",
            2);
        await AssertScalarAsync(database,
            "SELECT count(DISTINCT master_type) FROM sync.commercial_master_changes",
            10);
        await AssertScalarAsync(database,
            "SELECT count(DISTINCT status) FROM sync.commercial_master_changes WHERE status IN ('Active', 'Inactive')",
            2);
        await AssertScalarAsync(database,
            "SELECT count(*) FROM sync.commercial_master_changes WHERE payload_json::text ILIKE '%Protected Contact%' OR payload_json::text ILIKE '%protected.upgrade@example.test%' OR payload_json::text ILIKE '%GST-UPGRADE-01%' OR payload_json::text ILIKE '%Protected address%' OR payload_json::text ILIKE '%Protected notes%'",
            0);
        await AssertScalarAsync(database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_policies WHERE company_id = @id",
            1, ("id", companyId));
        await AssertScalarAsync(database,
            "SELECT count(*) FROM procurement.commercial_receiving_sessions",
            0);
        await AssertScalarAsync(database,
            "SELECT count(*) FROM procurement.receiving_session_pocs WHERE id = @id",
            1, ("id", pocSessionId));
        await AssertScalarAsync(database,
            "SELECT count(*) FROM pg_constraint WHERE conname IN ('fk_commercial_receiving_sessions_supplier', 'fk_commercial_receiving_entries_product', 'ck_commercial_receiving_operation_claims_shape')",
            3);
        await AssertScalarAsync(database,
            "SELECT count(*) FROM pg_indexes WHERE indexname IN ('ux_commercial_receiving_reference_reservations_operation', 'ix_cr_sessions_destination', 'ix_cr_entries_standard_weight')",
            3);
    }

    [Fact]
    public void Final_Task_7C1_migrations_reject_schema_downgrade()
    {
        AssertForwardOnly(new FinalizeCommercialReceivingBackendContracts());
        AssertForwardOnly(new SealCommercialReceivingReferenceContracts());

        static void AssertForwardOnly(Migration migration)
        {
            var down = migration.GetType().GetMethod(
                "Down",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)!;
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
                () => down.Invoke(migration, [new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL")]));
            var unsupported = Assert.IsType<NotSupportedException>(exception.InnerException);
            Assert.Contains("restore the database from backup", unsupported.Message);
        }
    }

    [Fact]
    public async Task Original_Task_7C1_production_rows_upgrade_and_exact_operations_replay_without_new_reference_or_events()
    {
        const string originalMigration =
            "20260801182716_AddCommercialReceivingBackend";
        await using var database = await fixture.CreateDatabaseAsync(originalMigration);
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
        using var client = factory.CreateClient();
        var identity = await CreateUpgradeIdentityAsync(client, "RECEIVING-ORIGINAL-7C1");

        var locationId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/operations/locations",
            new { code = "LEG-YARD", name = "Legacy yard", locationType = "Yard" },
            "legacy-location", HttpStatusCode.Created);
        var weightPolicyId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/procurement/weight-policies",
            new { code = "LEG-WEIGHT", name = "Legacy weight", decimalPlaces = 2, processingMethod = "Standard" },
            "legacy-weight", HttpStatusCode.Created);
        var bagTypeId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/procurement/bag-types",
            new { code = "LEG-BAG", name = "Legacy bag", constructionClass = "Jute", standardTareWeightKg = "0.200000", isReturnable = true },
            "legacy-bag", HttpStatusCode.Created);
        var groupId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/catalog/product-groups",
            new { code = "LEG-GROUP", name = "Legacy group" },
            "legacy-group", HttpStatusCode.Created);
        var productId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/catalog/products",
            new
            {
                productGroupId = groupId,
                code = "LEG-PRODUCT",
                name = "Legacy product",
                productType = "RawMaterial",
                isPurchasable = true,
                processingFamilyCode = "PADDY",
            },
            "legacy-product", HttpStatusCode.Created);
        var supplierId = await CreateUpgradeIdAsync(
            client, identity.OwnerToken, HttpMethod.Post,
            "/api/v1/procurement/suppliers",
            new
            {
                code = "LEG-SUPPLIER",
                name = "Legacy supplier",
                supplierType = "Business",
                productScopeMode = "Unrestricted",
                initialProductIds = Array.Empty<Guid>(),
            },
            "legacy-supplier", HttpStatusCode.Created);
        Guid settingsId;
        using (var settings = await SendUpgradeAsync(
                   client, identity.OwnerToken, HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationId,
                       defaultWeightProcessingPolicyId = weightPolicyId,
                       vehicleSelectionMode = "Optional",
                   },
                   "legacy-settings", null, HttpStatusCode.OK))
        using (var settingsJson = await ReadUpgradeJsonAsync(settings))
        {
            settingsId = settingsJson.RootElement.GetProperty("result").GetProperty("id").GetGuid();
        }

        Guid workspaceId;
        Guid companyId;
        Guid branchId;
        Guid operatorUserId;
        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT workspace.id, company.id, branch.id, platform_user.id
                FROM platform.workspaces workspace
                JOIN platform.companies company ON company.workspace_id = workspace.id
                JOIN platform.branches branch
                  ON branch.workspace_id = workspace.id AND branch.company_id = company.id
                JOIN platform.users platform_user
                  ON platform_user.workspace_id = workspace.id
                JOIN platform.user_credentials login
                  ON login.workspace_id = platform_user.workspace_id
                 AND login.user_id = platform_user.id
                 AND login.normalized_login = 'OPERATOR'
                WHERE workspace.normalized_workspace_code = 'RECEIVING-ORIGINAL-7C1'
                """;
            await using var reader = await command.ExecuteReaderAsync(CancellationToken);
            Assert.True(await reader.ReadAsync(CancellationToken));
            workspaceId = reader.GetGuid(0);
            companyId = reader.GetGuid(1);
            branchId = reader.GetGuid(2);
            operatorUserId = reader.GetGuid(3);
        }

        var sessionId = Uuid7.NewGuid();
        var ownershipId = Uuid7.NewGuid();
        var leaseId = Uuid7.NewGuid();
        var startOperationId = Uuid7.NewGuid();
        var entryOperationId = Uuid7.NewGuid();
        var entryId = Uuid7.NewGuid();
        var startCorrelation = Uuid7.NewGuid().ToString("D");
        var entryCorrelation = Uuid7.NewGuid().ToString("D");
        const string cloudReference = "RCV-2026-08-0010";
        var startPayload = JsonSerializer.Serialize(new
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
            externalReference = "LEGACY-PRODUCTION",
            startedAtDeviceUtc = UtcNow,
        }, JsonOptions);
        var entryPayload = JsonSerializer.Serialize(new
        {
            operationId = entryOperationId,
            entryId,
            sessionId,
            localSequence = 2,
            productId,
            productVersion = 1,
            supplierProductScopeId = (Guid?)null,
            supplierProductScopeVersion = (long?)null,
            bagTypeId,
            bagTypeVersion = 1,
            productStandardBagWeightId = (Guid?)null,
            productStandardBagWeightVersion = (long?)null,
            bagCount = 4,
            rawWeightKg = "10.005",
            processedWeightKg = "10.010000",
            displayWeightKg = "10.01",
            decimalPlaces = 2,
            processingMethod = "Standard",
            weightSource = "ManualScale",
            capturedAtDeviceUtc = UtcNow,
        }, JsonOptions);
        var startPayloadHash = CommercialReceivingRequestHash.PayloadHash(startPayload);
        var entryPayloadHash = CommercialReceivingRequestHash.PayloadHash(entryPayload);
        var startRequestHash = CommercialReceivingRequestHash.ForMobileOperation(
            CommercialReceivingOperationTypes.Start, startOperationId, sessionId, 1,
            null, workspaceId, companyId, operatorUserId,
            identity.OperatorDeviceId, startPayloadHash, startPayload);
        var entryRequestHash = CommercialReceivingRequestHash.ForMobileOperation(
            CommercialReceivingOperationTypes.RecordEntry, entryOperationId, sessionId, 2,
            1, workspaceId, companyId, operatorUserId,
            identity.OperatorDeviceId, entryPayloadHash, entryPayload);
        var startResult = JsonSerializer.Serialize(new
        {
            result = new
            {
                cloudReference,
                sessionStatus = "InProgress",
                sessionVersion = 1,
                ownershipGeneration = 1,
                leaseId,
                leaseExpiresAtUtc = UtcNow.AddMinutes(5),
                entryCount = 0,
                processedTotalWeightKg = "0.000000",
            },
            correlationId = startCorrelation,
        }, JsonOptions);
        var entryResult = JsonSerializer.Serialize(new
        {
            result = new
            {
                cloudReference,
                sessionStatus = "InProgress",
                sessionVersion = 2,
                ownershipGeneration = 1,
                leaseId,
                leaseExpiresAtUtc = UtcNow.AddMinutes(5),
                entryCount = 1,
                processedTotalWeightKg = "10.010000",
            },
            correlationId = entryCorrelation,
        }, JsonOptions);

        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                INSERT INTO procurement.commercial_receiving_reference_counters(
                    workspace_id, company_id, policy_id, period_key, next_number, version)
                SELECT @workspace_id, @company_id, id, '2026-08', 11, 2
                FROM procurement.commercial_receiving_reference_policies
                WHERE workspace_id = @workspace_id AND company_id = @company_id;

                INSERT INTO procurement.commercial_receiving_sessions(
                    id, workspace_id, company_id, branch_id, cloud_reference,
                    cloud_reference_sequence, reference_policy_version_snapshot,
                    external_reference, status, supplier_id, supplier_version_snapshot,
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
                VALUES (
                    @session_id, @workspace_id, @company_id, @branch_id, @cloud_reference,
                    10, 1, 'LEGACY-PRODUCTION', 1, @supplier_id, 1,
                    'LEG-SUPPLIER', 'Legacy supplier', 1,
                    @settings_id, 1, 1, @location_id, 1, 'LEG-YARD', 'Legacy yard',
                    @weight_policy_id, 1, 2, 0, NULL, NULL, NULL, NULL, NULL,
                    3, 1, 10.010000, @now, @now, NULL, @now, @now, 2);

                INSERT INTO procurement.commercial_receiving_ownerships(
                    id, workspace_id, company_id, receiving_session_id,
                    editor_device_id, ownership_generation, lease_id,
                    lease_expires_at_utc, last_heartbeat_at_utc,
                    last_reacquired_at_utc, last_transferred_at_utc,
                    last_transferred_by_user_id, version, created_at_utc, updated_at_utc)
                VALUES (@ownership_id, @workspace_id, @company_id, @session_id,
                        @device_id, 1, @lease_id, @lease_expires, @now,
                        NULL, NULL, NULL, 1, @now, @now);

                INSERT INTO procurement.commercial_receiving_entries(
                    id, workspace_id, company_id, receiving_session_id,
                    operation_id, local_sequence, product_id, product_version_snapshot,
                    product_code_snapshot, product_name_snapshot, product_type_snapshot,
                    processing_family_code_snapshot, supplier_product_scope_id,
                    supplier_product_scope_version_snapshot, supplier_scope_mode_snapshot,
                    supplier_scope_validation_result_snapshot, bag_type_id,
                    bag_type_version_snapshot, bag_type_code_snapshot, bag_type_name_snapshot,
                    bag_construction_class_snapshot, bag_tare_weight_kg_snapshot,
                    bag_returnable_snapshot, product_standard_bag_weight_id,
                    product_standard_bag_weight_version_snapshot,
                    standard_bag_weight_label_snapshot, standard_content_weight_kg_snapshot,
                    bag_count, raw_weight_kg, processed_weight_kg, display_weight_kg,
                    decimal_places_snapshot, processing_method_snapshot, weight_source,
                    captured_at_device_utc, accepted_at_server_utc)
                VALUES (@entry_id, @workspace_id, @company_id, @session_id,
                        @entry_operation_id, 2, @product_id, 1,
                        'LEG-PRODUCT', 'Legacy product', 1, 'PADDY', NULL, NULL,
                        1, 'Unrestricted', @bag_type_id, 1, 'LEG-BAG', 'Legacy bag',
                        1, 0.200000, true, NULL, NULL, NULL, NULL,
                        4, '10.005', 10.010000, '10.01', 2, 0, 'ManualScale', @now, @now);

                INSERT INTO platform.idempotency_records(
                    id, workspace_id, idempotency_key, command_type, request_hash,
                    status, result_payload_json, result_status_code,
                    created_at_utc, completed_at_utc, expires_at_utc)
                VALUES
                    (@start_idempotency_id, @workspace_id, @start_operation_key,
                     'Procurement.CommercialReceiving.MobileSyncOperation', @start_hash,
                     2, @start_result::jsonb, 200, @now, @now, NULL),
                    (@entry_idempotency_id, @workspace_id, @entry_operation_key,
                     'Procurement.CommercialReceiving.MobileSyncOperation', @entry_hash,
                     2, @entry_result::jsonb, 200, @now, @now, NULL);

                INSERT INTO platform.audit_events(
                    id, workspace_id, company_id, branch_id, actor_user_id,
                    actor_device_id, action, aggregate_type, aggregate_id,
                    permission_key, reason, before_snapshot_json, after_snapshot_json,
                    correlation_id, occurred_at_utc)
                VALUES
                    (@start_audit_id, @workspace_id, @company_id, @branch_id,
                     @user_id, @device_id, 'Procurement.CommercialReceiving.SessionStarted',
                     'Procurement.CommercialReceivingSession', @session_id, NULL, NULL, NULL,
                     jsonb_build_object('sessionId', @session_id,
                         'ownershipGeneration', 1, 'version', 1),
                     @start_correlation, @now),
                    (@entry_audit_id, @workspace_id, @company_id, @branch_id,
                     @user_id, @device_id, 'Procurement.CommercialReceiving.EntryAccepted',
                     'Procurement.CommercialReceivingSession', @session_id, NULL, NULL, NULL,
                     jsonb_build_object('sessionId', @session_id, 'entryId', @entry_id,
                         'localSequence', 2, 'version', 2),
                     @entry_correlation, @now);

                INSERT INTO platform.outbox_messages(
                    id, workspace_id, event_stream, event_type, event_version,
                    aggregate_type, aggregate_id, aggregate_version, payload_json,
                    correlation_id, occurred_at_utc, status, attempt_count)
                VALUES
                    (@start_owner_event, @workspace_id, 3, 'CommercialReceivingSessionStarted', 1,
                     'Procurement.CommercialReceivingSession', @session_id, 1, '{"legacy":true}', @start_correlation, @now, 1, 0),
                    (@start_device_event, @workspace_id, 3, 'CommercialReceivingSessionStarted', 1,
                     'Procurement.CommercialReceivingSession', @session_id, 1, '{"legacy":true}', @start_correlation, @now, 1, 0),
                    (@entry_owner_event, @workspace_id, 3, 'CommercialReceivingEntryAccepted', 1,
                     'Procurement.CommercialReceivingSession', @session_id, 2, '{"legacy":true}', @entry_correlation, @now, 1, 0),
                    (@entry_device_event, @workspace_id, 3, 'CommercialReceivingEntryAccepted', 1,
                     'Procurement.CommercialReceivingSession', @session_id, 2, '{"legacy":true}', @entry_correlation, @now, 1, 0);

                INSERT INTO platform.commercial_outbox_audiences(
                    outbox_message_id, workspace_id, company_id, audience, target_device_id)
                VALUES
                    (@start_owner_event, @workspace_id, @company_id, 1, NULL),
                    (@start_device_event, @workspace_id, @company_id, 2, @device_id),
                    (@entry_owner_event, @workspace_id, @company_id, 1, NULL),
                    (@entry_device_event, @workspace_id, @company_id, 2, @device_id);
                """;
            foreach (var (name, value) in new (string, object)[]
                     {
                         ("workspace_id", workspaceId), ("company_id", companyId),
                         ("branch_id", branchId), ("user_id", operatorUserId),
                         ("device_id", identity.OperatorDeviceId), ("session_id", sessionId),
                         ("ownership_id", ownershipId), ("lease_id", leaseId),
                         ("lease_expires", UtcNow.AddMinutes(5)), ("now", UtcNow),
                         ("cloud_reference", cloudReference), ("supplier_id", supplierId),
                         ("settings_id", settingsId), ("location_id", locationId),
                         ("weight_policy_id", weightPolicyId), ("product_id", productId),
                         ("bag_type_id", bagTypeId), ("entry_id", entryId),
                         ("entry_operation_id", entryOperationId),
                         ("start_idempotency_id", Uuid7.NewGuid()),
                         ("entry_idempotency_id", Uuid7.NewGuid()),
                         ("start_operation_key", startOperationId.ToString("D")),
                         ("entry_operation_key", entryOperationId.ToString("D")),
                         ("start_hash", startRequestHash), ("entry_hash", entryRequestHash),
                         ("start_result", startResult), ("entry_result", entryResult),
                         ("start_audit_id", Uuid7.NewGuid()), ("entry_audit_id", Uuid7.NewGuid()),
                         ("start_correlation", startCorrelation), ("entry_correlation", entryCorrelation),
                         ("start_owner_event", Uuid7.NewGuid()), ("start_device_event", Uuid7.NewGuid()),
                         ("entry_owner_event", Uuid7.NewGuid()), ("entry_device_event", Uuid7.NewGuid()),
                     })
                command.Parameters.AddWithValue(name, value);
            await command.ExecuteNonQueryAsync(CancellationToken);
        }

        var invariantBefore = await ReadLegacyReceivingFingerprintAsync(database, sessionId);
        await using (var upgradeConnection = await database.OpenConnectionAsync())
        await using (var preflight = upgradeConnection.CreateCommand())
        {
            preflight.CommandText =
                "ALTER TABLE procurement.commercial_receiving_sessions DISABLE TRIGGER USER";
            await preflight.ExecuteNonQueryAsync(CancellationToken);
        }
        await database.ApplyMigrationsAsync(
            "20260802090000_HardenCommercialReceivingBackendContracts");
        await database.ApplyMigrationsAsync();
        Assert.Equal(invariantBefore, await ReadLegacyReceivingFingerprintAsync(database, sessionId));
        await AssertScalarAsync(database,
            "SELECT count(*) FROM platform.__ef_migrations_history WHERE \"MigrationId\" IN ('20260801182716_AddCommercialReceivingBackend', '20260802090000_HardenCommercialReceivingBackendContracts', '20260802120000_FinalizeCommercialReceivingBackendContracts', '20260802130000_SealCommercialReceivingReferenceContracts')",
            4);
        await AssertScalarAsync(database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_reservations WHERE session_id = @id",
            1, ("id", sessionId));
        await AssertScalarAsync(database,
            "SELECT next_number FROM procurement.commercial_receiving_reference_counters WHERE company_id = @id",
            11, ("id", companyId));

        object StartEnvelope(string payload) => new
        {
            operationId = startOperationId,
            operationType = CommercialReceivingOperationTypes.Start,
            sessionId,
            localSequence = 1,
            ownershipGeneration = (long?)null,
            expectedCloudVersion = (long?)null,
            payloadJson = payload,
            payloadHash = CommercialReceivingRequestHash.PayloadHash(payload),
            lease = (object?)null,
        };
        object EntryEnvelope(string payload) => new
        {
            operationId = entryOperationId,
            operationType = CommercialReceivingOperationTypes.RecordEntry,
            sessionId,
            localSequence = 2,
            ownershipGeneration = (long?)1,
            expectedCloudVersion = (long?)null,
            payloadJson = payload,
            payloadHash = CommercialReceivingRequestHash.PayloadHash(payload),
            lease = new { leaseId },
        };
        async Task AssertReplayAsync(object operation)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/commercial-sync/operations")
            {
                Content = JsonContent.Create(new { operations = new[] { operation } }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", identity.OperatorToken);
            using var response = await client.SendAsync(request, CancellationToken);
            using var json = await ReadUpgradeJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var replayed = json.RootElement.GetProperty("operations")[0];
            Assert.True(
                replayed.GetProperty("status").GetString() == "PreviouslyProcessed",
                replayed.ToString());
        }
        await AssertReplayAsync(StartEnvelope(startPayload));
        await AssertReplayAsync(EntryEnvelope(entryPayload));

        var changedStartPayload = startPayload.Replace(
            "LEGACY-PRODUCTION", "LEGACY-CHANGED", StringComparison.Ordinal);
        var conflictRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/mobile/commercial-sync/operations")
        {
            Content = JsonContent.Create(new { operations = new[] { StartEnvelope(changedStartPayload) } }),
        };
        conflictRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", identity.OperatorToken);
        using (var conflict = await client.SendAsync(conflictRequest, CancellationToken))
        using (var conflictJson = await ReadUpgradeJsonAsync(conflict))
        {
            Assert.Equal(HttpStatusCode.OK, conflict.StatusCode);
            Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT",
                conflictJson.RootElement.GetProperty("operations")[0]
                    .GetProperty("error").GetProperty("code").GetString());
        }

        var changedType = new
        {
            operationId = startOperationId,
            operationType = CommercialReceivingOperationTypes.RecordEntry,
            sessionId,
            localSequence = 1,
            ownershipGeneration = (long?)1,
            expectedCloudVersion = (long?)null,
            payloadJson = startPayload,
            payloadHash = startPayloadHash,
            lease = new { leaseId },
        };
        var changedTypeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/mobile/commercial-sync/operations")
        {
            Content = JsonContent.Create(new { operations = new[] { changedType } }),
        };
        changedTypeRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", identity.OperatorToken);
        using (var conflict = await client.SendAsync(changedTypeRequest, CancellationToken))
        using (var conflictJson = await ReadUpgradeJsonAsync(conflict))
        {
            Assert.Equal(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                conflictJson.RootElement.GetProperty("operations")[0]
                    .GetProperty("error").GetProperty("code").GetString());
        }

        var otherDeviceOperatorToken = await LoginUpgradeDeviceAsync(
            client,
            identity.WorkspaceCode,
            "operator",
            OperatorPassword,
            identity.OwnerDeviceId,
            identity.OwnerDeviceSecret);
        var changedDeviceRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/mobile/commercial-sync/operations")
        {
            Content = JsonContent.Create(new { operations = new[] { StartEnvelope(startPayload) } }),
        };
        changedDeviceRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", otherDeviceOperatorToken);
        using (var conflict = await client.SendAsync(changedDeviceRequest, CancellationToken))
        using (var conflictJson = await ReadUpgradeJsonAsync(conflict))
        {
            Assert.Equal(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                conflictJson.RootElement.GetProperty("operations")[0]
                    .GetProperty("error").GetProperty("code").GetString());
        }

        Assert.Equal(invariantBefore, await ReadLegacyReceivingFingerprintAsync(database, sessionId));
        await AssertScalarAsync(database,
            "SELECT count(*) FROM procurement.commercial_receiving_reference_reservations WHERE session_id = @id",
            1, ("id", sessionId));
        await AssertScalarAsync(database,
            "SELECT count(*) FROM platform.audit_events WHERE aggregate_id = @id",
            2, ("id", sessionId));
        await AssertScalarAsync(database,
            "SELECT count(*) FROM platform.outbox_messages WHERE aggregate_id = @id",
            4, ("id", sessionId));
    }

    private static async Task<UpgradeIdentity> CreateUpgradeIdentityAsync(
        HttpClient client,
        string workspaceCode)
    {
        using var bootstrap = await client.PostAsJsonAsync(
            "/api/v1/spikes/identity/bootstrap",
            new
            {
                workspaceCode,
                ownerPassword = OwnerPassword,
                operatorPassword = OperatorPassword,
            },
            CancellationToken);
        using var bootstrapJson = await ReadUpgradeJsonAsync(bootstrap);
        Assert.True(
            bootstrap.StatusCode == HttpStatusCode.OK,
            bootstrapJson.RootElement.ToString());
        var root = bootstrapJson.RootElement;
        var ownerDeviceId = root.GetProperty("ownerDeviceId").GetGuid();
        var operatorDeviceId = root.GetProperty("operatorDeviceId").GetGuid();
        var ownerSecret = await ActivateUpgradeDeviceAsync(
            client, workspaceCode, ownerDeviceId,
            root.GetProperty("ownerActivation").GetProperty("activationCode").GetString()!);
        var operatorSecret = await ActivateUpgradeDeviceAsync(
            client, workspaceCode, operatorDeviceId,
            root.GetProperty("operatorActivation").GetProperty("activationCode").GetString()!);
        var ownerToken = await LoginUpgradeDeviceAsync(
            client, workspaceCode, "owner", OwnerPassword,
            ownerDeviceId, ownerSecret);
        var operatorToken = await LoginUpgradeDeviceAsync(
            client, workspaceCode, "operator", OperatorPassword,
            operatorDeviceId, operatorSecret);
        return new(
            workspaceCode,
            ownerDeviceId,
            operatorDeviceId,
            ownerSecret,
            operatorSecret,
            ownerToken,
            operatorToken);
    }

    private static async Task<string> ActivateUpgradeDeviceAsync(
        HttpClient client,
        string workspaceCode,
        Guid deviceId,
        string activationCode)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/device-activations/redeem",
            new
            {
                workspaceCode,
                activationCode,
                clientInstallationReference = $"upgrade-{deviceId:D}",
                deviceLabel = "Upgrade device",
                platform = "Testing",
            },
            CancellationToken);
        using var json = await ReadUpgradeJsonAsync(response);
        Assert.True(response.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
        return json.RootElement.GetProperty("deviceSecret").GetString()!;
    }

    private static async Task<string> LoginUpgradeDeviceAsync(
        HttpClient client,
        string workspaceCode,
        string login,
        string password,
        Guid deviceId,
        string deviceSecret)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { workspaceCode, login, password, deviceId, deviceSecret },
            CancellationToken);
        using var json = await ReadUpgradeJsonAsync(response);
        Assert.True(response.StatusCode == HttpStatusCode.OK, json.RootElement.ToString());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<Guid> CreateUpgradeIdAsync(
        HttpClient client,
        string token,
        HttpMethod method,
        string path,
        object body,
        string idempotencyKey,
        HttpStatusCode expected)
    {
        using var response = await SendUpgradeAsync(
            client, token, method, path, body,
            idempotencyKey, null, expected);
        using var json = await ReadUpgradeJsonAsync(response);
        return json.RootElement.GetProperty("result").GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> SendUpgradeAsync(
        HttpClient client,
        string token,
        HttpMethod method,
        string path,
        object? body,
        string idempotencyKey,
        long? expectedVersion,
        HttpStatusCode expected)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (expectedVersion is not null)
        {
            request.Headers.Add("X-Expected-Version", expectedVersion.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        }
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var response = await client.SendAsync(request, CancellationToken);
        if (response.StatusCode != expected)
        {
            using var json = await ReadUpgradeJsonAsync(response);
            Assert.Fail(json.RootElement.ToString());
        }
        return response;
    }

    private static async Task<string> ReadUpgradeFingerprintAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        Guid companyId,
        Guid branchId,
        Guid supplierId,
        Guid productId,
        Guid vehicleId,
        Guid pocSessionId,
        Guid outboxId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT jsonb_build_array(
                (SELECT version FROM platform.workspaces WHERE id = @workspace_id),
                (SELECT version FROM platform.companies WHERE id = @company_id),
                (SELECT version FROM platform.branches WHERE id = @branch_id),
                (SELECT jsonb_build_array(version, status, contact_name, contact_number,
                    email, address_line, tax_registration_number, notes)
                 FROM procurement.suppliers WHERE id = @supplier_id),
                (SELECT jsonb_build_array(version, status, name)
                 FROM catalog.products WHERE id = @product_id),
                (SELECT jsonb_build_array(version, status, display_name)
                 FROM procurement.receiving_vehicles WHERE id = @vehicle_id),
                (SELECT jsonb_build_array(version, status, editor_device_id, lease_id)
                 FROM procurement.receiving_session_pocs WHERE id = @poc_session_id),
                (SELECT jsonb_build_array(event_stream, event_type, aggregate_id,
                    aggregate_version, payload_json, status, attempt_count)
                 FROM platform.outbox_messages WHERE id = @outbox_id),
                (SELECT count(*) FROM platform.users WHERE workspace_id = @workspace_id),
                (SELECT count(*) FROM platform.devices WHERE workspace_id = @workspace_id),
                (SELECT count(*) FROM platform.user_credentials WHERE workspace_id = @workspace_id),
                (SELECT count(*) FROM platform.refresh_token_families WHERE workspace_id = @workspace_id),
                (SELECT jsonb_agg(jsonb_build_array(id, event_stream, event_type,
                    aggregate_id, aggregate_version, payload_json, status, attempt_count)
                    ORDER BY sequence)
                 FROM platform.outbox_messages WHERE workspace_id = @workspace_id))
            """;
        command.Parameters.AddWithValue("workspace_id", workspaceId);
        command.Parameters.AddWithValue("company_id", companyId);
        command.Parameters.AddWithValue("branch_id", branchId);
        command.Parameters.AddWithValue("supplier_id", supplierId);
        command.Parameters.AddWithValue("product_id", productId);
        command.Parameters.AddWithValue("vehicle_id", vehicleId);
        command.Parameters.AddWithValue("poc_session_id", pocSessionId);
        command.Parameters.AddWithValue("outbox_id", outboxId);
        return (string)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static async Task<string> ReadLegacyReceivingFingerprintAsync(
        IsolatedPostgreSqlDatabase database,
        Guid sessionId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT jsonb_build_array(
                jsonb_build_array(s.id, s.cloud_reference,
                    s.cloud_reference_sequence, s.status,
                    s.next_expected_local_sequence, s.entry_count,
                    s.processed_total_weight_kg, s.version),
                (SELECT jsonb_build_array(o.id, o.editor_device_id,
                    o.ownership_generation, o.lease_id,
                    o.lease_expires_at_utc, o.version)
                 FROM procurement.commercial_receiving_ownerships o
                 WHERE o.receiving_session_id = s.id),
                (SELECT jsonb_agg(jsonb_build_array(e.id, e.operation_id,
                    e.local_sequence, e.raw_weight_kg, e.processed_weight_kg,
                    e.display_weight_kg) ORDER BY e.local_sequence)
                 FROM procurement.commercial_receiving_entries e
                 WHERE e.receiving_session_id = s.id),
                (SELECT jsonb_agg(jsonb_build_array(c.period_key,
                    c.next_number, c.version) ORDER BY c.period_key)
                 FROM procurement.commercial_receiving_reference_counters c
                 WHERE c.workspace_id = s.workspace_id AND c.company_id = s.company_id),
                (SELECT count(*) FROM platform.idempotency_records i
                 WHERE i.idempotency_key IN (
                     SELECT e.operation_id::text
                     FROM procurement.commercial_receiving_entries e
                     WHERE e.receiving_session_id = s.id)
                    OR i.result_payload_json #>> '{result,cloudReference}' = s.cloud_reference),
                (SELECT count(*) FROM platform.audit_events a
                 WHERE a.aggregate_id = s.id),
                (SELECT count(*) FROM platform.outbox_messages m
                 WHERE m.aggregate_id = s.id),
                (SELECT count(*) FROM platform.commercial_outbox_audiences audience
                 JOIN platform.outbox_messages message
                   ON message.id = audience.outbox_message_id
                 WHERE message.aggregate_id = s.id))
            FROM procurement.commercial_receiving_sessions s
            WHERE s.id = @session_id
            """;
        command.Parameters.AddWithValue("session_id", sessionId);
        return (string)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static async Task AssertScalarAsync(
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
        Assert.Equal(expected, (long)(await command.ExecuteScalarAsync(CancellationToken))!);
    }

    private static async Task<JsonDocument> ReadUpgradeJsonAsync(
        HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(CancellationToken),
            cancellationToken: CancellationToken);

    private static async Task<bool> ExistsAsync(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (bool)(await command.ExecuteScalarAsync(
            TestContext.Current.CancellationToken))!;
    }

    private sealed record UpgradeIdentity(
        string WorkspaceCode,
        Guid OwnerDeviceId,
        Guid OperatorDeviceId,
        string OwnerDeviceSecret,
        string OperatorDeviceSecret,
        string OwnerToken,
        string OperatorToken);
}
