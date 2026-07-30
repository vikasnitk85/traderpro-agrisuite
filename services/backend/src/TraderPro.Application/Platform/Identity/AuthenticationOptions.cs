namespace TraderPro.Application.Platform.Identity;

public sealed record TraderProAuthenticationOptions(
    string Issuer,
    string Audience,
    int AccessTokenMinutes,
    int RefreshTokenDays,
    int RefreshReplaySeconds,
    string SigningKey,
    string DataProtectionKeyRingPath,
    int LockoutFailureLimit,
    int LockoutMinutes,
    int ActivationCodeMinutes,
    bool RateLimitingEnabled,
    bool RequireHttps,
    bool ForwardedHeadersEnabled,
    IReadOnlyList<string> TrustedProxyAddresses)
{
    public const int DefaultAccessTokenMinutes = 15;
    public const int DefaultRefreshTokenDays = 30;
    public const int DefaultRefreshReplaySeconds = 30;
    public const int DefaultLockoutFailureLimit = 5;
    public const int DefaultLockoutMinutes = 15;
    public const int DefaultActivationCodeMinutes = 15;
    public const bool DefaultRequireHttps = true;
}
