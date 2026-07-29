namespace TraderPro.Application.Platform.CommandProbes;

public interface ICommandProbeCommandExecutor
{
    Task<IdempotentCommandResult<CommandProbeResult>> CreateAsync(
        CreateCommandProbe command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<CommandProbeResult>> IncrementAsync(
        IncrementCommandProbe command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken);
}

public interface ICommandProbeReader
{
    Task<CommandProbeResult?> FindAsync(
        Guid id,
        CancellationToken cancellationToken);
}

public interface ICloudEventCursorReader
{
    Task<CloudEventCursorResult> ReadEventsAsync(
        long after,
        int limit,
        CancellationToken cancellationToken);
}

/// <summary>
/// Temporary development/testing workspace binding for the spike.
/// This is not authentication or authorization.
/// </summary>
public interface ITemporaryWorkspaceContextResolver
{
    Task<bool> TryBindAsync(
        Guid workspaceId,
        CancellationToken cancellationToken);
}
