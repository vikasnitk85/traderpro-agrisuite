using System.Text.Json;
using System.Text.RegularExpressions;
using TraderPro.Application.Platform.Identity;
using TraderPro.Application.Procurement.Receiving;
using TraderPro.Domain.Procurement.Receiving;

namespace TraderPro.UnitTests;

public sealed partial class CommercialReceivingContractFixtureTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
    };

    [Fact]
    public void Operation_vectors_match_payload_types_hashes_statuses_and_codes()
    {
        using var document = LoadFixture(
            "golden-vectors",
            "commercial-receiving-operations.v1.json");
        var root = document.RootElement;

        Assert.Equal(
            CommercialReceivingRequestHash.ContractVersion,
            RequiredString(root, "serverHashContractVersion"));
        Assert.Equal(
            CommercialReceivingOperationTypes.SharedCommandScope,
            RequiredString(root, "sharedCommandScope"));
        Assert.Equal(
            Enum.GetNames<CommercialMobileOperationStatus>(),
            Strings(root.GetProperty("operationStatuses")));
        Assert.Equal(["Manual"], Strings(root.GetProperty("mobileWeightSources")));

        var context = root.GetProperty("context");
        var workspaceId = RequiredGuid(context, "workspaceId");
        var companyId = RequiredGuid(context, "companyId");
        var userId = RequiredGuid(context, "userId");
        var deviceId = RequiredGuid(context, "deviceId");
        var expectedTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            [CommercialReceivingOperationTypes.Start] =
                typeof(StartCommercialReceivingSessionPayload),
            [CommercialReceivingOperationTypes.RecordEntry] =
                typeof(RecordCommercialReceivingEntryPayload),
            [CommercialReceivingOperationTypes.Submit] =
                typeof(SubmitCommercialReceivingSessionPayload),
        };

        foreach (var operation in root.GetProperty("operations").EnumerateArray())
        {
            var operationType = RequiredString(operation, "operationType");
            var operationId = RequiredGuid(operation, "operationId");
            var sessionId = RequiredGuid(operation, "sessionId");
            var localSequence = operation.GetProperty("localSequence").GetInt64();
            long? ownershipGeneration = operation
                .GetProperty("ownershipGeneration")
                .ValueKind is JsonValueKind.Null
                ? null
                : operation.GetProperty("ownershipGeneration").GetInt64();
            var payloadJson = RequiredString(operation, "payloadJson");
            var payloadHash = RequiredString(operation, "payloadHash");

            Assert.True(expectedTypes.TryGetValue(operationType, out var payloadType));
            using var payloadDocument = JsonDocument.Parse(payloadJson);
            AssertMatchesProductionDto(payloadDocument.RootElement, payloadType!);
            Assert.Equal(
                CommercialReceivingRequestHash.PayloadHash(payloadJson),
                payloadHash);
            Assert.Equal(
                CommercialReceivingRequestHash.ForMobileOperation(
                    operationType,
                    operationId,
                    sessionId,
                    localSequence,
                    ownershipGeneration,
                    workspaceId,
                    companyId,
                    userId,
                    deviceId,
                    payloadHash,
                    payloadJson),
                RequiredString(operation, "canonicalRequestHash"));
        }

        Assert.Equal(
            expectedTypes.Keys.Order(StringComparer.Ordinal),
            root.GetProperty("operations")
                .EnumerateArray()
                .Select(item => RequiredString(item, "operationType"))
                .Order(StringComparer.Ordinal));

        var sourceCodes = Strings(root.GetProperty("sourceErrorCodes"));
        Assert.Equal(ExpectedSourceErrorCodes, sourceCodes);
        var productionSource = ReadProductionReceivingSource();
        var activeSourceCodes = ProductionErrorCodePattern().Matches(productionSource)
            .Select(match => match.Groups["code"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(sourceCodes, activeSourceCodes);

        Assert.DoesNotContain(
            "COMMERCIAL_MOBILE_OPERATION_WAITING_FOR_PRIOR_SEQUENCE",
            productionSource,
            StringComparison.Ordinal);
        var waiting = root.GetProperty("resultExamples")
            .EnumerateArray()
            .Single(item => RequiredString(item, "name") == "prior-sequence-waiting");
        Assert.Equal("NeedsAttention", RequiredString(waiting, "status"));
        Assert.Equal(
            "RECEIVING_OPERATION_WAITING_FOR_PRIOR_SEQUENCE",
            RequiredString(waiting.GetProperty("error"), "code"));
        Assert.True(waiting.GetProperty("error").GetProperty("retryable").GetBoolean());
        Assert.True(waiting.GetProperty("error").GetProperty("requiresAction").GetBoolean());
        Assert.False(waiting.GetProperty("claimCreated").GetBoolean());
        Assert.False(waiting.GetProperty("businessMutationCreated").GetBoolean());
    }

    [Fact]
    public void Http_fixture_freezes_routes_and_uses_no_real_capabilities()
    {
        using var document = LoadFixture(
            "openapi",
            "traderpro-commercial-mobile.v1.json");
        var root = document.RootElement;
        var routes = root.GetProperty("routes")
            .EnumerateArray()
            .ToDictionary(
                item => RequiredString(item, "name"),
                item => $"{RequiredString(item, "method")} {RequiredString(item, "path")}",
                StringComparer.Ordinal);

        Assert.Equal(ExpectedRoutes, routes);
        var registrationRoot = Path.Combine(
            RepositoryRoot(),
            "services", "backend", "src", "TraderPro.Api", "Http");
        var registeredRoutes = ReadRegisteredRoutes(Path.Combine(
                registrationRoot,
                "IdentityEndpointRegistration.cs"))
            .Where(route =>
                route.Contains(" /api/v1/auth/", StringComparison.Ordinal) ||
                route.Contains(" /api/v1/devices/", StringComparison.Ordinal))
            .Concat(ReadRegisteredRoutes(Path.Combine(
                registrationRoot,
                "CommercialReceivingEndpointRegistration.cs")))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var fixtureRoutes = routes.Values
            .Select(NormalizeExpectedRoute)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(fixtureRoutes, registeredRoutes);

        var authentication = root.GetProperty("authenticationExamples");
        AssertMatchesProductionDto(
            authentication.GetProperty("activation").GetProperty("request"),
            typeof(RedeemDeviceActivationRequest));
        AssertMatchesProductionDto(
            authentication.GetProperty("activation").GetProperty("response"),
            typeof(RedeemDeviceActivationResult));
        AssertMatchesProductionDto(
            authentication.GetProperty("login").GetProperty("request"),
            typeof(LoginRequest));
        AssertMatchesProductionDto(
            authentication.GetProperty("login").GetProperty("response"),
            typeof(AuthenticationTokenResult));
        AssertMatchesProductionDto(
            authentication.GetProperty("refresh").GetProperty("request"),
            typeof(RefreshRequest));
        AssertMatchesProductionDto(
            authentication.GetProperty("me").GetProperty("response"),
            typeof(CurrentIdentityResult));
        var reference = root.GetProperty("referencePolicyExample");
        Assert.Equal(
            CommercialReceivingReferencePolicy.DefaultFormatTemplate,
            RequiredString(reference, "formatTemplate"));
        Assert.Equal(
            CommercialReceivingReferenceResetPolicy.Never.ToString(),
            RequiredString(reference, "resetPolicy"));
        Assert.Equal(1, reference.GetProperty("startingNumber").GetInt64());
        var defaultPolicy = CommercialReceivingReferencePolicy.CreateDefault(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal("RCV-000001", defaultPolicy.Render(1, defaultPolicy.CreatedAtUtc));
        var serialized = root.GetRawText();
        Assert.Contains("<runtime-secret-or-capability-not-frozen>", serialized);
        Assert.DoesNotMatch(GuidCapabilityPattern(), serialized);

    }

    [Fact]
    public void Event_vectors_freeze_envelopes_payloads_and_transfer_audiences()
    {
        using var document = LoadFixture(
            "events",
            "commercial-receiving-events.v1.json");
        var events = document.RootElement.GetProperty("events").EnumerateArray().ToArray();
        AssertNoSensitiveProperties(document.RootElement);
        var expectedEnvelope = new[]
        {
            "sequence", "eventId", "eventType", "eventVersion", "aggregateId",
            "aggregateVersion", "audience", "targetDeviceId", "occurredAtUtc",
            "correlationId", "payload",
        };
        var expectedPayloads = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["CommercialReceivingSessionStarted"] =
            [
                "sessionId", "cloudReference", "status", "version", "supplierCode",
                "supplierName", "destinationCode", "destinationName", "vehicleDisplayName",
                "editorDeviceId", "ownershipGeneration", "leaseExpiresAtUtc", "entryCount",
                "processedTotalWeightKg", "updatedAtUtc",
            ],
            ["CommercialReceivingEntryAccepted"] =
            [
                "sessionId", "entryId", "localSequence", "productCode", "productName",
                "bagTypeCode", "bagTypeName", "bagCount", "rawWeightKg",
                "processedWeightKg", "displayWeightKg", "capturedAtDeviceUtc",
                "acceptedAtServerUtc", "entryCount", "processedTotalWeightKg", "version",
            ],
            ["CommercialReceivingSessionSubmitted"] =
            [
                "sessionId", "cloudReference", "status", "submittedAtUtc", "entryCount",
                "processedTotalWeightKg", "version", "ownershipClosed",
            ],
            ["CommercialReceivingOwnershipChanged"] =
            [
                "sessionId", "cloudReference", "previousEditorDeviceId", "editorDeviceId",
                "previousGeneration", "ownershipGeneration", "changedAtUtc",
                "leaseAcquisitionRequired",
            ],
        };

        foreach (var item in events)
        {
            AssertNames(item, expectedEnvelope);
            var eventType = RequiredString(item, "eventType");
            AssertNames(item.GetProperty("payload"), expectedPayloads[eventType]);
            AssertNoSensitiveProperties(item);
        }

        Assert.Equal(
            expectedPayloads.Keys.Order(StringComparer.Ordinal),
            events.Select(item => RequiredString(item, "eventType"))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));
        foreach (var eventType in new[]
                 {
                     "CommercialReceivingSessionStarted",
                     "CommercialReceivingEntryAccepted",
                     "CommercialReceivingSessionSubmitted",
                 })
        {
            var issued = events
                .Where(item => RequiredString(item, "eventType") == eventType)
                .ToArray();
            Assert.Equal(2, issued.Length);
            Assert.Single(
                issued,
                item => RequiredString(item, "audience") == "OwnerBroadcast");
            Assert.Single(
                issued,
                item => RequiredString(item, "audience") == "TargetDevice");
        }
        var transfer = events
            .Where(item => RequiredString(item, "eventType") ==
                           "CommercialReceivingOwnershipChanged")
            .ToArray();
        Assert.Equal(3, transfer.Length);
        Assert.Single(transfer, item => RequiredString(item, "audience") == "OwnerBroadcast");
        Assert.Equal(
            2,
            transfer.Count(item => RequiredString(item, "audience") == "TargetDevice"));
        Assert.Equal(
            2,
            transfer.Where(item => RequiredString(item, "audience") == "TargetDevice")
                .Select(item => RequiredString(item, "targetDeviceId"))
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public void Master_vectors_cover_all_ten_redacted_payload_shapes()
    {
        using var document = LoadFixture(
            "golden-vectors",
            "commercial-master-changes.v1.json");
        var changes = document.RootElement.GetProperty("changes").EnumerateArray().ToArray();
        AssertNoSensitiveProperties(document.RootElement);
        var common = new[] { "contractVersion", "id", "version", "status" };
        var shapes = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["CompanyProcurementSettings"] = [.. common, "defaultBranchId", "defaultDestinationLocationId", "defaultWeightProcessingPolicyId", "vehicleSelectionMode"],
            ["BusinessLocation"] = [.. common, "branchId", "code", "name", "localName", "locationType"],
            ["ReceivingVehicle"] = [.. common, "code", "registrationNumber", "displayName", "vehicleType"],
            ["BagType"] = [.. common, "code", "name", "localName", "constructionClass", "standardTareWeightKg", "isReturnable"],
            ["WeightProcessingPolicy"] = [.. common, "code", "name", "decimalPlaces", "processingMethod"],
            ["Supplier"] = [.. common, "code", "name", "localName", "supplierType", "productScopeMode"],
            ["SupplierProductScope"] = [.. common, "supplierId", "productId"],
            ["ProductGroup"] = [.. common, "code", "name", "localName"],
            ["Product"] = [.. common, "productGroupId", "code", "name", "localName", "productType", "isPurchasable", "processingFamilyCode"],
            ["ProductStandardBagWeight"] = [.. common, "productId", "bagTypeId", "label", "standardContentWeightKg", "isDefault"],
        };

        Assert.Equal(10, changes.Length);
        Assert.Equal(
            shapes.Keys.Order(StringComparer.Ordinal),
            changes.Select(item => RequiredString(item, "masterType"))
                .Order(StringComparer.Ordinal));
        foreach (var change in changes)
        {
            AssertNames(
                change,
                ["sequence", "masterType", "masterId", "masterVersion", "status", "occurredAtUtc", "payload"]);
            var type = RequiredString(change, "masterType");
            var payload = change.GetProperty("payload");
            AssertNames(payload, shapes[type]);
            Assert.Equal(RequiredString(change, "masterId"), RequiredString(payload, "id"));
            Assert.Equal(change.GetProperty("masterVersion").GetInt64(), payload.GetProperty("version").GetInt64());
            Assert.Equal(RequiredString(change, "status"), RequiredString(payload, "status"));
            AssertNoSensitiveProperties(payload);
        }

        Assert.Equal(
            JsonValueKind.String,
            changes.Single(item => RequiredString(item, "masterType") == "BagType")
                .GetProperty("payload").GetProperty("standardTareWeightKg").ValueKind);
        Assert.Equal(
            JsonValueKind.String,
            changes.Single(item => RequiredString(item, "masterType") == "ProductStandardBagWeight")
                .GetProperty("payload").GetProperty("standardContentWeightKg").ValueKind);
    }

    private static readonly string[] ExpectedSourceErrorCodes =
    [
        "AUTHORIZATION_DENIED", "COMMERCIAL_MASTER_CURSOR_INVALID",
        "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID", "COMMERCIAL_MOBILE_OPERATION_CLAIM_MISSING",
        "COMMERCIAL_MOBILE_OPERATION_LEGACY_NON_REPLAYABLE",
        "COMMERCIAL_MOBILE_OPERATION_PAYLOAD_HASH_INVALID", "COMMERCIAL_MOBILE_OPERATION_REJECTED",
        "COMMERCIAL_MOBILE_OPERATION_TYPE_UNSUPPORTED", "COMMERCIAL_SYNC_CURSOR_INVALID",
        "IDEMPOTENCY_IN_PROGRESS", "IDEMPOTENCY_KEY_INVALID", "IDEMPOTENCY_PAYLOAD_CONFLICT",
        "IDEMPOTENCY_PREVIOUS_ATTEMPT_FAILED",
        "OWNER_ROLE_REQUIRED", "RECEIVING_BAG_TYPE_INACTIVE", "RECEIVING_BAG_TYPE_VERSION_STALE",
        "RECEIVING_DEFAULT_DESTINATION_MISMATCH", "RECEIVING_DEFAULT_POLICY_MISMATCH",
        "RECEIVING_DESTINATION_INACTIVE", "RECEIVING_ENTRY_REQUIRED", "RECEIVING_LEASE_INVALID",
        "RECEIVING_LEASE_REACQUISITION_REQUIRED", "RECEIVING_LEASE_REQUIRED",
        "RECEIVING_LEASE_STILL_ACTIVE", "RECEIVING_MASTER_NEEDS_ATTENTION",
        "RECEIVING_OPERATION_WAITING_FOR_PRIOR_SEQUENCE", "RECEIVING_OWNERSHIP_DEVICE_MISMATCH",
        "RECEIVING_OWNERSHIP_GENERATION_STALE", "RECEIVING_OWNERSHIP_TARGET_INVALID",
        "RECEIVING_OWNERSHIP_TRANSFER_CONFLICT", "RECEIVING_PRODUCT_INACTIVE",
        "RECEIVING_PRODUCT_NOT_PURCHASABLE", "RECEIVING_PRODUCT_SCOPE_INVALID",
        "RECEIVING_PRODUCT_SCOPE_REQUIRED", "RECEIVING_PRODUCT_VERSION_STALE",
        "RECEIVING_REFERENCE_CONFLICT", "RECEIVING_REFERENCE_POLICY_INVALID",
        "RECEIVING_REFERENCE_POLICY_NOT_CONFIGURED", "RECEIVING_REFERENCE_SERIES_STARTED",
        "RECEIVING_SEQUENCE_CONFLICT", "RECEIVING_SEQUENCE_GAP", "RECEIVING_SESSION_ID_INVALID",
        "RECEIVING_SESSION_NOT_FOUND", "RECEIVING_SETTINGS_NOT_CONFIGURED",
        "RECEIVING_SETTINGS_VERSION_STALE", "RECEIVING_STANDARD_BAG_WEIGHT_INVALID",
        "RECEIVING_STANDARD_BAG_WEIGHT_VERSION_STALE", "RECEIVING_STATUS_INVALID",
        "RECEIVING_SUPPLIER_INACTIVE", "RECEIVING_SUPPLIER_VERSION_STALE",
        "RECEIVING_TOTAL_WEIGHT_EXCEEDED", "RECEIVING_TOTAL_WEIGHT_REQUIRED",
        "RECEIVING_VEHICLE_INACTIVE", "RECEIVING_WEIGHT_POLICY_INACTIVE",
        "RECEIVING_WEIGHT_PROCESSING_MISMATCH", "REQUEST_BODY_INVALID",
        "TEMPORARY_COMMAND_FAILURE",
    ];

    private static readonly Dictionary<string, string> ExpectedRoutes = new(StringComparer.Ordinal)
    {
        ["redeemActivation"] = "POST /api/v1/auth/device-activations/redeem",
        ["login"] = "POST /api/v1/auth/login",
        ["refresh"] = "POST /api/v1/auth/refresh",
        ["logout"] = "POST /api/v1/auth/logout",
        ["logoutAll"] = "POST /api/v1/auth/logout-all",
        ["me"] = "GET /api/v1/auth/me",
        ["issueActivationCode"] = "POST /api/v1/devices/{deviceId:guid}/activation-codes",
        ["operations"] = "POST /api/v1/mobile/commercial-sync/operations",
        ["events"] = "GET /api/v1/mobile/commercial-sync/events?cursor={opaque}&limit={1-100}",
        ["masters"] = "GET /api/v1/mobile/commercial-sync/masters?cursor={opaque}&limit={1-100}",
        ["listSessions"] = "GET /api/v1/procurement/receiving-sessions?status={optional}&search={optional}&cursor={opaque}&limit={1-100}",
        ["liveView"] = "GET /api/v1/procurement/receiving-sessions/{id:guid}/live-view",
        ["heartbeat"] = "POST /api/v1/procurement/receiving-sessions/{id:guid}/lease/heartbeat",
        ["reacquire"] = "POST /api/v1/procurement/receiving-sessions/{id:guid}/lease/reacquire",
        ["transfer"] = "POST /api/v1/procurement/receiving-sessions/{id:guid}/ownership/transfer",
        ["getReferencePolicy"] = "GET /api/v1/procurement/receiving-reference-policy",
        ["updateReferencePolicy"] = "PUT /api/v1/procurement/receiving-reference-policy",
    };

    private static void AssertMatchesProductionDto(JsonElement fixture, Type dtoType)
    {
        var value = JsonSerializer.Deserialize(fixture.GetRawText(), dtoType, JsonOptions);
        Assert.NotNull(value);
        var serialized = JsonSerializer.SerializeToElement(value, dtoType, JsonOptions);
        AssertSameJsonShape(fixture, serialized);
    }

    private static void AssertSameJsonShape(JsonElement fixture, JsonElement serialized)
    {
        Assert.Equal(serialized.ValueKind, fixture.ValueKind);
        if (fixture.ValueKind is JsonValueKind.Object)
        {
            AssertNames(fixture, serialized.EnumerateObject().Select(property => property.Name));
            foreach (var property in fixture.EnumerateObject())
            {
                AssertSameJsonShape(property.Value, serialized.GetProperty(property.Name));
            }
        }
        else if (fixture.ValueKind is JsonValueKind.Array)
        {
            var fixtureItems = fixture.EnumerateArray().ToArray();
            var serializedItems = serialized.EnumerateArray().ToArray();
            Assert.Equal(serializedItems.Length, fixtureItems.Length);
            for (var index = 0; index < fixtureItems.Length; index++)
            {
                AssertSameJsonShape(fixtureItems[index], serializedItems[index]);
            }
        }
    }

    private static void AssertNames(JsonElement element, IEnumerable<string> expected)
    {
        Assert.Equal(
            expected.Order(StringComparer.Ordinal),
            element.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
    }

    private static void AssertNoSensitiveProperties(JsonElement element)
    {
        var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "token", "refreshToken", "activationCode", "leaseId", "databaseKey",
            "contactName", "contactNumber", "email", "addressLine", "taxRegistrationNumber",
            "normalizedTaxRegistrationNumber", "notes", "reason",
        };
        if (element.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AssertNoSensitiveProperties(item);
            }

            return;
        }

        if (element.ValueKind is not JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            Assert.DoesNotContain(property.Name, forbidden);
            AssertNoSensitiveProperties(property.Value);
        }
    }

    private static string[] ReadRegisteredRoutes(string path)
    {
        var source = File.ReadAllText(path);
        var groups = MapGroupPattern().Matches(source)
            .ToDictionary(
                match => match.Groups["group"].Value,
                match => match.Groups["prefix"].Value,
                StringComparer.Ordinal);
        var routes = DirectRoutePattern().Matches(source)
            .Select(match => NormalizeRoute(
                match.Groups["method"].Value,
                match.Groups["path"].Value))
            .ToList();
        routes.AddRange(GroupRoutePattern().Matches(source)
            .Where(match => !string.Equals(
                match.Groups["group"].Value,
                "endpoints",
                StringComparison.Ordinal))
            .Select(match =>
        {
            var group = match.Groups["group"].Value;
            Assert.True(groups.TryGetValue(group, out var prefix));
            var pathSuffix = match.Groups["path"].Value;
            return NormalizeRoute(
                match.Groups["method"].Value,
                $"{prefix!.TrimEnd('/')}/{pathSuffix.TrimStart('/')}");
        }));
        return routes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    private static string NormalizeExpectedRoute(string route)
    {
        var separator = route.IndexOf(' ', StringComparison.Ordinal);
        Assert.True(separator > 0);
        return NormalizeRoute(route[..separator], route[(separator + 1)..]);
    }

    private static string NormalizeRoute(string method, string path)
    {
        var query = path.IndexOf('?', StringComparison.Ordinal);
        var pathOnly = query < 0 ? path : path[..query];
        if (pathOnly.Length > 1)
        {
            pathOnly = pathOnly.TrimEnd('/');
        }

        return $"{method.ToUpperInvariant()} {pathOnly}";
    }

    private static string ReadProductionReceivingSource()
    {
        var root = RepositoryRoot();
        var files = Directory.EnumerateFiles(
                Path.Combine(root, "services", "backend", "src"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path =>
                path.Contains("CommercialReceiving", StringComparison.Ordinal) ||
                path.EndsWith("CommercialAuthorization.cs", StringComparison.Ordinal) ||
                path.EndsWith("IdempotentCommandExecutor.cs", StringComparison.Ordinal));
        return string.Join('\n', files.Select(File.ReadAllText));
    }

    private static JsonDocument LoadFixture(params string[] parts) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "contracts",
            Path.Combine(parts))));

    private static string RequiredString(JsonElement element, string name) =>
        element.GetProperty(name).GetString() ??
        throw new InvalidDataException($"Fixture property '{name}' must be a string.");

    private static Guid RequiredGuid(JsonElement element, string name) =>
        Guid.Parse(RequiredString(element, name));

    private static string[] Strings(JsonElement array) =>
        array.EnumerateArray()
            .Select(item => item.GetString() ?? throw new InvalidDataException("Expected a string."))
            .ToArray();

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TraderPro.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "contracts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the TraderPro repository root.");
    }

    [GeneratedRegex("\\\"(?:leaseId|refreshToken|activationCode|accessToken)\\\"\\s*:\\s*\\\"[0-9a-f]{8}-[0-9a-f-]{27,}\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex GuidCapabilityPattern();

    [GeneratedRegex("\\\"(?<code>(?:AUTHORIZATION_DENIED|OWNER_ROLE_REQUIRED|REQUEST_BODY_INVALID|TEMPORARY_COMMAND_FAILURE|IDEMPOTENCY_[A-Z0-9_]+|COMMERCIAL_(?:MASTER|MOBILE|SYNC)_[A-Z0-9_]+|RECEIVING_[A-Z0-9_]+))\\\"")]
    private static partial Regex ProductionErrorCodePattern();

    [GeneratedRegex("var\\s+(?<group>\\w+)\\s*=\\s*endpoints\\.MapGroup\\(\\s*\\\"(?<prefix>[^\\\"]+)\\\"")]
    private static partial Regex MapGroupPattern();

    [GeneratedRegex("endpoints\\.Map(?<method>Get|Post|Put|Delete)\\(\\s*\\\"(?<path>[^\\\"]+)\\\"")]
    private static partial Regex DirectRoutePattern();

    [GeneratedRegex("(?<group>\\w+)\\.Map(?<method>Get|Post|Put|Delete)\\(\\s*\\\"(?<path>[^\\\"]+)\\\"")]
    private static partial Regex GroupRoutePattern();
}
