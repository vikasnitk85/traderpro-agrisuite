using System.Text.Json;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Platform.Identity;

namespace TraderPro.Api.Http;

public static class IdentityEndpointRegistration
{
    public const string LoginRatePolicy = "TraderProIdentityLogin";
    public const string ActivationRatePolicy =
        "TraderProIdentityActivation";
    public const string RefreshRatePolicy = "TraderProIdentityRefresh";

    public static IEndpointRouteBuilder MapTraderProIdentityEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool rateLimitingEnabled,
        bool bootstrapEnabled,
        bool testingEnvironment)
    {
        var activation = endpoints.MapPost(
                "/api/v1/auth/device-activations/redeem",
                async (
                    HttpContext context,
                    IProductionIdentityService service) =>
                {
                    var request =
                        await ReadRequiredJsonBodyAsync<
                            RedeemDeviceActivationRequest>(context);
                    var result = await service.RedeemDeviceActivationAsync(
                        request,
                        CommercialIdentityRequestMiddleware.GetCorrelationId(
                            context),
                        context.RequestAborted);
                    return Results.Ok(result);
                })
            .AllowAnonymous()
            .WithTraderProAnonymousCommercialContext(
                secretBearingResponse: true);
        var login = endpoints.MapPost(
                "/api/v1/auth/login",
                async (
                    HttpContext context,
                    IProductionIdentityService service) =>
                {
                    var request =
                        await ReadRequiredJsonBodyAsync<LoginRequest>(context);
                    var result = await service.LoginAsync(
                        request,
                        CommercialIdentityRequestMiddleware.GetCorrelationId(
                            context),
                        context.RequestAborted);
                    return Results.Ok(result);
                })
            .AllowAnonymous()
            .WithTraderProAnonymousCommercialContext(
                secretBearingResponse: true);
        var refresh = endpoints.MapPost(
                "/api/v1/auth/refresh",
                async (
                    HttpContext context,
                    IProductionIdentityService service) =>
                {
                    var request =
                        await ReadRequiredJsonBodyAsync<RefreshRequest>(context);
                    var result = await service.RefreshAsync(
                        request,
                        CommercialIdentityRequestMiddleware.GetCorrelationId(
                            context),
                        context.RequestAborted);
                    return Results.Ok(result);
                })
            .AllowAnonymous()
            .WithTraderProAnonymousCommercialContext(
                secretBearingResponse: true);
        if (rateLimitingEnabled)
        {
            activation.RequireRateLimiting(ActivationRatePolicy);
            login.RequireRateLimiting(LoginRatePolicy);
            refresh.RequireRateLimiting(RefreshRatePolicy);
        }

        endpoints.MapPost(
                "/api/v1/auth/logout",
                async (
                    HttpContext context,
                    IProductionIdentityService service) =>
                {
                    await service.LogoutAsync(
                        CommercialIdentityRequestMiddleware.GetCorrelationId(
                            context),
                        context.RequestAborted);
                    return Results.NoContent();
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.CommercialUser,
                allowRevokedTokenFamily: true);
        endpoints.MapPost(
                "/api/v1/auth/logout-all",
                async (
                    HttpContext context,
                    IProductionIdentityService service) =>
                {
                    await service.LogoutAllAsync(
                        CommercialIdentityRequestMiddleware.GetCorrelationId(
                            context),
                        context.RequestAborted);
                    return Results.NoContent();
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.CommercialUser);
        endpoints.MapGet(
                "/api/v1/auth/me",
                async (
                    HttpContext context,
                    IProductionIdentityService service) =>
                {
                    var result = await service.GetCurrentAsync(
                        context.RequestAborted);
                    return Results.Ok(result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.CommercialUser);
        endpoints.MapPost(
                "/api/v1/devices/{deviceId:guid}/activation-codes",
                async (
                    Guid deviceId,
                    HttpContext context,
                    IProductionIdentityService service) =>
                {
                    var result =
                        await service.IssueDeviceActivationCodeAsync(
                            deviceId,
                            GetHeader(context, "Idempotency-Key"),
                            CommercialIdentityRequestMiddleware
                                .GetCorrelationId(context),
                            context.RequestAborted);
                    return Results.Created(
                        $"/api/v1/devices/{deviceId:D}/activation-codes",
                        result);
                })
            .RequireTraderProCommercialAuthorization(
                TraderProAuthorizationPolicies.Owner,
                secretBearingResponse: true);
        if (bootstrapEnabled)
        {
            endpoints.MapPost(
                    "/api/v1/spikes/identity/bootstrap",
                    async (
                        HttpContext context,
                        IProductionIdentityService service) =>
                    {
                        var request =
                            await ReadRequiredJsonBodyAsync<
                                DevelopmentIdentityBootstrapRequest>(context);
                        var result =
                            await service.BootstrapDevelopmentAsync(
                                request,
                                CommercialIdentityRequestMiddleware
                                    .GetCorrelationId(context),
                                context.RequestAborted);
                        return Results.Ok(result);
                    })
                .AllowAnonymous()
                .WithTraderProAnonymousCommercialContext(
                    secretBearingResponse: true);
        }

        if (testingEnvironment)
        {
            endpoints.MapGet(
                    "/api/v1/testing/commercial-context",
                    (IAuthenticatedTraderProContext currentIdentity) =>
                        Results.Ok(
                            new
                            {
                                currentIdentity.WorkspaceId,
                                currentIdentity.UserId,
                                currentIdentity.DeviceId,
                                currentIdentity.CompanyId,
                                currentIdentity.DefaultBranchId,
                                Role = currentIdentity.IsOwner
                                    ? "Owner"
                                    : "Operator",
                            }))
                .RequireTraderProCommercialAuthorization(
                    TraderProAuthorizationPolicies.CommercialUser);
            endpoints.MapGet(
                    "/api/v1/testing/commercial-context/denied",
                    () => Results.NoContent())
                .RequireTraderProCommercialAuthorization(
                    TraderProAuthorizationPolicies.CommercialUser)
                .RequireAuthorization(
                    policy => policy.RequireAssertion(_ => false));
        }

        return endpoints;
    }

    private static async Task<T> ReadRequiredJsonBodyAsync<T>(
        HttpContext context)
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

    private static string GetHeader(HttpContext context, string name)
    {
        var values = context.Request.Headers[name];
        return values.Count == 1 ? values[0] ?? string.Empty : string.Empty;
    }

    private static ApplicationProblemException RequestBodyInvalid()
    {
        return new ApplicationProblemException(
            "REQUEST_BODY_INVALID",
            "A valid JSON request body is required.",
            ApplicationErrorCategory.Validation);
    }
}
