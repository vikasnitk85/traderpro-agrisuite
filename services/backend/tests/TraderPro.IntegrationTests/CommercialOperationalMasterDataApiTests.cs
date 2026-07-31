using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CommercialOperationalMasterDataApiTests(
    PostgreSqlFixture fixture)
{
    private const string OwnerPassword =
        "commercial owner development passphrase";
    private const string OperatorPassword =
        "commercial operator development passphrase";
    private const string PreviousMigration =
        "20260730074437_HardenProductionIdentitySessionSecurity";
    private const string OriginalCommercialMigration =
        "20260731070704_AddCommercialOperationalMasterData";

    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 31, 8, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task Owner_lifecycle_settings_access_and_internal_events_work()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var first = await CreateIdentityAsync(factory, "COMMERCIAL-ONE");
        var second = await CreateIdentityAsync(factory, "COMMERCIAL-TWO");

        using (var anonymous = factory.CreateClient())
        using (var response = await anonymous.GetAsync(
                   "/api/v1/operations/locations",
                   CancellationToken))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using (var absent = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Get,
                   "/api/v1/procurement/settings"))
        using (var json = await ReadJsonAsync(absent))
        {
            Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
            Assert.Equal(
                "PROCUREMENT_SETTINGS_NOT_CONFIGURED",
                ErrorCode(json));
        }

        var locationOneBody = new
        {
            code = "yard-01",
            name = "North Yard",
            locationType = "Yard",
            addressLine = "Market Road",
            workspaceId = second.WorkspaceId,
            companyId = second.CompanyId,
            branchId = second.BranchId,
            deviceId = second.OwnerDeviceId,
        };
        Guid locationOneId;
        string firstCorrelation;
        using (var created = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/operations/locations",
                   locationOneBody,
                   "location-create-one",
                   temporaryWorkspaceId: second.WorkspaceId,
                   temporaryDeviceId: second.OwnerDeviceId))
        using (var json = await ReadJsonAsync(created))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var result = Result(json);
            locationOneId = result.GetProperty("id").GetGuid();
            Assert.Equal("YARD-01", result.GetProperty("code").GetString());
            Assert.Equal(
                first.BranchId,
                result.GetProperty("branchId").GetGuid());
            firstCorrelation = json.RootElement
                .GetProperty("meta")
                .GetProperty("correlationId")
                .GetString()!;
        }

        using (var replay = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/operations/locations",
                   locationOneBody,
                   "location-create-one"))
        using (var json = await ReadJsonAsync(replay))
        {
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            Assert.Equal(
                locationOneId,
                Result(json).GetProperty("id").GetGuid());
            Assert.Equal(
                "PreviouslyProcessed",
                json.RootElement
                    .GetProperty("meta")
                    .GetProperty("idempotencyStatus")
                    .GetString());
            Assert.Equal(
                firstCorrelation,
                json.RootElement
                    .GetProperty("meta")
                    .GetProperty("correlationId")
                    .GetString());
        }

        using (var conflict = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/operations/locations",
                   new
                   {
                       code = "yard-01",
                       name = "Changed retry",
                       locationType = "Yard",
                   },
                   "location-create-one"))
        using (var json = await ReadJsonAsync(conflict))
        {
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT", ErrorCode(json));
        }

        using (var operatorRead = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Get,
                   $"/api/v1/operations/locations/{locationOneId:D}"))
        {
            Assert.Equal(HttpStatusCode.OK, operatorRead.StatusCode);
        }

        using (var operatorWrite = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "TRUCK-01",
                       registrationNumber = "KA 01 AB 1234",
                       vehicleType = "Truck",
                   },
                   "operator-mutation"))
        using (var json = await ReadJsonAsync(operatorWrite))
        {
            Assert.Equal(HttpStatusCode.Forbidden, operatorWrite.StatusCode);
            Assert.Equal("OWNER_ROLE_REQUIRED", ErrorCode(json));
        }

        var locationTwoId = await CreateLocationAsync(
            factory,
            first.OwnerToken,
            "WH-02",
            "Main Warehouse",
            "Warehouse",
            "location-create-two");
        var secondWorkspaceLocationId = await CreateLocationAsync(
            factory,
            second.OwnerToken,
            "OTHER-01",
            "Other Workspace Yard",
            "Yard",
            "other-location");

        using (var isolated = await SendAsync(
                   factory,
                   second.OwnerToken,
                   HttpMethod.Get,
                   $"/api/v1/operations/locations/{locationOneId:D}"))
        using (var json = await ReadJsonAsync(isolated))
        {
            Assert.Equal(HttpStatusCode.NotFound, isolated.StatusCode);
            Assert.Equal("BUSINESS_LOCATION_NOT_FOUND", ErrorCode(json));
        }

        Guid vehicleId;
        using (var vehicle = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "truck-01",
                       registrationNumber = "KA 01 AB 1234",
                       displayName = "Primary truck",
                       vehicleType = "Truck",
                   },
                   "vehicle-create"))
        using (var json = await ReadJsonAsync(vehicle))
        {
            Assert.Equal(HttpStatusCode.Created, vehicle.StatusCode);
            vehicleId = Result(json).GetProperty("id").GetGuid();
        }

        using (var duplicate = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "truck-02",
                       registrationNumber = "ka-01-ab-1234",
                       vehicleType = "Truck",
                   },
                   "vehicle-duplicate"))
        using (var json = await ReadJsonAsync(duplicate))
        {
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal(
                "RECEIVING_VEHICLE_REGISTRATION_EXISTS",
                ErrorCode(json));
        }

        using (var bag = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/bag-types",
                   new
                   {
                       code = "double-50",
                       name = "Double plastic 50kg",
                       constructionClass = "DoublePlastic",
                       standardTareWeightKg = "0.200000",
                       isReturnable = true,
                   },
                   "bag-create"))
        using (var json = await ReadJsonAsync(bag))
        {
            Assert.Equal(HttpStatusCode.Created, bag.StatusCode);
            Assert.Equal(
                "DoublePlastic",
                Result(json)
                    .GetProperty("constructionClass")
                    .GetString());
            Assert.Equal(
                "0.200000",
                Result(json)
                    .GetProperty("standardTareWeightKg")
                    .GetString());
        }

        using (var invalidBag = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/bag-types",
                   new
                   {
                       code = "BAD-BAG",
                       name = "Invalid",
                       constructionClass = "Jute",
                       standardTareWeightKg = "-0.1",
                       isReturnable = false,
                   },
                   "bag-invalid"))
        using (var json = await ReadJsonAsync(invalidBag))
        {
            Assert.Equal(HttpStatusCode.BadRequest, invalidBag.StatusCode);
            Assert.Equal("BAG_TYPE_TARE_WEIGHT_INVALID", ErrorCode(json));
        }

        var standardPolicyId = await CreatePolicyAsync(
            factory,
            first.OwnerToken,
            "STANDARD-2",
            2,
            "Standard",
            "policy-standard");
        var floorPolicyId = await CreatePolicyAsync(
            factory,
            first.OwnerToken,
            "FLOOR-2",
            2,
            "Floor",
            "policy-floor");
        _ = await CreatePolicyAsync(
            factory,
            first.OwnerToken,
            "CEILING-3",
            3,
            "Ceiling",
            "policy-ceiling");

        long settingsVersion;
        Guid settingsId;
        using (var configured = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationOneId,
                       defaultWeightProcessingPolicyId = standardPolicyId,
                       vehicleSelectionMode = "Optional",
                   },
                   "settings-configure"))
        using (var json = await ReadJsonAsync(configured))
        {
            Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
            settingsId = Result(json).GetProperty("id").GetGuid();
            settingsVersion = Result(json).GetProperty("version").GetInt64();
            Assert.Equal(1, settingsVersion);
        }

        using (var settingsReplay = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationOneId,
                       defaultWeightProcessingPolicyId = standardPolicyId,
                       vehicleSelectionMode = "Optional",
                   },
                   "settings-configure"))
        using (var json = await ReadJsonAsync(settingsReplay))
        {
            Assert.Equal(HttpStatusCode.OK, settingsReplay.StatusCode);
            Assert.Equal(
                settingsId,
                Result(json).GetProperty("id").GetGuid());
            Assert.Equal(
                "PreviouslyProcessed",
                json.RootElement
                    .GetProperty("meta")
                    .GetProperty("idempotencyStatus")
                    .GetString());
        }

        await AssertConflictAsync(
            factory,
            first.OwnerToken,
            $"/api/v1/operations/locations/{locationOneId:D}/deactivate",
            "location-default-deactivate",
            1,
            "PROCUREMENT_DEFAULT_LOCATION_IN_USE");
        await AssertConflictAsync(
            factory,
            first.OwnerToken,
            $"/api/v1/procurement/weight-policies/{standardPolicyId:D}/deactivate",
            "policy-default-deactivate",
            1,
            "PROCUREMENT_DEFAULT_WEIGHT_POLICY_IN_USE");

        using (var updated = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationTwoId,
                       defaultWeightProcessingPolicyId = floorPolicyId,
                       vehicleSelectionMode = "Disabled",
                   },
                   "settings-update",
                   expectedVersion: settingsVersion))
        using (var json = await ReadJsonAsync(updated))
        {
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
            Assert.Equal(
                "Disabled",
                Result(json)
                    .GetProperty("vehicleSelectionMode")
                    .GetString());
        }

        using (var oldLocation = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/operations/locations/{locationOneId:D}/deactivate",
                   idempotencyKey: "location-old-deactivate",
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.OK, oldLocation.StatusCode);
        }

        using (var oldPolicy = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/weight-policies/{standardPolicyId:D}/deactivate",
                   idempotencyKey: "policy-old-deactivate",
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.OK, oldPolicy.StatusCode);
        }

        var locationThreeId = await CreateLocationAsync(
            factory,
            first.OwnerToken,
            "ZZ-03",
            "Overflow Yard",
            "Yard",
            "location-create-three");
        string nextCursor;
        using (var activeList = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Get,
                   "/api/v1/operations/locations?limit=1"))
        using (var json = await ReadJsonAsync(activeList))
        {
            Assert.Equal(HttpStatusCode.OK, activeList.StatusCode);
            var page = Result(json);
            Assert.Single(page.GetProperty("items").EnumerateArray());
            Assert.True(page.GetProperty("hasMore").GetBoolean());
            Assert.Equal(
                locationTwoId,
                page.GetProperty("items")[0].GetProperty("id").GetGuid());
            nextCursor = page.GetProperty("nextCursor").GetString()!;
        }

        using (var nextPage = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Get,
                   "/api/v1/operations/locations?limit=1&cursor=" +
                   Uri.EscapeDataString(nextCursor)))
        using (var json = await ReadJsonAsync(nextPage))
        {
            Assert.Equal(HttpStatusCode.OK, nextPage.StatusCode);
            var page = Result(json);
            Assert.Single(page.GetProperty("items").EnumerateArray());
            Assert.False(page.GetProperty("hasMore").GetBoolean());
            Assert.Equal(
                locationThreeId,
                page.GetProperty("items")[0].GetProperty("id").GetGuid());
        }

        using (var inactiveSearch = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Get,
                   "/api/v1/operations/locations?status=Inactive&search=yard"))
        using (var json = await ReadJsonAsync(inactiveSearch))
        {
            Assert.Equal(HttpStatusCode.OK, inactiveSearch.StatusCode);
            var items = Result(json).GetProperty("items");
            Assert.Single(items.EnumerateArray());
            Assert.Equal(
                locationOneId,
                items[0].GetProperty("id").GetGuid());
        }

        using (var cursor = factory.CreateClient())
        using (var request = new HttpRequestMessage(
                   HttpMethod.Get,
                   "/api/v1/mobile/sync/events"))
        {
            request.Headers.Add(
                "X-TraderPro-Workspace-ID",
                first.WorkspaceId.ToString("D"));
            using var response = await cursor.SendAsync(
                request,
                CancellationToken);
            using var json = await ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(
                json.RootElement
                    .GetProperty("events")
                    .EnumerateArray());
        }

        await using (var verification = database.CreateContext(
                         first.WorkspaceId))
        {
            Assert.Equal(
                1,
                await verification.AuditEvents.CountAsync(
                    item =>
                        item.AggregateId == locationOneId &&
                        item.Action ==
                            "Operations.BusinessLocation.Created",
                    CancellationToken));
            Assert.Equal(
                1,
                await verification.OutboxMessages.CountAsync(
                    item =>
                        item.AggregateId == locationOneId &&
                        item.EventType ==
                            "Operations.BusinessLocationCreated" &&
                        item.EventStream == OutboxEventStream.Internal,
                    CancellationToken));
            Assert.Equal(
                1,
                await verification.ReceivingVehicles.CountAsync(
                    item => item.Id == vehicleId,
                    CancellationToken));
        }

        using (var crossReference = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId =
                           secondWorkspaceLocationId,
                       defaultWeightProcessingPolicyId = floorPolicyId,
                       vehicleSelectionMode = "Optional",
                   },
                   "settings-cross-workspace",
                   expectedVersion: 2))
        using (var json = await ReadJsonAsync(crossReference))
        {
            Assert.Equal(HttpStatusCode.BadRequest, crossReference.StatusCode);
            Assert.Equal("PROCUREMENT_SETTINGS_INVALID", ErrorCode(json));
        }
    }

    [Fact]
    public async Task Every_master_lifecycle_persists_actual_revisions()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "REVISION-LIFECYCLE");

        Guid locationId;
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/operations/locations",
                   new
                   {
                       code = "REV-LOC",
                       name = "Revision Yard",
                       locationType = "Yard",
                   },
                   "revision-location-create"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            locationId = Result(json).GetProperty("id").GetGuid();
            Assert.Equal(1, Result(json).GetProperty("version").GetInt64());
        }

        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/operations/locations/{locationId:D}",
                   new
                   {
                       name = "Revision Warehouse",
                       locationType = "Warehouse",
                       notes = "first revision",
                   },
                   "revision-location-update-one",
                   expectedVersion: 1))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
        }

        await AssertReadVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/operations/locations/{locationId:D}",
            2);

        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/operations/locations/{locationId:D}",
                   new
                   {
                       name = "Revision Mill",
                       locationType = "Mill",
                   },
                   "revision-location-update-two",
                   expectedVersion: 2))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        await AssertVersionConflictAsync(
            factory,
            identity.OwnerToken,
            HttpMethod.Put,
            $"/api/v1/operations/locations/{locationId:D}",
            new { name = "Stale", locationType = "Office" },
            "revision-location-stale",
            1);
        await AssertStatusVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/operations/locations/{locationId:D}/deactivate",
            "revision-location-deactivate",
            3,
            4);
        await AssertStatusVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/operations/locations/{locationId:D}/reactivate",
            "revision-location-reactivate",
            4,
            5);

        Guid vehicleId;
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "REV-VEH",
                       registrationNumber = "KA 01 AB 1234",
                       vehicleType = "Truck",
                   },
                   "revision-vehicle-create"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            vehicleId = Result(json).GetProperty("id").GetGuid();
        }

        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/procurement/vehicles/{vehicleId:D}",
                   new
                   {
                       registrationNumber = "TN-02-CD-5678",
                       displayName = "Corrected vehicle",
                       vehicleType = "Van",
                   },
                   "revision-vehicle-update",
                   expectedVersion: 1))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
        }

        await AssertVersionConflictAsync(
            factory,
            identity.OwnerToken,
            HttpMethod.Put,
            $"/api/v1/procurement/vehicles/{vehicleId:D}",
            new
            {
                registrationNumber = "TN 03 EF 9999",
                vehicleType = "Truck",
            },
            "revision-vehicle-stale",
            1);
        await AssertStatusVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/procurement/vehicles/{vehicleId:D}/deactivate",
            "revision-vehicle-deactivate",
            2,
            3);
        await AssertStatusVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/procurement/vehicles/{vehicleId:D}/reactivate",
            "revision-vehicle-reactivate",
            3,
            4);

        using (var context = database.CreateContext(identity.WorkspaceId))
        {
            var vehicle = await context.ReceivingVehicles.SingleAsync(
                item => item.Id == vehicleId,
                CancellationToken);
            Assert.Equal("TN-02-CD-5678", vehicle.RegistrationNumber);
            Assert.Equal("TN02CD5678", vehicle.NormalizedRegistrationNumber);
            Assert.Equal(4, vehicle.Version);
        }

        using (var missing = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/bag-types",
                   new
                   {
                       code = "MISSING-RETURN",
                       name = "Missing returnability",
                       constructionClass = "Jute",
                       standardTareWeightKg = "0.100000",
                   },
                   "revision-bag-missing-return"))
        using (var json = await ReadJsonAsync(missing))
        {
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
            Assert.Equal("BAG_TYPE_INVALID", ErrorCode(json));
        }

        Guid bagTypeId;
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/bag-types",
                   new
                   {
                       code = "REV-BAG",
                       name = "Jute bag",
                       constructionClass = "Jute",
                       standardTareWeightKg = "0.100000",
                       isReturnable = false,
                   },
                   "revision-bag-create"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            bagTypeId = Result(json).GetProperty("id").GetGuid();
            Assert.False(Result(json).GetProperty("isReturnable").GetBoolean());
        }

        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/procurement/bag-types/{bagTypeId:D}",
                   new
                   {
                       name = "Double plastic bag",
                       constructionClass = "DoublePlastic",
                       standardTareWeightKg = "0.234567",
                       isReturnable = true,
                   },
                   "revision-bag-update",
                   expectedVersion: 1))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("0.234567", Result(json)
                .GetProperty("standardTareWeightKg").GetString());
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
        }

        await AssertStatusVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/procurement/bag-types/{bagTypeId:D}/deactivate",
            "revision-bag-deactivate",
            2,
            3);
        await AssertStatusVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/procurement/bag-types/{bagTypeId:D}/reactivate",
            "revision-bag-reactivate",
            3,
            4);

        Guid policyId;
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/weight-policies",
                   new
                   {
                       code = "REV-POL",
                       name = "Standard policy",
                       decimalPlaces = 1,
                       processingMethod = "Standard",
                   },
                   "revision-policy-create"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            policyId = Result(json).GetProperty("id").GetGuid();
        }

        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/procurement/weight-policies/{policyId:D}",
                   new
                   {
                       name = "Ceiling policy",
                       decimalPlaces = 3,
                       processingMethod = "Ceiling",
                   },
                   "revision-policy-update",
                   expectedVersion: 1))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
        }

        await AssertStatusVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/procurement/weight-policies/{policyId:D}/deactivate",
            "revision-policy-deactivate",
            2,
            3);
        await AssertStatusVersionAsync(
            factory,
            identity.OwnerToken,
            $"/api/v1/procurement/weight-policies/{policyId:D}/reactivate",
            "revision-policy-reactivate",
            3,
            4);

        var secondLocationId = await CreateLocationAsync(
            factory,
            identity.OwnerToken,
            "REV-L2",
            "Second location",
            "Yard",
            "revision-location-two");
        var secondPolicyId = await CreatePolicyAsync(
            factory,
            identity.OwnerToken,
            "REV-P2",
            2,
            "Floor",
            "revision-policy-two");

        using (var configured = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationId,
                       defaultWeightProcessingPolicyId = policyId,
                       vehicleSelectionMode = "Optional",
                   },
                   "revision-settings-create"))
        using (var json = await ReadJsonAsync(configured))
        {
            Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
            Assert.Equal(1, Result(json).GetProperty("version").GetInt64());
        }

        using (var updated = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = secondLocationId,
                       defaultWeightProcessingPolicyId = secondPolicyId,
                       vehicleSelectionMode = "Disabled",
                   },
                   "revision-settings-update-one",
                   expectedVersion: 1))
        using (var json = await ReadJsonAsync(updated))
        {
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
        }

        await AssertReadVersionAsync(
            factory,
            identity.OwnerToken,
            "/api/v1/procurement/settings",
            2);

        using (var updated = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationId,
                       defaultWeightProcessingPolicyId = policyId,
                       vehicleSelectionMode = "Optional",
                   },
                   "revision-settings-update-two",
                   expectedVersion: 2))
        using (var json = await ReadJsonAsync(updated))
        {
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal(3, Result(json).GetProperty("version").GetInt64());
        }

        await AssertVersionConflictAsync(
            factory,
            identity.OwnerToken,
            HttpMethod.Put,
            "/api/v1/procurement/settings",
            new
            {
                defaultDestinationLocationId = secondLocationId,
                defaultWeightProcessingPolicyId = secondPolicyId,
                vehicleSelectionMode = "Disabled",
            },
            "revision-settings-stale",
            1);

        await using var verification = database.CreateContext(
            identity.WorkspaceId);
        var location = await verification.BusinessLocations.SingleAsync(
            item => item.Id == locationId,
            CancellationToken);
        Assert.Equal(5, location.Version);
        Assert.Equal(UtcNow, location.UpdatedAtUtc);
        var reactivatedOutbox = await verification.OutboxMessages.SingleAsync(
            item =>
                item.AggregateId == locationId &&
                item.EventType == "Operations.BusinessLocationReactivated",
            CancellationToken);
        Assert.Equal(location.Version, reactivatedOutbox.AggregateVersion);
        var reactivatedAudit = await verification.AuditEvents.SingleAsync(
            item =>
                item.AggregateId == locationId &&
                item.Action == "Operations.BusinessLocation.Reactivated",
            CancellationToken);
        using var after = JsonDocument.Parse(
            reactivatedAudit.AfterSnapshotJson!);
        Assert.Equal(location.Version, after.RootElement
            .GetProperty("version").GetInt64());
        Assert.Equal(location.UpdatedAtUtc, after.RootElement
            .GetProperty("updatedAtUtc").GetDateTimeOffset());
    }

    [Fact]
    public async Task Concurrent_stale_writes_have_one_revision_winner()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "REVISION-RACE");
        var locationId = await CreateLocationAsync(
            factory,
            identity.OwnerToken,
            "RACE-LOC",
            "Race location",
            "Yard",
            "race-location-create");

        var firstTask = SendAsync(
            factory,
            identity.OwnerToken,
            HttpMethod.Put,
            $"/api/v1/operations/locations/{locationId:D}",
            new { name = "Winner A", locationType = "Warehouse" },
            "race-location-update-a",
            expectedVersion: 1);
        var secondTask = SendAsync(
            factory,
            identity.OwnerToken,
            HttpMethod.Put,
            $"/api/v1/operations/locations/{locationId:D}",
            new { name = "Winner B", locationType = "Mill" },
            "race-location-update-b",
            expectedVersion: 1);
        var responses = await Task.WhenAll(firstTask, secondTask);
        using var first = responses[0];
        using var second = responses[1];
        Assert.Equal(
            1,
            responses.Count(item => item.StatusCode == HttpStatusCode.OK));
        Assert.Equal(
            1,
            responses.Count(item => item.StatusCode == HttpStatusCode.Conflict));
        var conflict = responses.Single(
            item => item.StatusCode == HttpStatusCode.Conflict);
        using (var conflictJson = await ReadJsonAsync(conflict))
        {
            Assert.Equal("MASTER_VERSION_CONFLICT", ErrorCode(conflictJson));
        }

        await using (var verification = database.CreateContext(
                         identity.WorkspaceId))
        {
            var location = await verification.BusinessLocations.SingleAsync(
                item => item.Id == locationId,
                CancellationToken);
            Assert.Equal(2, location.Version);
            Assert.Equal(
                1,
                await verification.AuditEvents.CountAsync(
                    item =>
                        item.AggregateId == locationId &&
                        item.Action == "Operations.BusinessLocation.Updated",
                    CancellationToken));
            Assert.Equal(
                1,
                await verification.OutboxMessages.CountAsync(
                    item =>
                        item.AggregateId == locationId &&
                        item.EventType ==
                            "Operations.BusinessLocationUpdated" &&
                        item.AggregateVersion == 2,
                    CancellationToken));
        }

        var vehicleOne = SendAsync(
            factory,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/vehicles",
            new
            {
                code = "RACE-V1",
                registrationNumber = "KA 09 ZZ 9999",
                vehicleType = "Truck",
            },
            "race-vehicle-one");
        var vehicleTwo = SendAsync(
            factory,
            identity.OwnerToken,
            HttpMethod.Post,
            "/api/v1/procurement/vehicles",
            new
            {
                code = "RACE-V2",
                registrationNumber = "ka-09-zz-9999",
                vehicleType = "Truck",
            },
            "race-vehicle-two");
        var vehicleResponses = await Task.WhenAll(vehicleOne, vehicleTwo);
        using var vehicleFirst = vehicleResponses[0];
        using var vehicleSecond = vehicleResponses[1];
        Assert.Equal(
            1,
            vehicleResponses.Count(
                item => item.StatusCode == HttpStatusCode.Created));
        Assert.Equal(
            1,
            vehicleResponses.Count(
                item => item.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Internal_outbox_failure_rolls_back_master_revision()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "REVISION-ROLLBACK");
        var locationId = await CreateLocationAsync(
            factory,
            identity.OwnerToken,
            "ROLLBACK-LOC",
            "Before rollback",
            "Yard",
            "rollback-location-create");

        await CreateRejectingCommercialOutboxTriggerAsync(database);
        try
        {
            using var response = await SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/operations/locations/{locationId:D}",
                new
                {
                    name = "Must roll back",
                    locationType = "Warehouse",
                },
                "rollback-location-update",
                expectedVersion: 1);
            using var json = await ReadJsonAsync(response);
            Assert.Equal(
                HttpStatusCode.ServiceUnavailable,
                response.StatusCode);
            Assert.Equal("TEMPORARY_COMMAND_FAILURE", ErrorCode(json));
        }
        finally
        {
            await DropRejectingCommercialOutboxTriggerAsync(database);
        }

        await using var verification = database.CreateContext(
            identity.WorkspaceId);
        var location = await verification.BusinessLocations.SingleAsync(
            item => item.Id == locationId,
            CancellationToken);
        Assert.Equal("Before rollback", location.Name);
        Assert.Equal(1, location.Version);
        Assert.Equal(UtcNow, location.UpdatedAtUtc);
        Assert.Equal(
            0,
            await verification.AuditEvents.CountAsync(
                item =>
                    item.AggregateId == locationId &&
                    item.Action == "Operations.BusinessLocation.Updated",
                CancellationToken));
        Assert.Equal(
            0,
            await verification.OutboxMessages.CountAsync(
                item =>
                    item.AggregateId == locationId &&
                    item.EventType == "Operations.BusinessLocationUpdated",
                CancellationToken));
        Assert.Equal(
            0,
            await verification.IdempotencyRecords.CountAsync(
                item =>
                    item.IdempotencyKey == "rollback-location-update" &&
                    item.CommandType ==
                        "Operations.BusinessLocation.Update",
                CancellationToken));
    }

    [Fact]
    public async Task Canonical_retries_match_persisted_optional_text_and_registration()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "CANONICAL-RETRY");

        using (var created = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/operations/locations",
                   new
                   {
                       code = "CANON-LOC",
                       name = "Canonical Yard",
                       locationType = "Yard",
                       notes = "   ",
                   },
                   "canonical-location"))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        using (var replay = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/operations/locations",
                   new
                   {
                       code = "CANON-LOC",
                       name = "Canonical Yard",
                       locationType = "Yard",
                       notes = (string?)null,
                   },
                   "canonical-location"))
        using (var json = await ReadJsonAsync(replay))
        {
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            Assert.Equal(
                "PreviouslyProcessed",
                json.RootElement.GetProperty("meta")
                    .GetProperty("idempotencyStatus").GetString());
        }

        using (var created = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "CANON-VEH",
                       registrationNumber = " KA 01 AB 7777 ",
                       displayName = "   ",
                       vehicleType = "Truck",
                   },
                   "canonical-vehicle"))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        using (var replay = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "CANON-VEH",
                       registrationNumber = "KA 01 AB 7777",
                       displayName = (string?)null,
                       vehicleType = "Truck",
                   },
                   "canonical-vehicle"))
        using (var json = await ReadJsonAsync(replay))
        {
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            Assert.Equal(
                "PreviouslyProcessed",
                json.RootElement.GetProperty("meta")
                    .GetProperty("idempotencyStatus").GetString());
        }

        using (var changed = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "CANON-VEH",
                       registrationNumber = "KA-01-AB-7777",
                       vehicleType = "Truck",
                   },
                   "canonical-vehicle"))
        using (var json = await ReadJsonAsync(changed))
        {
            Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
            Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT", ErrorCode(json));
        }
    }

    [Fact]
    public async Task Cursor_scope_keyset_and_internal_ordering_are_isolated()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var first = await CreateIdentityAsync(factory, "CURSOR-FIRST");
        var second = await CreateIdentityAsync(factory, "CURSOR-SECOND");

        await using (var lockConnection = await database.OpenConnectionAsync())
        await using (var lockTransaction =
            await lockConnection.BeginTransactionAsync(CancellationToken))
        {
            await using var lockCommand = lockConnection.CreateCommand();
            lockCommand.Transaction = lockTransaction;
            lockCommand.CommandText =
                """
                SELECT pg_advisory_xact_lock(
                    hashtextextended(
                        'TraderPro.OutboxSequence.CommitOrder.v1',
                        0))
                """;
            await lockCommand.ExecuteNonQueryAsync(CancellationToken);

            using var internalCommand = await SendAsync(
                    factory,
                    first.OwnerToken,
                    HttpMethod.Post,
                    "/api/v1/operations/locations",
                    new
                    {
                        code = "AA",
                        name = "Cursor A",
                        locationType = "Yard",
                    },
                    "cursor-location-aa")
                .WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);
            Assert.Equal(HttpStatusCode.Created, internalCommand.StatusCode);
            await lockTransaction.RollbackAsync(CancellationToken);
        }

        _ = await CreateLocationAsync(
            factory,
            first.OwnerToken,
            "CC",
            "Cursor C",
            "Yard",
            "cursor-location-cc");

        string cursor;
        using (var firstPage = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Get,
                   "/api/v1/operations/locations?limit=1"))
        using (var json = await ReadJsonAsync(firstPage))
        {
            Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
            Assert.Equal(
                "AA",
                Result(json).GetProperty("items")[0]
                    .GetProperty("code").GetString());
            cursor = Result(json).GetProperty("nextCursor").GetString()!;
        }

        _ = await CreateLocationAsync(
            factory,
            first.OwnerToken,
            "BB",
            "Cursor B",
            "Yard",
            "cursor-location-bb");

        string secondCursor;
        using (var nextPage = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Get,
                   "/api/v1/operations/locations?limit=1&cursor=" +
                   Uri.EscapeDataString(cursor)))
        using (var json = await ReadJsonAsync(nextPage))
        {
            Assert.Equal(HttpStatusCode.OK, nextPage.StatusCode);
            Assert.Equal(
                "BB",
                Result(json).GetProperty("items")[0]
                    .GetProperty("code").GetString());
            secondCursor = Result(json)
                .GetProperty("nextCursor").GetString()!;
        }

        using (var finalPage = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Get,
                   "/api/v1/operations/locations?limit=1&cursor=" +
                   Uri.EscapeDataString(secondCursor)))
        using (var json = await ReadJsonAsync(finalPage))
        {
            Assert.Equal(HttpStatusCode.OK, finalPage.StatusCode);
            Assert.Equal(
                "CC",
                Result(json).GetProperty("items")[0]
                    .GetProperty("code").GetString());
        }

        await AssertCursorInvalidAsync(
            factory,
            first.OwnerToken,
            "/api/v1/operations/locations?status=All&limit=1&cursor=" +
            Uri.EscapeDataString(cursor));
        await AssertCursorInvalidAsync(
            factory,
            first.OwnerToken,
            "/api/v1/operations/locations?search=AA&limit=1&cursor=" +
            Uri.EscapeDataString(cursor));
        await AssertCursorInvalidAsync(
            factory,
            first.OwnerToken,
            "/api/v1/procurement/vehicles?limit=1&cursor=" +
            Uri.EscapeDataString(cursor));
        await AssertCursorInvalidAsync(
            factory,
            second.OwnerToken,
            "/api/v1/operations/locations?limit=1&cursor=" +
            Uri.EscapeDataString(cursor));
    }

    [Fact]
    public async Task Procurement_default_lock_serializes_assignment_and_deactivation()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "DEFAULT-LOCK");
        var locationOneId = await CreateLocationAsync(
            factory,
            identity.OwnerToken,
            "LOCK-L1",
            "Lock location one",
            "Yard",
            "lock-location-one");
        var locationTwoId = await CreateLocationAsync(
            factory,
            identity.OwnerToken,
            "LOCK-L2",
            "Lock location two",
            "Warehouse",
            "lock-location-two");
        var policyOneId = await CreatePolicyAsync(
            factory,
            identity.OwnerToken,
            "LOCK-P1",
            1,
            "Standard",
            "lock-policy-one");
        var policyTwoId = await CreatePolicyAsync(
            factory,
            identity.OwnerToken,
            "LOCK-P2",
            2,
            "Floor",
            "lock-policy-two");

        Guid settingsId;
        using (var configured = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationTwoId,
                       defaultWeightProcessingPolicyId = policyTwoId,
                       vehicleSelectionMode = "Optional",
                   },
                   "lock-settings-create"))
        using (var json = await ReadJsonAsync(configured))
        {
            Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
            settingsId = Result(json).GetProperty("id").GetGuid();
        }

        await RunDefaultsRaceAsync(
            database,
            settingsId,
            1,
            locationOneId,
            policyTwoId,
            "operations.business_locations",
            locationOneId,
            deactivationShouldFail: true);
        await RunDefaultsRaceAsync(
            database,
            settingsId,
            2,
            locationOneId,
            policyOneId,
            "procurement.weight_processing_policies",
            policyOneId,
            deactivationShouldFail: true);
        await RunDefaultsRaceAsync(
            database,
            settingsId,
            3,
            locationTwoId,
            policyTwoId,
            "operations.business_locations",
            locationOneId,
            deactivationShouldFail: false);

        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                UPDATE procurement.weight_processing_policies
                SET status = 2,
                    version = version + 1,
                    updated_at_utc = updated_at_utc
                WHERE id = @id
                """;
            command.Parameters.AddWithValue("id", policyOneId);
            Assert.Equal(
                1,
                await command.ExecuteNonQueryAsync(CancellationToken));
        }

        await using var verification = await database.OpenConnectionAsync();
        await using var verify = verification.CreateCommand();
        verify.CommandText =
            """
            SELECT
                settings.version = 4,
                location.status = 1,
                policy.status = 1,
                old_location.status = 2,
                old_policy.status = 2
            FROM procurement.company_procurement_settings AS settings
            JOIN operations.business_locations AS location
              ON location.id =
                 settings.default_destination_location_id
            JOIN procurement.weight_processing_policies AS policy
              ON policy.id =
                 settings.default_weight_processing_policy_id
            JOIN operations.business_locations AS old_location
              ON old_location.id = @old_location_id
            JOIN procurement.weight_processing_policies AS old_policy
              ON old_policy.id = @old_policy_id
            WHERE settings.id = @settings_id
            """;
        verify.Parameters.AddWithValue("settings_id", settingsId);
        verify.Parameters.AddWithValue("old_location_id", locationOneId);
        verify.Parameters.AddWithValue("old_policy_id", policyOneId);
        await using var reader = await verify.ExecuteReaderAsync(
            CancellationToken);
        Assert.True(await reader.ReadAsync(CancellationToken));
        for (var index = 0; index < reader.FieldCount; index++)
        {
            Assert.True(reader.GetBoolean(index));
        }
    }

    [Fact]
    public async Task Database_rejects_delete_identity_changes_and_bad_defaults()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await AssertSchemaControlsAsync(database);
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "DIRECT-SQL");
        var locationId = await CreateLocationAsync(
            factory,
            identity.OwnerToken,
            "SQL-YARD",
            "SQL Yard",
            "Yard",
            "sql-location");
        var policyId = await CreatePolicyAsync(
            factory,
            identity.OwnerToken,
            "SQL-POLICY",
            2,
            "Standard",
            "sql-policy");
        Guid vehicleId;
        using (var vehicle = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "SQL-VEH",
                       registrationNumber = "KA 01 AA 0001",
                       vehicleType = "Truck",
                   },
                   "sql-vehicle"))
        using (var json = await ReadJsonAsync(vehicle))
        {
            Assert.Equal(HttpStatusCode.Created, vehicle.StatusCode);
            vehicleId = Result(json).GetProperty("id").GetGuid();
        }

        Guid bagTypeId;
        using (var bag = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/bag-types",
                   new
                   {
                       code = "SQL-BAG",
                       name = "SQL bag",
                       constructionClass = "Jute",
                       standardTareWeightKg = "0.100000",
                       isReturnable = false,
                   },
                   "sql-bag"))
        using (var json = await ReadJsonAsync(bag))
        {
            Assert.Equal(HttpStatusCode.Created, bag.StatusCode);
            bagTypeId = Result(json).GetProperty("id").GetGuid();
        }

        Guid settingsId;
        using (var configured = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   "/api/v1/procurement/settings",
                   new
                   {
                       defaultDestinationLocationId = locationId,
                       defaultWeightProcessingPolicyId = policyId,
                       vehicleSelectionMode = "Optional",
                   },
                   "sql-settings"))
        using (var json = await ReadJsonAsync(configured))
        {
            Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
            settingsId = Result(json).GetProperty("id").GetGuid();
        }

        await AssertSqlRejectedAsync(
            database,
            """
            DELETE FROM operations.business_locations
            WHERE id = @id
            """,
            ("id", locationId));
        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE operations.business_locations
            SET code = 'CHANGED',
                normalized_code = 'CHANGED',
                version = version + 1
            WHERE id = @id
            """,
            ("id", locationId));
        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE operations.business_locations
            SET status = 2,
                version = version + 1
            WHERE id = @id
            """,
            ("id", locationId));
        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE procurement.weight_processing_policies
            SET status = 2,
                version = version + 1
            WHERE id = @id
            """,
            ("id", policyId));
        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE procurement.company_procurement_settings
            SET default_branch_id = @branch_id,
                version = version + 1
            WHERE workspace_id = @workspace_id
              AND company_id = @company_id
            """,
            ("branch_id", Guid.CreateVersion7()),
            ("workspace_id", identity.WorkspaceId),
            ("company_id", identity.CompanyId));
        await AssertSqlRejectedAsync(
            database,
            """
            INSERT INTO procurement.bag_types (
                id, workspace_id, company_id, code, normalized_code,
                name, construction_class, standard_tare_weight_kg,
                is_returnable, status, created_at_utc, updated_at_utc,
                version)
            VALUES (
                @id, @workspace_id, @company_id, 'BAD', 'BAD',
                'Bad', 99, -0.1, false, 99, @now, @now, 0)
            """,
            ("id", Guid.CreateVersion7()),
            ("workspace_id", identity.WorkspaceId),
            ("company_id", identity.CompanyId),
            ("now", UtcNow));

        var revisionTargets = new[]
        {
            new RevisionTarget(
                "operations",
                "business_locations",
                locationId,
                "name = name || ' X'"),
            new RevisionTarget(
                "procurement",
                "receiving_vehicles",
                vehicleId,
                "display_name = COALESCE(display_name, '') || ' X'"),
            new RevisionTarget(
                "procurement",
                "bag_types",
                bagTypeId,
                "name = name || ' X'"),
            new RevisionTarget(
                "procurement",
                "weight_processing_policies",
                policyId,
                "name = name || ' X'"),
            new RevisionTarget(
                "procurement",
                "company_procurement_settings",
                settingsId,
                "vehicle_selection_mode = CASE vehicle_selection_mode WHEN 1 THEN 2 ELSE 1 END"),
        };
        foreach (var target in revisionTargets)
        {
            await AssertRevisionTransitionsRejectedAsync(database, target);
        }

        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE procurement.receiving_vehicles
            SET registration_number = 'TN 01 BB 0002',
                normalized_registration_number = 'MISMATCH',
                version = version + 1,
                updated_at_utc = updated_at_utc
            WHERE id = @id
            """,
            ("id", vehicleId));
    }

    [Fact]
    public async Task Current_identity_database_upgrades_without_data_loss()
    {
        await using var database = await fixture.CreateDatabaseAsync(
            PreviousMigration);
        var workspaceId = Uuid7.NewGuid();
        var now = UtcNow;
        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                INSERT INTO platform.workspaces (
                    id, code, workspace_code,
                    normalized_workspace_code, display_name, status,
                    created_at_utc, updated_at_utc, version)
                VALUES (
                    @workspace_id, 'upgrade-commercial',
                    'UPGRADE-COMMERCIAL', 'UPGRADE-COMMERCIAL',
                    'Upgrade Commercial', 1, @now, @now, 1);

                INSERT INTO platform.outbox_messages (
                    id, workspace_id, event_stream, event_type,
                    event_version, aggregate_type, aggregate_id,
                    aggregate_version, payload_json, correlation_id,
                    occurred_at_utc, status, attempt_count)
                VALUES (
                    @outbox_id, @workspace_id, 1, 'ExistingInternal',
                    1, 'ExistingAggregate', @aggregate_id,
                    1, CAST('{"preserved":true}' AS jsonb),
                    @correlation_id, @now, 1, 0);
                """;
            command.Parameters.AddWithValue("workspace_id", workspaceId);
            command.Parameters.AddWithValue("now", now);
            command.Parameters.AddWithValue("outbox_id", Uuid7.NewGuid());
            command.Parameters.AddWithValue(
                "aggregate_id",
                Uuid7.NewGuid());
            command.Parameters.AddWithValue(
                "correlation_id",
                Uuid7.NewGuid().ToString("D"));
            await command.ExecuteNonQueryAsync(CancellationToken);
        }

        await database.ApplyMigrationsAsync();

        await using var verification = database.CreateContext(workspaceId);
        Assert.Equal(
            1,
            await verification.Workspaces.CountAsync(
                item => item.Id == workspaceId,
                CancellationToken));
        Assert.Equal(
            1,
            await verification.OutboxMessages.CountAsync(
                item =>
                    item.EventType == "ExistingInternal" &&
                    item.EventStream == OutboxEventStream.Internal,
                CancellationToken));
        Assert.Equal(
            0,
            await verification.BusinessLocations.CountAsync(
                CancellationToken));
        Assert.Equal(
            0,
            await verification.CompanyProcurementSettings.CountAsync(
                CancellationToken));
    }

    [Fact]
    public async Task Original_commercial_database_upgrades_without_rewriting_data()
    {
        await using var database = await fixture.CreateDatabaseAsync(
            OriginalCommercialMigration);
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(
            factory,
            "COMMERCIAL-HARDEN-UPGRADE");
        var locationId = await CreateLocationAsync(
            factory,
            identity.OwnerToken,
            "UPGRADE-L",
            "Upgrade location",
            "Yard",
            "upgrade-location-create");

        Guid vehicleId;
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/vehicles",
                   new
                   {
                       code = "UPGRADE-V",
                       registrationNumber = "KA 01 UP 1000",
                       vehicleType = "Truck",
                   },
                   "upgrade-vehicle-create"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            vehicleId = Result(json).GetProperty("id").GetGuid();
        }

        await database.ApplyMigrationsAsync();

        await using var verification = database.CreateContext(
            identity.WorkspaceId);
        Assert.Equal(
            1,
            await verification.BusinessLocations.CountAsync(
                item => item.Id == locationId && item.Version == 1,
                CancellationToken));
        Assert.Equal(
            1,
            await verification.ReceivingVehicles.CountAsync(
                item =>
                    item.Id == vehicleId &&
                    item.NormalizedRegistrationNumber == "KA01UP1000" &&
                    item.Version == 1,
                CancellationToken));
    }

    private static TraderProApiFactory Factory(
        IsolatedPostgreSqlDatabase database)
    {
        return new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: true,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);
    }

    private static async Task<CommercialIdentity> CreateIdentityAsync(
        TraderProApiFactory factory,
        string workspaceCode)
    {
        using var client = factory.CreateClient();
        using var bootstrap = await client.PostAsJsonAsync(
            "/api/v1/spikes/identity/bootstrap",
            new
            {
                workspaceCode,
                ownerPassword = OwnerPassword,
                operatorPassword = OperatorPassword,
            },
            CancellationToken);
        Assert.Equal(HttpStatusCode.OK, bootstrap.StatusCode);
        using var setup = await ReadJsonAsync(bootstrap);
        var root = setup.RootElement;
        var workspaceId = root.GetProperty("workspaceId").GetGuid();
        var ownerDeviceId = root.GetProperty("ownerDeviceId").GetGuid();
        var operatorDeviceId = root
            .GetProperty("operatorDeviceId")
            .GetGuid();
        var ownerSecret = await ActivateAsync(
            factory,
            workspaceCode,
            ownerDeviceId,
            root.GetProperty("ownerActivation")
                .GetProperty("activationCode")
                .GetString()!);
        var operatorSecret = await ActivateAsync(
            factory,
            workspaceCode,
            operatorDeviceId,
            root.GetProperty("operatorActivation")
                .GetProperty("activationCode")
                .GetString()!);
        var ownerToken = await LoginAsync(
            factory,
            workspaceCode,
            "owner",
            OwnerPassword,
            ownerDeviceId,
            ownerSecret);
        var operatorToken = await LoginAsync(
            factory,
            workspaceCode,
            "operator",
            OperatorPassword,
            operatorDeviceId,
            operatorSecret);
        return new CommercialIdentity(
            workspaceId,
            ReadGuidClaim(ownerToken, "cid"),
            ReadGuidClaim(ownerToken, "bid"),
            ownerDeviceId,
            operatorDeviceId,
            ownerToken,
            operatorToken);
    }

    private static async Task<string> ActivateAsync(
        TraderProApiFactory factory,
        string workspaceCode,
        Guid deviceId,
        string activationCode)
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/device-activations/redeem",
            new
            {
                workspaceCode,
                activationCode,
                clientInstallationReference = $"commercial-{deviceId:D}",
                deviceLabel = "Commercial test device",
                platform = "Testing",
            },
            CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("deviceSecret").GetString()!;
    }

    private static async Task<string> LoginAsync(
        TraderProApiFactory factory,
        string workspaceCode,
        string login,
        string password,
        Guid deviceId,
        string deviceSecret)
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                workspaceCode,
                login,
                password,
                deviceId,
                deviceSecret,
            },
            CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<Guid> CreateLocationAsync(
        TraderProApiFactory factory,
        string token,
        string code,
        string name,
        string type,
        string key)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            "/api/v1/operations/locations",
            new
            {
                code,
                name,
                locationType = type,
            },
            key);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return Result(json).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreatePolicyAsync(
        TraderProApiFactory factory,
        string token,
        string code,
        int decimalPlaces,
        string method,
        string key)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            "/api/v1/procurement/weight-policies",
            new
            {
                code,
                name = $"{method} policy",
                decimalPlaces,
                processingMethod = method,
            },
            key);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return Result(json).GetProperty("id").GetGuid();
    }

    private static async Task AssertConflictAsync(
        TraderProApiFactory factory,
        string token,
        string path,
        string key,
        long expectedVersion,
        string expectedCode)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            path,
            idempotencyKey: key,
            expectedVersion: expectedVersion);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(expectedCode, ErrorCode(json));
    }

    private static async Task AssertReadVersionAsync(
        TraderProApiFactory factory,
        string token,
        string path,
        long expectedVersion)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Get,
            path);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            expectedVersion,
            Result(json).GetProperty("version").GetInt64());
    }

    private static async Task AssertVersionConflictAsync(
        TraderProApiFactory factory,
        string token,
        HttpMethod method,
        string path,
        object body,
        string key,
        long expectedVersion)
    {
        using var response = await SendAsync(
            factory,
            token,
            method,
            path,
            body,
            key,
            expectedVersion);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("MASTER_VERSION_CONFLICT", ErrorCode(json));
    }

    private static async Task AssertStatusVersionAsync(
        TraderProApiFactory factory,
        string token,
        string path,
        string key,
        long expectedVersion,
        long returnedVersion)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            path,
            idempotencyKey: key,
            expectedVersion: expectedVersion);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            returnedVersion,
            Result(json).GetProperty("version").GetInt64());
    }

    private static async Task AssertCursorInvalidAsync(
        TraderProApiFactory factory,
        string token,
        string path)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Get,
            path);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("MASTER_CURSOR_INVALID", ErrorCode(json));
    }

    private static async Task<HttpResponseMessage> SendAsync(
        TraderProApiFactory factory,
        string token,
        HttpMethod method,
        string path,
        object? body = null,
        string? idempotencyKey = null,
        long? expectedVersion = null,
        Guid? temporaryWorkspaceId = null,
        Guid? temporaryDeviceId = null)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        if (expectedVersion is not null)
        {
            request.Headers.Add(
                "X-Expected-Version",
                expectedVersion.Value.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
        }

        if (temporaryWorkspaceId is not null)
        {
            request.Headers.Add(
                "X-TraderPro-Workspace-ID",
                temporaryWorkspaceId.Value.ToString("D"));
        }

        if (temporaryDeviceId is not null)
        {
            request.Headers.Add(
                "X-TraderPro-Device-ID",
                temporaryDeviceId.Value.ToString("D"));
        }

        return await client.SendAsync(request, CancellationToken);
    }

    private static async Task AssertSqlRejectedAsync(
        IsolatedPostgreSqlDatabase database,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(CancellationToken));
    }

    private static async Task AssertRevisionTransitionsRejectedAsync(
        IsolatedPostgreSqlDatabase database,
        RevisionTarget target)
    {
        var table = $"{target.Schema}.{target.Table}";
        await AssertSqlRejectedAsync(
            database,
            $"UPDATE {table} SET {target.MutableAssignment} WHERE id = @id",
            ("id", target.Id));
        await AssertSqlRejectedAsync(
            database,
            $"UPDATE {table} SET {target.MutableAssignment}, version = version + 2, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", target.Id));
        await AssertSqlRejectedAsync(
            database,
            $"UPDATE {table} SET {target.MutableAssignment}, version = version - 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", target.Id));
        await AssertSqlRejectedAsync(
            database,
            $"UPDATE {table} SET {target.MutableAssignment}, version = version + 1, updated_at_utc = updated_at_utc - interval '1 second' WHERE id = @id",
            ("id", target.Id));
        await AssertSqlRejectedAsync(
            database,
            $"UPDATE {table} SET version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", target.Id));
        await AssertSqlRejectedAsync(
            database,
            $"UPDATE {table} SET {target.MutableAssignment}, version = version + 1, updated_at_utc = updated_at_utc, created_at_utc = created_at_utc + interval '1 second' WHERE id = @id",
            ("id", target.Id));
    }

    private static async Task RunDefaultsRaceAsync(
        IsolatedPostgreSqlDatabase database,
        Guid settingsId,
        long expectedSettingsVersion,
        Guid destinationLocationId,
        Guid weightPolicyId,
        string deactivationTable,
        Guid deactivationId,
        bool deactivationShouldFail)
    {
        await using var settingsConnection =
            await database.OpenConnectionAsync();
        await using var settingsTransaction =
            await settingsConnection.BeginTransactionAsync(
                CancellationToken);
        await using (var settingsCommand = settingsConnection.CreateCommand())
        {
            settingsCommand.Transaction = settingsTransaction;
            settingsCommand.CommandText =
                """
                UPDATE procurement.company_procurement_settings
                SET default_destination_location_id = @location_id,
                    default_weight_processing_policy_id = @policy_id,
                    version = version + 1,
                    updated_at_utc = updated_at_utc
                WHERE id = @settings_id
                  AND version = @expected_version
                """;
            settingsCommand.Parameters.AddWithValue(
                "location_id",
                destinationLocationId);
            settingsCommand.Parameters.AddWithValue(
                "policy_id",
                weightPolicyId);
            settingsCommand.Parameters.AddWithValue(
                "settings_id",
                settingsId);
            settingsCommand.Parameters.AddWithValue(
                "expected_version",
                expectedSettingsVersion);
            Assert.Equal(
                1,
                await settingsCommand.ExecuteNonQueryAsync(
                    CancellationToken));
        }

        await using var deactivationConnection =
            await database.OpenConnectionAsync();
        await using var deactivationTransaction =
            await deactivationConnection.BeginTransactionAsync(
                CancellationToken);
        await using var deactivationCommand =
            deactivationConnection.CreateCommand();
        deactivationCommand.Transaction = deactivationTransaction;
        deactivationCommand.CommandText =
            $"UPDATE {deactivationTable} SET status = 2, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id";
        deactivationCommand.Parameters.AddWithValue("id", deactivationId);
        var deactivationTask = deactivationCommand.ExecuteNonQueryAsync(
            CancellationToken);
        await Task.Delay(150, CancellationToken);
        Assert.False(deactivationTask.IsCompleted);

        await settingsTransaction.CommitAsync(CancellationToken);
        if (deactivationShouldFail)
        {
            await Assert.ThrowsAsync<PostgresException>(
                async () => await deactivationTask);
            await deactivationTransaction.RollbackAsync(CancellationToken);
        }
        else
        {
            Assert.Equal(1, await deactivationTask);
            await deactivationTransaction.CommitAsync(CancellationToken);
        }
    }

    private static async Task CreateRejectingCommercialOutboxTriggerAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE FUNCTION platform.reject_task_7b1_internal_outbox()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                IF NEW.event_stream = 1
                   AND NEW.event_type =
                       'Operations.BusinessLocationUpdated' THEN
                    RAISE EXCEPTION
                        'Injected Task 7B1 Internal outbox failure';
                END IF;
                RETURN NEW;
            END;
            $function$;

            CREATE TRIGGER tr_reject_task_7b1_internal_outbox
            BEFORE INSERT ON platform.outbox_messages
            FOR EACH ROW
            EXECUTE FUNCTION platform.reject_task_7b1_internal_outbox();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task DropRejectingCommercialOutboxTriggerAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP TRIGGER IF EXISTS tr_reject_task_7b1_internal_outbox
                ON platform.outbox_messages;
            DROP FUNCTION IF EXISTS
                platform.reject_task_7b1_internal_outbox();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task AssertSchemaControlsAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                (
                    SELECT COUNT(*)
                    FROM information_schema.tables
                    WHERE (table_schema, table_name) IN (
                        ('operations', 'business_locations'),
                        ('procurement', 'receiving_vehicles'),
                        ('procurement', 'bag_types'),
                        ('procurement', 'weight_processing_policies'),
                        ('procurement', 'company_procurement_settings')
                    )
                ) = 5,
                EXISTS (
                    SELECT 1
                    FROM pg_indexes
                    WHERE schemaname = 'operations'
                      AND indexname =
                          'ix_business_locations_active_destination_lookup'
                ),
                EXISTS (
                    SELECT 1
                    FROM pg_indexes
                    WHERE schemaname = 'procurement'
                      AND indexname =
                          'ux_receiving_vehicles_workspace_company_registration'
                ),
                (
                    SELECT COUNT(*)
                    FROM pg_trigger
                    WHERE tgname IN (
                        'tr_business_locations_protect',
                        'tr_receiving_vehicles_protect',
                        'tr_bag_types_protect',
                        'tr_weight_processing_policies_protect',
                        'tr_company_procurement_settings_protect',
                        'tr_company_procurement_settings_validate',
                        'tr_business_locations_prevent_default_deactivation',
                        'tr_weight_policies_prevent_default_deactivation',
                        'tr_business_locations_revision_contract',
                        'tr_receiving_vehicles_revision_contract',
                        'tr_bag_types_revision_contract',
                        'tr_weight_processing_policies_revision_contract',
                        'tr_company_procurement_settings_revision_contract'
                    )
                      AND NOT tgisinternal
                ) = 13,
                EXISTS (
                    SELECT 1
                    FROM pg_proc AS procedure
                    JOIN pg_namespace AS namespace
                      ON namespace.oid = procedure.pronamespace
                    WHERE namespace.nspname = 'procurement'
                      AND procedure.proname =
                          'lock_procurement_defaults'
                ),
                EXISTS (
                    SELECT 1
                    FROM pg_proc AS procedure
                    JOIN pg_namespace AS namespace
                      ON namespace.oid = procedure.pronamespace
                    WHERE namespace.nspname = 'platform'
                      AND procedure.proname =
                          'enforce_commercial_master_revision'
                );
            """;
        await using var reader = await command.ExecuteReaderAsync(
            CancellationToken);
        Assert.True(await reader.ReadAsync(CancellationToken));
        for (var index = 0; index < reader.FieldCount; index++)
        {
            Assert.True(reader.GetBoolean(index));
        }
    }

    private static Guid ReadGuidClaim(string token, string claim)
    {
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(
            payload.Length + ((4 - payload.Length % 4) % 4),
            '=');
        using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
        return Guid.ParseExact(
            json.RootElement.GetProperty(claim).GetString()!,
            "D");
    }

    private static JsonElement Result(JsonDocument document)
    {
        return document.RootElement.GetProperty("result");
    }

    private static string? ErrorCode(JsonDocument document)
    {
        return document.RootElement
            .GetProperty("error")
            .GetProperty("code")
            .GetString();
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

    private sealed record CommercialIdentity(
        Guid WorkspaceId,
        Guid CompanyId,
        Guid BranchId,
        Guid OwnerDeviceId,
        Guid OperatorDeviceId,
        string OwnerToken,
        string OperatorToken);

    private sealed record RevisionTarget(
        string Schema,
        string Table,
        Guid Id,
        string MutableAssignment);
}
