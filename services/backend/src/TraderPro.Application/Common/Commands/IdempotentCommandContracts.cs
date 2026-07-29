namespace TraderPro.Application.Common.Commands;

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
