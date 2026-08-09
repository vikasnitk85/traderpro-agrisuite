using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Platform.Identity;
using TraderPro.Infrastructure.Modules.Platform.Identity;

namespace TraderPro.UnitTests;

public sealed class ProductionIdentityTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Workspace_code_normalization_is_strict_and_invariant()
    {
        Assert.Equal(
            "TRADERPRO-DEMO",
            WorkspaceCodeNormalizer.Normalize("traderpro-demo"));
        Assert.Throws<ArgumentException>(
            () => WorkspaceCodeNormalizer.Normalize(" traderpro-demo"));
        Assert.Throws<ArgumentException>(
            () => WorkspaceCodeNormalizer.Normalize("trader pro"));
    }

    [Fact]
    public void Login_normalization_is_culture_invariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal("I", LoginNormalizer.Normalize("i"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("          ")]
    public void Password_policy_rejects_invalid_passwords(string password)
    {
        Assert.Throws<ArgumentException>(
            () => PasswordPolicy.Validate(password));
    }

    [Fact]
    public void Supported_password_hasher_never_stores_raw_password()
    {
        const string password = "a long password-manager passphrase";
        var user = PlatformUser.Create(
            Uuid7.NewGuid(),
            "owner",
            "Owner",
            TraderProRole.Owner,
            UtcNow);
        var hasher = new PasswordHasher<PlatformUser>();
        var hash = hasher.HashPassword(user, password);

        Assert.DoesNotContain(password, hash, StringComparison.Ordinal);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(user, hash, password));
        Assert.Equal(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(user, hash, "incorrect password"));
    }

    [Fact]
    public void Credential_lockout_and_password_version_transitions_are_controlled()
    {
        var credential = UserCredential.Create(
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            "owner",
            "supported-password-hash",
            UtcNow);
        for (var failure = 0; failure < 5; failure++)
        {
            credential.RecordFailedSignIn(
                UtcNow,
                5,
                TimeSpan.FromMinutes(15));
        }

        Assert.True(credential.IsLocked(UtcNow.AddMinutes(14)));
        Assert.False(credential.IsLocked(UtcNow.AddMinutes(15)));
        credential.RecordSuccessfulSignIn();
        Assert.Equal(0, credential.FailedSignInCount);
        credential.ChangePassword(
            "another-supported-password-hash",
            UtcNow.AddMinutes(16));
        Assert.Equal(2, credential.CredentialVersion);
    }

    [Fact]
    public void Device_secrets_are_random_hashed_and_constant_time_verified()
    {
        var raw = IdentitySecretCryptography.GenerateUrlSafeToken();
        var hash = IdentitySecretCryptography.Hash(raw);

        Assert.NotEqual(raw, hash);
        Assert.Equal(64, hash.Length);
        Assert.True(IdentitySecretCryptography.VerifyHash(raw, hash));
        Assert.False(
            IdentitySecretCryptography.VerifyHash($"{raw}x", hash));
    }

    [Fact]
    public void Device_credential_reactivation_rotates_version()
    {
        var credential = DeviceCredential.Create(
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            new string('a', 64),
            null,
            UtcNow);
        credential.Reactivate(
            new string('b', 64),
            new string('c', 64),
            UtcNow.AddMinutes(1));

        Assert.Equal(2, credential.SecretVersion);
        Assert.Equal(new string('b', 64), credential.DeviceSecretHash);
    }

    [Fact]
    public void Activation_code_is_one_time_and_expiry_aware()
    {
        var code = DeviceActivationCode.Create(
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            new string('a', 64),
            UtcNow.AddMinutes(15),
            Uuid7.NewGuid(),
            UtcNow);
        code.Consume(
            UtcNow.AddMinutes(1),
            new string('b', 64),
            new string('c', 64),
            "protected-result",
            UtcNow.AddMinutes(16));

        Assert.NotNull(code.UsedAtUtc);
        Assert.Throws<InvalidOperationException>(
            () => code.Consume(
                UtcNow.AddMinutes(2),
                new string('b', 64),
                new string('c', 64),
                "protected-result",
                UtcNow.AddMinutes(17)));
        Assert.False(code.IsExpired(UtcNow.AddMinutes(14)));
        Assert.True(code.IsExpired(UtcNow.AddMinutes(15)));
    }

    [Fact]
    public void Refresh_token_rotation_has_bounded_replay_window()
    {
        var workspaceId = Uuid7.NewGuid();
        var family = RefreshTokenFamily.Create(
            workspaceId,
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            1,
            1,
            UtcNow,
            UtcNow.AddDays(30));
        var token = RefreshToken.Create(
            workspaceId,
            family.Id,
            new string('a', 64),
            UtcNow,
            UtcNow.AddDays(30));
        token.RotateTo(
            Uuid7.NewGuid(),
            "protected-replacement",
            UtcNow.AddMinutes(1),
            UtcNow.AddMinutes(1).AddSeconds(30));

        Assert.True(token.CanReplay(
            UtcNow.AddMinutes(1).AddSeconds(30)));
        Assert.False(token.CanReplay(
            UtcNow.AddMinutes(1).AddSeconds(31)));
    }

    [Fact]
    public void Consumed_refresh_token_can_clear_only_replay_material()
    {
        var workspaceId = Uuid7.NewGuid();
        var familyId = Uuid7.NewGuid();
        var replacementId = Uuid7.NewGuid();
        var token = RefreshToken.Create(
            workspaceId,
            familyId,
            new string('a', 64),
            UtcNow,
            UtcNow.AddDays(30));
        token.RotateTo(
            replacementId,
            "protected-replacement",
            UtcNow.AddMinutes(1),
            UtcNow.AddMinutes(2));

        token.ClearReplayMaterial();

        Assert.Equal(replacementId, token.RotatedToTokenId);
        Assert.Equal(UtcNow.AddMinutes(1), token.ConsumedAtUtc);
        Assert.Null(token.ReplayProtectedToken);
        Assert.Null(token.ReplayAllowedUntilUtc);
        Assert.False(token.CanReplay(UtcNow.AddMinutes(1)));
    }

    [Fact]
    public void Role_rules_enforce_owner_and_operator_or_owner()
    {
        Assert.True(
            TraderProAuthorizationRules.IsOwner(TraderProRole.Owner));
        Assert.False(
            TraderProAuthorizationRules.IsOwner(TraderProRole.Operator));
        Assert.True(
            TraderProAuthorizationRules.IsOperatorOrOwner(
                TraderProRole.Operator));
    }

    [Fact]
    public void Jwt_contains_canonical_required_identity_claims()
    {
        var key = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));
        var options = new TraderProAuthenticationOptions(
            "TraderPro.Tests",
            "TraderPro.Tests.Client",
            15,
            30,
            30,
            key,
            Path.GetTempPath(),
            5,
            15,
            15,
            false,
            false,
            false,
            []);
        var workspaceId = Uuid7.NewGuid();
        var userId = Uuid7.NewGuid();
        var deviceId = Uuid7.NewGuid();
        var companyId = Uuid7.NewGuid();
        var branchId = Uuid7.NewGuid();
        var familyId = Uuid7.NewGuid();
        var issued = new JwtAccessTokenIssuer(
                options,
                new FixedClock(UtcNow))
            .Issue(
                workspaceId,
                userId,
                deviceId,
                companyId,
                branchId,
                TraderProRole.Owner,
                3,
                4,
                familyId);
        using var payload = ReadPayload(issued.Token);

        Assert.Equal(userId.ToString("D"), Claim(payload, "sub"));
        Assert.Equal(workspaceId.ToString("D"), Claim(payload, "wid"));
        Assert.Equal(deviceId.ToString("D"), Claim(payload, "did"));
        Assert.Equal(companyId.ToString("D"), Claim(payload, "cid"));
        Assert.Equal(branchId.ToString("D"), Claim(payload, "bid"));
        Assert.Equal("Owner", Claim(payload, "role"));
        Assert.Equal("3", Claim(payload, "ucv"));
        Assert.Equal("4", Claim(payload, "dsv"));
        Assert.Equal(familyId.ToString("D"), Claim(payload, "sid"));
        Assert.True(Guid.TryParseExact(Claim(payload, "jti"), "D", out _));
    }

    [Fact]
    public void Authenticated_token_invariants_reject_empty_authority()
    {
        Assert.Throws<ArgumentException>(
            () => AuthenticatedContextInvariants.ValidateTokenIdentity(
                new AuthenticatedAccessToken(
                    Guid.Empty,
                    Uuid7.NewGuid(),
                    Uuid7.NewGuid(),
                    Uuid7.NewGuid(),
                    Uuid7.NewGuid(),
                    "Owner",
                    1,
                    1,
                    Uuid7.NewGuid(),
                    Uuid7.NewGuid().ToString("D"))));
    }

    [Fact]
    public void Authentication_configuration_rejects_missing_or_weak_secrets()
    {
        var valid = ValidAuthenticationOptions();
        var invalid = new[]
        {
            valid with { Issuer = string.Empty },
            valid with { Audience = string.Empty },
            valid with { SigningKey = string.Empty },
            valid with
            {
                SigningKey = Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(31)),
            },
            valid with { DataProtectionKeyRingPath = string.Empty },
            valid with { DataProtectionKeyRingPath = "relative-key-ring" },
            valid with { RequireHttps = false },
            valid with
            {
                ForwardedHeadersEnabled = true,
                TrustedProxyAddresses = [],
            },
            valid with
            {
                ForwardedHeadersEnabled = true,
                TrustedProxyAddresses = ["not-an-ip-address"],
            },
        };

        Assert.All(
            invalid,
            options => Assert.Contains(
                "AUTHENTICATION_CONFIGURATION_INVALID",
                Assert.Throws<InvalidOperationException>(
                    () => AuthenticationConfiguration.Validate(
                        options,
                        production: true)).Message,
                StringComparison.Ordinal));
    }

    [Fact]
    public void Production_rejects_temporary_key_ring_but_testing_accepts_it()
    {
        var options = ValidAuthenticationOptions() with
        {
            DataProtectionKeyRingPath = Path.Combine(
                Path.GetTempPath(),
                $"traderpro-key-ring-{Guid.NewGuid():N}"),
        };

        Assert.Throws<InvalidOperationException>(
            () => AuthenticationConfiguration.Validate(
                options,
                production: true));
        AuthenticationConfiguration.Validate(
            options,
            production: false);
    }

    private static TraderProAuthenticationOptions
        ValidAuthenticationOptions()
    {
        return new TraderProAuthenticationOptions(
            "TraderPro.Tests",
            "TraderPro.Tests.Client",
            15,
            30,
            30,
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)),
            Path.Combine(
                Environment.CurrentDirectory,
                "artifacts",
                "identity-unit-test-key-ring"),
            5,
            15,
            15,
            false,
            true,
            false,
            []);
    }

    private static JsonDocument ReadPayload(string token)
    {
        var value = token.Split('.')[1]
            .Replace('-', '+')
            .Replace('_', '/');
        value = value.PadRight(
            value.Length + ((4 - value.Length % 4) % 4),
            '=');
        return JsonDocument.Parse(Convert.FromBase64String(value));
    }

    private static string Claim(JsonDocument token, string type)
    {
        return token.RootElement.GetProperty(type).GetString()!;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
