using TraderPro.Application.Common.Commands;

namespace TraderPro.Application.Platform.CommandProbes;

public sealed class CreateCommandProbeHandler(
    ICommandProbeCommandExecutor executor)
{
    public Task<IdempotentCommandResult<CommandProbeResult>> HandleAsync(
        CreateCommandProbe command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var validated = new CreateCommandProbe(
            CommandProbeInputRules.RequireName(command.Name));

        return executor.CreateAsync(
            validated,
            CommandProbeInputRules.RequireIdempotencyKey(idempotencyKey),
            correlationId,
            cancellationToken);
    }
}

public sealed class IncrementCommandProbeHandler(
    ICommandProbeCommandExecutor executor)
{
    public Task<IdempotentCommandResult<CommandProbeResult>> HandleAsync(
        IncrementCommandProbe command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var validated = new IncrementCommandProbe(
            command.Id,
            CommandProbeInputRules.RequirePositiveDelta(command.Delta),
            CommandProbeInputRules.RequireExpectedVersion(
                command.ExpectedVersion));

        return executor.IncrementAsync(
            validated,
            CommandProbeInputRules.RequireIdempotencyKey(idempotencyKey),
            correlationId,
            cancellationToken);
    }
}
