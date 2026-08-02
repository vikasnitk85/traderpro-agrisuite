using TraderPro.Domain.Common;

namespace TraderPro.Domain.Procurement.Receiving;

public sealed class CommercialReceivingReferenceReservation : IWorkspaceScoped, ICreatedAtUtc
{
    private CommercialReceivingReferenceReservation()
    {
    }

    private CommercialReceivingReferenceReservation(
        Guid workspaceId,
        Guid companyId,
        Guid policyId,
        string periodKey,
        Guid operationId,
        Guid sessionId,
        string requestHash,
        long policyVersion,
        long sequence,
        string renderedReference,
        DateTimeOffset now)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = CommercialReceivingSession.RequiredId(workspaceId, nameof(workspaceId));
        CompanyId = CommercialReceivingSession.RequiredId(companyId, nameof(companyId));
        PolicyId = CommercialReceivingSession.RequiredId(policyId, nameof(policyId));
        PeriodKey = CommercialReceivingSession.Required(periodKey, 20, nameof(periodKey));
        CommercialReceivingSession.RequireUuid7(operationId, nameof(operationId));
        OperationId = operationId;
        CommercialReceivingSession.RequireUuid7(sessionId, nameof(sessionId));
        SessionId = sessionId;
        RequestHash = requestHash.Length == 64 ? requestHash : throw new ArgumentException("A request hash is required.", nameof(requestHash));
        PolicyVersion = CommercialReceivingSession.Positive(policyVersion, nameof(policyVersion));
        Sequence = CommercialReceivingSession.Positive(sequence, nameof(sequence));
        RenderedReference = CommercialReceivingSession.Required(renderedReference, 100, nameof(renderedReference));
        ReservedAtUtc = CommercialReceivingSession.Utc(now, nameof(now));
        CreatedAtUtc = ReservedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid PolicyId { get; private set; }
    public string PeriodKey { get; private set; } = string.Empty;
    public Guid OperationId { get; private set; }
    public Guid SessionId { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public long PolicyVersion { get; private set; }
    public long Sequence { get; private set; }
    public string RenderedReference { get; private set; } = string.Empty;
    public DateTimeOffset ReservedAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CommercialReceivingReferenceReservation Create(
        Guid workspaceId,
        Guid companyId,
        Guid policyId,
        string periodKey,
        Guid operationId,
        Guid sessionId,
        string requestHash,
        long policyVersion,
        long sequence,
        string renderedReference,
        DateTimeOffset now) =>
        new(workspaceId, companyId, policyId, periodKey, operationId, sessionId,
            requestHash, policyVersion, sequence, renderedReference, now);

    public void Consume(Guid sessionId, DateTimeOffset now)
    {
        if (sessionId != SessionId)
        {
            throw new InvalidOperationException("The reservation belongs to another Receiving Session.");
        }

        ConsumedAtUtc ??= CommercialReceivingSession.Utc(now, nameof(now));
    }
}
