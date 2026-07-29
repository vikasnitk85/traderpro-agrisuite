using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Tenancy;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.CommandProbes;
using TraderPro.Domain.Platform;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Platform;

internal sealed class CommandProbeService(
    TraderProDbContext dbContext,
    ICurrentWorkspaceAccessor currentWorkspace,
    IClock clock) :
    ICommandProbeCommandExecutor,
    ICommandProbeReader
{
    private const string AggregateType = "CommandProbe";
    private const int EventVersion = 1;
    private const int CreateStatusCode = 201;
    private const int IncrementStatusCode = 200;
    private const string EventCommitOrderLockScope =
        "TraderPro.OutboxSequence.CommitOrder.v1";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<IdempotentCommandResult<CommandProbeResult>> CreateAsync(
        CreateCommandProbe command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var workspaceId = RequireWorkspace();
        var requestHash = CommandProbeRequestHash.ForCreate(command.Name);

        return ExecuteIdempotentlyAsync(
            workspaceId,
            CommandProbeCommandTypes.Create,
            idempotencyKey,
            requestHash,
            correlationId,
            CreateStatusCode,
            () =>
            {
                var occurredAtUtc = clock.UtcNow;
                var probe = CommandProbe.Create(
                    workspaceId,
                    command.Name,
                    occurredAtUtc);
                var result = ToResult(probe);
                var payload = JsonSerializer.Serialize(
                    new
                    {
                        probeId = probe.Id,
                        probe.Name,
                        probe.Counter,
                        probe.Version,
                    },
                    JsonOptions);
                var afterSnapshot = JsonSerializer.Serialize(result, JsonOptions);

                dbContext.CommandProbes.Add(probe);
                dbContext.AuditEvents.Add(
                    AuditEvent.Create(
                        workspaceId,
                        null,
                        null,
                        null,
                        null,
                        "Platform.CommandProbe.Created",
                        AggregateType,
                        probe.Id,
                        null,
                        null,
                        null,
                        afterSnapshot,
                        correlationId,
                        occurredAtUtc));
                dbContext.OutboxMessages.Add(
                    OutboxMessage.Create(
                        workspaceId,
                        "CommandProbeCreated",
                        EventVersion,
                        AggregateType,
                        probe.Id,
                        probe.Version,
                        payload,
                        correlationId,
                        occurredAtUtc,
                        OutboxEventStream.MobileSync));

                return Task.FromResult(result);
            },
            null,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<CommandProbeResult>> IncrementAsync(
        IncrementCommandProbe command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var workspaceId = RequireWorkspace();
        var requestHash = CommandProbeRequestHash.ForIncrement(
            command.Id,
            command.ExpectedVersion,
            command.Delta);

        return ExecuteIdempotentlyAsync(
            workspaceId,
            CommandProbeCommandTypes.Increment,
            idempotencyKey,
            requestHash,
            correlationId,
            IncrementStatusCode,
            async () =>
            {
                var probe = await dbContext.CommandProbes.SingleOrDefaultAsync(
                    candidate => candidate.Id == command.Id,
                    cancellationToken);
                if (probe is null)
                {
                    throw NotFound();
                }

                if (probe.Version != command.ExpectedVersion)
                {
                    throw VersionConflict(
                        command.ExpectedVersion,
                        probe.Version);
                }

                var occurredAtUtc = clock.UtcNow;
                var beforeSnapshot = JsonSerializer.Serialize(
                    ToResult(probe),
                    JsonOptions);
                probe.Increment(command.Delta);
                var nextVersion = checked(probe.Version + 1);
                var result = new CommandProbeResult(
                    probe.Id,
                    probe.Name,
                    probe.Counter,
                    nextVersion);
                var payload = JsonSerializer.Serialize(
                    new
                    {
                        probeId = probe.Id,
                        command.Delta,
                        probe.Counter,
                        version = nextVersion,
                    },
                    JsonOptions);
                var afterSnapshot = JsonSerializer.Serialize(result, JsonOptions);

                dbContext.AuditEvents.Add(
                    AuditEvent.Create(
                        workspaceId,
                        null,
                        null,
                        null,
                        null,
                        "Platform.CommandProbe.Incremented",
                        AggregateType,
                        probe.Id,
                        null,
                        null,
                        beforeSnapshot,
                        afterSnapshot,
                        correlationId,
                        occurredAtUtc));
                dbContext.OutboxMessages.Add(
                    OutboxMessage.Create(
                        workspaceId,
                        "CommandProbeIncremented",
                        EventVersion,
                        AggregateType,
                        probe.Id,
                        nextVersion,
                        payload,
                        correlationId,
                        occurredAtUtc,
                        OutboxEventStream.MobileSync));

                return result;
            },
            async () =>
            {
                dbContext.ChangeTracker.Clear();
                var currentVersion = await dbContext.CommandProbes
                    .AsNoTracking()
                    .Where(candidate => candidate.Id == command.Id)
                    .Select(candidate => (long?)candidate.Version)
                    .SingleOrDefaultAsync(cancellationToken);
                return currentVersion is null
                    ? NotFound()
                    : VersionConflict(
                        command.ExpectedVersion,
                        currentVersion.Value);
            },
            cancellationToken);
    }

    public async Task<CommandProbeResult?> FindAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireWorkspace();

        return await dbContext.CommandProbes
            .AsNoTracking()
            .Where(probe => probe.Id == id)
            .Select(probe => new CommandProbeResult(
                probe.Id,
                probe.Name,
                probe.Counter,
                probe.Version))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<IdempotentCommandResult<CommandProbeResult>>
        ExecuteIdempotentlyAsync(
            Guid workspaceId,
            string commandType,
            string idempotencyKey,
            string requestHash,
            string correlationId,
            int resultStatusCode,
            Func<Task<CommandProbeResult>> execute,
            Func<Task<ApplicationProblemException>>? concurrencyProblem,
            CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

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

                var stored = ReadStoredCommandResult(
                    existing.ResultPayloadJson);
                await transaction.CommitAsync(cancellationToken);
                return new IdempotentCommandResult<CommandProbeResult>(
                    stored.Result!,
                    stored.CorrelationId!,
                    IdempotencyExecutionStatus.PreviouslyProcessed,
                    existing.ResultStatusCode.Value);
            }

            // Sequence allocation must stay ordered with transaction commit.
            // Holding this database lock through commit prevents a later
            // sequence from becoming cursor-visible before an earlier one.
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({EventCommitOrderLockScope}, 0))",
                cancellationToken);

            var idempotencyRecord = IdempotencyRecord.Create(
                workspaceId,
                idempotencyKey,
                commandType,
                requestHash,
                clock.UtcNow);
            dbContext.IdempotencyRecords.Add(idempotencyRecord);

            var result = await execute();
            var storedPayload = JsonSerializer.Serialize(
                new StoredCommandResult(result, correlationId),
                JsonOptions);
            idempotencyRecord.Complete(
                storedPayload,
                resultStatusCode,
                clock.UtcNow);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new IdempotentCommandResult<CommandProbeResult>(
                result,
                correlationId,
                IdempotencyExecutionStatus.Processed,
                resultStatusCode);
        }
        catch (DbUpdateConcurrencyException)
            when (concurrencyProblem is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw await concurrencyProblem();
        }
        catch (ApplicationProblemException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception exception)
            when (exception is DbUpdateException or PostgresException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw TemporaryFailure(exception);
        }
    }

    private Guid RequireWorkspace()
    {
        return currentWorkspace.WorkspaceId ??
            throw new ApplicationProblemException(
                "WORKSPACE_CONTEXT_REQUIRED",
                "A current workspace is required.",
                ApplicationErrorCategory.Validation);
    }

    private static CommandProbeResult ToResult(CommandProbe probe)
    {
        return new CommandProbeResult(
            probe.Id,
            probe.Name,
            probe.Counter,
            probe.Version);
    }

    private static ApplicationProblemException NotFound()
    {
        return new ApplicationProblemException(
            "COMMAND_PROBE_NOT_FOUND",
            "The command probe was not found.",
            ApplicationErrorCategory.NotFound);
    }

    private static ApplicationProblemException VersionConflict(
        long expectedVersion,
        long currentVersion)
    {
        return new ApplicationProblemException(
            "COMMAND_PROBE_VERSION_CONFLICT",
            "The command probe changed before this command was applied.",
            ApplicationErrorCategory.Conflict,
            details: new Dictionary<string, object?>
            {
                ["expectedVersion"] = expectedVersion,
                ["currentVersion"] = currentVersion,
            });
    }

    private static ApplicationProblemException TemporaryFailure(
        Exception? exception = null)
    {
        return new ApplicationProblemException(
            "TEMPORARY_COMMAND_FAILURE",
            "The command could not be completed due to a temporary system failure.",
            ApplicationErrorCategory.Unavailable,
            retryable: true,
            innerException: exception);
    }

    private static StoredCommandResult ReadStoredCommandResult(string payload)
    {
        try
        {
            var stored = JsonSerializer.Deserialize<StoredCommandResult>(
                payload,
                JsonOptions);
            if (stored?.Result is null ||
                stored.Result.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(stored.Result.Name) ||
                stored.Result.Counter < 0 ||
                stored.Result.Version <= 0 ||
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

    private sealed record StoredCommandResult(
        CommandProbeResult? Result,
        string? CorrelationId);
}
