using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class Branch :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private Branch()
    {
    }

    private Branch(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        bool isDefault,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        CompanyId = PlatformEntityGuard.RequiredId(companyId, nameof(companyId));
        Code = PlatformEntityGuard.Required(code, 64, nameof(code));
        Name = PlatformEntityGuard.Required(name, 200, nameof(name));
        IsDefault = isDefault;
        Status = BranchStatus.Active;
        CreatedAtUtc = PlatformEntityGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public BranchStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static Branch Create(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        bool isDefault,
        DateTimeOffset createdAtUtc)
    {
        return new Branch(
            workspaceId,
            companyId,
            code,
            name,
            isDefault,
            createdAtUtc);
    }

    public void Rename(string name)
    {
        Name = PlatformEntityGuard.Required(name, 200, nameof(name));
    }

    public void SetDefault(bool isDefault)
    {
        if (isDefault && Status is not BranchStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active branch can be the default branch.");
        }

        IsDefault = isDefault;
    }

    public void SetStatus(BranchStatus status)
    {
        var validatedStatus = PlatformEntityGuard.Defined(
            status,
            nameof(status));
        if (IsDefault && validatedStatus is not BranchStatus.Active)
        {
            throw new InvalidOperationException(
                "A default branch must remain active. Clear IsDefault first.");
        }

        Status = validatedStatus;
    }
}
