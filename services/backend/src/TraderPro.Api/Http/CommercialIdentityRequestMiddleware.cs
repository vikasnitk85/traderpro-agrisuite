using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Platform.Identity;

namespace TraderPro.Api.Http;

public sealed class CommercialIdentityRequestMiddleware(RequestDelegate next)
{
    public const string CorrelationItemKey =
        "TraderPro.Commercial.CorrelationId";

    public async Task InvokeAsync(
        HttpContext context,
        ICommercialIdentityContextResolver resolver,
        TraderProAuthenticationOptions authenticationOptions)
    {
        var options = context.GetEndpoint()?.Metadata
            .GetMetadata<TraderProCommercialContextOptions>();
        if (options is null)
        {
            await next(context);
            return;
        }

        if (options.SecretBearingResponse)
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";
        }

        var correlationId = EstablishCorrelationId(context);
        if (options.RequiresSecureTransport &&
            authenticationOptions.RequireHttps &&
            !context.Request.IsHttps)
        {
            throw new ApplicationProblemException(
                "HTTPS_REQUIRED",
                "A secure HTTPS connection is required.",
                ApplicationErrorCategory.Authentication);
        }

        if (options.RequiresAuthenticatedContext &&
            context.User.Identity?.IsAuthenticated == true)
        {
            await resolver.BindAsync(
                ParseToken(context.User),
                correlationId,
                options.AllowRevokedTokenFamily,
                context.RequestAborted);
        }

        await next(context);
    }

    public static string GetCorrelationId(HttpContext context)
    {
        return context.Items[CorrelationItemKey] as string ??
            throw new InvalidOperationException(
                "Commercial request correlation was not established.");
    }

    private static AuthenticatedAccessToken ParseToken(
        ClaimsPrincipal principal)
    {
        try
        {
            return new AuthenticatedAccessToken(
                RequiredGuid(principal, "wid"),
                RequiredGuid(principal, JwtRegisteredClaimNames.Sub),
                RequiredGuid(principal, "did"),
                RequiredGuid(principal, "cid"),
                RequiredGuid(principal, "bid"),
                Required(principal, "role"),
                RequiredPositiveInt(principal, "ucv"),
                RequiredPositiveInt(principal, "dsv"),
                RequiredGuid(principal, "sid"),
                Required(principal, JwtRegisteredClaimNames.Jti));
        }
        catch (Exception exception) when (
            exception is ArgumentException or FormatException or OverflowException)
        {
            throw new ApplicationProblemException(
                "ACCESS_TOKEN_INVALID",
                "The access token is invalid.",
                ApplicationErrorCategory.Authentication,
                innerException: exception);
        }
    }

    private static Guid RequiredGuid(
        ClaimsPrincipal principal,
        string type)
    {
        var value = Required(principal, type);
        if (!Guid.TryParseExact(value, "D", out var parsed) ||
            parsed == Guid.Empty)
        {
            throw new FormatException("A canonical UUID claim is required.");
        }

        return parsed;
    }

    private static int RequiredPositiveInt(
        ClaimsPrincipal principal,
        string type)
    {
        var value = Required(principal, type);
        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed) ||
            parsed <= 0)
        {
            throw new FormatException("A positive version claim is required.");
        }

        return parsed;
    }

    private static string Required(
        ClaimsPrincipal principal,
        string type)
    {
        var values = principal.FindAll(type).ToArray();
        if (values.Length != 1 ||
            string.IsNullOrWhiteSpace(values[0].Value))
        {
            throw new FormatException("One required claim is missing.");
        }

        return values[0].Value;
    }

    private static string EstablishCorrelationId(HttpContext context)
    {
        var safeCorrelationId = Guid.CreateVersion7().ToString("D");
        context.Items[CorrelationItemKey] = safeCorrelationId;
        context.Response.Headers["X-Correlation-ID"] = safeCorrelationId;
        var values = context.Request.Headers["X-Correlation-ID"];
        if (values.Count == 0)
        {
            return safeCorrelationId;
        }

        if (values.Count != 1 ||
            !Guid.TryParseExact(values[0], "D", out var supplied) ||
            supplied == Guid.Empty)
        {
            throw new ApplicationProblemException(
                "CORRELATION_ID_INVALID",
                "X-Correlation-ID must be one non-empty UUID in D format.",
                ApplicationErrorCategory.Validation);
        }

        var suppliedCorrelationId = supplied.ToString("D");
        context.Items[CorrelationItemKey] = suppliedCorrelationId;
        context.Response.Headers["X-Correlation-ID"] =
            suppliedCorrelationId;
        return suppliedCorrelationId;
    }
}
