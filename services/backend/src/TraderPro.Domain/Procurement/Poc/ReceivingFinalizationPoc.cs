using TraderPro.Domain.Common;

namespace TraderPro.Domain.Procurement.Poc;

/// <summary>
/// Immutable Task 6A completion evidence. It is not a Purchase Bill or posting.
/// </summary>
public sealed class ReceivingFinalizationPoc :
    IWorkspaceScoped,
    IVersionedEntity
{
    private ReceivingFinalizationPoc()
    {
    }

    private ReceivingFinalizationPoc(
        Guid workspaceId,
        Guid receivingSessionId,
        int finalEntryCount,
        decimal finalProcessedTotalWeightKg,
        Guid approvedByDeviceId,
        Guid finalizedByDeviceId,
        DateTimeOffset finalizedAtUtc)
    {
        if (finalEntryCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(finalEntryCount));
        }

        if (finalProcessedTotalWeightKg < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finalProcessedTotalWeightKg));
        }

        Id = Uuid7.NewGuid();
        WorkspaceId = RequiredId(workspaceId, nameof(workspaceId));
        ReceivingSessionId = RequiredId(
            receivingSessionId,
            nameof(receivingSessionId));
        FinalEntryCount = finalEntryCount;
        FinalProcessedTotalWeightKg = finalProcessedTotalWeightKg;
        ApprovedByDeviceId = RequiredId(
            approvedByDeviceId,
            nameof(approvedByDeviceId));
        FinalizedByDeviceId = RequiredId(
            finalizedByDeviceId,
            nameof(finalizedByDeviceId));
        if (finalizedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The timestamp must use a zero UTC offset.",
                nameof(finalizedAtUtc));
        }

        FinalizedAtUtc = finalizedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid ReceivingSessionId { get; private set; }

    public int FinalEntryCount { get; private set; }

    public decimal FinalProcessedTotalWeightKg { get; private set; }

    public Guid ApprovedByDeviceId { get; private set; }

    public Guid FinalizedByDeviceId { get; private set; }

    public DateTimeOffset FinalizedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static ReceivingFinalizationPoc Create(
        Guid workspaceId,
        Guid receivingSessionId,
        int finalEntryCount,
        decimal finalProcessedTotalWeightKg,
        Guid approvedByDeviceId,
        Guid finalizedByDeviceId,
        DateTimeOffset finalizedAtUtc)
    {
        return new ReceivingFinalizationPoc(
            workspaceId,
            receivingSessionId,
            finalEntryCount,
            finalProcessedTotalWeightKg,
            approvedByDeviceId,
            finalizedByDeviceId,
            finalizedAtUtc);
    }

    private static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A non-empty identifier is required.",
                parameterName);
        }

        return value;
    }
}
