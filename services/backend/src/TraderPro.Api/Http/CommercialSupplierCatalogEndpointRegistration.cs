using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TraderPro.Application.Catalog;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Platform.Identity;
using TraderPro.Application.Procurement.Suppliers;

namespace TraderPro.Api.Http;

public static class CommercialSupplierCatalogEndpointRegistration
{
    public static IEndpointRouteBuilder
        MapTraderProCommercialSupplierCatalogEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        MapSuppliers(endpoints);
        MapProductGroups(endpoints);
        MapProducts(endpoints);
        return endpoints;
    }

    private static void MapSuppliers(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/procurement/suppliers");
        group.MapPost(
                "/",
                async (HttpContext context, ISupplierService service) =>
                {
                    var body = await ReadBodyAsync<CreateSupplierBody>(context);
                    var result = await service.CreateAsync(
                        new CreateSupplierCommand(
                            body.Code ?? string.Empty,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.SupplierType ?? string.Empty,
                            body.ProductScopeMode ?? string.Empty,
                            body.ContactName,
                            body.ContactNumber,
                            body.Email,
                            body.AddressLine,
                            body.TaxRegistrationNumber,
                            body.Notes,
                            body.InitialProductIds ?? []),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/",
                async (HttpContext context, ISupplierService service) =>
                {
                    var supplierType = Query(context, "supplierType");
                    var scopeMode = Query(context, "productScopeMode");
                    var page = await service.ListAsync(
                        new SupplierListQuery(
                            ListQuery(context),
                            supplierType,
                            scopeMode),
                        context.RequestAborted);
                    return ReadResult(context, page);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapGet(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    ISupplierService service) =>
                    ReadResult(
                        context,
                        await service.GetAsync(id, context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    ISupplierService service) =>
                {
                    var body = await ReadBodyAsync<UpdateSupplierBody>(context);
                    RejectImmutableFields(body.AdditionalFields, "code");
                    var result = await service.UpdateAsync(
                        new UpdateSupplierCommand(
                            id,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.SupplierType ?? string.Empty,
                            body.ProductScopeMode ?? string.Empty,
                            body.ContactName,
                            body.ContactNumber,
                            body.Email,
                            body.AddressLine,
                            body.TaxRegistrationNumber,
                            body.Notes,
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        MapSupplierStatus(group, activate: false);
        MapSupplierStatus(group, activate: true);

        group.MapPost(
                "/{supplierId:guid}/product-scopes",
                async (
                    Guid supplierId,
                    HttpContext context,
                    ISupplierService service) =>
                {
                    var body = await ReadBodyAsync<AddScopeBody>(context);
                    var result = await service.AddScopeAsync(
                        new AddSupplierProductScopeCommand(
                            supplierId,
                            body.ProductId),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/{supplierId:guid}/product-scopes",
                async (
                    Guid supplierId,
                    HttpContext context,
                    ISupplierService service) =>
                    ReadResult(
                        context,
                        await service.ListScopesAsync(
                            supplierId,
                            ListQuery(context),
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        MapScopeStatus(group, activate: false);
        MapScopeStatus(group, activate: true);
    }

    private static void MapSupplierStatus(
        RouteGroupBuilder group,
        bool activate)
    {
        group.MapPost(
                activate
                    ? "/{id:guid}/reactivate"
                    : "/{id:guid}/deactivate",
                async (
                    Guid id,
                    HttpContext context,
                    ISupplierService service) =>
                {
                    var result = await service.SetStatusAsync(
                        new MasterStatusCommand(id, ExpectedVersion(context)),
                        activate,
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
    }

    private static void MapScopeStatus(
        RouteGroupBuilder group,
        bool activate)
    {
        group.MapPost(
                activate
                    ? "/{supplierId:guid}/product-scopes/{id:guid}/reactivate"
                    : "/{supplierId:guid}/product-scopes/{id:guid}/deactivate",
                async (
                    Guid supplierId,
                    Guid id,
                    HttpContext context,
                    ISupplierService service) =>
                {
                    var result = await service.SetScopeStatusAsync(
                        new SupplierProductScopeStatusCommand(
                            supplierId,
                            id,
                            ExpectedVersion(context)),
                        activate,
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
    }

    private static void MapProductGroups(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/catalog/product-groups");
        group.MapPost(
                "/",
                async (HttpContext context, ICatalogService service) =>
                {
                    var body = await ReadBodyAsync<ProductGroupBody>(context);
                    var result = await service.CreateGroupAsync(
                        new CreateProductGroupCommand(
                            body.Code ?? string.Empty,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.Description),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/",
                async (HttpContext context, ICatalogService service) =>
                    ReadResult(
                        context,
                        await service.ListGroupsAsync(
                            ListQuery(context),
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapGet(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    ICatalogService service) =>
                    ReadResult(
                        context,
                        await service.GetGroupAsync(
                            id,
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    ICatalogService service) =>
                {
                    var body = await ReadBodyAsync<UpdateProductGroupBody>(
                        context);
                    RejectImmutableFields(body.AdditionalFields, "code");
                    var result = await service.UpdateGroupAsync(
                        new UpdateProductGroupCommand(
                            id,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.Description,
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        MapCatalogStatus(
            group,
            (service, command, activate, key, cancellationToken) =>
                service.SetGroupStatusAsync(
                    command,
                    activate,
                    key,
                    cancellationToken));
    }

    private static void MapProducts(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/catalog/products");
        group.MapPost(
                "/",
                async (HttpContext context, ICatalogService service) =>
                {
                    var body = await ReadBodyAsync<CreateProductBody>(context);
                    var result = await service.CreateProductAsync(
                        new CreateProductCommand(
                            body.ProductGroupId,
                            body.Code ?? string.Empty,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.ProductType ?? string.Empty,
                            CommercialMasterDataInputRules
                                .RequireProductPurchasable(body.IsPurchasable),
                            body.ProcessingFamilyCode,
                            body.Description,
                            body.Notes),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/",
                async (HttpContext context, ICatalogService service) =>
                {
                    var type = Query(context, "productType");
                    var page = await service.ListProductsAsync(
                        new ProductListQuery(
                            ListQuery(context),
                            OptionalGuidQuery(context, "productGroupId"),
                            type,
                            OptionalBoolQuery(context, "isPurchasable"),
                            CommercialMasterDataInputRules
                                .NormalizeProcessingFamilyFilter(
                                    Query(
                                        context,
                                        "processingFamilyCode"))),
                        context.RequestAborted);
                    return ReadResult(context, page);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapGet(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    ICatalogService service) =>
                    ReadResult(
                        context,
                        await service.GetProductAsync(
                            id,
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    ICatalogService service) =>
                {
                    var body = await ReadBodyAsync<UpdateProductBody>(context);
                    RejectImmutableFields(body.AdditionalFields, "code");
                    var result = await service.UpdateProductAsync(
                        new UpdateProductCommand(
                            id,
                            body.ProductGroupId,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.ProductType ?? string.Empty,
                            CommercialMasterDataInputRules
                                .RequireProductPurchasable(body.IsPurchasable),
                            body.ProcessingFamilyCode,
                            body.Description,
                            body.Notes,
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        MapCatalogStatus(
            group,
            (service, command, activate, key, cancellationToken) =>
                service.SetProductStatusAsync(
                    command,
                    activate,
                    key,
                    cancellationToken));

        group.MapPost(
                "/{productId:guid}/bag-standards",
                async (
                    Guid productId,
                    HttpContext context,
                    ICatalogService service) =>
                {
                    var body = await ReadBodyAsync<CreateBagStandardBody>(
                        context);
                    var result = await service.CreateBagStandardAsync(
                        new CreateProductStandardBagWeightCommand(
                            productId,
                            body.BagTypeId,
                            body.Label,
                            CommercialMasterDataInputRules
                                .ParseStandardContentWeight(
                                    body.StandardContentWeightKg),
                            body.IsDefault ?? false),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/{productId:guid}/bag-standards",
                async (
                    Guid productId,
                    HttpContext context,
                    ICatalogService service) =>
                    ReadResult(
                        context,
                        await service.ListBagStandardsAsync(
                            productId,
                            ListQuery(context),
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapGet(
                "/{productId:guid}/bag-standards/{id:guid}",
                async (
                    Guid productId,
                    Guid id,
                    HttpContext context,
                    ICatalogService service) =>
                    ReadResult(
                        context,
                        await service.GetBagStandardAsync(
                            productId,
                            id,
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapPut(
                "/{productId:guid}/bag-standards/{id:guid}",
                async (
                    Guid productId,
                    Guid id,
                    HttpContext context,
                    ICatalogService service) =>
                {
                    var body = await ReadBodyAsync<UpdateBagStandardBody>(
                        context);
                    RejectImmutableFields(
                        body.AdditionalFields,
                        "productId",
                        "bagTypeId",
                        "isDefault");
                    var result = await service.UpdateBagStandardAsync(
                        new UpdateProductStandardBagWeightCommand(
                            productId,
                            id,
                            body.Label,
                            CommercialMasterDataInputRules
                                .ParseStandardContentWeight(
                                    body.StandardContentWeightKg),
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        MapBagStandardStatus(group, activate: false);
        MapBagStandardStatus(group, activate: true);
        group.MapPost(
                "/{productId:guid}/bag-standards/{id:guid}/set-default",
                async (
                    Guid productId,
                    Guid id,
                    HttpContext context,
                    ICatalogService service) =>
                {
                    var result = await service.SetDefaultBagStandardAsync(
                        new ProductStandardBagWeightStatusCommand(
                            productId,
                            id,
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
    }

    private static void MapCatalogStatus<TResult>(
        RouteGroupBuilder group,
        Func<
            ICatalogService,
            MasterStatusCommand,
            bool,
            string,
            CancellationToken,
            Task<IdempotentCommandResult<TResult>>> execute)
        where TResult : class
    {
        foreach (var activate in new[] { false, true })
        {
            var captured = activate;
            group.MapPost(
                    captured
                        ? "/{id:guid}/reactivate"
                        : "/{id:guid}/deactivate",
                    async (
                        Guid id,
                        HttpContext context,
                        ICatalogService service) =>
                    {
                        var result = await execute(
                            service,
                            new MasterStatusCommand(
                                id,
                                ExpectedVersion(context)),
                            captured,
                            Header(context, "Idempotency-Key"),
                            context.RequestAborted);
                        return CommandResult(context, result);
                    })
                .RequireTraderProCommercialAuthorization(
                    TraderProAuthorizationPolicies.Owner);
        }
    }

    private static void MapBagStandardStatus(
        RouteGroupBuilder group,
        bool activate)
    {
        group.MapPost(
                activate
                    ? "/{productId:guid}/bag-standards/{id:guid}/reactivate"
                    : "/{productId:guid}/bag-standards/{id:guid}/deactivate",
                async (
                    Guid productId,
                    Guid id,
                    HttpContext context,
                    ICatalogService service) =>
                {
                    var result = await service.SetBagStandardStatusAsync(
                        new ProductStandardBagWeightStatusCommand(
                            productId,
                            id,
                            ExpectedVersion(context)),
                        activate,
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
    }

    private static MasterListQuery ListQuery(HttpContext context)
    {
        return CommercialMasterDataInputRules.CreateListQuery(
            Query(context, "status"),
            Query(context, "search"),
            Query(context, "cursor"),
            IntQuery(context, "limit", 50));
    }

    private static string? Query(HttpContext context, string name)
    {
        var values = context.Request.Query[name];
        if (values.Count == 0)
        {
            return null;
        }

        if (values.Count != 1)
        {
            throw QueryInvalid(name);
        }

        return values[0];
    }

    private static Guid? OptionalGuidQuery(HttpContext context, string name)
    {
        var value = Query(context, name);
        if (value is null)
        {
            return null;
        }

        if (!Guid.TryParseExact(value, "D", out var parsed) ||
            parsed == Guid.Empty)
        {
            throw QueryInvalid(name);
        }

        return parsed;
    }

    private static bool? OptionalBoolQuery(HttpContext context, string name)
    {
        return Query(context, name) switch
        {
            null => null,
            "true" => true,
            "false" => false,
            _ => throw QueryInvalid(name),
        };
    }

    private static int IntQuery(
        HttpContext context,
        string name,
        int defaultValue)
    {
        var value = Query(context, name);
        if (value is null)
        {
            return defaultValue;
        }

        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            throw QueryInvalid(name);
        }

        return parsed;
    }

    private static long ExpectedVersion(HttpContext context)
    {
        var value = Header(context, "X-Expected-Version");
        return long.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed)
            ? CommercialMasterDataInputRules.RequireExpectedVersion(parsed)
            : CommercialMasterDataInputRules.RequireExpectedVersion(null);
    }

    private static string Header(HttpContext context, string name)
    {
        var values = context.Request.Headers[name];
        return values.Count == 1 ? values[0] ?? string.Empty : string.Empty;
    }

    private static IResult CommandResult<T>(
        HttpContext context,
        IdempotentCommandResult<T> result)
        where T : class
    {
        context.Response.Headers["X-Correlation-ID"] = result.CorrelationId;
        return Results.Json(
            new CommandResponse<T>(
                result.Result,
                new ResponseMeta(
                    result.CorrelationId,
                    result.IdempotencyStatus.ToString())),
            statusCode: result.StatusCode);
    }

    private static IResult ReadResult<T>(HttpContext context, T result)
    {
        return Results.Ok(
            new ReadResponse<T>(
                result,
                new ResponseMeta(
                    CommercialIdentityRequestMiddleware.GetCorrelationId(
                        context),
                    null)));
    }

    private static async Task<T> ReadBodyAsync<T>(HttpContext context)
    {
        try
        {
            return await context.Request.ReadFromJsonAsync<T>(
                    cancellationToken: context.RequestAborted) ??
                throw RequestBodyInvalid();
        }
        catch (Exception exception)
            when (exception is JsonException or
                BadHttpRequestException or
                NotSupportedException or
                InvalidOperationException)
        {
            throw RequestBodyInvalid();
        }
    }

    private static ApplicationProblemException QueryInvalid(string name) =>
        new(
            "MASTER_QUERY_INVALID",
            $"Query parameter {name} is invalid.",
            ApplicationErrorCategory.Validation);

    private static ApplicationProblemException RequestBodyInvalid() =>
        new(
            "REQUEST_BODY_INVALID",
            "A valid JSON request body is required.",
            ApplicationErrorCategory.Validation);

    private static void RejectImmutableFields(
        IReadOnlyDictionary<string, JsonElement>? additionalFields,
        params string[] immutableFields)
    {
        if (additionalFields is null || additionalFields.Count == 0)
        {
            return;
        }

        var supplied = immutableFields
            .Where(field => additionalFields.Keys.Any(
                key => string.Equals(
                    key,
                    field,
                    StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (supplied.Length == 0)
        {
            return;
        }

        const string code = "MASTER_IMMUTABLE_FIELD_SUPPLIED";
        throw new ApplicationProblemException(
            code,
            "Immutable or command-only fields cannot be supplied on update.",
            ApplicationErrorCategory.Validation,
            fieldErrors: supplied.Select(field =>
                new ApplicationFieldError(
                    field,
                    code,
                    $"Field {field} cannot be supplied on update."))
                .ToArray());
    }

    public sealed record CreateSupplierBody(
        string? Code,
        string? Name,
        string? LocalName,
        string? SupplierType,
        string? ProductScopeMode,
        string? ContactName,
        string? ContactNumber,
        string? Email,
        string? AddressLine,
        string? TaxRegistrationNumber,
        string? Notes,
        IReadOnlyList<Guid>? InitialProductIds);

    public sealed record UpdateSupplierBody(
        string? Name,
        string? LocalName,
        string? SupplierType,
        string? ProductScopeMode,
        string? ContactName,
        string? ContactNumber,
        string? Email,
        string? AddressLine,
        string? TaxRegistrationNumber,
        string? Notes)
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
    }

    public sealed record AddScopeBody(Guid ProductId);

    public sealed record ProductGroupBody(
        string? Code,
        string? Name,
        string? LocalName,
        string? Description);

    public sealed record UpdateProductGroupBody(
        string? Name,
        string? LocalName,
        string? Description)
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
    }

    public sealed record CreateProductBody(
        Guid ProductGroupId,
        string? Code,
        string? Name,
        string? LocalName,
        string? ProductType,
        bool? IsPurchasable,
        string? ProcessingFamilyCode,
        string? Description,
        string? Notes);

    public sealed record UpdateProductBody(
        Guid ProductGroupId,
        string? Name,
        string? LocalName,
        string? ProductType,
        bool? IsPurchasable,
        string? ProcessingFamilyCode,
        string? Description,
        string? Notes)
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
    }

    public sealed record CreateBagStandardBody(
        Guid BagTypeId,
        string? Label,
        string? StandardContentWeightKg,
        bool? IsDefault);

    public sealed record UpdateBagStandardBody(
        string? Label,
        string? StandardContentWeightKg)
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
    }

    private sealed record CommandResponse<T>(T Result, ResponseMeta Meta);

    private sealed record ReadResponse<T>(T Result, ResponseMeta Meta);

    private sealed record ResponseMeta(
        string CorrelationId,
        string? IdempotencyStatus);
}
