using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Tenancy;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.CommandProbes;
using TraderPro.Domain.Platform;
using TraderPro.Infrastructure.Modules.Shared;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Platform;

internal sealed class CommandProbeService(
    TraderProDbContext dbContext,
    ICurrentWorkspaceAccessor currentWorkspace,
    IClock clock,
    PostgreSqlIdempotentCommandExecutor idempotency) :
    ICommandProbeCommandExecutor,
    ICommandProbeReader
{
    private const string AggregateType = "CommandProbe";
    private const int EventVersion = 1;
    private const int CreateStatusCode = 201;
    private const int IncrementStatusCode = 200;
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

        return idempotency.ExecuteAsync(
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
            IsReplayable,
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

        return idempotency.ExecuteAsync(
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
            IsReplayable,
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

    private static bool IsReplayable(CommandProbeResult result)
    {
        return result.Id != Guid.Empty &&
            !string.IsNullOrWhiteSpace(result.Name) &&
            result.Counter >= 0 &&
            result.Version > 0;
    }
}
