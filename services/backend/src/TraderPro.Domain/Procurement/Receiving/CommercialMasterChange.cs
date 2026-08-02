using TraderPro.Domain.Common;

namespace TraderPro.Domain.Procurement.Receiving;

public sealed class CommercialMasterChange : IWorkspaceScoped, IOccurredAtUtc
{
    private CommercialMasterChange()
    {
    }

    public long Sequence { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public string MasterType { get; private set; } = string.Empty;
    public Guid MasterId { get; private set; }
    public long MasterVersion { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
}
