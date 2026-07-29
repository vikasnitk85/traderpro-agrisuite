using System.Globalization;
using System.Text.Json;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Procurement.Poc;

namespace TraderPro.Api.Http;

public static class ProcurementPocEndpointRegistration
{
    public static IEndpointRouteBuilder MapTraderProProcurementPocEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/v1/mobile/sync/operations",
            async (
                HttpContext context,
                IReceivingPocService service) =>
            {
                var request =
                    await ReadRequiredJsonBodyAsync<MobileSyncOperationsCommand>(
                        context);
                var result = await service.ProcessOperationsAsync(
                    request,
                    SpikeRequestContextMiddleware.GetCorrelationId(context),
                    context.RequestAborted);
                return Results.Json(
                    new ReadResponse<MobileSyncOperationsResult>(
                        result,
                        Meta(context)));
            });

        var sessions = endpoints.MapGroup(
            "/api/v1/spikes/procurement-poc/receiving-sessions");

        sessions.MapPost(
            "/{id}/heartbeat",
            async (
                string id,
                HttpContext context,
                IReceivingPocService service) =>
            {
                var sessionId = ParseSessionId(id);
                var body =
                    await ReadRequiredJsonBodyAsync<HeartbeatRequest>(context);
                var leaseId = ParseLeaseId(body.LeaseId);
                var result = await service.HeartbeatAsync(
                    new ReceivingPocHeartbeatCommand(sessionId, leaseId),
                    GetHeader(context, "Idempotency-Key"),
                    SpikeRequestContextMiddleware.GetCorrelationId(context),
                    context.RequestAborted);
                return CommandResponse(context, result);
            });

        sessions.MapPost(
            "/{id}/approve",
            async (
                string id,
                HttpContext context,
                IReceivingPocService service) =>
            {
                var result = await service.ApproveAsync(
                    new ReceivingPocVersionedCommand(
                        ParseSessionId(id),
                        GetExpectedVersion(context)),
                    GetHeader(context, "Idempotency-Key"),
                    SpikeRequestContextMiddleware.GetCorrelationId(context),
                    context.RequestAborted);
                return CommandResponse(context, result);
            });

        sessions.MapPost(
            "/{id}/finalize",
            async (
                string id,
                HttpContext context,
                IReceivingPocService service) =>
            {
                var result = await service.FinalizeAsync(
                    new ReceivingPocVersionedCommand(
                        ParseSessionId(id),
                        GetExpectedVersion(context)),
                    GetHeader(context, "Idempotency-Key"),
                    SpikeRequestContextMiddleware.GetCorrelationId(context),
                    context.RequestAborted);
                return CommandResponse(context, result);
            });

        sessions.MapGet(
            "/{id}/live-view",
            async (
                string id,
                HttpContext context,
                IReceivingPocService service) =>
            {
                var result = await service.FindLiveViewAsync(
                    ParseSessionId(id),
                    context.RequestAborted);
                if (result is null)
                {
                    throw new ApplicationProblemException(
                        "RECEIVING_POC_SESSION_NOT_FOUND",
                        "The POC Receiving Session was not found.",
                        ApplicationErrorCategory.NotFound);
                }

                return Results.Json(
                    new ReadResponse<ReceivingPocLiveView>(
                        result,
                        Meta(context)));
            });

        sessions.MapGet(
            "/",
            async (
                HttpContext context,
                IReceivingPocService service) =>
            {
                var after = GetNullableLongQuery(context, "after");
                var limit = GetLimit(context);
                var status = GetSingleQuery(context, "status");
                var result = await service.ListAsync(
                    status,
                    after,
                    limit,
                    context.RequestAborted);
                return Results.Json(
                    new ReadResponse<ReceivingPocListResult>(
                        result,
                        Meta(context)));
            });

        endpoints.MapPost(
            "/api/v1/spikes/procurement-poc/bootstrap",
            async (
                HttpContext context,
                IReceivingPocService service) =>
            {
                var result = await service.BootstrapAsync(
                    context.RequestAborted);
                return Results.Json(
                    new ReadResponse<ProcurementPocBootstrapResult>(
                        result,
                        Meta(context)));
            });

        return endpoints;
    }

    private static IResult CommandResponse(
        HttpContext context,
        IdempotentCommandResult<ReceivingPocCommandResult> result)
    {
        context.Items[SpikeRequestContextMiddleware.CorrelationItemKey] =
            result.CorrelationId;
        context.Response.Headers["X-Correlation-ID"] = result.CorrelationId;
        return Results.Json(
            new CommandEnvelope<ReceivingPocCommandResult>(
                result.Result,
                new CommandMeta(
                    result.CorrelationId,
                    result.IdempotencyStatus.ToString())),
            statusCode: result.StatusCode);
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

    private static ApplicationProblemException RequestBodyInvalid()
    {
        return new ApplicationProblemException(
            "REQUEST_BODY_INVALID",
            "A valid JSON request body is required.",
            ApplicationErrorCategory.Validation);
    }

    private static Guid ParseSessionId(string value)
    {
        if (!Guid.TryParseExact(value, "D", out var id) ||
            id == Guid.Empty ||
            id.Version != 7)
        {
            throw new ApplicationProblemException(
                "RECEIVING_POC_SESSION_ID_INVALID",
                "The POC session ID must be a canonical UUIDv7.",
                ApplicationErrorCategory.Validation);
        }

        return id;
    }

    private static Guid ParseLeaseId(string? value)
    {
        if (!Guid.TryParseExact(value, "D", out var id) ||
            id == Guid.Empty)
        {
            throw new ApplicationProblemException(
                "RECEIVING_POC_LEASE_INVALID",
                "leaseId must be one non-empty UUID in D format.",
                ApplicationErrorCategory.Validation);
        }

        return id;
    }

    private static long GetExpectedVersion(HttpContext context)
    {
        if (!long.TryParse(
                GetHeader(context, "X-Expected-Version"),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var version) ||
            version <= 0)
        {
            throw new ApplicationProblemException(
                "RECEIVING_POC_VERSION_CONFLICT",
                "X-Expected-Version must be a positive integer.",
                ApplicationErrorCategory.Validation);
        }

        return version;
    }

    private static long? GetNullableLongQuery(
        HttpContext context,
        string name)
    {
        var value = GetSingleQuery(context, name);
        if (value is null)
        {
            return null;
        }

        if (!long.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed) ||
            parsed < 0)
        {
            throw new ApplicationProblemException(
                "SYNC_OPERATION_BATCH_INVALID",
                "The list cursor must be non-negative.",
                ApplicationErrorCategory.Validation);
        }

        return parsed;
    }

    private static int GetLimit(HttpContext context)
    {
        var value = GetSingleQuery(context, "limit");
        if (value is null)
        {
            return 50;
        }

        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var parsed) ||
            parsed is < 1 or > 100)
        {
            throw new ApplicationProblemException(
                "SYNC_OPERATION_BATCH_INVALID",
                "Limit must be between 1 and 100.",
                ApplicationErrorCategory.Validation);
        }

        return parsed;
    }

    private static string? GetSingleQuery(HttpContext context, string name)
    {
        var values = context.Request.Query[name];
        if (values.Count == 0)
        {
            return null;
        }

        if (values.Count != 1)
        {
            throw new ApplicationProblemException(
                "SYNC_OPERATION_BATCH_INVALID",
                $"Query parameter {name} may be supplied only once.",
                ApplicationErrorCategory.Validation);
        }

        return values[0];
    }

    private static string GetHeader(HttpContext context, string name)
    {
        var values = context.Request.Headers[name];
        return values.Count == 1 ? values[0] ?? string.Empty : string.Empty;
    }

    private static ReadMeta Meta(HttpContext context)
    {
        return new ReadMeta(
            SpikeRequestContextMiddleware.GetCorrelationId(context));
    }

    public sealed record HeartbeatRequest(string? LeaseId);

    private sealed record CommandEnvelope<T>(T Result, CommandMeta Meta);

    private sealed record CommandMeta(
        string CorrelationId,
        string IdempotencyStatus);

    private sealed record ReadResponse<T>(T Result, ReadMeta Meta);

    private sealed record ReadMeta(string CorrelationId);
}
