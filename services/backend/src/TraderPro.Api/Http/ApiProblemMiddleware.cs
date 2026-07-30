using TraderPro.Application.Common.Errors;

namespace TraderPro.Api.Http;

public sealed class ApiProblemMiddleware(
    RequestDelegate next,
    ILogger<ApiProblemMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApplicationProblemException exception)
        {
            await ApiProblemWriter.WriteAsync(
                context,
                exception,
                GetOrCreateCorrelationId(context));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API request failure.");
            await ApiProblemWriter.WriteAsync(
                context,
                new ApplicationProblemException(
                    "TEMPORARY_COMMAND_FAILURE",
                    "The request could not be completed due to a temporary system failure.",
                    ApplicationErrorCategory.Unavailable,
                    retryable: true),
                GetOrCreateCorrelationId(context));
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(
                CommercialIdentityRequestMiddleware.CorrelationItemKey,
                out var commercialValue) &&
            commercialValue is string commercialCorrelationId)
        {
            return commercialCorrelationId;
        }

        if (context.Items.TryGetValue(
                SpikeRequestContextMiddleware.CorrelationItemKey,
                out var value) &&
            value is string correlationId)
        {
            return correlationId;
        }

        correlationId = Guid.CreateVersion7().ToString("D");
        context.Items[SpikeRequestContextMiddleware.CorrelationItemKey] =
            correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        return correlationId;
    }
}

internal static class ApiProblemWriter
{
    public static Task WriteAsync(
        HttpContext context,
        ApplicationProblemException exception,
        string correlationId)
    {
        context.Response.StatusCode = exception.Category switch
        {
            ApplicationErrorCategory.Validation =>
                StatusCodes.Status400BadRequest,
            ApplicationErrorCategory.Authentication =>
                StatusCodes.Status401Unauthorized,
            ApplicationErrorCategory.Authorization =>
                StatusCodes.Status403Forbidden,
            ApplicationErrorCategory.NotFound =>
                StatusCodes.Status404NotFound,
            ApplicationErrorCategory.Conflict =>
                StatusCodes.Status409Conflict,
            ApplicationErrorCategory.TooManyRequests =>
                StatusCodes.Status429TooManyRequests,
            ApplicationErrorCategory.Unavailable =>
                StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError,
        };
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        return context.Response.WriteAsJsonAsync(
            new
            {
                error = new
                {
                    exception.Code,
                    exception.Message,
                    category = exception.Category.ToString(),
                    exception.Retryable,
                    exception.FieldErrors,
                    exception.Details,
                },
                meta = new
                {
                    correlationId,
                },
            });
    }
}
