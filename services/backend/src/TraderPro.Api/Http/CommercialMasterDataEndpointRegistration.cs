using System.Globalization;
using System.Text.Json;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Operations;
using TraderPro.Application.Platform.Identity;
using TraderPro.Application.Procurement.MasterData;

namespace TraderPro.Api.Http;

public static class CommercialMasterDataEndpointRegistration
{
    public static IEndpointRouteBuilder
        MapTraderProCommercialMasterDataEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        MapLocations(endpoints);
        MapVehicles(endpoints);
        MapBagTypes(endpoints);
        MapWeightPolicies(endpoints);
        MapSettings(endpoints);
        return endpoints;
    }

    private static void MapLocations(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/operations/locations");
        group.MapPost(
                "/",
                async (
                    HttpContext context,
                    IBusinessLocationService service) =>
                {
                    var body =
                        await ReadBodyAsync<CreateBusinessLocationBody>(
                            context);
                    var result = await service.CreateAsync(
                        new CreateBusinessLocationCommand(
                            body.Code ?? string.Empty,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.LocationType ?? string.Empty,
                            body.AddressLine,
                            body.Notes),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/",
                async (
                    HttpContext context,
                    IBusinessLocationService service) =>
                {
                    var page = await service.ListAsync(
                        ListQuery(context),
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
                    IBusinessLocationService service) =>
                {
                    var result = await service.GetAsync(
                        id,
                        context.RequestAborted);
                    return ReadResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    IBusinessLocationService service) =>
                {
                    var body =
                        await ReadBodyAsync<UpdateBusinessLocationBody>(
                            context);
                    var result = await service.UpdateAsync(
                        new UpdateBusinessLocationCommand(
                            id,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.LocationType ?? string.Empty,
                            body.AddressLine,
                            body.Notes,
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        MapLocationStatus(group, activate: false);
        MapLocationStatus(group, activate: true);
    }

    private static void MapLocationStatus(
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
                    IBusinessLocationService service) =>
                {
                    var command = new MasterStatusCommand(
                        id,
                        ExpectedVersion(context));
                    var result = activate
                        ? await service.ReactivateAsync(
                            command,
                            Header(context, "Idempotency-Key"),
                            context.RequestAborted)
                        : await service.DeactivateAsync(
                            command,
                            Header(context, "Idempotency-Key"),
                            context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
    }

    private static void MapVehicles(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/procurement/vehicles");
        group.MapPost(
                "/",
                async (
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                {
                    var body =
                        await ReadBodyAsync<CreateReceivingVehicleBody>(
                            context);
                    var result = await service.CreateVehicleAsync(
                        new CreateReceivingVehicleCommand(
                            body.Code ?? string.Empty,
                            body.RegistrationNumber ?? string.Empty,
                            body.DisplayName,
                            body.VehicleType ?? string.Empty,
                            body.OwnerName,
                            body.ContactNumber,
                            body.Notes),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/",
                async (
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                    ReadResult(
                        context,
                        await service.ListVehiclesAsync(
                            ListQuery(context),
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapGet(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                    ReadResult(
                        context,
                        await service.GetVehicleAsync(
                            id,
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                {
                    var body =
                        await ReadBodyAsync<UpdateReceivingVehicleBody>(
                            context);
                    var result = await service.UpdateVehicleAsync(
                        new UpdateReceivingVehicleCommand(
                            id,
                            body.RegistrationNumber ?? string.Empty,
                            body.DisplayName,
                            body.VehicleType ?? string.Empty,
                            body.OwnerName,
                            body.ContactNumber,
                            body.Notes,
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        MapProcurementStatus(
            group,
            static (service, command, activate, key, cancellationToken) =>
                service.SetVehicleStatusAsync(
                    command,
                    activate,
                    key,
                    cancellationToken));
    }

    private static void MapBagTypes(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/procurement/bag-types");
        group.MapPost(
                "/",
                async (
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                {
                    var body = await ReadBodyAsync<CreateBagTypeBody>(context);
                    var isReturnable = CommercialMasterDataInputRules
                        .RequireBagReturnability(body.IsReturnable);
                    var result = await service.CreateBagTypeAsync(
                        new CreateBagTypeCommand(
                            body.Code ?? string.Empty,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.ConstructionClass ?? string.Empty,
                            CommercialMasterDataInputRules.ParseTareWeight(
                                body.StandardTareWeightKg),
                            isReturnable,
                            body.Notes),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/",
                async (
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                    ReadResult(
                        context,
                        await service.ListBagTypesAsync(
                            ListQuery(context),
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapGet(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                    ReadResult(
                        context,
                        await service.GetBagTypeAsync(
                            id,
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                {
                    var body = await ReadBodyAsync<UpdateBagTypeBody>(context);
                    var isReturnable = CommercialMasterDataInputRules
                        .RequireBagReturnability(body.IsReturnable);
                    var result = await service.UpdateBagTypeAsync(
                        new UpdateBagTypeCommand(
                            id,
                            body.Name ?? string.Empty,
                            body.LocalName,
                            body.ConstructionClass ?? string.Empty,
                            CommercialMasterDataInputRules.ParseTareWeight(
                                body.StandardTareWeightKg),
                            isReturnable,
                            body.Notes,
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        MapProcurementStatus(
            group,
            static (service, command, activate, key, cancellationToken) =>
                service.SetBagTypeStatusAsync(
                    command,
                    activate,
                    key,
                    cancellationToken));
    }

    private static void MapWeightPolicies(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(
            "/api/v1/procurement/weight-policies");
        group.MapPost(
                "/",
                async (
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                {
                    var body =
                        await ReadBodyAsync<CreateWeightPolicyBody>(context);
                    var result = await service.CreateWeightPolicyAsync(
                        new CreateWeightProcessingPolicyCommand(
                            body.Code ?? string.Empty,
                            body.Name ?? string.Empty,
                            body.DecimalPlaces,
                            body.ProcessingMethod ?? string.Empty,
                            body.Notes),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        group.MapGet(
                "/",
                async (
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                    ReadResult(
                        context,
                        await service.ListWeightPoliciesAsync(
                            ListQuery(context),
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapGet(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                    ReadResult(
                        context,
                        await service.GetWeightPolicyAsync(
                            id,
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        group.MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                {
                    var body =
                        await ReadBodyAsync<UpdateWeightPolicyBody>(context);
                    var result = await service.UpdateWeightPolicyAsync(
                        new UpdateWeightProcessingPolicyCommand(
                            id,
                            body.Name ?? string.Empty,
                            body.DecimalPlaces,
                            body.ProcessingMethod ?? string.Empty,
                            body.Notes,
                            ExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
        MapProcurementStatus(
            group,
            static (service, command, activate, key, cancellationToken) =>
                service.SetWeightPolicyStatusAsync(
                    command,
                    activate,
                    key,
                    cancellationToken));
    }

    private static void MapSettings(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/v1/procurement/settings",
                async (
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                    ReadResult(
                        context,
                        await service.GetSettingsAsync(
                            context.RequestAborted)))
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.OperatorOrOwner);
        endpoints.MapPut(
                "/api/v1/procurement/settings",
                async (
                    HttpContext context,
                    IProcurementMasterDataService service) =>
                {
                    var body = await ReadBodyAsync<SettingsBody>(context);
                    var result = await service.ConfigureSettingsAsync(
                        new ConfigureCompanyProcurementSettingsCommand(
                            body.DefaultDestinationLocationId,
                            body.DefaultWeightProcessingPolicyId,
                            body.VehicleSelectionMode ?? string.Empty,
                            OptionalExpectedVersion(context)),
                        Header(context, "Idempotency-Key"),
                        context.RequestAborted);
                    return CommandResult(context, result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner);
    }

    private static void MapProcurementStatus<TResult>(
        RouteGroupBuilder group,
        Func<
            IProcurementMasterDataService,
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
                        IProcurementMasterDataService service) =>
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

    private static MasterListQuery ListQuery(HttpContext context)
    {
        var limit = IntQuery(context, "limit", 50);
        return CommercialMasterDataInputRules.CreateListQuery(
            Query(context, "status"),
            Query(context, "search"),
            Query(context, "cursor"),
            limit);
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
            throw new ApplicationProblemException(
                "MASTER_QUERY_INVALID",
                $"Query parameter {name} must be supplied once.",
                ApplicationErrorCategory.Validation);
        }

        return values[0];
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
            throw new ApplicationProblemException(
                "MASTER_PAGE_SIZE_INVALID",
                "Limit must be between 1 and 100.",
                ApplicationErrorCategory.Validation);
        }

        return parsed;
    }

    private static long ExpectedVersion(HttpContext context)
    {
        return CommercialMasterDataInputRules.RequireExpectedVersion(
            ParseExpectedVersion(context, required: true));
    }

    private static long? OptionalExpectedVersion(HttpContext context)
    {
        return ParseExpectedVersion(context, required: false);
    }

    private static long? ParseExpectedVersion(
        HttpContext context,
        bool required)
    {
        var value = Header(context, "X-Expected-Version");
        if (value.Length == 0 && !required)
        {
            return null;
        }

        if (!long.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed) ||
            parsed <= 0)
        {
            throw new ApplicationProblemException(
                "MASTER_EXPECTED_VERSION_REQUIRED",
                "X-Expected-Version must be a positive integer.",
                ApplicationErrorCategory.Validation);
        }

        return parsed;
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

    private static ApplicationProblemException RequestBodyInvalid()
    {
        return new ApplicationProblemException(
            "REQUEST_BODY_INVALID",
            "A valid JSON request body is required.",
            ApplicationErrorCategory.Validation);
    }

    public sealed record CreateBusinessLocationBody(
        string? Code,
        string? Name,
        string? LocalName,
        string? LocationType,
        string? AddressLine,
        string? Notes);

    public sealed record UpdateBusinessLocationBody(
        string? Name,
        string? LocalName,
        string? LocationType,
        string? AddressLine,
        string? Notes);

    public sealed record CreateReceivingVehicleBody(
        string? Code,
        string? RegistrationNumber,
        string? DisplayName,
        string? VehicleType,
        string? OwnerName,
        string? ContactNumber,
        string? Notes);

    public sealed record UpdateReceivingVehicleBody(
        string? RegistrationNumber,
        string? DisplayName,
        string? VehicleType,
        string? OwnerName,
        string? ContactNumber,
        string? Notes);

    public sealed record CreateBagTypeBody(
        string? Code,
        string? Name,
        string? LocalName,
        string? ConstructionClass,
        string? StandardTareWeightKg,
        bool? IsReturnable,
        string? Notes);

    public sealed record UpdateBagTypeBody(
        string? Name,
        string? LocalName,
        string? ConstructionClass,
        string? StandardTareWeightKg,
        bool? IsReturnable,
        string? Notes);

    public sealed record CreateWeightPolicyBody(
        string? Code,
        string? Name,
        int DecimalPlaces,
        string? ProcessingMethod,
        string? Notes);

    public sealed record UpdateWeightPolicyBody(
        string? Name,
        int DecimalPlaces,
        string? ProcessingMethod,
        string? Notes);

    public sealed record SettingsBody(
        Guid DefaultDestinationLocationId,
        Guid DefaultWeightProcessingPolicyId,
        string? VehicleSelectionMode);

    private sealed record CommandResponse<T>(T Result, ResponseMeta Meta);

    private sealed record ReadResponse<T>(T Result, ResponseMeta Meta);

    private sealed record ResponseMeta(
        string CorrelationId,
        string? IdempotencyStatus);
}
