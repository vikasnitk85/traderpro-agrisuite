using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.MasterData;

namespace TraderPro.Application.Operations;

public interface IBusinessLocationService
{
    Task<IdempotentCommandResult<BusinessLocationResult>> CreateAsync(
        CreateBusinessLocationCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<BusinessLocationResult>> UpdateAsync(
        UpdateBusinessLocationCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<BusinessLocationResult>> DeactivateAsync(
        MasterStatusCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<BusinessLocationResult>> ReactivateAsync(
        MasterStatusCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<BusinessLocationResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MasterPage<BusinessLocationResult>> ListAsync(
        MasterListQuery query,
        CancellationToken cancellationToken);
}

public interface IBusinessLocationDefaultUsageReader
{
    Task<bool> IsCurrentDefaultAsync(
        Guid locationId,
        CancellationToken cancellationToken);
}
