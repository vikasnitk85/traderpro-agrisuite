using TraderPro.Application.Common.Errors;
using TraderPro.Application.Platform.CommandProbes;
using TraderPro.Application.Procurement.Poc;

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
        ITemporaryWorkspaceContextResolver workspaceResolver,
        ITemporaryDeviceContextResolver deviceResolver,
        IConfiguration configuration)
    {
        if (!context.Request.Path.StartsWithSegments(
                "/api/v1/spikes",
                StringComparison.OrdinalIgnoreCase) &&
            !context.Request.Path.StartsWithSegments(
                "/api/v1/mobile/sync/events",
                StringComparison.OrdinalIgnoreCase) &&
            !context.Request.Path.StartsWithSegments(
                "/api/v1/mobile/sync/operations",
                StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var procurementPocEnabled =
            configuration.GetValue<bool>(
                "TraderPro:Spikes:ProcurementPoc:Enabled");
        var isProcurementPocRoute =
            context.Request.Path.StartsWithSegments(
                "/api/v1/spikes/procurement-poc",
                StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.StartsWithSegments(
                "/api/v1/mobile/sync/operations",
                StringComparison.OrdinalIgnoreCase);
        if (isProcurementPocRoute && !procurementPocEnabled)
        {
            await next(context);
            return;
        }

        var correlationId = ResolveCorrelationId(context);
        context.Items[CorrelationItemKey] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var isBootstrap = context.Request.Path.Equals(
            "/api/v1/spikes/procurement-poc/bootstrap",
            StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.Equals(
                "/api/v1/spikes/procurement-poc/bootstrap/",
                StringComparison.OrdinalIgnoreCase);
        if (isBootstrap)
        {
            await next(context);
            return;
        }

        var requiresDeviceContext = procurementPocEnabled &&
            (isProcurementPocRoute ||
             context.Request.Path.StartsWithSegments(
                 "/api/v1/mobile/sync/events",
                 StringComparison.OrdinalIgnoreCase));
        if (requiresDeviceContext)
        {
            var workspaceHeader =
                context.Request.Headers["X-TraderPro-Workspace-ID"];
            var deviceHeader =
                context.Request.Headers["X-TraderPro-Device-ID"];
            if (workspaceHeader.Count == 0 || deviceHeader.Count == 0)
            {
                throw new ApplicationProblemException(
                    "DEVICE_CONTEXT_REQUIRED",
                    "Both temporary workspace and device headers are required.",
                    ApplicationErrorCategory.Validation);
            }

            if (workspaceHeader.Count != 1 ||
                deviceHeader.Count != 1 ||
                !Guid.TryParseExact(
                    workspaceHeader[0],
                    "D",
                    out var procurementWorkspaceId) ||
                procurementWorkspaceId == Guid.Empty ||
                !Guid.TryParseExact(
                    deviceHeader[0],
                    "D",
                    out var deviceId) ||
                deviceId == Guid.Empty)
            {
                throw new ApplicationProblemException(
                    "DEVICE_CONTEXT_INVALID",
                    "The temporary workspace and device headers must be canonical non-empty UUIDs.",
                    ApplicationErrorCategory.Validation);
            }

            await deviceResolver.BindAsync(
                procurementWorkspaceId,
                deviceId,
                context.RequestAborted);
            await next(context);
            return;
        }

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
