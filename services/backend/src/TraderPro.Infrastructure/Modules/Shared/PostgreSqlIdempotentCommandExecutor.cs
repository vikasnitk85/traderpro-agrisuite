using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Time;
using TraderPro.Domain.Platform;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Shared;

internal enum OutboxCommitOrdering
{
    MobileSyncCursor,
    IndependentInternal,
}

/// <summary>
/// Shared Task 5/Task 6A database-backed idempotent command transaction.
/// Correctness is provided by PostgreSQL transaction locks and constraints,
/// never a process-local lock.
/// </summary>
internal sealed class PostgreSqlIdempotentCommandExecutor(
    TraderProDbContext dbContext,
    IClock clock)
{
    private const string EventCommitOrderLockScope =
        "TraderPro.OutboxSequence.CommitOrder.v1";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<IdempotentCommandResult<T>> ExecuteAsync<T>(
        Guid workspaceId,
        string commandType,
        string idempotencyKey,
        string requestHash,
        string correlationId,
        int resultStatusCode,
        Func<Task<T>> execute,
        Func<T, bool> isReplayable,
        Func<Task<ApplicationProblemException>>? concurrencyProblem,
        CancellationToken cancellationToken)
        where T : class
    {
        return await ExecuteAsync(
            workspaceId,
            commandType,
            idempotencyKey,
            requestHash,
            correlationId,
            resultStatusCode,
            execute,
            isReplayable,
            concurrencyProblem,
            persistenceProblem: null,
            cancellationToken);
    }

    public async Task<IdempotentCommandResult<T>> ExecuteAsync<T>(
        Guid workspaceId,
        string commandType,
        string idempotencyKey,
        string requestHash,
        string correlationId,
        int resultStatusCode,
        Func<Task<T>> execute,
        Func<T, bool> isReplayable,
        Func<Task<ApplicationProblemException>>? concurrencyProblem,
        Func<Exception, ApplicationProblemException?>? persistenceProblem,
        CancellationToken cancellationToken)
        where T : class
    {
        return await ExecuteAsync(
            workspaceId,
            commandType,
            idempotencyKey,
            requestHash,
            correlationId,
            resultStatusCode,
            execute,
            isReplayable,
            concurrencyProblem,
            persistenceProblem,
            OutboxCommitOrdering.MobileSyncCursor,
            cancellationToken);
    }

    public async Task<IdempotentCommandResult<T>> ExecuteAsync<T>(
        Guid workspaceId,
        string commandType,
        string idempotencyKey,
        string requestHash,
        string correlationId,
        int resultStatusCode,
        Func<Task<T>> execute,
        Func<T, bool> isReplayable,
        Func<Task<ApplicationProblemException>>? concurrencyProblem,
        Func<Exception, ApplicationProblemException?>? persistenceProblem,
        OutboxCommitOrdering outboxCommitOrdering,
        CancellationToken cancellationToken)
        where T : class
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            var lockScope =
                $"{workspaceId:D}\n{commandType}\n{idempotencyKey}";
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockScope}, 0))",
                cancellationToken);

            var existing = await dbContext.IdempotencyRecords.SingleOrDefaultAsync(
                record =>
                    record.CommandType == commandType &&
                    record.IdempotencyKey == idempotencyKey,
                cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(
                        existing.RequestHash,
                        requestHash,
                        StringComparison.Ordinal))
                {
                    throw new ApplicationProblemException(
                        "IDEMPOTENCY_PAYLOAD_CONFLICT",
                        "The idempotency key was already used for another request.",
                        ApplicationErrorCategory.Conflict);
                }

                if (existing.Status == IdempotencyRecordStatus.Failed)
                {
                    throw PreviousAttemptFailed();
                }

                if (existing.Status == IdempotencyRecordStatus.Pending)
                {
                    throw new ApplicationProblemException(
                        "IDEMPOTENCY_IN_PROGRESS",
                        "The command result is not yet available.",
                        ApplicationErrorCategory.Conflict,
                        retryable: true);
                }

                if (existing.Status != IdempotencyRecordStatus.Completed ||
                    existing.ResultPayloadJson is null ||
                    existing.ResultStatusCode is null)
                {
                    throw PreviousAttemptFailed();
                }

                var stored = ReadStoredResult(
                    existing.ResultPayloadJson,
                    isReplayable);
                await transaction.CommitAsync(cancellationToken);
                return new IdempotentCommandResult<T>(
                    stored.Result!,
                    stored.CorrelationId!,
                    IdempotencyExecutionStatus.PreviouslyProcessed,
                    existing.ResultStatusCode.Value);
            }

            if (outboxCommitOrdering is
                OutboxCommitOrdering.MobileSyncCursor)
            {
                // MobileSync cursor writers share this transaction lock so
                // sequence visibility stays ordered with commit. Internal
                // events do not participate in that cursor contract.
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({EventCommitOrderLockScope}, 0))",
                    cancellationToken);
            }

            var idempotencyRecord = IdempotencyRecord.Create(
                workspaceId,
                idempotencyKey,
                commandType,
                requestHash,
                clock.UtcNow);
            dbContext.IdempotencyRecords.Add(idempotencyRecord);

            var result = await execute();
            var storedPayload = JsonSerializer.Serialize(
                new StoredCommandResult<T>(result, correlationId),
                JsonOptions);
            idempotencyRecord.Complete(
                storedPayload,
                resultStatusCode,
                clock.UtcNow);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new IdempotentCommandResult<T>(
                result,
                correlationId,
                IdempotencyExecutionStatus.Processed,
                resultStatusCode);
        }
        catch (DbUpdateConcurrencyException)
            when (concurrencyProblem is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            throw await concurrencyProblem();
        }
        catch (ApplicationProblemException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            throw;
        }
        catch (Exception exception)
            when (exception is DbUpdateException or PostgresException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var mapped = persistenceProblem?.Invoke(exception);
            if (mapped is not null)
            {
                throw mapped;
            }

            throw TemporaryFailure(exception);
        }
    }

    private static StoredCommandResult<T> ReadStoredResult<T>(
        string payload,
        Func<T, bool> isReplayable)
        where T : class
    {
        try
        {
            var stored = JsonSerializer.Deserialize<StoredCommandResult<T>>(
                payload,
                JsonOptions);
            if (stored?.Result is null ||
                !isReplayable(stored.Result) ||
                stored.CorrelationId is null ||
                !Guid.TryParseExact(
                    stored.CorrelationId,
                    "D",
                    out var correlationId) ||
                correlationId == Guid.Empty)
            {
                throw PreviousAttemptFailed();
            }

            return stored with
            {
                CorrelationId = correlationId.ToString("D"),
            };
        }
        catch (JsonException)
        {
            throw PreviousAttemptFailed();
        }
    }

    private static ApplicationProblemException PreviousAttemptFailed()
    {
        return new ApplicationProblemException(
            "IDEMPOTENCY_PREVIOUS_ATTEMPT_FAILED",
            "A previous attempt with this idempotency key did not produce a replayable result. Use a new idempotency key.",
            ApplicationErrorCategory.Conflict);
    }

    private static ApplicationProblemException TemporaryFailure(
        Exception exception)
    {
        return new ApplicationProblemException(
            "TEMPORARY_COMMAND_FAILURE",
            "The command could not be completed due to a temporary system failure.",
            ApplicationErrorCategory.Unavailable,
            retryable: true,
            innerException: exception);
    }

    private sealed record StoredCommandResult<T>(
        T? Result,
        string? CorrelationId)
        where T : class;
}
