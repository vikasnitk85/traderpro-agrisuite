using TraderPro.Domain.Common;

namespace TraderPro.Domain.Procurement.Receiving;

public enum CommercialReceivingOperationClaimState : short
{
    Pending = 1,
    NeedsAttention = 2,
    Rejected = 3,
    Completed = 4,
}

public sealed class CommercialReceivingOperationClaim : IWorkspaceScoped, ICreatedAtUtc, IUpdatedAtUtc
{
    public const string CommandScope = "Procurement.CommercialReceiving.MobileSyncOperation";

    private CommercialReceivingOperationClaim()
    {
    }

    private CommercialReceivingOperationClaim(
        Guid workspaceId,
        Guid companyId,
        Guid operationId,
        string operationType,
        Guid sessionId,
        Guid deviceId,
        long? ownershipGeneration,
        string requestHash,
        DateTimeOffset now)
    {
        WorkspaceId = CommercialReceivingSession.RequiredId(workspaceId, nameof(workspaceId));
        CompanyId = CommercialReceivingSession.RequiredId(companyId, nameof(companyId));
        OperationId = RequiredUuid7(operationId, nameof(operationId));
        CommandScopeValue = CommandScope;
        OperationType = CommercialReceivingSession.Required(operationType, 100, nameof(operationType));
        SessionId = RequiredUuid7(sessionId, nameof(sessionId));
        DeviceId = CommercialReceivingSession.RequiredId(deviceId, nameof(deviceId));
        if (ownershipGeneration is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipGeneration));
        }

        OwnershipGeneration = ownershipGeneration;
        RequestHash = RequiredHash(requestHash);
        State = CommercialReceivingOperationClaimState.Pending;
        FirstSeenAtUtc = CommercialReceivingSession.Utc(now, nameof(now));
        CreatedAtUtc = FirstSeenAtUtc;
        UpdatedAtUtc = FirstSeenAtUtc;
    }

    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public string CommandScopeValue { get; private set; } = string.Empty;
    public Guid OperationId { get; private set; }
    public string OperationType { get; private set; } = string.Empty;
    public Guid SessionId { get; private set; }
    public Guid DeviceId { get; private set; }
    public long? OwnershipGeneration { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public CommercialReceivingOperationClaimState State { get; private set; }
    public string? AttentionCode { get; private set; }
    public string? AttentionMessage { get; private set; }
    public bool Retryable { get; private set; }
    public DateTimeOffset FirstSeenAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static CommercialReceivingOperationClaim Create(
        Guid workspaceId,
        Guid companyId,
        Guid operationId,
        string operationType,
        Guid sessionId,
        Guid deviceId,
        long? ownershipGeneration,
        string requestHash,
        DateTimeOffset now) =>
        new(workspaceId, companyId, operationId, operationType, sessionId, deviceId,
            ownershipGeneration, requestHash, now);

    public void BeginRetry(DateTimeOffset now)
    {
        if (State is CommercialReceivingOperationClaimState.Completed or CommercialReceivingOperationClaimState.Rejected)
        {
            return;
        }

        State = CommercialReceivingOperationClaimState.Pending;
        AttentionCode = null;
        AttentionMessage = null;
        Retryable = false;
        UpdatedAtUtc = CommercialReceivingSession.Utc(now, nameof(now));
    }

    public void NeedsAttention(string code, string message, bool retryable, DateTimeOffset now) =>
        SetFailure(CommercialReceivingOperationClaimState.NeedsAttention, code, message, retryable, now);

    public void Reject(string code, string message, bool retryable, DateTimeOffset now) =>
        SetFailure(CommercialReceivingOperationClaimState.Rejected, code, message, retryable, now);

    public void Complete(DateTimeOffset now)
    {
        if (State is CommercialReceivingOperationClaimState.Rejected)
        {
            throw new InvalidOperationException("A rejected operation claim cannot complete.");
        }

        State = CommercialReceivingOperationClaimState.Completed;
        AttentionCode = null;
        AttentionMessage = null;
        Retryable = false;
        CompletedAtUtc = CommercialReceivingSession.Utc(now, nameof(now));
        UpdatedAtUtc = CompletedAtUtc.Value;
    }

    private void SetFailure(
        CommercialReceivingOperationClaimState state,
        string code,
        string message,
        bool retryable,
        DateTimeOffset now)
    {
        if (State is CommercialReceivingOperationClaimState.Completed or
            CommercialReceivingOperationClaimState.Rejected)
        {
            return;
        }

        State = state;
        AttentionCode = CommercialReceivingSession.Required(code, 200, nameof(code));
        AttentionMessage = CommercialReceivingSession.Required(message, 1000, nameof(message));
        Retryable = retryable;
        UpdatedAtUtc = CommercialReceivingSession.Utc(now, nameof(now));
    }

    private static Guid RequiredUuid7(Guid value, string name)
    {
        CommercialReceivingSession.RequireUuid7(value, name);
        return value;
    }

    private static string RequiredHash(string value) =>
        value.Length == 64 && value.All(character => char.IsAsciiHexDigit(character) && !char.IsAsciiLetterUpper(character))
            ? value
            : throw new ArgumentException("A lowercase SHA-256 hash is required.", nameof(value));
}
