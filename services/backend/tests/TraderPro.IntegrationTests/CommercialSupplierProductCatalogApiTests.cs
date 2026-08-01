using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CommercialSupplierProductCatalogApiTests(
    PostgreSqlFixture fixture)
{
    private const string OwnerPassword =
        "catalog owner development passphrase";
    private const string OperatorPassword =
        "catalog operator development passphrase";
    private const string PreviousMigration =
        "20260731093940_HardenCommercialOperationalMasterDataContracts";
    private const string InitialTask7B2Migration =
        "20260731115429_AddCommercialSupplierAndProductCatalog";
    private const string HardenedTask7B2Migration =
        "20260801030919_HardenCommercialSupplierProductCatalogContracts";

    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 31, 12, 30, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task Authenticated_catalog_supplier_lifecycle_and_in_use_rules_work()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var first = await CreateIdentityAsync(factory, "CATALOG-ONE");
        var second = await CreateIdentityAsync(factory, "CATALOG-TWO");

        using (var anonymous = factory.CreateClient())
        using (var response = await anonymous.GetAsync(
                   "/api/v1/catalog/products",
                   CancellationToken))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using (var denied = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Post,
                   "/api/v1/catalog/product-groups",
                   new { code = "PADDY", name = "Paddy" },
                   "operator-group"))
        using (var json = await ReadJsonAsync(denied))
        {
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal("OWNER_ROLE_REQUIRED", ErrorCode(json));
        }

        var groupId = await CreateGroupAsync(
            factory,
            first.OwnerToken,
            "PADDY",
            "Paddy Products",
            "group-paddy");
        var hAmanId = await CreateProductAsync(
            factory,
            first.OwnerToken,
            groupId,
            "H-AMAN",
            "H Aman",
            "RawMaterial",
            true,
            "Aman",
            "product-h-aman");
        var pAmanId = await CreateProductAsync(
            factory,
            first.OwnerToken,
            groupId,
            "P-AMAN",
            "P Aman",
            "RawMaterial",
            true,
            "AMAN",
            "product-p-aman");
        var byProductId = await CreateProductAsync(
            factory,
            first.OwnerToken,
            groupId,
            "HUSK",
            "Rice Husk",
            "ByProduct",
            false,
            null,
            "product-husk");

        using (var productRead = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Get,
                   $"/api/v1/catalog/products/{hAmanId:D}"))
        using (var json = await ReadJsonAsync(productRead))
        {
            Assert.Equal(HttpStatusCode.OK, productRead.StatusCode);
            var result = Result(json);
            Assert.Equal("Aman", result
                .GetProperty("processingFamilyCode").GetString());
            Assert.Equal("AMAN", result
                .GetProperty("normalizedProcessingFamilyCode").GetString());
            Assert.True(result.GetProperty("isPurchasable").GetBoolean());
        }

        var bagTypeId = await CreateBagTypeAsync(
            factory,
            first.OwnerToken,
            "JUTE-50",
            "Jute 50 kg",
            "bag-jute");
        var standardOne = await CreateBagStandardAsync(
            factory,
            first.OwnerToken,
            hAmanId,
            bagTypeId,
            "50 kg content",
            "50.125",
            true,
            "standard-one");
        var standardTwo = await CreateBagStandardAsync(
            factory,
            first.OwnerToken,
            hAmanId,
            bagTypeId,
            "49 kg content",
            "49.500000",
            false,
            "standard-two");
        Assert.Equal("50.125000", standardOne.Weight);
        Assert.True(standardOne.IsDefault);

        using (var changed = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/catalog/products/{hAmanId:D}/bag-standards/{standardTwo.Id:D}/set-default",
                   idempotencyKey: "standard-two-default",
                   expectedVersion: 1))
        using (var json = await ReadJsonAsync(changed))
        {
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            Assert.True(Result(json).GetProperty("isDefault").GetBoolean());
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
        }

        using (var previous = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Get,
                   $"/api/v1/catalog/products/{hAmanId:D}/bag-standards/{standardOne.Id:D}"))
        using (var json = await ReadJsonAsync(previous))
        {
            Assert.Equal(HttpStatusCode.OK, previous.StatusCode);
            Assert.False(Result(json).GetProperty("isDefault").GetBoolean());
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
        }

        var unrestrictedId = await CreateSupplierAsync(
            factory,
            first.OwnerToken,
            "SUP-OPEN",
            "Open Supplier",
            "Business",
            null,
            [pAmanId],
            "GST-01 AB",
            "supplier-open");
        var restrictedId = await CreateSupplierAsync(
            factory,
            first.OwnerToken,
            "SUP-LIMIT",
            "Restricted Supplier",
            "Individual",
            "Restricted",
            [pAmanId, hAmanId],
            "GST-02 AB",
            "supplier-restricted",
            temporaryWorkspaceId: second.WorkspaceId,
            temporaryDeviceId: second.OwnerDeviceId);

        using (var missingScope = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/suppliers",
                   new
                   {
                       code = "SUP-BAD",
                       name = "Invalid Restricted",
                       supplierType = "Business",
                       productScopeMode = "Restricted",
                   },
                   "supplier-bad"))
        using (var json = await ReadJsonAsync(missingScope))
        {
            Assert.Equal(HttpStatusCode.Conflict, missingScope.StatusCode);
            Assert.Equal("SUPPLIER_PRODUCT_SCOPE_REQUIRED", ErrorCode(json));
        }

        using (var duplicateTax = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/suppliers",
                   new
                   {
                       code = "SUP-TAX",
                       name = "Duplicate tax",
                       supplierType = "Business",
                       taxRegistrationNumber = "gst 01-ab",
                   },
                   "supplier-duplicate-tax"))
        using (var json = await ReadJsonAsync(duplicateTax))
        {
            Assert.Equal(HttpStatusCode.Conflict, duplicateTax.StatusCode);
            Assert.Equal(
                "SUPPLIER_TAX_REGISTRATION_EXISTS",
                ErrorCode(json));
        }

        using (var updated = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/procurement/suppliers/{unrestrictedId:D}",
                   new
                   {
                       name = "Open Supplier Updated",
                       supplierType = "Business",
                       productScopeMode = "Unrestricted",
                       email = " SALES@EXAMPLE.COM ",
                       notes = "   ",
                   },
                   "supplier-update",
                   1))
        using (var json = await ReadJsonAsync(updated))
        {
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal(2, Result(json).GetProperty("version").GetInt64());
            Assert.Equal(
                "sales@example.com",
                Result(json).GetProperty("email").GetString());
            Assert.Equal(JsonValueKind.Null, Result(json)
                .GetProperty("notes").ValueKind);
        }

        using (var stale = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/procurement/suppliers/{unrestrictedId:D}",
                   new
                   {
                       name = "Stale",
                       supplierType = "Business",
                       productScopeMode = "Unrestricted",
                   },
                   "supplier-stale",
                   1))
        using (var json = await ReadJsonAsync(stale))
        {
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("MASTER_VERSION_CONFLICT", ErrorCode(json));
        }

        using (var isolated = await SendAsync(
                   factory,
                   second.OwnerToken,
                   HttpMethod.Get,
                   $"/api/v1/procurement/suppliers/{restrictedId:D}"))
        using (var json = await ReadJsonAsync(isolated))
        {
            Assert.Equal(HttpStatusCode.NotFound, isolated.StatusCode);
            Assert.Equal("SUPPLIER_NOT_FOUND", ErrorCode(json));
        }

        using (var scopes = await SendAsync(
                   factory,
                   first.OperatorToken,
                   HttpMethod.Get,
                   $"/api/v1/procurement/suppliers/{restrictedId:D}/product-scopes?status=Active"))
        using (var json = await ReadJsonAsync(scopes))
        {
            Assert.Equal(HttpStatusCode.OK, scopes.StatusCode);
            Assert.Equal(2, Result(json).GetProperty("items").GetArrayLength());
        }

        var restrictedScopes = await ReadScopeIdsAsync(
            factory,
            first.OwnerToken,
            restrictedId);
        await AssertOkAsync(await SendAsync(
            factory,
            first.OwnerToken,
            HttpMethod.Post,
            $"/api/v1/procurement/suppliers/{restrictedId:D}/product-scopes/{restrictedScopes[0]:D}/deactivate",
            idempotencyKey: "scope-first-off",
            expectedVersion: 1));
        using (var finalScope = await SendAsync(
                   factory,
                   first.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/procurement/suppliers/{restrictedId:D}/product-scopes/{restrictedScopes[1]:D}/deactivate",
                   idempotencyKey: "scope-final-off",
                   expectedVersion: 1))
        using (var json = await ReadJsonAsync(finalScope))
        {
            Assert.Equal(HttpStatusCode.Conflict, finalScope.StatusCode);
            Assert.Equal("SUPPLIER_PRODUCT_SCOPE_REQUIRED", ErrorCode(json));
        }

        await AssertConflictAsync(
            await SendAsync(
                factory,
                first.OwnerToken,
                HttpMethod.Post,
                $"/api/v1/catalog/product-groups/{groupId:D}/deactivate",
                idempotencyKey: "group-in-use",
                expectedVersion: 1),
            "PRODUCT_GROUP_IN_USE");
        await AssertConflictAsync(
            await SendAsync(
                factory,
                first.OwnerToken,
                HttpMethod.Post,
                $"/api/v1/catalog/products/{hAmanId:D}/deactivate",
                idempotencyKey: "product-standard-in-use",
                expectedVersion: 1),
            "PRODUCT_STANDARD_BAG_WEIGHT_IN_USE");
        await AssertConflictAsync(
            await SendAsync(
                factory,
                first.OwnerToken,
                HttpMethod.Post,
                $"/api/v1/procurement/bag-types/{bagTypeId:D}/deactivate",
                idempotencyKey: "bag-in-use",
                expectedVersion: 1),
            "BAG_TYPE_PRODUCT_STANDARD_IN_USE");
        await AssertOkAsync(await SendAsync(
            factory,
            first.OwnerToken,
            HttpMethod.Post,
            $"/api/v1/catalog/products/{hAmanId:D}/bag-standards/{standardOne.Id:D}/deactivate",
            idempotencyKey: "standard-one-off",
            expectedVersion: 2));
        await AssertOkAsync(await SendAsync(
            factory,
            first.OwnerToken,
            HttpMethod.Post,
            $"/api/v1/catalog/products/{hAmanId:D}/bag-standards/{standardTwo.Id:D}/deactivate",
            idempotencyKey: "standard-two-off",
            expectedVersion: 2));
        _ = await CreateSupplierAsync(
            factory,
            first.OwnerToken,
            "SUP-PRODUCT",
            "Product Dependency Supplier",
            "Business",
            "Restricted",
            [hAmanId],
            null,
            "supplier-product-dependency");
        await AssertConflictAsync(
            await SendAsync(
                factory,
                first.OwnerToken,
                HttpMethod.Post,
                $"/api/v1/catalog/products/{hAmanId:D}/deactivate",
                idempotencyKey: "product-supplier-in-use",
                expectedVersion: 1),
            "PRODUCT_SUPPLIER_SCOPE_IN_USE");

        await AssertInternalFactsSafeAsync(
            database,
            first.WorkspaceId,
            restrictedId);
        using (var mobile = await SendPocCursorAsync(
                   factory,
                   first.WorkspaceId))
        using (var json = await ReadJsonAsync(mobile))
        {
            Assert.Equal(HttpStatusCode.OK, mobile.StatusCode);
            Assert.DoesNotContain(
                json.RootElement.GetProperty("events").EnumerateArray(),
                item => item.GetProperty("eventType").GetString()!
                    .Contains("Supplier", StringComparison.Ordinal));
        }

        Assert.NotEqual(Guid.Empty, byProductId);
    }

    [Fact]
    public async Task Immutable_update_fields_are_rejected_without_side_effects()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "CATALOG-IMMUTABLE");
        var groupId = await CreateGroupAsync(
            factory,
            identity.OwnerToken,
            "IMM-GROUP",
            "Immutable Group",
            "immutable-group");
        var productId = await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "IMM-PRODUCT",
            "Immutable Product",
            "RawMaterial",
            true,
            "AMAN",
            "immutable-product");
        var bagTypeId = await CreateBagTypeAsync(
            factory,
            identity.OwnerToken,
            "IMM-BAG",
            "Immutable Bag",
            "immutable-bag");
        var standard = await CreateBagStandardAsync(
            factory,
            identity.OwnerToken,
            productId,
            bagTypeId,
            "Immutable Standard",
            "50",
            false,
            "immutable-standard");
        var supplierId = await CreateSupplierAsync(
            factory,
            identity.OwnerToken,
            "IMM-SUPPLIER",
            "Immutable Supplier",
            "Business",
            "Unrestricted",
            [],
            null,
            "immutable-supplier");
        var before = await ReadCommercialMutationStateAsync(
            database,
            identity.WorkspaceId,
            supplierId,
            groupId,
            productId,
            standard.Id);

        await AssertImmutableFieldRejectedAsync(
            await SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/procurement/suppliers/{supplierId:D}",
                new
                {
                    code = "CHANGED",
                    name = "Would Change",
                    supplierType = "Business",
                    productScopeMode = "Unrestricted",
                },
                "immutable-supplier-code",
                1),
            "code");
        await AssertImmutableFieldRejectedAsync(
            await SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/catalog/product-groups/{groupId:D}",
                new { code = "CHANGED", name = "Would Change" },
                "immutable-group-code",
                1),
            "code");
        await AssertImmutableFieldRejectedAsync(
            await SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/catalog/products/{productId:D}",
                new
                {
                    code = "CHANGED",
                    productGroupId = groupId,
                    name = "Would Change",
                    productType = "RawMaterial",
                    isPurchasable = true,
                    processingFamilyCode = "BORO",
                },
                "immutable-product-code",
                1),
            "code");
        await AssertImmutableFieldRejectedAsync(
            await SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/catalog/products/{productId:D}/bag-standards/{standard.Id:D}",
                new
                {
                    productId = Guid.CreateVersion7(),
                    label = "Would Change",
                    standardContentWeightKg = "49",
                },
                "immutable-standard-product",
                1),
            "productId");
        await AssertImmutableFieldRejectedAsync(
            await SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/catalog/products/{productId:D}/bag-standards/{standard.Id:D}",
                new
                {
                    bagTypeId = Guid.CreateVersion7(),
                    label = "Would Change",
                    standardContentWeightKg = "49",
                },
                "immutable-standard-bag",
                1),
            "bagTypeId");
        await AssertImmutableFieldRejectedAsync(
            await SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/catalog/products/{productId:D}/bag-standards/{standard.Id:D}",
                new
                {
                    isDefault = true,
                    label = "Would Change",
                    standardContentWeightKg = "49",
                },
                "immutable-standard-default",
                1),
            "isDefault");

        Assert.Equal(
            before,
            await ReadCommercialMutationStateAsync(
                database,
                identity.WorkspaceId,
                supplierId,
                groupId,
                productId,
                standard.Id));
    }

    [Fact]
    public async Task Audit_and_event_projections_are_safe_and_initial_scopes_are_complete()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "CATALOG-FACTS");
        var groupId = await CreateGroupAsync(
            factory,
            identity.OwnerToken,
            "FACT-GROUP",
            "Fact Group",
            "facts-group");
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/catalog/product-groups/{groupId:D}",
                   new
                   {
                       name = "Fact Group Updated",
                       localName = "Local Fact Group",
                       description = "Safe group description",
                   },
                   "facts-group-update",
                   1))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var firstProductId = await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "FACT-AMAN",
            "Fact Aman",
            "RawMaterial",
            true,
            "AMAN",
            "facts-product-one");
        var secondProductId = await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "FACT-BORO",
            "Fact Boro",
            "RawMaterial",
            true,
            "BORO",
            "facts-product-two");
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/catalog/products/{firstProductId:D}",
                   new
                   {
                       productGroupId = groupId,
                       name = "Fact Aman",
                       productType = "RawMaterial",
                       isPurchasable = true,
                       processingFamilyCode = "AUS",
                   },
                   "facts-product-update",
                   1))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var bagTypeId = await CreateBagTypeAsync(
            factory,
            identity.OwnerToken,
            "FACT-BAG",
            "Fact Bag",
            "facts-bag");
        var standard = await CreateBagStandardAsync(
            factory,
            identity.OwnerToken,
            firstProductId,
            bagTypeId,
            "Fact Standard",
            "50.100000",
            false,
            "facts-standard");
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/catalog/products/{firstProductId:D}/bag-standards/{standard.Id:D}",
                   new
                   {
                       label = "Fact Standard Updated",
                       standardContentWeightKg = "50.125000",
                   },
                   "facts-standard-update",
                   1))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var initialProductIds = new[] { firstProductId, secondProductId }
            .OrderByDescending(id => id)
            .ToArray();
        var supplierBody = new
        {
            code = "FACT-SUPPLIER",
            name = "Fact Supplier",
            supplierType = "Business",
            productScopeMode = "Restricted",
            initialProductIds,
            contactName = "Original Protected Contact",
            contactNumber = "+91 81111 11111",
            email = "original.protected@example.com",
            addressLine = "Original Protected Address",
            taxRegistrationNumber = "GST-PROTECTED-01",
            notes = "Original Protected Notes",
        };
        Guid supplierId;
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/suppliers",
                   supplierBody,
                   "facts-supplier"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            supplierId = Result(json).GetProperty("id").GetGuid();
        }

        using (var replay = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/suppliers",
                   supplierBody,
                   "facts-supplier"))
        using (var json = await ReadJsonAsync(replay))
        {
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            Assert.Equal(
                "PreviouslyProcessed",
                json.RootElement.GetProperty("meta")
                    .GetProperty("idempotencyStatus").GetString());
        }

        var protectedUpdate = new
        {
            name = "Fact Supplier",
            supplierType = "Business",
            productScopeMode = "Restricted",
            contactName = "Updated Protected Contact",
            contactNumber = "+91 92222 22222",
            email = "updated.protected@example.com",
            addressLine = "Updated Protected Address",
            taxRegistrationNumber = "GST-PROTECTED-02",
            notes = "Updated Protected Notes",
        };
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/procurement/suppliers/{supplierId:D}",
                   protectedUpdate,
                   "facts-supplier-update",
                   1))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using (var replay = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/procurement/suppliers/{supplierId:D}",
                   protectedUpdate,
                   "facts-supplier-update",
                   1))
        {
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        }

        await AssertCatalogAuditProjectionsAsync(
            database,
            groupId,
            firstProductId,
            standard.Id);
        await AssertSupplierFactsAsync(
            database,
            supplierId,
            new[] { firstProductId, secondProductId }.OrderBy(id => id)
                .ToArray());
    }

    [Fact]
    public async Task Product_description_and_notes_audit_is_safe_meaningful_and_replayable()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "PRODUCT-AUDIT");
        var groupId = await CreateGroupAsync(
            factory,
            identity.OwnerToken,
            "AUDIT-GROUP",
            "Audit Group",
            "product-audit-group");
        const string originalNotes = "Original private product notes";
        const string updatedNotes = "Updated private product notes";
        Guid productId;
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/catalog/products",
                   new
                   {
                       productGroupId = groupId,
                       code = "AUDIT-PRODUCT",
                       name = "Audit Product",
                       productType = "RawMaterial",
                       isPurchasable = true,
                       processingFamilyCode = "AMAN",
                       description = "Original safe description",
                       notes = originalNotes,
                   },
                   "product-audit-create"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            productId = Result(json).GetProperty("id").GetGuid();
        }

        var update = new
        {
            productGroupId = groupId,
            name = "Audit Product",
            productType = "RawMaterial",
            isPurchasable = true,
            processingFamilyCode = "AMAN",
            description = "Updated safe description",
            notes = updatedNotes,
        };
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/catalog/products/{productId:D}",
                   update,
                   "product-audit-update",
                   1))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using (var replay = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Put,
                   $"/api/v1/catalog/products/{productId:D}",
                   update,
                   "product-audit-update",
                   1))
        using (var json = await ReadJsonAsync(replay))
        {
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            Assert.Equal(
                "PreviouslyProcessed",
                json.RootElement.GetProperty("meta")
                    .GetProperty("idempotencyStatus").GetString());
        }

        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.audit_events WHERE aggregate_id = @id AND action = 'Catalog.Product.Updated'",
            1,
            ("id", productId));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.outbox_messages WHERE aggregate_id = @id AND event_type = 'Catalog.ProductUpdated'",
            1,
            ("id", productId));

        var audit = await ReadAuditFactAsync(
            database,
            productId,
            "Catalog.Product.Updated");
        using (var before = JsonDocument.Parse(audit.BeforeJson!))
        using (var after = JsonDocument.Parse(audit.AfterJson))
        {
            Assert.Equal(
                "Original safe description",
                before.RootElement.GetProperty("description").GetString());
            Assert.Equal(
                "Updated safe description",
                after.RootElement.GetProperty("description").GetString());
            Assert.Empty(
                before.RootElement.GetProperty("changedFields")
                    .EnumerateArray());
            Assert.Equal(
                new[] { "notes" },
                after.RootElement.GetProperty("changedFields")
                    .EnumerateArray()
                    .Select(item => item.GetString())
                    .ToArray());
        }

        var eventPayload = await ReadOutboxPayloadAsync(
            database,
            productId,
            "Catalog.ProductUpdated");
        using (var payload = JsonDocument.Parse(eventPayload))
        {
            AssertJsonProperties(
                payload.RootElement,
                "productId",
                "productGroupId",
                "code",
                "productType",
                "isPurchasable",
                "status",
                "version");
        }

        var allPayloads = await ReadAggregatePayloadsAsync(database, productId);
        Assert.DoesNotContain(originalNotes, allPayloads, StringComparison.Ordinal);
        Assert.DoesNotContain(updatedNotes, allPayloads, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unrestricted_supplier_initial_scopes_are_ordered_and_replay_once()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "UNRESTRICTED-SCOPES");
        var groupId = await CreateGroupAsync(
            factory,
            identity.OwnerToken,
            "SCOPE-GROUP",
            "Scope Group",
            "unrestricted-scope-group");
        var firstProductId = await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "SCOPE-A",
            "Scope A",
            "RawMaterial",
            true,
            "AMAN",
            "unrestricted-scope-product-a");
        var secondProductId = await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "SCOPE-B",
            "Scope B",
            "RawMaterial",
            true,
            "BORO",
            "unrestricted-scope-product-b");
        var supplierBody = new
        {
            code = "UNRESTRICTED-SUPPLIER",
            name = "Unrestricted Supplier",
            supplierType = "Business",
            productScopeMode = "Unrestricted",
            initialProductIds = new[] { secondProductId, firstProductId },
        };
        Guid supplierId;
        using (var response = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/suppliers",
                   supplierBody,
                   "unrestricted-initial-scopes"))
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            supplierId = Result(json).GetProperty("id").GetGuid();
        }

        using (var replay = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/suppliers",
                   supplierBody,
                   "unrestricted-initial-scopes"))
        using (var json = await ReadJsonAsync(replay))
        {
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            Assert.Equal(
                "PreviouslyProcessed",
                json.RootElement.GetProperty("meta")
                    .GetProperty("idempotencyStatus").GetString());
        }

        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM procurement.supplier_product_scopes WHERE supplier_id = @id",
            2,
            ("id", supplierId));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.audit_events WHERE aggregate_id = @id AND action = 'Procurement.Supplier.Created'",
            1,
            ("id", supplierId));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.outbox_messages WHERE aggregate_id = @id AND event_type = 'Procurement.SupplierCreated'",
            1,
            ("id", supplierId));
        await AssertInitialScopeFactsAsync(
            database,
            supplierId,
            new[] { firstProductId, secondProductId }.OrderBy(id => id)
                .ToArray());
    }

    [Fact]
    public async Task Idempotency_concurrency_filters_and_parent_cursors_are_stable()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "CATALOG-CONCURRENT");

        var createTasks = Enumerable.Range(0, 2)
            .Select(_ => SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Post,
                "/api/v1/catalog/product-groups",
                new { code = "RICE", name = "Rice" },
                "same-group"))
            .ToArray();
        var createResponses = await Task.WhenAll(createTasks);
        var createdIds = new List<Guid>();
        foreach (var response in createResponses)
        {
            using (response)
            using (var json = await ReadJsonAsync(response))
            {
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                createdIds.Add(Result(json).GetProperty("id").GetGuid());
            }
        }

        Assert.Single(createdIds.Distinct());
        var groupId = createdIds[0];
        var productId = await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "RICE-RAW",
            "Raw Rice",
            "RawMaterial",
            true,
            "Rice",
            "product-rice");

        var updates = new[]
        {
            SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/catalog/products/{productId:D}",
                new
                {
                    productGroupId = groupId,
                    name = "Raw Rice A",
                    productType = "RawMaterial",
                    isPurchasable = true,
                    processingFamilyCode = "RICE",
                },
                "product-update-a",
                1),
            SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Put,
                $"/api/v1/catalog/products/{productId:D}",
                new
                {
                    productGroupId = groupId,
                    name = "Raw Rice B",
                    productType = "RawMaterial",
                    isPurchasable = true,
                    processingFamilyCode = "RICE",
                },
                "product-update-b",
                1),
        };
        var updateResponses = await Task.WhenAll(updates);
        Assert.Single(updateResponses, item => item.StatusCode == HttpStatusCode.OK);
        Assert.Single(updateResponses, item => item.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in updateResponses)
        {
            response.Dispose();
        }

        await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "RICE-FIN",
            "Finished Rice",
            "FinishedGood",
            false,
            "RICE",
            "product-finished");
        await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "RICE-RAW-2",
            "Raw Rice Second",
            "RawMaterial",
            true,
            "RICE",
            "product-raw-second");
        string cursor;
        using (var firstPage = await SendAsync(
                   factory,
                   identity.OperatorToken,
                   HttpMethod.Get,
                   $"/api/v1/catalog/products?productGroupId={groupId:D}&productType=RawMaterial&limit=1"))
        using (var json = await ReadJsonAsync(firstPage))
        {
            Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
            cursor = Result(json).GetProperty("nextCursor").GetString()!;
        }

        await AssertCursorInvalidAsync(
            await SendAsync(
                factory,
                identity.OperatorToken,
                HttpMethod.Get,
                $"/api/v1/catalog/products?productGroupId={groupId:D}&productType=FinishedGood&limit=1&cursor={Uri.EscapeDataString(cursor)}"));
        await AssertCursorInvalidAsync(
            await SendAsync(
                factory,
                identity.OperatorToken,
                HttpMethod.Get,
                $"/api/v1/procurement/suppliers?limit=1&cursor={Uri.EscapeDataString(cursor)}"));

        var bagTypeId = await CreateBagTypeAsync(
            factory,
            identity.OwnerToken,
            "BAG-A",
            "Bag A",
            "bag-a");
        var bagTypeB = await CreateBagTypeAsync(
            factory,
            identity.OwnerToken,
            "BAG-B",
            "Bag B",
            "bag-b");
        var firstStandard = await CreateBagStandardAsync(
            factory,
            identity.OwnerToken,
            productId,
            bagTypeId,
            null,
            "50",
            false,
            "cursor-standard-a");
        var secondStandard = await CreateBagStandardAsync(
            factory,
            identity.OwnerToken,
            productId,
            bagTypeB,
            null,
            "40",
            false,
            "cursor-standard-b");
        using (var page = await SendAsync(
                   factory,
                   identity.OperatorToken,
                   HttpMethod.Get,
                   $"/api/v1/catalog/products/{productId:D}/bag-standards?limit=1"))
        using (var json = await ReadJsonAsync(page))
        {
            cursor = Result(json).GetProperty("nextCursor").GetString()!;
        }

        await AssertCursorInvalidAsync(
            await SendAsync(
                factory,
                identity.OperatorToken,
                HttpMethod.Get,
                $"/api/v1/catalog/products/{Guid.CreateVersion7():D}/bag-standards?limit=1&cursor={Uri.EscapeDataString(cursor)}"));

        var defaultResponses = await Task.WhenAll(
            SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Post,
                $"/api/v1/catalog/products/{productId:D}/bag-standards/{firstStandard.Id:D}/set-default",
                idempotencyKey: "concurrent-default-a",
                expectedVersion: 1),
            SendAsync(
                factory,
                identity.OwnerToken,
                HttpMethod.Post,
                $"/api/v1/catalog/products/{productId:D}/bag-standards/{secondStandard.Id:D}/set-default",
                idempotencyKey: "concurrent-default-b",
                expectedVersion: 1));
        foreach (var response in defaultResponses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response.Dispose();
        }

        await AssertCountAsync(
            database,
            $"SELECT count(*) FROM catalog.product_standard_bag_weights WHERE product_id = '{productId:D}' AND status = 1 AND is_default",
            1);
        Assert.NotEqual(Guid.Empty, firstStandard.Id);
    }

    [Fact]
    public async Task Rollback_direct_sql_and_migration_upgrade_controls_hold()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = Factory(database);
        var identity = await CreateIdentityAsync(factory, "CATALOG-INTEGRITY");
        var groupId = await CreateGroupAsync(
            factory,
            identity.OwnerToken,
            "GRAIN",
            "Grain",
            "integrity-group");
        var productId = await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "AMAN",
            "Aman",
            "RawMaterial",
            true,
            "Aman",
            "integrity-product");
        var secondProductId = await CreateProductAsync(
            factory,
            identity.OwnerToken,
            groupId,
            "BORO",
            "Boro",
            "RawMaterial",
            true,
            "Boro",
            "integrity-product-two");
        var bagTypeId = await CreateBagTypeAsync(
            factory,
            identity.OwnerToken,
            "BAG-INT",
            "Integrity Bag",
            "integrity-bag");

        await CreateRejectingOutboxTriggerAsync(
            database,
            "Procurement.SupplierProductScopeAdded");
        using (var failed = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   "/api/v1/procurement/suppliers",
                   new
                   {
                       code = "ROLLBACK-SUP",
                       name = "Rollback Supplier",
                       supplierType = "Business",
                       productScopeMode = "Restricted",
                       initialProductIds = new[]
                       {
                           secondProductId,
                           productId,
                       },
                   },
                   "rollback-supplier"))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        }
        await DropRejectingOutboxTriggerAsync(database);
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM procurement.suppliers WHERE code = 'ROLLBACK-SUP'",
            0);
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM procurement.supplier_product_scopes",
            0);
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.audit_events WHERE action IN ('Procurement.Supplier.Created', 'Procurement.SupplierProductScope.Created')",
            0);
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.outbox_messages WHERE event_type IN ('Procurement.SupplierCreated', 'Procurement.SupplierProductScopeAdded')",
            0);
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.idempotency_records WHERE idempotency_key = 'rollback-supplier'",
            0);

        var first = await CreateBagStandardAsync(
            factory,
            identity.OwnerToken,
            productId,
            bagTypeId,
            "First",
            "50",
            true,
            "rollback-standard-a");
        var second = await CreateBagStandardAsync(
            factory,
            identity.OwnerToken,
            productId,
            bagTypeId,
            "Second",
            "49",
            false,
            "rollback-standard-b");
        await CreateRejectingOutboxTriggerAsync(
            database,
            "Catalog.ProductStandardBagWeightDefaultChanged");
        using (var failed = await SendAsync(
                   factory,
                   identity.OwnerToken,
                   HttpMethod.Post,
                   $"/api/v1/catalog/products/{productId:D}/bag-standards/{second.Id:D}/set-default",
                   idempotencyKey: "rollback-default",
                   expectedVersion: 1))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        }
        await DropRejectingOutboxTriggerAsync(database);
        await AssertStandardStateAsync(database, first.Id, true, 1);
        await AssertStandardStateAsync(database, second.Id, false, 1);
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.idempotency_records WHERE idempotency_key = 'rollback-default'",
            0);

        var supplierId = await CreateSupplierAsync(
            factory,
            identity.OwnerToken,
            "SQL-SUP",
            "SQL Supplier",
            "Business",
            "Restricted",
            [productId],
            "SQL-01",
            "sql-supplier");
        var scopeIds = await ReadScopeIdsAsync(
            factory,
            identity.OwnerToken,
            supplierId);
        foreach (var invalidEmail in new[]
                 {
                     "multiple@@example.com",
                     "@example.com",
                     "trailing@",
                     "embedded space@example.com",
                     "embedded\t@example.com",
                     "embedded\n@example.com",
                     "embedded\u0001@example.com",
                     "UPPER@example.com",
                     " surrounding@example.com ",
                 })
        {
            await AssertSupplierEmailSqlRejectedAsync(
                database,
                supplierId,
                invalidEmail);
        }

        await AssertSupplierEmailSqlAcceptedAsync(
            database,
            supplierId,
            "valid.lowercase@example.com");
        await AssertSqlRejectedAsync(
            database,
            "DELETE FROM catalog.products WHERE id = @id",
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.products SET code = 'CHANGED', normalized_code = 'CHANGED', version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.products SET workspace_id = @workspace_id, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("workspace_id", Guid.CreateVersion7()),
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE procurement.suppliers SET supplier_type = 9, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", supplierId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE procurement.suppliers SET product_scope_mode = 9, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", supplierId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.products SET product_type = 9, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.products SET status = 9, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.products SET name = 'Revision jump', version = version + 2, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.products SET name = 'Timestamp regression', version = version + 1, updated_at_utc = updated_at_utc - interval '1 second' WHERE id = @id",
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE procurement.suppliers SET normalized_tax_registration_number = 'MISMATCH', version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", supplierId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.products SET normalized_processing_family_code = 'MISMATCH', version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.product_standard_bag_weights SET standard_content_weight_kg = 0, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", first.Id));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.product_standard_bag_weights SET is_default = true, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", second.Id));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.product_standard_bag_weights SET bag_type_id = @bag_type_id, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("bag_type_id", Guid.CreateVersion7()),
            ("id", first.Id));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE procurement.supplier_product_scopes SET product_id = @product_id, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("product_id", Guid.CreateVersion7()),
            ("id", scopeIds.Single()));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE procurement.supplier_product_scopes SET status = 2, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", scopeIds.Single()));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.product_groups SET status = 2, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", groupId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE catalog.products SET status = 2, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", productId));
        await AssertSqlRejectedAsync(
            database,
            "UPDATE procurement.bag_types SET status = 2, version = version + 1, updated_at_utc = updated_at_utc WHERE id = @id",
            ("id", bagTypeId));
        await AssertSchemaControlsAsync(database);
    }

    [Fact]
    public async Task Task_7b2_existing_data_survives_contract_hardening()
    {
        await using var database = await fixture.CreateDatabaseAsync(
            InitialTask7B2Migration);
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.__ef_migrations_history WHERE \"MigrationId\" = @migration_id",
            1,
            ("migration_id", InitialTask7B2Migration));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.__ef_migrations_history WHERE \"MigrationId\" = @migration_id",
            0,
            ("migration_id", HardenedTask7B2Migration));
        Guid supplierId;
        Guid groupId;
        Guid productId;
        Guid bagTypeId;
        Guid standardId;
        Guid scopeId;
        await using (var factory = Factory(database))
        {
            var identity = await CreateIdentityAsync(
                factory,
                "TASK-7B2-UPGRADE");
            groupId = await CreateGroupAsync(
                factory,
                identity.OwnerToken,
                "UPGRADE-GROUP",
                "Upgrade Group",
                "task-7b2-upgrade-group");
            productId = await CreateProductAsync(
                factory,
                identity.OwnerToken,
                groupId,
                "UPGRADE-PRODUCT",
                "Upgrade Product",
                "RawMaterial",
                true,
                "AMAN",
                "task-7b2-upgrade-product");
            bagTypeId = await CreateBagTypeAsync(
                factory,
                identity.OwnerToken,
                "UPGRADE-BAG-7B2",
                "Upgrade Bag 7B2",
                "task-7b2-upgrade-bag");
            var standard = await CreateBagStandardAsync(
                factory,
                identity.OwnerToken,
                productId,
                bagTypeId,
                "Upgrade Standard",
                "50.000000",
                true,
                "task-7b2-upgrade-standard");
            standardId = standard.Id;
            using (var response = await SendAsync(
                       factory,
                       identity.OwnerToken,
                       HttpMethod.Post,
                       "/api/v1/procurement/suppliers",
                       new
                       {
                           code = "UPGRADE-SUPPLIER",
                           name = "Upgrade Supplier",
                           supplierType = "Business",
                           productScopeMode = "Restricted",
                           initialProductIds = new[] { productId },
                           email = "existing.lowercase@example.com",
                       },
                       "task-7b2-upgrade-supplier"))
            using (var json = await ReadJsonAsync(response))
            {
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                supplierId = Result(json).GetProperty("id").GetGuid();
            }

            scopeId = Assert.Single(await ReadScopeIdsAsync(
                factory,
                identity.OwnerToken,
                supplierId));
        }

        var before = await ReadTask7B2RowsAsync(
            database,
            supplierId,
            groupId,
            productId,
            bagTypeId,
            standardId,
            scopeId);

        await database.ApplyMigrationsAsync(HardenedTask7B2Migration);
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.__ef_migrations_history WHERE \"MigrationId\" = @migration_id",
            1,
            ("migration_id", HardenedTask7B2Migration));

        Assert.Equal(
            before,
            await ReadTask7B2RowsAsync(
                database,
                supplierId,
                groupId,
                productId,
                bagTypeId,
                standardId,
                scopeId));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM procurement.suppliers WHERE id = @id AND email = 'existing.lowercase@example.com'",
            1,
            ("id", supplierId));
        await AssertSupplierEmailSqlAcceptedAsync(
            database,
            supplierId,
            "still.valid.lowercase@example.com");
        foreach (var invalidEmail in new[]
                 {
                     "multiple@@example.com",
                     "@example.com",
                     "trailing@",
                     "embedded space@example.com",
                     "embedded\t@example.com",
                     "embedded\n@example.com",
                     "embedded\u0001@example.com",
                     "UPPER@example.com",
                     " surrounding@example.com ",
                 })
        {
            await AssertSupplierEmailSqlRejectedAsync(
                database,
                supplierId,
                invalidEmail);
        }

        await AssertSchemaControlsAsync(database);
    }

    [Fact]
    public async Task Task_7b1_database_upgrades_without_catalog_data_loss_or_seeding()
    {
        await using var database = await fixture.CreateDatabaseAsync(
            PreviousMigration);
        Guid bagTypeId;
        await using (var factory = Factory(database))
        {
            var identity = await CreateIdentityAsync(factory, "CATALOG-UPGRADE");
            bagTypeId = await CreateBagTypeAsync(
                factory,
                identity.OwnerToken,
                "UPGRADE-BAG",
                "Upgrade Bag",
                "upgrade-bag");
        }

        await database.ApplyMigrationsAsync();
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM procurement.bag_types WHERE id = @id",
            1,
            ("id", bagTypeId));
        await AssertCountAsync(
            database,
            "SELECT (SELECT COUNT(*) FROM procurement.suppliers) + (SELECT COUNT(*) FROM catalog.product_groups) + (SELECT COUNT(*) FROM catalog.products) + (SELECT COUNT(*) FROM catalog.product_standard_bag_weights) + (SELECT COUNT(*) FROM procurement.supplier_product_scopes)",
            0);
        await AssertSchemaControlsAsync(database);
    }

    private static TraderProApiFactory Factory(
        IsolatedPostgreSqlDatabase database) =>
        new(
            database.ConnectionString,
            spikesEnabled: true,
            identityBootstrapEnabled: true,
            utcNow: UtcNow);

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
        var ownerDeviceId = root.GetProperty("ownerDeviceId").GetGuid();
        var operatorDeviceId = root.GetProperty("operatorDeviceId").GetGuid();
        var ownerSecret = await ActivateAsync(
            factory,
            workspaceCode,
            ownerDeviceId,
            root.GetProperty("ownerActivation")
                .GetProperty("activationCode").GetString()!);
        var operatorSecret = await ActivateAsync(
            factory,
            workspaceCode,
            operatorDeviceId,
            root.GetProperty("operatorActivation")
                .GetProperty("activationCode").GetString()!);
        return new CommercialIdentity(
            root.GetProperty("workspaceId").GetGuid(),
            ownerDeviceId,
            await LoginAsync(
                factory,
                workspaceCode,
                "owner",
                OwnerPassword,
                ownerDeviceId,
                ownerSecret),
            await LoginAsync(
                factory,
                workspaceCode,
                "operator",
                OperatorPassword,
                operatorDeviceId,
                operatorSecret));
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
                clientInstallationReference = $"catalog-{deviceId:D}",
                deviceLabel = "Catalog test device",
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
            new { workspaceCode, login, password, deviceId, deviceSecret },
            CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<Guid> CreateGroupAsync(
        TraderProApiFactory factory,
        string token,
        string code,
        string name,
        string key)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            "/api/v1/catalog/product-groups",
            new { code, name },
            key);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return Result(json).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateProductAsync(
        TraderProApiFactory factory,
        string token,
        Guid groupId,
        string code,
        string name,
        string type,
        bool purchasable,
        string? family,
        string key)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            "/api/v1/catalog/products",
            new
            {
                productGroupId = groupId,
                code,
                name,
                productType = type,
                isPurchasable = purchasable,
                processingFamilyCode = family,
            },
            key);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return Result(json).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateBagTypeAsync(
        TraderProApiFactory factory,
        string token,
        string code,
        string name,
        string key)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            "/api/v1/procurement/bag-types",
            new
            {
                code,
                name,
                constructionClass = "Jute",
                standardTareWeightKg = "0.200000",
                isReturnable = true,
            },
            key);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return Result(json).GetProperty("id").GetGuid();
    }

    private static async Task<StandardIdentity> CreateBagStandardAsync(
        TraderProApiFactory factory,
        string token,
        Guid productId,
        Guid bagTypeId,
        string? label,
        string weight,
        bool isDefault,
        string key)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            $"/api/v1/catalog/products/{productId:D}/bag-standards",
            new
            {
                bagTypeId,
                label,
                standardContentWeightKg = weight,
                isDefault,
            },
            key);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        var result = Result(json);
        return new StandardIdentity(
            result.GetProperty("id").GetGuid(),
            result.GetProperty("standardContentWeightKg").GetString()!,
            result.GetProperty("isDefault").GetBoolean());
    }

    private static async Task<Guid> CreateSupplierAsync(
        TraderProApiFactory factory,
        string token,
        string code,
        string name,
        string supplierType,
        string? mode,
        Guid[] initialProducts,
        string? tax,
        string key,
        Guid? temporaryWorkspaceId = null,
        Guid? temporaryDeviceId = null)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Post,
            "/api/v1/procurement/suppliers",
            new
            {
                code,
                name,
                supplierType,
                productScopeMode = mode,
                initialProductIds = initialProducts,
                taxRegistrationNumber = tax,
                contactNumber = "+91 98765 43210",
            },
            key,
            temporaryWorkspaceId: temporaryWorkspaceId,
            temporaryDeviceId: temporaryDeviceId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return Result(json).GetProperty("id").GetGuid();
    }

    private static async Task<Guid[]> ReadScopeIdsAsync(
        TraderProApiFactory factory,
        string token,
        Guid supplierId)
    {
        using var response = await SendAsync(
            factory,
            token,
            HttpMethod.Get,
            $"/api/v1/procurement/suppliers/{supplierId:D}/product-scopes?status=All");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return Result(json).GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();
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

    private static async Task<HttpResponseMessage> SendPocCursorAsync(
        TraderProApiFactory factory,
        Guid workspaceId)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/mobile/sync/events?after=0&limit=100");
        request.Headers.Add(
            "X-TraderPro-Workspace-ID",
            workspaceId.ToString("D"));
        return await client.SendAsync(request, CancellationToken);
    }

    private static async Task<string> ReadCommercialMutationStateAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        Guid supplierId,
        Guid groupId,
        Guid productId,
        Guid standardId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT jsonb_build_object(
                'supplier', (
                    SELECT to_jsonb(supplier)
                    FROM procurement.suppliers AS supplier
                    WHERE supplier.id = @supplier_id),
                'productGroup', (
                    SELECT to_jsonb(product_group)
                    FROM catalog.product_groups AS product_group
                    WHERE product_group.id = @group_id),
                'product', (
                    SELECT to_jsonb(product)
                    FROM catalog.products AS product
                    WHERE product.id = @product_id),
                'standard', (
                    SELECT to_jsonb(standard)
                    FROM catalog.product_standard_bag_weights AS standard
                    WHERE standard.id = @standard_id),
                'auditCount', (
                    SELECT COUNT(*)
                    FROM platform.audit_events
                    WHERE workspace_id = @workspace_id),
                'outboxCount', (
                    SELECT COUNT(*)
                    FROM platform.outbox_messages
                    WHERE workspace_id = @workspace_id),
                'idempotencyCount', (
                    SELECT COUNT(*)
                    FROM platform.idempotency_records
                    WHERE workspace_id = @workspace_id))::text;
            """;
        command.Parameters.AddWithValue("workspace_id", workspaceId);
        command.Parameters.AddWithValue("supplier_id", supplierId);
        command.Parameters.AddWithValue("group_id", groupId);
        command.Parameters.AddWithValue("product_id", productId);
        command.Parameters.AddWithValue("standard_id", standardId);
        return (string)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static async Task<string> ReadTask7B2RowsAsync(
        IsolatedPostgreSqlDatabase database,
        Guid supplierId,
        Guid groupId,
        Guid productId,
        Guid bagTypeId,
        Guid standardId,
        Guid scopeId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT jsonb_build_object(
                'supplier', (
                    SELECT to_jsonb(supplier)
                    FROM procurement.suppliers AS supplier
                    WHERE supplier.id = @supplier_id),
                'productGroup', (
                    SELECT to_jsonb(product_group)
                    FROM catalog.product_groups AS product_group
                    WHERE product_group.id = @group_id),
                'product', (
                    SELECT to_jsonb(product)
                    FROM catalog.products AS product
                    WHERE product.id = @product_id),
                'bagType', (
                    SELECT to_jsonb(bag_type)
                    FROM procurement.bag_types AS bag_type
                    WHERE bag_type.id = @bag_type_id),
                'bagStandard', (
                    SELECT to_jsonb(standard)
                    FROM catalog.product_standard_bag_weights AS standard
                    WHERE standard.id = @standard_id),
                'supplierProductScope', (
                    SELECT to_jsonb(scope)
                    FROM procurement.supplier_product_scopes AS scope
                    WHERE scope.id = @scope_id))::text;
            """;
        command.Parameters.AddWithValue("supplier_id", supplierId);
        command.Parameters.AddWithValue("group_id", groupId);
        command.Parameters.AddWithValue("product_id", productId);
        command.Parameters.AddWithValue("bag_type_id", bagTypeId);
        command.Parameters.AddWithValue("standard_id", standardId);
        command.Parameters.AddWithValue("scope_id", scopeId);
        return (string)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static async Task AssertImmutableFieldRejectedAsync(
        HttpResponseMessage response,
        string field)
    {
        using (response)
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                "MASTER_IMMUTABLE_FIELD_SUPPLIED",
                ErrorCode(json));
            var fieldErrors = json.RootElement.GetProperty("error")
                .GetProperty("fieldErrors");
            Assert.Contains(
                fieldErrors.EnumerateArray(),
                item => item.GetProperty("field").GetString() == field &&
                    item.GetProperty("code").GetString() ==
                    "MASTER_IMMUTABLE_FIELD_SUPPLIED");
        }
    }

    private static async Task AssertCatalogAuditProjectionsAsync(
        IsolatedPostgreSqlDatabase database,
        Guid groupId,
        Guid productId,
        Guid standardId)
    {
        var group = await ReadAuditFactAsync(
            database,
            groupId,
            "Catalog.ProductGroup.Updated");
        using (var before = JsonDocument.Parse(group.BeforeJson!))
        using (var after = JsonDocument.Parse(group.AfterJson))
        {
            AssertJsonProperties(
                before.RootElement,
                "code",
                "name",
                "localName",
                "description",
                "status",
                "version");
            Assert.Equal(
                "Fact Group",
                before.RootElement.GetProperty("name").GetString());
            Assert.Equal(
                "Fact Group Updated",
                after.RootElement.GetProperty("name").GetString());
            Assert.Equal(
                "Safe group description",
                after.RootElement.GetProperty("description").GetString());
        }

        var groupEvent = await ReadOutboxPayloadAsync(
            database,
            groupId,
            "Catalog.ProductGroupUpdated");
        using (var payload = JsonDocument.Parse(groupEvent))
        {
            AssertJsonProperties(
                payload.RootElement,
                "productGroupId",
                "code",
                "status",
                "version");
        }

        var product = await ReadAuditFactAsync(
            database,
            productId,
            "Catalog.Product.Updated");
        using (var before = JsonDocument.Parse(product.BeforeJson!))
        using (var after = JsonDocument.Parse(product.AfterJson))
        {
            AssertJsonProperties(
                after.RootElement,
                "code",
                "productGroupId",
                "name",
                "localName",
                "productType",
                "isPurchasable",
                "processingFamilyCode",
                "description",
                "status",
                "version",
                "changedFields");
            Assert.Empty(
                after.RootElement.GetProperty("changedFields")
                    .EnumerateArray());
            Assert.Equal(
                "AMAN",
                before.RootElement.GetProperty("processingFamilyCode")
                    .GetString());
            Assert.Equal(
                "AUS",
                after.RootElement.GetProperty("processingFamilyCode")
                    .GetString());
        }

        var productEvent = await ReadOutboxPayloadAsync(
            database,
            productId,
            "Catalog.ProductUpdated");
        using (var payload = JsonDocument.Parse(productEvent))
        {
            AssertJsonProperties(
                payload.RootElement,
                "productId",
                "productGroupId",
                "code",
                "productType",
                "isPurchasable",
                "status",
                "version");
        }

        var standard = await ReadAuditFactAsync(
            database,
            standardId,
            "Catalog.ProductStandardBagWeight.Updated");
        using (var before = JsonDocument.Parse(standard.BeforeJson!))
        using (var after = JsonDocument.Parse(standard.AfterJson))
        {
            AssertJsonProperties(
                after.RootElement,
                "productId",
                "bagTypeId",
                "label",
                "standardContentWeightKg",
                "isDefault",
                "status",
                "version");
            Assert.Equal(
                "50.100000",
                before.RootElement.GetProperty("standardContentWeightKg")
                    .GetString());
            Assert.Equal(
                "50.125000",
                after.RootElement.GetProperty("standardContentWeightKg")
                    .GetString());
        }

        var standardEvent = await ReadOutboxPayloadAsync(
            database,
            standardId,
            "Catalog.ProductStandardBagWeightUpdated");
        using (var payload = JsonDocument.Parse(standardEvent))
        {
            AssertJsonProperties(
                payload.RootElement,
                "standardId",
                "productId",
                "bagTypeId",
                "bagTypeCode",
                "isDefault",
                "status",
                "version");
        }
    }

    private static async Task AssertSupplierFactsAsync(
        IsolatedPostgreSqlDatabase database,
        Guid supplierId,
        Guid[] expectedProductIds)
    {
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.audit_events WHERE aggregate_id = @id AND action = 'Procurement.Supplier.Created'",
            1,
            ("id", supplierId));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.outbox_messages WHERE aggregate_id = @id AND event_type = 'Procurement.SupplierCreated'",
            1,
            ("id", supplierId));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.audit_events WHERE aggregate_id = @id AND action = 'Procurement.Supplier.Updated'",
            1,
            ("id", supplierId));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.outbox_messages WHERE aggregate_id = @id AND event_type = 'Procurement.SupplierUpdated'",
            1,
            ("id", supplierId));

        var updated = await ReadAuditFactAsync(
            database,
            supplierId,
            "Procurement.Supplier.Updated");
        using (var after = JsonDocument.Parse(updated.AfterJson))
        {
            AssertJsonProperties(
                after.RootElement,
                "supplierId",
                "code",
                "name",
                "localName",
                "supplierType",
                "productScopeMode",
                "status",
                "version",
                "changedFields");
            Assert.Equal(
                new[]
                {
                    "contactName",
                    "contactNumber",
                    "email",
                    "addressLine",
                    "taxRegistrationNumber",
                    "notes",
                },
                after.RootElement.GetProperty("changedFields")
                    .EnumerateArray()
                    .Select(item => item.GetString())
                    .ToArray());
        }

        var updatedEvent = await ReadOutboxPayloadAsync(
            database,
            supplierId,
            "Procurement.SupplierUpdated");
        using (var payload = JsonDocument.Parse(updatedEvent))
        {
            AssertJsonProperties(
                payload.RootElement,
                "supplierId",
                "code",
                "supplierType",
                "productScopeMode",
                "status",
                "version");
        }

        await AssertInitialScopeFactsAsync(
            database,
            supplierId,
            expectedProductIds);

        var allPayloads = await ReadAggregatePayloadsAsync(database, supplierId);
        foreach (var protectedValue in new[]
                 {
                     "Original Protected Contact",
                     "+91 81111 11111",
                     "original.protected@example.com",
                     "Original Protected Address",
                     "GST-PROTECTED-01",
                     "Original Protected Notes",
                     "Updated Protected Contact",
                     "+91 92222 22222",
                     "updated.protected@example.com",
                     "Updated Protected Address",
                     "GST-PROTECTED-02",
                     "Updated Protected Notes",
                 })
        {
            Assert.DoesNotContain(
                protectedValue,
                allPayloads,
                StringComparison.Ordinal);
        }
    }

    private static async Task AssertInitialScopeFactsAsync(
        IsolatedPostgreSqlDatabase database,
        Guid supplierId,
        Guid[] expectedProductIds)
    {
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.audit_events WHERE action = 'Procurement.SupplierProductScope.Created' AND after_snapshot_json->>'supplierId' = @supplier_id",
            expectedProductIds.Length,
            ("supplier_id", supplierId.ToString("D")));
        await AssertCountAsync(
            database,
            "SELECT COUNT(*) FROM platform.outbox_messages WHERE event_type = 'Procurement.SupplierProductScopeAdded' AND payload_json->>'supplierId' = @supplier_id",
            expectedProductIds.Length,
            ("supplier_id", supplierId.ToString("D")));

        var auditScopes = await ReadJsonPayloadsAsync(
            database,
            "SELECT after_snapshot_json::text FROM platform.audit_events WHERE action = 'Procurement.SupplierProductScope.Created' AND after_snapshot_json->>'supplierId' = @supplier_id ORDER BY after_snapshot_json->>'productId'",
            ("supplier_id", supplierId.ToString("D")));
        var eventScopes = await ReadJsonPayloadsAsync(
            database,
            "SELECT payload_json::text FROM platform.outbox_messages WHERE event_type = 'Procurement.SupplierProductScopeAdded' AND payload_json->>'supplierId' = @supplier_id ORDER BY sequence",
            ("supplier_id", supplierId.ToString("D")));
        Assert.Equal(expectedProductIds.Length, auditScopes.Length);
        Assert.Equal(expectedProductIds.Length, eventScopes.Length);
        var expectedOrdered = expectedProductIds.Select(id => id.ToString("D"))
            .ToArray();
        Assert.Equal(
            expectedOrdered,
            auditScopes.Select(payload =>
                JsonDocument.Parse(payload).RootElement
                    .GetProperty("productId").GetString()).ToArray());
        Assert.Equal(
            expectedOrdered,
            eventScopes.Select(payload =>
                JsonDocument.Parse(payload).RootElement
                    .GetProperty("productId").GetString()).ToArray());
        foreach (var payloadJson in auditScopes.Concat(eventScopes))
        {
            using var payload = JsonDocument.Parse(payloadJson);
            AssertJsonProperties(
                payload.RootElement,
                "scopeId",
                "supplierId",
                "productId",
                "productCode",
                "status",
                "version");
        }
    }

    private static async Task<AuditFact> ReadAuditFactAsync(
        IsolatedPostgreSqlDatabase database,
        Guid aggregateId,
        string action)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT before_snapshot_json::text, after_snapshot_json::text FROM platform.audit_events WHERE aggregate_id = @aggregate_id AND action = @action";
        command.Parameters.AddWithValue("aggregate_id", aggregateId);
        command.Parameters.AddWithValue("action", action);
        await using var reader = await command.ExecuteReaderAsync(
            CancellationToken);
        Assert.True(await reader.ReadAsync(CancellationToken));
        var fact = new AuditFact(
            reader.IsDBNull(0) ? null : reader.GetString(0),
            reader.GetString(1));
        Assert.False(await reader.ReadAsync(CancellationToken));
        return fact;
    }

    private static async Task<string> ReadOutboxPayloadAsync(
        IsolatedPostgreSqlDatabase database,
        Guid aggregateId,
        string eventType)
    {
        var payloads = await ReadJsonPayloadsAsync(
            database,
            "SELECT payload_json::text FROM platform.outbox_messages WHERE aggregate_id = @aggregate_id AND event_type = @event_type",
            ("aggregate_id", aggregateId),
            ("event_type", eventType));
        return Assert.Single(payloads);
    }

    private static async Task<string[]> ReadJsonPayloadsAsync(
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

        var payloads = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(
            CancellationToken);
        while (await reader.ReadAsync(CancellationToken))
        {
            payloads.Add(reader.GetString(0));
        }

        return [.. payloads];
    }

    private static async Task<string> ReadAggregatePayloadsAsync(
        IsolatedPostgreSqlDatabase database,
        Guid aggregateId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT string_agg(payload, E'\n')
            FROM (
                SELECT coalesce(before_snapshot_json::text, '') ||
                       coalesce(after_snapshot_json::text, '') AS payload
                FROM platform.audit_events
                WHERE aggregate_id = @supplier_id
                UNION ALL
                SELECT payload_json::text
                FROM platform.outbox_messages
                WHERE aggregate_id = @supplier_id
            ) AS facts;
            """;
        command.Parameters.AddWithValue("supplier_id", aggregateId);
        return (string)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static void AssertJsonProperties(
        JsonElement value,
        params string[] expected)
    {
        Assert.Equal(
            expected.OrderBy(item => item, StringComparer.Ordinal),
            value.EnumerateObject().Select(item => item.Name)
                .OrderBy(item => item, StringComparer.Ordinal));
    }

    private static async Task AssertInternalFactsSafeAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        Guid supplierId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT source, event_stream, payload
            FROM (
                SELECT 'outbox' AS source,
                       event_stream,
                       payload_json::text AS payload
                FROM platform.outbox_messages
                WHERE workspace_id = @workspace_id
                  AND aggregate_id = @supplier_id
                UNION ALL
                SELECT 'audit' AS source,
                       1::smallint AS event_stream,
                       coalesce(before_snapshot_json::text, '') ||
                           coalesce(after_snapshot_json::text, '') AS payload
                FROM platform.audit_events
                WHERE workspace_id = @workspace_id
                  AND aggregate_id = @supplier_id
            ) AS facts
            """;
        command.Parameters.AddWithValue("workspace_id", workspaceId);
        command.Parameters.AddWithValue("supplier_id", supplierId);
        await using var reader = await command.ExecuteReaderAsync(
            CancellationToken);
        var count = 0;
        while (await reader.ReadAsync(CancellationToken))
        {
            count++;
            Assert.Equal(1, reader.GetInt16(1));
            var payload = reader.GetString(2);
            Assert.DoesNotContain(
                "contact",
                payload,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "email",
                payload,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "tax",
                payload,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "notes",
                payload,
                StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(count >= 2);
    }

    private static async Task AssertConflictAsync(
        HttpResponseMessage response,
        string code)
    {
        using (response)
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(code, ErrorCode(json));
        }
    }

    private static async Task AssertCursorInvalidAsync(
        HttpResponseMessage response)
    {
        using (response)
        using (var json = await ReadJsonAsync(response))
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("MASTER_CURSOR_INVALID", ErrorCode(json));
        }
    }

    private static Task AssertOkAsync(HttpResponseMessage response)
    {
        using (response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        return Task.CompletedTask;
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

    private static Task AssertSupplierEmailSqlRejectedAsync(
        IsolatedPostgreSqlDatabase database,
        Guid supplierId,
        string email) =>
        AssertSqlRejectedAsync(
            database,
            "UPDATE procurement.suppliers SET email = @email, version = version + 1, updated_at_utc = updated_at_utc + interval '1 second' WHERE id = @id",
            ("email", email),
            ("id", supplierId));

    private static async Task AssertSupplierEmailSqlAcceptedAsync(
        IsolatedPostgreSqlDatabase database,
        Guid supplierId,
        string email)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE procurement.suppliers SET email = @email, version = version + 1, updated_at_utc = updated_at_utc + interval '1 second' WHERE id = @id";
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("id", supplierId);
        Assert.Equal(
            1,
            await command.ExecuteNonQueryAsync(CancellationToken));
    }

    private static async Task CreateRejectingOutboxTriggerAsync(
        IsolatedPostgreSqlDatabase database,
        string eventType)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS platform.task_7b2_reject_event (
                event_type text PRIMARY KEY
            );
            TRUNCATE platform.task_7b2_reject_event;

            CREATE OR REPLACE FUNCTION platform.reject_task_7b2_outbox()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                IF NEW.event_stream = 1
                   AND EXISTS (
                       SELECT 1
                       FROM platform.task_7b2_reject_event AS rejected
                       WHERE rejected.event_type = NEW.event_type
                   ) THEN
                    RAISE EXCEPTION 'Injected Task 7B2 outbox failure';
                END IF;
                RETURN NEW;
            END;
            $function$;

            CREATE TRIGGER tr_reject_task_7b2_outbox
            BEFORE INSERT ON platform.outbox_messages
            FOR EACH ROW
            EXECUTE FUNCTION platform.reject_task_7b2_outbox();
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText =
            "INSERT INTO platform.task_7b2_reject_event(event_type) VALUES (@event_type)";
        insertCommand.Parameters.AddWithValue("event_type", eventType);
        await insertCommand.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task DropRejectingOutboxTriggerAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP TRIGGER IF EXISTS tr_reject_task_7b2_outbox
                ON platform.outbox_messages;
            DROP FUNCTION IF EXISTS platform.reject_task_7b2_outbox();
            DROP TABLE IF EXISTS platform.task_7b2_reject_event;
            """;
        await command.ExecuteNonQueryAsync(CancellationToken);
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
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        Assert.Equal(
            expected,
            Convert.ToInt64(
                await command.ExecuteScalarAsync(CancellationToken),
                System.Globalization.CultureInfo.InvariantCulture));
    }

    private static async Task AssertStandardStateAsync(
        IsolatedPostgreSqlDatabase database,
        Guid id,
        bool isDefault,
        long version)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT is_default, version FROM catalog.product_standard_bag_weights WHERE id = @id";
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(
            CancellationToken);
        Assert.True(await reader.ReadAsync(CancellationToken));
        Assert.Equal(isDefault, reader.GetBoolean(0));
        Assert.Equal(version, reader.GetInt64(1));
    }

    private static async Task AssertSchemaControlsAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                (SELECT COUNT(*) FROM information_schema.tables
                 WHERE (table_schema, table_name) IN (
                    ('procurement', 'suppliers'),
                    ('catalog', 'product_groups'),
                    ('catalog', 'products'),
                    ('catalog', 'product_standard_bag_weights'),
                    ('procurement', 'supplier_product_scopes'))) = 5,
                EXISTS (SELECT 1 FROM pg_indexes
                    WHERE indexname =
                        'ux_product_standard_bag_weights_active_default'),
                EXISTS (SELECT 1 FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'procurement'
                      AND p.proname = 'lock_supplier_product_scope'),
                EXISTS (SELECT 1 FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'catalog'
                      AND p.proname = 'lock_product_bag_default'),
                (SELECT COUNT(*) FROM pg_trigger
                 WHERE tgname IN (
                    'tr_suppliers_protect',
                    'tr_product_groups_protect',
                    'tr_products_protect',
                    'tr_product_standard_bag_weights_protect',
                    'tr_supplier_product_scopes_protect',
                    'tr_suppliers_scope_invariant',
                    'tr_supplier_product_scopes_invariant',
                    'tr_product_standard_bag_weights_validate',
                    'tr_bag_types_prevent_product_standard_deactivation')
                   AND NOT tgisinternal) = 9;
            """;
        await using var reader = await command.ExecuteReaderAsync(
            CancellationToken);
        Assert.True(await reader.ReadAsync(CancellationToken));
        for (var index = 0; index < reader.FieldCount; index++)
        {
            Assert.True(reader.GetBoolean(index));
        }
    }

    private static JsonElement Result(JsonDocument document) =>
        document.RootElement.GetProperty("result");

    private static string? ErrorCode(JsonDocument document) =>
        document.RootElement.GetProperty("error").GetProperty("code")
            .GetString();

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
        Guid OwnerDeviceId,
        string OwnerToken,
        string OperatorToken);

    private sealed record StandardIdentity(
        Guid Id,
        string Weight,
        bool IsDefault);

    private sealed record AuditFact(
        string? BeforeJson,
        string AfterJson);
}
