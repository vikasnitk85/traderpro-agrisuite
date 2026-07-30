using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform.Identity;

public sealed class UserCredential :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private UserCredential()
    {
    }

    private UserCredential(
        Guid workspaceId,
        Guid userId,
        string normalizedLogin,
        string passwordHash,
        DateTimeOffset now)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = RequiredId(workspaceId, nameof(workspaceId));
        UserId = RequiredId(userId, nameof(userId));
        NormalizedLogin = LoginNormalizer.Normalize(normalizedLogin);
        PasswordHash = Required(passwordHash, 1000, nameof(passwordHash));
        PasswordChangedAtUtc = EnsureUtc(now, nameof(now));
        CredentialVersion = 1;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid UserId { get; private set; }

    public string NormalizedLogin { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset PasswordChangedAtUtc { get; private set; }

    public int FailedSignInCount { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public int CredentialVersion { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static UserCredential Create(
        Guid workspaceId,
        Guid userId,
        string login,
        string passwordHash,
        DateTimeOffset now)
    {
        return new UserCredential(
            workspaceId,
            userId,
            login,
            passwordHash,
            now);
    }

    public bool IsLocked(DateTimeOffset now)
    {
        return LockoutEndUtc is not null &&
            EnsureUtc(now, nameof(now)) < LockoutEndUtc;
    }

    public void RecordFailedSignIn(
        DateTimeOffset now,
        int failureLimit,
        TimeSpan lockoutDuration)
    {
        EnsureUtc(now, nameof(now));
        if (failureLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(failureLimit));
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        }

        if (LockoutEndUtc is not null && now >= LockoutEndUtc)
        {
            FailedSignInCount = 0;
            LockoutEndUtc = null;
        }

        FailedSignInCount = checked(FailedSignInCount + 1);
        if (FailedSignInCount >= failureLimit)
        {
            LockoutEndUtc = now.Add(lockoutDuration);
        }
    }

    public void RecordSuccessfulSignIn()
    {
        FailedSignInCount = 0;
        LockoutEndUtc = null;
    }

    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = Required(passwordHash, 1000, nameof(passwordHash));
        PasswordChangedAtUtc = EnsureUtc(now, nameof(now));
        CredentialVersion = checked(CredentialVersion + 1);
        RecordSuccessfulSignIn();
    }

    private static Guid RequiredId(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A non-empty identifier is required.",
                name);
        }

        return value;
    }

    private static string Required(string? value, int maximum, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum)
        {
            throw new ArgumentException("A valid value is required.", name);
        }

        return value;
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The timestamp must have a zero UTC offset.",
                name);
        }

        return value;
    }
}
