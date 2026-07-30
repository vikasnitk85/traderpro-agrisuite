using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform.Identity;

public sealed class RefreshToken :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid workspaceId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (workspaceId == Guid.Empty || familyId == Guid.Empty)
        {
            throw new ArgumentException(
                "Non-empty ownership identifiers are required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "Token expiry must follow creation.",
                nameof(expiresAtUtc));
        }

        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        FamilyId = familyId;
        TokenHash = RequiredHash(tokenHash);
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? RotatedToTokenId { get; private set; }

    public string? ReplayProtectedToken { get; private set; }

    public DateTimeOffset? ReplayAllowedUntilUtc { get; private set; }

    public long Version { get; private set; }

    public static RefreshToken Create(
        Guid workspaceId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new RefreshToken(
            workspaceId,
            familyId,
            tokenHash,
            createdAtUtc,
            expiresAtUtc);
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return EnsureUtc(now, nameof(now)) >= ExpiresAtUtc;
    }

    public bool CanReplay(DateTimeOffset now)
    {
        now = EnsureUtc(now, nameof(now));
        return ConsumedAtUtc is not null &&
            ReplayAllowedUntilUtc is not null &&
            now <= ReplayAllowedUntilUtc &&
            ReplayProtectedToken is not null &&
            RotatedToTokenId is not null;
    }

    public void RotateTo(
        Guid replacementTokenId,
        string protectedReplacementToken,
        DateTimeOffset consumedAtUtc,
        DateTimeOffset replayAllowedUntilUtc)
    {
        if (ConsumedAtUtc is not null ||
            RevokedAtUtc is not null ||
            replacementTokenId == Guid.Empty ||
            string.IsNullOrWhiteSpace(protectedReplacementToken))
        {
            throw new InvalidOperationException(
                "Only an active refresh token can be rotated.");
        }

        consumedAtUtc = EnsureUtc(consumedAtUtc, nameof(consumedAtUtc));
        replayAllowedUntilUtc = EnsureUtc(
            replayAllowedUntilUtc,
            nameof(replayAllowedUntilUtc));
        if (replayAllowedUntilUtc <= consumedAtUtc ||
            replayAllowedUntilUtc > ExpiresAtUtc)
        {
            throw new ArgumentException(
                "The replay window must follow consumption and remain within token expiry.",
                nameof(replayAllowedUntilUtc));
        }

        ConsumedAtUtc = consumedAtUtc;
        RotatedToTokenId = replacementTokenId;
        ReplayProtectedToken = protectedReplacementToken;
        ReplayAllowedUntilUtc = replayAllowedUntilUtc;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAtUtc ??= EnsureUtc(now, nameof(now));
    }

    public void ClearReplayMaterial()
    {
        if (ConsumedAtUtc is null || RotatedToTokenId is null)
        {
            throw new InvalidOperationException(
                "Only a consumed, rotated token can clear replay material.");
        }

        ReplayProtectedToken = null;
        ReplayAllowedUntilUtc = null;
    }

    private static string RequiredHash(string value)
    {
        if (value.Length != 64 ||
            value.Any(character =>
                !char.IsAsciiHexDigit(character) ||
                char.IsAsciiLetterUpper(character)))
        {
            throw new ArgumentException(
                "A lowercase SHA-256 hash is required.",
                nameof(value));
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
