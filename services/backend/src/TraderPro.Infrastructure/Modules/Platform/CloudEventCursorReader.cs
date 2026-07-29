using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Tenancy;
using TraderPro.Application.Platform.CommandProbes;
using TraderPro.Domain.Platform;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Platform;

internal sealed class CloudEventCursorReader(
    TraderProDbContext dbContext,
    ICurrentWorkspaceAccessor currentWorkspace) : ICloudEventCursorReader
{
    public async Task<CloudEventCursorResult> ReadEventsAsync(
        long after,
        int limit,
        CancellationToken cancellationToken)
    {
        RequireWorkspace();

        if (after < 0)
        {
            throw new ApplicationProblemException(
                "EVENT_CURSOR_AFTER_INVALID",
                "The after cursor must be non-negative.",
                ApplicationErrorCategory.Validation);
        }

        if (limit is < 1 or > 100)
        {
            throw new ApplicationProblemException(
                "EVENT_CURSOR_LIMIT_INVALID",
                "Limit must be between 1 and 100.",
                ApplicationErrorCategory.Validation);
        }

        var rows = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message =>
                message.EventStream == OutboxEventStream.MobileSync &&
                message.Sequence > after)
            .OrderBy(message => message.Sequence)
            .Take(limit + 1)
            .Select(message => new
            {
                message.Sequence,
                EventId = message.Id,
                message.EventType,
                message.EventVersion,
                message.AggregateType,
                message.AggregateId,
                message.AggregateVersion,
                message.OccurredAtUtc,
                message.CorrelationId,
                message.PayloadJson,
            })
            .ToListAsync(cancellationToken);
        var hasMore = rows.Count > limit;
        var events = rows
            .Take(limit)
            .Select(row =>
            {
                using var document = JsonDocument.Parse(row.PayloadJson);
                return new CloudEventCursorItem(
                    row.Sequence,
                    row.EventId,
                    row.EventType,
                    row.EventVersion,
                    row.AggregateType,
                    row.AggregateId,
                    row.AggregateVersion,
                    row.OccurredAtUtc,
                    row.CorrelationId,
                    document.RootElement.Clone());
            })
            .ToArray();
        var nextCursor = events.Length == 0
            ? after
            : events[^1].Sequence;

        return new CloudEventCursorResult(events, nextCursor, hasMore);
    }

    private void RequireWorkspace()
    {
        if (currentWorkspace.WorkspaceId is null)
        {
            throw new ApplicationProblemException(
                "WORKSPACE_CONTEXT_REQUIRED",
                "A current workspace is required.",
                ApplicationErrorCategory.Validation);
        }
    }
}
