using System.Globalization;
using System.Text.Json;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Platform.Identity;
using TraderPro.Application.Procurement.Receiving;

namespace TraderPro.Api.Http;

public static class CommercialReceivingEndpointRegistration
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapTraderProCommercialReceivingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sync = endpoints.MapGroup("/api/v1/mobile/commercial-sync");
        sync.MapPost("/operations", async (HttpContext context, ICommercialReceivingService service) =>
        {
            var body = await ReadBodyAsync<CommercialMobileOperationsBody>(context);
            var operations = body.Operations?.Select(item => new CommercialMobileOperationCommand(
                item.OperationId, item.OperationType ?? string.Empty, item.SessionId, item.LocalSequence,
                item.OwnershipGeneration, item.ExpectedCloudVersion, item.PayloadJson ?? string.Empty,
                item.PayloadHash ?? string.Empty, item.Lease is null ? null : new(item.Lease.LeaseId))).ToArray() ?? [];
            var result = await service.ProcessOperationsAsync(new(operations), context.RequestAborted);
            return Results.Ok(new
            {
                operations = result.Operations.Select(item => new
                {
                    item.OperationId,
                    item.SessionId,
                    item.LocalSequence,
                    status = item.Status.ToString(),
                    item.Cloud,
                    item.Error,
                }),
                meta = Meta(context),
            });
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.Operator);
        sync.MapGet("/events", async (HttpContext context, ICommercialReceivingService service) =>
        {
            var result = await service.ReadEventsAsync(new(context.Request.Query["cursor"].FirstOrDefault(), Limit(context)), context.RequestAborted);
            return Results.Ok(new { result.Events, result.NextCursor, result.HasMore, meta = Meta(context) });
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.OperatorOrOwner);
        sync.MapGet("/masters", async (HttpContext context, ICommercialReceivingService service) =>
        {
            var result = await service.ReadMastersAsync(new(context.Request.Query["cursor"].FirstOrDefault(), Limit(context)), context.RequestAborted);
            return Results.Ok(new { result.Changes, result.NextCursor, result.BootstrapHighWaterSequence, result.HasMore, meta = Meta(context) });
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.OperatorOrOwner);

        var sessions = endpoints.MapGroup("/api/v1/procurement/receiving-sessions");
        sessions.MapGet("/", async (HttpContext context, ICommercialReceivingService service) =>
        {
            var result = await service.ListAsync(new(context.Request.Query["status"].FirstOrDefault(), context.Request.Query["search"].FirstOrDefault(), context.Request.Query["cursor"].FirstOrDefault(), Limit(context)), context.RequestAborted);
            return Results.Ok(new { result.Items, result.NextCursor, result.HasMore, meta = Meta(context) });
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.OperatorOrOwner);
        sessions.MapGet("/{id:guid}/live-view", async (Guid id, HttpContext context, ICommercialReceivingService service) =>
        {
            var result = await service.GetLiveViewAsync(id, context.RequestAborted);
            return Results.Ok(new { result, meta = Meta(context) });
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.OperatorOrOwner);
        sessions.MapPost("/{id:guid}/lease/heartbeat", async (Guid id, HttpContext context, ICommercialReceivingService service) =>
        {
            var body = await ReadBodyAsync<LeaseBody>(context);
            var result = await service.HeartbeatAsync(new(id, body.OwnershipGeneration, body.LeaseId), Header(context, "Idempotency-Key"), context.RequestAborted);
            return Command(context, result);
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.Operator);
        sessions.MapPost("/{id:guid}/lease/reacquire", async (Guid id, HttpContext context, ICommercialReceivingService service) =>
        {
            var body = await ReadBodyAsync<ReacquireBody>(context);
            var result = await service.ReacquireAsync(new(id, body.OwnershipGeneration, null), Header(context, "Idempotency-Key"), context.RequestAborted);
            return Command(context, result);
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.Operator);
        sessions.MapPost("/{id:guid}/ownership/transfer", async (Guid id, HttpContext context, ICommercialReceivingService service) =>
        {
            var body = await ReadBodyAsync<TransferBody>(context);
            var result = await service.TransferAsync(new(id, body.TargetDeviceId, ExpectedVersion(context, "RECEIVING_OWNERSHIP_TRANSFER_CONFLICT"), body.ExpectedOwnershipGeneration, body.Reason ?? string.Empty), Header(context, "Idempotency-Key"), context.RequestAborted);
            return Command(context, result);
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.Owner);

        var policy = endpoints.MapGroup("/api/v1/procurement/receiving-reference-policy");
        policy.MapGet("/", async (HttpContext context, ICommercialReceivingService service) =>
        {
            var result = await service.GetReferencePolicyAsync(context.RequestAborted);
            return Results.Ok(new { result, meta = Meta(context) });
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.Owner);
        policy.MapPut("/", async (HttpContext context, ICommercialReceivingService service) =>
        {
            var body = await ReadBodyAsync<ReferencePolicyBody>(context);
            var result = await service.UpdateReferencePolicyAsync(new(body.FormatTemplate ?? string.Empty, body.ResetPolicy ?? string.Empty, body.StartingNumber, ExpectedVersion(context, "RECEIVING_REFERENCE_CONFLICT")), Header(context, "Idempotency-Key"), context.RequestAborted);
            return Command(context, result);
        }).RequireTraderProCommercialAuthorization(TraderProAuthorizationPolicies.Owner);
        return endpoints;
    }

    private static IResult Command<T>(HttpContext context, IdempotentCommandResult<T> result) => Results.Json(new { result = result.Result, meta = new { correlationId = result.CorrelationId, idempotencyStatus = result.IdempotencyStatus.ToString() } }, statusCode: result.StatusCode);
    private static object Meta(HttpContext context) => new { correlationId = CommercialIdentityRequestMiddleware.GetCorrelationId(context) };
    private static string Header(HttpContext context, string name) => context.Request.Headers[name].FirstOrDefault() ?? string.Empty;
    private static int Limit(HttpContext context)
    {
        var value = context.Request.Query["limit"].FirstOrDefault();
        return value is null ? 50 : int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }
    private static long ExpectedVersion(HttpContext context, string errorCode)
    {
        var value = Header(context, "X-Expected-Version");
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
            throw new ApplicationProblemException(errorCode, "A positive X-Expected-Version is required.", ApplicationErrorCategory.Validation);
        return parsed;
    }
    private static async Task<T> ReadBodyAsync<T>(HttpContext context)
    {
        try { return await JsonSerializer.DeserializeAsync<T>(context.Request.Body, JsonOptions, context.RequestAborted) ?? throw new JsonException(); }
        catch (JsonException exception) { throw new ApplicationProblemException("REQUEST_BODY_INVALID", "The JSON request body is invalid.", ApplicationErrorCategory.Validation, innerException: exception); }
    }

    private sealed record CommercialMobileOperationsBody(IReadOnlyList<CommercialMobileOperationBody>? Operations);
    private sealed record CommercialMobileOperationBody(Guid OperationId, string? OperationType, Guid SessionId, long LocalSequence, long? OwnershipGeneration, long? ExpectedCloudVersion, string? PayloadJson, string? PayloadHash, LeaseMetadataBody? Lease);
    private sealed record LeaseMetadataBody(Guid LeaseId);
    private sealed record LeaseBody(long OwnershipGeneration, Guid? LeaseId);
    private sealed record ReacquireBody(long OwnershipGeneration);
    private sealed record TransferBody(Guid TargetDeviceId, long ExpectedOwnershipGeneration, string? Reason);
    private sealed record ReferencePolicyBody(string? FormatTemplate, string? ResetPolicy, long StartingNumber);
}
