using TraderPro.Application.Common.Errors;
using TraderPro.Application.Platform.CommandProbes;

namespace TraderPro.Api.Http;

/// <summary>
/// Establishes temporary Development/Testing request context for the spike.
/// The workspace header is not authentication.
/// </summary>
public sealed class SpikeRequestContextMiddleware(RequestDelegate next)
{
    public const string CorrelationItemKey =
        "TraderPro.Spike.CorrelationId";

    public async Task InvokeAsync(
        HttpContext context,
        ITemporaryWorkspaceContextResolver workspaceResolver)
    {
        if (!context.Request.Path.StartsWithSegments(
                "/api/v1/spikes",
                StringComparison.OrdinalIgnoreCase) &&
            !context.Request.Path.StartsWithSegments(
                "/api/v1/mobile/sync/events",
                StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var correlationId = ResolveCorrelationId(context);
        context.Items[CorrelationItemKey] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var workspaceValues =
            context.Request.Headers["X-TraderPro-Workspace-ID"];
        if (workspaceValues.Count == 0)
        {
            throw new ApplicationProblemException(
                "WORKSPACE_CONTEXT_REQUIRED",
                "X-TraderPro-Workspace-ID is required for this temporary spike.",
                ApplicationErrorCategory.Validation);
        }

        if (workspaceValues.Count != 1 ||
            !Guid.TryParseExact(workspaceValues[0], "D", out var workspaceId) ||
            workspaceId == Guid.Empty)
        {
            throw new ApplicationProblemException(
                "WORKSPACE_CONTEXT_INVALID",
                "X-TraderPro-Workspace-ID must be one non-empty UUID in D format.",
                ApplicationErrorCategory.Validation);
        }

        if (!await workspaceResolver.TryBindAsync(
                workspaceId,
                context.RequestAborted))
        {
            throw new ApplicationProblemException(
                "WORKSPACE_NOT_FOUND",
                "The requested workspace was not found.",
                ApplicationErrorCategory.NotFound);
        }

        await next(context);
    }

    public static string GetCorrelationId(HttpContext context)
    {
        return context.Items[CorrelationItemKey] as string ??
            throw new InvalidOperationException(
                "The spike request correlation context was not established.");
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var values = context.Request.Headers["X-Correlation-ID"];
        if (values.Count == 0)
        {
            return Guid.CreateVersion7().ToString("D");
        }

        if (values.Count != 1 ||
            values[0] is null ||
            values[0]!.Length != 36 ||
            !Guid.TryParseExact(values[0], "D", out var supplied) ||
            supplied == Guid.Empty)
        {
            throw new ApplicationProblemException(
                "CORRELATION_ID_INVALID",
                "X-Correlation-ID must be one non-empty UUID in D format.",
                ApplicationErrorCategory.Validation);
        }

        return supplied.ToString("D");
    }
}
