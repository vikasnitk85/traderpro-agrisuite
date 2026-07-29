using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class OutboxMessage :
    IWorkspaceScoped,
    IOccurredAtUtc
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(
        Guid workspaceId,
        string eventType,
        int eventVersion,
        string aggregateType,
        Guid aggregateId,
        long aggregateVersion,
        string payloadJson,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        OutboxEventStream eventStream)
    {
        if (eventVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventVersion),
                eventVersion,
                "Event version must be greater than zero.");
        }

        if (aggregateVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(aggregateVersion),
                aggregateVersion,
                "Aggregate version must be greater than zero.");
        }

        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        EventStream = PlatformEntityGuard.Defined(
            eventStream,
            nameof(eventStream));
        EventType = PlatformEntityGuard.Required(
            eventType,
            200,
            nameof(eventType));
        EventVersion = eventVersion;
        AggregateType = PlatformEntityGuard.Required(
            aggregateType,
            200,
            nameof(aggregateType));
        AggregateId = PlatformEntityGuard.RequiredId(
            aggregateId,
            nameof(aggregateId));
        AggregateVersion = aggregateVersion;
        PayloadJson = PlatformEntityGuard.Json(payloadJson, nameof(payloadJson));
        CorrelationId = PlatformEntityGuard.Required(
            correlationId,
            100,
            nameof(correlationId));
        OccurredAtUtc = PlatformEntityGuard.Utc(
            occurredAtUtc,
            nameof(occurredAtUtc));
        Status = OutboxMessageStatus.Pending;
    }

    public Guid Id { get; private set; }

    public long Sequence { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public OutboxEventStream EventStream { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public int EventVersion { get; private set; }

    public string AggregateType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public long AggregateVersion { get; private set; }

    public string PayloadJson { get; private set; } = string.Empty;

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public OutboxMessageStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage Create(
        Guid workspaceId,
        string eventType,
        int eventVersion,
        string aggregateType,
        Guid aggregateId,
        long aggregateVersion,
        string payloadJson,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        OutboxEventStream eventStream = OutboxEventStream.Internal)
    {
        return new OutboxMessage(
            workspaceId,
            eventType,
            eventVersion,
            aggregateType,
            aggregateId,
            aggregateVersion,
            payloadJson,
            correlationId,
            occurredAtUtc,
            eventStream);
    }

    public void StartProcessing()
    {
        if (Status is not OutboxMessageStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending outbox message can start processing.");
        }

        Status = OutboxMessageStatus.Processing;
        LastError = null;
        NextAttemptAtUtc = null;
    }

    public void RecordFailure(
        string error,
        DateTimeOffset? nextAttemptAtUtc)
    {
        if (Status is not OutboxMessageStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only a processing outbox message can record a failure.");
        }

        AttemptCount++;
        LastError = PlatformEntityGuard.Required(
            error,
            4000,
            nameof(error));
        NextAttemptAtUtc = PlatformEntityGuard.OptionalUtc(
            nextAttemptAtUtc,
            nameof(nextAttemptAtUtc));
        Status = nextAttemptAtUtc is null
            ? OutboxMessageStatus.Failed
            : OutboxMessageStatus.Pending;
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        if (Status is not OutboxMessageStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only a processing outbox message can be marked processed.");
        }

        AttemptCount++;
        ProcessedAtUtc = PlatformEntityGuard.Utc(
            processedAtUtc,
            nameof(processedAtUtc));
        NextAttemptAtUtc = null;
        LastError = null;
        Status = OutboxMessageStatus.Processed;
    }
}
