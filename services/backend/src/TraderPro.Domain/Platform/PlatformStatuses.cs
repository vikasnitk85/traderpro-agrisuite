namespace TraderPro.Domain.Platform;

public enum WorkspaceStatus : short
{
    Active = 1,
    Suspended = 2,
    Archived = 3,
}

public enum CompanyStatus : short
{
    Active = 1,
    Inactive = 2,
    Archived = 3,
}

public enum BranchStatus : short
{
    Active = 1,
    Inactive = 2,
    Archived = 3,
}

public enum PlatformUserStatus : short
{
    Active = 1,
    Disabled = 2,
}

public enum DeviceStatus : short
{
    Active = 1,
    Registered = Active,
    Disabled = 2,
}

public enum IdempotencyRecordStatus : short
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
}

public enum OutboxMessageStatus : short
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
}

public enum OutboxEventStream : short
{
    Internal = 1,
    MobileSync = 2,
}
