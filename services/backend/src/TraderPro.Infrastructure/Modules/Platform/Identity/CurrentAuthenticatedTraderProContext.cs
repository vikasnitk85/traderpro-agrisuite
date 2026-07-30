using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Platform.Identity;

namespace TraderPro.Infrastructure.Modules.Platform.Identity;

internal sealed class CurrentAuthenticatedTraderProContext :
    IAuthenticatedTraderProContext
{
    private AuthenticatedContextState? _state;

    public bool IsBound => _state is not null;

    public Guid WorkspaceId => State.WorkspaceId;

    public Guid UserId => State.UserId;

    public Guid DeviceId => State.DeviceId;

    public Guid CompanyId => State.CompanyId;

    public Guid DefaultBranchId => State.DefaultBranchId;

    public TraderProRole Role => State.Role;

    public bool IsOwner => TraderProAuthorizationRules.IsOwner(Role);

    public bool IsOperatorOrOwner =>
        TraderProAuthorizationRules.IsOperatorOrOwner(Role);

    public Guid TokenFamilyId => State.TokenFamilyId;

    public string CorrelationId => State.CorrelationId;

    public void Bind(
        Guid workspaceId,
        Guid userId,
        Guid deviceId,
        Guid companyId,
        Guid defaultBranchId,
        TraderProRole role,
        Guid tokenFamilyId,
        string correlationId)
    {
        var next = new AuthenticatedContextState(
            workspaceId,
            userId,
            deviceId,
            companyId,
            defaultBranchId,
            role,
            tokenFamilyId,
            correlationId);
        if (_state is not null && _state != next)
        {
            throw new InvalidOperationException(
                "The authenticated request context is already bound.");
        }

        _state = next;
    }

    private AuthenticatedContextState State =>
        _state ??
        throw new InvalidOperationException(
            "No authenticated TraderPro context is bound.");

    private sealed record AuthenticatedContextState(
        Guid WorkspaceId,
        Guid UserId,
        Guid DeviceId,
        Guid CompanyId,
        Guid DefaultBranchId,
        TraderProRole Role,
        Guid TokenFamilyId,
        string CorrelationId);
}
