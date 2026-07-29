using System.Text.Json;

namespace TraderPro.Application.Platform.CommandProbes;

public static class CommandProbeCommandTypes
{
    public const string Create = "Platform.CommandProbe.Create";
    public const string Increment = "Platform.CommandProbe.Increment";
}

public sealed record CreateCommandProbe(string Name);

public sealed record IncrementCommandProbe(
    Guid Id,
    int Delta,
    long ExpectedVersion);

public sealed record CommandProbeResult(
    Guid Id,
    string Name,
    long Counter,
    long Version);

public enum IdempotencyExecutionStatus
{
    Processed,
    PreviouslyProcessed,
}

public sealed record IdempotentCommandResult<T>(
    T Result,
    string CorrelationId,
    IdempotencyExecutionStatus IdempotencyStatus,
    int StatusCode);

public sealed record CloudEventCursorItem(
    long Sequence,
    Guid EventId,
    string EventType,
    int EventVersion,
    string AggregateType,
    Guid AggregateId,
    long AggregateVersion,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    JsonElement Payload);

public sealed record CloudEventCursorResult(
    IReadOnlyList<CloudEventCursorItem> Events,
    long NextCursor,
    bool HasMore);
