using System.Globalization;
using System.Text.Json;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Platform.CommandProbes;

namespace TraderPro.Api.Http;

public static class SpikeEndpointRegistration
{
    public static IEndpointRouteBuilder MapTraderProSpikeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var probes = endpoints.MapGroup(
            "/api/v1/spikes/command-probes");

        probes.MapPost(
            "/",
            async (
                HttpContext context,
                CreateCommandProbeHandler handler) =>
            {
                var request =
                    await ReadRequiredJsonBodyAsync<CreateCommandProbeRequest>(
                        context);
                var result = await handler.HandleAsync(
                    new CreateCommandProbe(request.Name ?? string.Empty),
                    GetHeader(context, "Idempotency-Key"),
                    SpikeRequestContextMiddleware.GetCorrelationId(context),
                    context.RequestAborted);
                SetCommittedCorrelation(context, result.CorrelationId);
                return Results.Json(
                    new CommandResponse<CommandProbeResult>(
                        result.Result,
                        new CommandResponseMeta(
                            result.CorrelationId,
                            result.IdempotencyStatus.ToString())),
                    statusCode: result.StatusCode);
            });

        probes.MapPost(
            "/{id:guid}/increment",
            async (
                Guid id,
                HttpContext context,
                IncrementCommandProbeHandler handler) =>
            {
                var request =
                    await ReadRequiredJsonBodyAsync<IncrementCommandProbeRequest>(
                        context);
                var expectedVersion = GetExpectedVersion(context);
                var result = await handler.HandleAsync(
                    new IncrementCommandProbe(
                        id,
                        request.Delta,
                        expectedVersion),
                    GetHeader(context, "Idempotency-Key"),
                    SpikeRequestContextMiddleware.GetCorrelationId(context),
                    context.RequestAborted);
                SetCommittedCorrelation(context, result.CorrelationId);
                return Results.Json(
                    new CommandResponse<CommandProbeResult>(
                        result.Result,
                        new CommandResponseMeta(
                            result.CorrelationId,
                            result.IdempotencyStatus.ToString())),
                    statusCode: result.StatusCode);
            });

        probes.MapGet(
            "/{id:guid}",
            async (
                Guid id,
                HttpContext context,
                ICommandProbeReader reader) =>
            {
                var result = await reader.FindAsync(
                    id,
                    context.RequestAborted);
                if (result is null)
                {
                    throw new ApplicationProblemException(
                        "COMMAND_PROBE_NOT_FOUND",
                        "The command probe was not found.",
                        ApplicationErrorCategory.NotFound);
                }

                return Results.Json(
                    new ReadResponse<CommandProbeResult>(
                        result,
                        new ReadResponseMeta(
                            SpikeRequestContextMiddleware.GetCorrelationId(
                                context))));
            });

        endpoints.MapGet(
            "/api/v1/mobile/sync/events",
            async (HttpContext context, ICloudEventCursorReader reader) =>
            {
                var after = GetLongQuery(context, "after", 0);
                var limitValue = GetLongQuery(context, "limit", 50);
                if (limitValue is < int.MinValue or > int.MaxValue)
                {
                    throw CursorValidation(
                        "EVENT_CURSOR_LIMIT_INVALID",
                        "Limit must be between 1 and 100.");
                }

                var result = await reader.ReadEventsAsync(
                    after,
                    (int)limitValue,
                    context.RequestAborted);
                return Results.Json(result);
            });

        return endpoints;
    }

    private static async Task<TRequest> ReadRequiredJsonBodyAsync<TRequest>(
        HttpContext context)
    {
        try
        {
            var request = await context.Request.ReadFromJsonAsync<TRequest>(
                cancellationToken: context.RequestAborted);
            return request ??
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

    private static string GetHeader(HttpContext context, string name)
    {
        var values = context.Request.Headers[name];
        return values.Count == 1 ? values[0] ?? string.Empty : string.Empty;
    }

    private static long GetExpectedVersion(HttpContext context)
    {
        var value = GetHeader(context, "X-Expected-Version");
        if (!long.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var version))
        {
            throw new ApplicationProblemException(
                "COMMAND_PROBE_EXPECTED_VERSION_REQUIRED",
                "X-Expected-Version must be a positive integer.",
                ApplicationErrorCategory.Validation);
        }

        return version;
    }

    private static long GetLongQuery(
        HttpContext context,
        string name,
        long defaultValue)
    {
        var values = context.Request.Query[name];
        if (values.Count == 0)
        {
            return defaultValue;
        }

        if (values.Count != 1 ||
            !long.TryParse(
                values[0],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            throw CursorValidation(
                name == "after"
                    ? "EVENT_CURSOR_AFTER_INVALID"
                    : "EVENT_CURSOR_LIMIT_INVALID",
                name == "after"
                    ? "The after cursor must be non-negative."
                    : "Limit must be between 1 and 100.");
        }

        return parsed;
    }

    private static ApplicationProblemException CursorValidation(
        string code,
        string message)
    {
        return new ApplicationProblemException(
            code,
            message,
            ApplicationErrorCategory.Validation);
    }

    private static void SetCommittedCorrelation(
        HttpContext context,
        string correlationId)
    {
        context.Items[SpikeRequestContextMiddleware.CorrelationItemKey] =
            correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
    }

    public sealed record CreateCommandProbeRequest(string? Name);

    public sealed record IncrementCommandProbeRequest(int Delta);

    private sealed record CommandResponse<T>(
        T Result,
        CommandResponseMeta Meta);

    private sealed record CommandResponseMeta(
        string CorrelationId,
        string IdempotencyStatus);

    private sealed record ReadResponse<T>(T Result, ReadResponseMeta Meta);

    private sealed record ReadResponseMeta(string CorrelationId);
}
