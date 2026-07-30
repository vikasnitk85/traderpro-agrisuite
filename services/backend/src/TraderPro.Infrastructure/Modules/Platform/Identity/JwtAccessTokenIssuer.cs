using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Platform.Identity;

namespace TraderPro.Infrastructure.Modules.Platform.Identity;

public sealed record IssuedAccessToken(
    string Token,
    DateTimeOffset ExpiresAtUtc);

public sealed class JwtAccessTokenIssuer(
    TraderProAuthenticationOptions options,
    IClock clock)
{
    private readonly byte[] _signingKey =
        AuthenticationConfiguration.DecodeSigningKey(options.SigningKey);

    public IssuedAccessToken Issue(
        Guid workspaceId,
        Guid userId,
        Guid deviceId,
        Guid companyId,
        Guid defaultBranchId,
        TraderProRole role,
        int userCredentialVersion,
        int deviceSecretVersion,
        Guid tokenFamilyId)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString("D")),
            new Claim("wid", workspaceId.ToString("D")),
            new Claim("did", deviceId.ToString("D")),
            new Claim("cid", companyId.ToString("D")),
            new Claim("bid", defaultBranchId.ToString("D")),
            new Claim("role", role.ToString()),
            new Claim(
                "ucv",
                userCredentialVersion.ToString(CultureInfo.InvariantCulture)),
            new Claim(
                "dsv",
                deviceSecretVersion.ToString(CultureInfo.InvariantCulture)),
            new Claim("sid", tokenFamilyId.ToString("D")),
            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.CreateVersion7().ToString("D")),
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_signingKey),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            credentials);
        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires);
    }
}

public static class AuthenticationConfiguration
{
    public const string SectionName = "TraderPro:Authentication";
    public const string DataProtectionApplicationName =
        "TraderPro.AgriSuite.Identity";

    public static TraderProAuthenticationOptions ReadOptions(
        IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        return new TraderProAuthenticationOptions(
            section["Issuer"] ?? string.Empty,
            section["Audience"] ?? string.Empty,
            section.GetValue<int?>("AccessTokenMinutes") ??
                TraderProAuthenticationOptions.DefaultAccessTokenMinutes,
            section.GetValue<int?>("RefreshTokenDays") ??
                TraderProAuthenticationOptions.DefaultRefreshTokenDays,
            section.GetValue<int?>("RefreshReplaySeconds") ??
                TraderProAuthenticationOptions.DefaultRefreshReplaySeconds,
            section["SigningKey"] ?? string.Empty,
            section["DataProtectionKeyRingPath"] ?? string.Empty,
            section.GetValue<int?>("LockoutFailureLimit") ??
                TraderProAuthenticationOptions.DefaultLockoutFailureLimit,
            section.GetValue<int?>("LockoutMinutes") ??
                TraderProAuthenticationOptions.DefaultLockoutMinutes,
            section.GetValue<int?>("ActivationCodeMinutes") ??
                TraderProAuthenticationOptions.DefaultActivationCodeMinutes,
            section.GetValue<bool?>("RateLimitingEnabled") ?? true,
            section.GetValue<bool?>("RequireHttps") ??
                TraderProAuthenticationOptions.DefaultRequireHttps,
            section.GetValue<bool?>("ForwardedHeadersEnabled") ?? false,
            section.GetSection("TrustedProxyAddresses").Get<string[]>() ??
                []);
    }

    public static byte[] DecodeSigningKey(string configured)
    {
        try
        {
            var bytes = Convert.FromBase64String(configured);
            if (bytes.Length < 32)
            {
                throw Invalid();
            }

            return bytes;
        }
        catch (FormatException)
        {
            throw Invalid();
        }
    }

    public static void Validate(
        TraderProAuthenticationOptions options,
        bool production)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer) ||
            string.IsNullOrWhiteSpace(options.Audience))
        {
            throw Invalid();
        }

        _ = DecodeSigningKey(options.SigningKey);
        if (string.IsNullOrWhiteSpace(options.DataProtectionKeyRingPath) ||
            !Path.IsPathFullyQualified(options.DataProtectionKeyRingPath))
        {
            throw Invalid();
        }

        if (production)
        {
            if (!options.RequireHttps)
            {
                throw Invalid();
            }

            var fullPath = Path.GetFullPath(
                options.DataProtectionKeyRingPath);
            var temporaryPath = Path.GetFullPath(Path.GetTempPath());
            if (fullPath.StartsWith(
                    temporaryPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw Invalid();
            }
        }

        if (options.ForwardedHeadersEnabled &&
            (options.TrustedProxyAddresses.Count == 0 ||
             options.TrustedProxyAddresses.Any(address =>
                 !IPAddress.TryParse(address, out _))))
        {
            throw Invalid();
        }

        if (options.AccessTokenMinutes is < 1 or > 60 ||
            options.RefreshTokenDays is < 1 or > 365 ||
            options.RefreshReplaySeconds is < 1 or > 300 ||
            options.LockoutFailureLimit is < 1 or > 100 ||
            options.LockoutMinutes is < 1 or > 1440 ||
            options.ActivationCodeMinutes is < 1 or > 1440)
        {
            throw Invalid();
        }
    }

    private static InvalidOperationException Invalid()
    {
        return new InvalidOperationException(
            "AUTHENTICATION_CONFIGURATION_INVALID: configure a non-empty " +
            "issuer/audience, a Base64 signing key of at least 256 bits, " +
            "safe positive lifetimes, and an absolute persistent Data " +
            "Protection key-ring path. Production also requires HTTPS, " +
            "and forwarded headers require explicit trusted proxy addresses.");
    }
}
