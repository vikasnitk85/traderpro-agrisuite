using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Platform.Identity;
using TraderPro.Infrastructure.Persistence;
using TraderPro.Infrastructure.Tenancy;

namespace TraderPro.Infrastructure.Modules.Platform.Identity;

internal sealed class ProductionIdentityService :
    IProductionIdentityService
{
    private const string ActivationIssueCommand =
        "Platform.Identity.DeviceActivationCode.Issue";
    private const string ActivationRedeemCommand =
        "Platform.Identity.DeviceActivationCode.Redeem";
    private const int MaximumSecretLength = 512;
    private readonly TraderProDbContext _dbContext;
    private readonly CurrentWorkspaceAccessor _currentWorkspace;
    private readonly CurrentAuthenticatedTraderProContext _currentIdentity;
    private readonly IClock _clock;
    private readonly TraderProAuthenticationOptions _options;
    private readonly IPasswordHasher<PlatformUser> _passwordHasher;
    private readonly JwtAccessTokenIssuer _accessTokenIssuer;
    private readonly PostgreSqlIdentitySessionLock _sessionLock;
    private readonly IDataProtector _refreshReplayProtector;
    private readonly ITimeLimitedDataProtector _activationReplayProtector;
    private readonly string _dummyPasswordHash;

    public ProductionIdentityService(
        TraderProDbContext dbContext,
        CurrentWorkspaceAccessor currentWorkspace,
        CurrentAuthenticatedTraderProContext currentIdentity,
        IClock clock,
        TraderProAuthenticationOptions options,
        IPasswordHasher<PlatformUser> passwordHasher,
        JwtAccessTokenIssuer accessTokenIssuer,
        PostgreSqlIdentitySessionLock sessionLock,
        IDataProtectionProvider dataProtectionProvider)
    {
        _dbContext = dbContext;
        _currentWorkspace = currentWorkspace;
        _currentIdentity = currentIdentity;
        _clock = clock;
        _options = options;
        _passwordHasher = passwordHasher;
        _accessTokenIssuer = accessTokenIssuer;
        _sessionLock = sessionLock;
        _refreshReplayProtector = dataProtectionProvider.CreateProtector(
            "TraderPro.Identity.RefreshReplay.v1");
        _activationReplayProtector = dataProtectionProvider.CreateProtector(
                "TraderPro.Identity.DeviceActivationReplay.v1")
            .ToTimeLimitedDataProtector();
        _dummyPasswordHash = passwordHasher.HashPassword(
            null!,
            IdentitySecretCryptography.GenerateUrlSafeToken());
    }

    public async Task<RedeemDeviceActivationResult>
        RedeemDeviceActivationAsync(
            RedeemDeviceActivationRequest request,
            string idempotencyKey,
            string correlationId,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validatedKey = RequiredBounded(
            idempotencyKey,
            200,
            "Idempotency-Key");
        var idempotencyKeyHash = IdentitySecretCryptography.Hash(
            validatedKey);
        var normalizedWorkspaceCode = NormalizeWorkspaceCode(
            request.WorkspaceCode);
        var activationCode = RequiredSecret(
            request.ActivationCode,
            "activationCode");
        var label = RequiredBounded(request.DeviceLabel, 200, "deviceLabel");
        var platform = RequiredBounded(request.Platform, 50, "platform");
        var installationReference = string.IsNullOrWhiteSpace(
            request.ClientInstallationReference)
            ? null
            : RequiredBounded(
                request.ClientInstallationReference,
                MaximumSecretLength,
                "clientInstallationReference");
        var installationHash = installationReference is null
            ? null
            : IdentitySecretCryptography.Hash(installationReference);
        var workspace = await FindWorkspaceAsync(
            normalizedWorkspaceCode,
            cancellationToken);
        if (workspace is null)
        {
            throw DeviceActivationInvalid();
        }

        _currentWorkspace.SetWorkspace(workspace.Id);
        var codeHash = IdentitySecretCryptography.Hash(activationCode);
        var codeLocation = await _dbContext.DeviceActivationCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CodeHash == codeHash,
                cancellationToken);
        if (codeLocation is null)
        {
            throw DeviceActivationInvalid();
        }

        var requestHash = IdentitySecretCryptography.Hash(
            $"{ActivationRedeemCommand}\n" +
            $"{workspace.Id:D}\n" +
            $"{codeLocation.Id:D}\n" +
            $"{codeLocation.DeviceId:D}\n" +
            $"{installationHash ?? string.Empty}\n" +
            $"{label}\n" +
            platform);

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        await _sessionLock.AcquireActivationDeviceAsync(
            workspace.Id,
            codeLocation.DeviceId,
            cancellationToken);
        await _sessionLock.AcquireDeviceCredentialAsync(
            workspace.Id,
            codeLocation.DeviceId,
            cancellationToken);
        var familyIds = await _dbContext.RefreshTokenFamilies
            .AsNoTracking()
            .Where(item =>
                item.DeviceId == codeLocation.DeviceId &&
                item.RevokedAtUtc == null)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        await _sessionLock.AcquireTokenFamiliesAsync(
            workspace.Id,
            familyIds,
            cancellationToken);
        await _sessionLock.AcquireCommandIdempotencyAsync(
            workspace.Id,
            ActivationRedeemCommand,
            idempotencyKeyHash,
            cancellationToken);
        var code = await _dbContext.DeviceActivationCodes
            .SingleOrDefaultAsync(
                item =>
                    item.Id == codeLocation.Id &&
                    item.CodeHash == codeHash &&
                    item.DeviceId == codeLocation.DeviceId,
                cancellationToken);
        if (code is null)
        {
            throw DeviceActivationInvalid();
        }

        var existingAttempt = await _dbContext.DeviceActivationCodes
            .SingleOrDefaultAsync(
                item =>
                    item.RedemptionIdempotencyKeyHash ==
                    idempotencyKeyHash,
                cancellationToken);
        if (existingAttempt is not null &&
            (existingAttempt.Id != code.Id ||
                existingAttempt.RedemptionRequestHash != requestHash))
        {
            throw IdempotencyPayloadConflict(code.DeviceId);
        }

        var now = ToPostgreSqlTimestampPrecision(_clock.UtcNow);
        if (code.RevokedAtUtc is not null)
        {
            throw DeviceActivationInvalid();
        }

        if (code.UsedAtUtc is null && code.IsExpired(now))
        {
            throw Problem(
                "DEVICE_ACTIVATION_EXPIRED",
                "The device activation code expired.",
                ApplicationErrorCategory.Conflict);
        }

        var device = await _dbContext.Devices.SingleOrDefaultAsync(
            item => item.Id == code.DeviceId,
            cancellationToken);
        if (device?.Status is not DeviceStatus.Active)
        {
            throw Problem(
                "DEVICE_NOT_ACTIVE",
                "The device is not active.",
                ApplicationErrorCategory.Conflict);
        }

        if (code.UsedAtUtc is not null)
        {
            if (code.RedemptionIdempotencyKeyHash != idempotencyKeyHash)
            {
                throw DeviceActivationAlreadyUsed();
            }

            if (code.RedemptionRequestHash != requestHash)
            {
                throw IdempotencyPayloadConflict(code.DeviceId);
            }

            if (code.ReplayAllowedUntilUtc is null ||
                code.ReplayAllowedUntilUtc <= now)
            {
                if (code.ReplayProtectedResult is not null)
                {
                    code.ClearReplayMaterial();
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                throw DeviceActivationRecoveryExpired();
            }

            if (code.ReplayProtectedResult is null)
            {
                throw DeviceActivationAlreadyUsed();
            }

            if (!TryUnprotectActivationReplayResult(
                    code.ReplayProtectedResult,
                    out var replayedActivation))
            {
                throw Problem(
                    "DEVICE_ACTIVATION_RECOVERY_UNAVAILABLE",
                    "The activation result cannot be recovered temporarily.",
                    ApplicationErrorCategory.Unavailable,
                    retryable: true);
            }

            var replayCredential = await _dbContext.DeviceCredentials
                .SingleOrDefaultAsync(
                    item => item.DeviceId == device.Id,
                    cancellationToken);
            if (replayedActivation is null ||
                replayedActivation.WorkspaceId != workspace.Id ||
                replayedActivation.ActivationCodeId != code.Id ||
                replayedActivation.DeviceId != device.Id ||
                replayedActivation.IdempotencyKeyHash != idempotencyKeyHash ||
                replayedActivation.RequestHash != requestHash ||
                replayedActivation.ReplayAllowedUntilUtc !=
                    code.ReplayAllowedUntilUtc ||
                replayedActivation.ActivatedAtUtc != code.UsedAtUtc ||
                replayCredential is null ||
                replayCredential.SecretVersion !=
                    replayedActivation.SecretVersion ||
                !IdentitySecretCryptography.VerifyHash(
                    replayedActivation.DeviceSecret,
                    replayCredential.DeviceSecretHash))
            {
                throw DeviceActivationAlreadyUsed();
            }

            await transaction.CommitAsync(cancellationToken);
            return new RedeemDeviceActivationResult(
                replayedActivation.DeviceId,
                replayedActivation.DeviceSecret,
                replayedActivation.ActivatedAtUtc,
                replayedActivation.SecretVersion);
        }

        var rawSecret = IdentitySecretCryptography.GenerateUrlSafeToken();
        var secretHash = IdentitySecretCryptography.Hash(rawSecret);
        var credential = await _dbContext.DeviceCredentials
            .SingleOrDefaultAsync(
                item => item.DeviceId == device.Id,
                cancellationToken);
        var reactivated = credential is not null;
        if (credential is null)
        {
            credential = DeviceCredential.Create(
                workspace.Id,
                device.Id,
                secretHash,
                installationHash,
                now);
            _dbContext.DeviceCredentials.Add(credential);
        }
        else
        {
            credential.Reactivate(secretHash, installationHash, now);
        }

        device.UpdateActivationMetadata(label, platform);
        var replayAllowedUntilUtc = now.AddMinutes(
            _options.ActivationCodeMinutes);
        var replay = new ActivationReplayResult(
            workspace.Id,
            code.Id,
            device.Id,
            idempotencyKeyHash,
            requestHash,
            rawSecret,
            credential.ActivatedAtUtc,
            credential.SecretVersion,
            replayAllowedUntilUtc);
        code.Consume(
            now,
            idempotencyKeyHash,
            requestHash,
            _activationReplayProtector.Protect(
                JsonSerializer.Serialize(replay),
                TimeSpan.FromMinutes(_options.ActivationCodeMinutes)),
            replayAllowedUntilUtc);
        await ClearSupersededActivationReplayMaterialAsync(
            device.Id,
            code.Id,
            cancellationToken);
        await RevokeDeviceFamiliesAsync(
            device.Id,
            now,
            "DeviceReactivated",
            cancellationToken);
        _dbContext.AuditEvents.Add(
            CreateAudit(
                workspace.Id,
                null,
                null,
                null,
                device.Id,
                reactivated
                    ? "Platform.Identity.DeviceReactivated"
                    : "Platform.Identity.DeviceActivated",
                "Device",
                device.Id,
                correlationId,
                now));
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new RedeemDeviceActivationResult(
            device.Id,
            rawSecret,
            credential.ActivatedAtUtc,
            credential.SecretVersion);
    }

    public async Task<AuthenticationTokenResult> LoginAsync(
        LoginRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspaceCode = NormalizeWorkspaceCode(
            request.WorkspaceCode);
        var normalizedLogin = NormalizeLogin(request.Login);
        var suppliedPassword = RequiredSecret(request.Password, "password");
        var suppliedDeviceSecret = RequiredSecret(
            request.DeviceSecret,
            "deviceSecret");
        if (request.DeviceId == Guid.Empty)
        {
            throw AuthenticationFailed();
        }

        var workspace = await FindWorkspaceAsync(
            normalizedWorkspaceCode,
            cancellationToken);
        if (workspace is null)
        {
            VerifyDummyPassword(suppliedPassword);
            throw AuthenticationFailed();
        }

        _currentWorkspace.SetWorkspace(workspace.Id);
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        var credentialLocation = await _dbContext.UserCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.NormalizedLogin == normalizedLogin,
                cancellationToken);
        if (credentialLocation is null)
        {
            VerifyDummyPassword(suppliedPassword);
            throw AuthenticationFailed();
        }

        await _sessionLock.AcquireUserSessionAsync(
            workspace.Id,
            credentialLocation.UserId,
            cancellationToken);
        await _sessionLock.AcquireActivationDeviceAsync(
            workspace.Id,
            request.DeviceId,
            cancellationToken);
        await _sessionLock.AcquireDeviceCredentialAsync(
            workspace.Id,
            request.DeviceId,
            cancellationToken);
        var credential = await _dbContext.UserCredentials
            .SingleOrDefaultAsync(
                item =>
                    item.Id == credentialLocation.Id &&
                    item.NormalizedLogin == normalizedLogin,
                cancellationToken);
        if (credential is null)
        {
            VerifyDummyPassword(suppliedPassword);
            throw AuthenticationFailed();
        }

        var now = _clock.UtcNow;
        if (credential.IsLocked(now))
        {
            throw Problem(
                "AUTHENTICATION_TEMPORARILY_LOCKED",
                "Sign-in is temporarily locked. Try again later.",
                ApplicationErrorCategory.Authentication,
                retryable: true);
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == credential.UserId,
            cancellationToken);
        var device = await _dbContext.Devices.SingleOrDefaultAsync(
            item => item.Id == request.DeviceId,
            cancellationToken);
        var deviceCredential = await _dbContext.DeviceCredentials
            .SingleOrDefaultAsync(
                item => item.DeviceId == request.DeviceId,
                cancellationToken);
        var passwordValid = user is not null &&
            _passwordHasher.VerifyHashedPassword(
                user,
                credential.PasswordHash,
                suppliedPassword) is not PasswordVerificationResult.Failed;
        var deviceSecretValid = deviceCredential is not null &&
            deviceCredential.RevokedAtUtc is null &&
            IdentitySecretCryptography.VerifyHash(
                suppliedDeviceSecret,
                deviceCredential.DeviceSecretHash);
        var principalsActive =
            workspace.Status is WorkspaceStatus.Active &&
            user?.Status is PlatformUserStatus.Active &&
            device?.Status is DeviceStatus.Active &&
            device?.WorkspaceId == workspace.Id &&
            deviceCredential?.WorkspaceId == workspace.Id;
        if (!passwordValid || !deviceSecretValid || !principalsActive)
        {
            credential.RecordFailedSignIn(
                now,
                _options.LockoutFailureLimit,
                TimeSpan.FromMinutes(_options.LockoutMinutes));
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw AuthenticationFailed();
        }

        var commercial = await LoadCommercialConfigurationAsync(
            workspace.Id,
            cancellationToken);
        credential.RecordSuccessfulSignIn();
        deviceCredential!.MarkUsed(now);
        device!.MarkSeen(now);
        await ClearActivationReplayMaterialAfterConfirmationAsync(
            device.Id,
            cancellationToken);
        var familyExpiry = now.AddDays(_options.RefreshTokenDays);
        var family = RefreshTokenFamily.Create(
            workspace.Id,
            user!.Id,
            device.Id,
            credential.CredentialVersion,
            deviceCredential.SecretVersion,
            now,
            familyExpiry);
        var rawRefreshToken =
            IdentitySecretCryptography.GenerateUrlSafeToken();
        var refreshToken = RefreshToken.Create(
            workspace.Id,
            family.Id,
            IdentitySecretCryptography.Hash(rawRefreshToken),
            now,
            familyExpiry);
        _dbContext.RefreshTokenFamilies.Add(family);
        _dbContext.RefreshTokens.Add(refreshToken);
        _dbContext.AuditEvents.Add(
            CreateAudit(
                workspace.Id,
                commercial.Company.Id,
                commercial.Branch.Id,
                user.Id,
                device.Id,
                "Platform.Identity.LoginSucceeded",
                "RefreshTokenFamily",
                family.Id,
                correlationId,
                now));
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return BuildAuthenticationResult(
            workspace,
            user,
            device,
            credential,
            deviceCredential,
            family,
            commercial,
            rawRefreshToken);
    }

    public async Task<AuthenticationTokenResult> RefreshAsync(
        RefreshRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rawRefreshToken = RequiredSecret(
            request.RefreshToken,
            "refreshToken");
        var tokenHash = IdentitySecretCryptography.Hash(rawRefreshToken);
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        var tokenLocation = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.TokenHash == tokenHash,
                cancellationToken);
        if (tokenLocation is null)
        {
            throw Problem(
                "REFRESH_TOKEN_INVALID",
                "The refresh token is invalid.",
                ApplicationErrorCategory.Authentication);
        }

        _currentWorkspace.SetWorkspace(tokenLocation.WorkspaceId);
        await _sessionLock.AcquireTokenFamilyAsync(
            tokenLocation.WorkspaceId,
            tokenLocation.FamilyId,
            cancellationToken);
        await _sessionLock.AcquireTokenIdentityAsync(
            tokenLocation.WorkspaceId,
            tokenLocation.Id,
            cancellationToken);
        var token = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                item =>
                    item.Id == tokenLocation.Id &&
                    item.TokenHash == tokenHash,
                cancellationToken);
        if (token is null)
        {
            throw Problem(
                "REFRESH_TOKEN_INVALID",
                "The refresh token is invalid.",
                ApplicationErrorCategory.Authentication);
        }

        var now = _clock.UtcNow;
        if (token.RevokedAtUtc is not null)
        {
            throw Problem(
                "REFRESH_TOKEN_INVALID",
                "The refresh token is invalid.",
                ApplicationErrorCategory.Authentication);
        }

        if (token.IsExpired(now))
        {
            throw Problem(
                "REFRESH_TOKEN_EXPIRED",
                "The refresh token expired.",
                ApplicationErrorCategory.Authentication);
        }

        var state = await LoadRefreshStateAsync(
            token,
            cancellationToken);
        if (token.ConsumedAtUtc is not null &&
            state.Family.RevocationReason ==
                "RefreshTokenReuseDetected")
        {
            await transaction.CommitAsync(cancellationToken);
            throw RefreshTokenReuseDetected();
        }

        if (state.Family.RevokedAtUtc is not null)
        {
            throw Problem(
                "REFRESH_TOKEN_FAMILY_REVOKED",
                "The refresh-token family is revoked.",
                ApplicationErrorCategory.Authentication);
        }

        if (state.Family.IsExpired(now))
        {
            throw Problem(
                "REFRESH_TOKEN_EXPIRED",
                "The refresh token expired.",
                ApplicationErrorCategory.Authentication);
        }

        if (!VersionsMatch(state))
        {
            state.Family.Revoke(now, "CredentialVersionChanged");
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw Problem(
                "REFRESH_TOKEN_FAMILY_REVOKED",
                "The refresh-token family is revoked.",
                ApplicationErrorCategory.Authentication);
        }

        var replayCleanupChanged =
            await ClearExpiredReplayMaterialAsync(
                state.Family.Id,
                now,
                cancellationToken);
        if (token.ConsumedAtUtc is not null)
        {
            string? replayedRaw = null;
            if (token.CanReplay(now) &&
                TryUnprotectReplayToken(
                    token.ReplayProtectedToken!,
                    out replayedRaw))
            {
                var replayReplacement = await _dbContext.RefreshTokens
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == token.RotatedToTokenId &&
                            item.WorkspaceId == token.WorkspaceId &&
                            item.FamilyId == token.FamilyId,
                        cancellationToken);
                if (replayReplacement is not null &&
                    replayReplacement.RevokedAtUtc is null &&
                    replayReplacement.ConsumedAtUtc is null &&
                    !replayReplacement.IsExpired(now) &&
                    IdentitySecretCryptography.VerifyHash(
                        replayedRaw!,
                        replayReplacement.TokenHash))
                {
                    if (replayCleanupChanged)
                    {
                        await _dbContext.SaveChangesAsync(
                            cancellationToken);
                    }

                    await transaction.CommitAsync(cancellationToken);
                    return BuildAuthenticationResult(
                        state.Workspace,
                        state.User,
                        state.Device,
                        state.UserCredential,
                        state.DeviceCredential,
                        state.Family,
                        state.Commercial,
                        replayedRaw!);
                }
            }

            await RecordRefreshTokenReuseAsync(
                state,
                correlationId,
                now,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw RefreshTokenReuseDetected();
        }

        var replayUntil = now.AddSeconds(_options.RefreshReplaySeconds);
        if (replayUntil > token.ExpiresAtUtc)
        {
            replayUntil = token.ExpiresAtUtc;
        }

        if (replayUntil <= now)
        {
            throw Problem(
                "REFRESH_TOKEN_EXPIRED",
                "The refresh token expired.",
                ApplicationErrorCategory.Authentication);
        }

        var replacementRaw =
            IdentitySecretCryptography.GenerateUrlSafeToken();
        var replacement = RefreshToken.Create(
            token.WorkspaceId,
            token.FamilyId,
            IdentitySecretCryptography.Hash(replacementRaw),
            now,
            state.Family.ExpiresAtUtc);
        await ClearPredecessorReplayMaterialAsync(
            token.Id,
            token.FamilyId,
            cancellationToken);
        token.RotateTo(
            replacement.Id,
            _refreshReplayProtector.Protect(replacementRaw),
            now,
            replayUntil);
        state.Family.MarkUsed(now);
        _dbContext.RefreshTokens.Add(replacement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return BuildAuthenticationResult(
            state.Workspace,
            state.User,
            state.Device,
            state.UserCredential,
            state.DeviceCredential,
            state.Family,
            state.Commercial,
            replacementRaw);
    }

    public async Task LogoutAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedContext();
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        await _sessionLock.AcquireTokenFamilyAsync(
            _currentIdentity.WorkspaceId,
            _currentIdentity.TokenFamilyId,
            cancellationToken);
        var family = await _dbContext.RefreshTokenFamilies
            .SingleAsync(
                item => item.Id == _currentIdentity.TokenFamilyId,
                cancellationToken);
        if (family.Revoke(_clock.UtcNow, "Logout"))
        {
            _dbContext.AuditEvents.Add(
                CreateAudit(
                    _currentIdentity.WorkspaceId,
                    _currentIdentity.CompanyId,
                    _currentIdentity.DefaultBranchId,
                    _currentIdentity.UserId,
                    _currentIdentity.DeviceId,
                    "Platform.Identity.SessionLoggedOut",
                    "RefreshTokenFamily",
                    family.Id,
                    correlationId,
                    _clock.UtcNow));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task LogoutAllAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedContext();
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        await _sessionLock.AcquireUserSessionAsync(
            _currentIdentity.WorkspaceId,
            _currentIdentity.UserId,
            cancellationToken);
        var familyIds = await _dbContext.RefreshTokenFamilies
            .AsNoTracking()
            .Where(item =>
                item.UserId == _currentIdentity.UserId &&
                item.RevokedAtUtc == null)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        await _sessionLock.AcquireTokenFamiliesAsync(
            _currentIdentity.WorkspaceId,
            familyIds,
            cancellationToken);
        var families = await _dbContext.RefreshTokenFamilies
            .Where(item =>
                item.UserId == _currentIdentity.UserId &&
                item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        var now = _clock.UtcNow;
        var changed = false;
        foreach (var family in families)
        {
            changed |= family.Revoke(now, "LogoutAll");
        }

        if (changed)
        {
            _dbContext.AuditEvents.Add(
                CreateAudit(
                    _currentIdentity.WorkspaceId,
                    _currentIdentity.CompanyId,
                    _currentIdentity.DefaultBranchId,
                    _currentIdentity.UserId,
                    _currentIdentity.DeviceId,
                    "Platform.Identity.AllSessionsLoggedOut",
                    "PlatformUser",
                    _currentIdentity.UserId,
                    correlationId,
                    now));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<CurrentIdentityResult> GetCurrentAsync(
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedContext();
        var workspace = await _dbContext.Workspaces.AsNoTracking()
            .SingleAsync(
                item => item.Id == _currentIdentity.WorkspaceId,
                cancellationToken);
        var user = await _dbContext.Users.AsNoTracking()
            .SingleAsync(
                item => item.Id == _currentIdentity.UserId,
                cancellationToken);
        var device = await _dbContext.Devices.AsNoTracking()
            .SingleAsync(
                item => item.Id == _currentIdentity.DeviceId,
                cancellationToken);
        return new CurrentIdentityResult(
            new AuthenticatedUserResult(
                user.Id,
                user.DisplayName,
                _currentIdentity.Role.ToString()),
            new AuthenticatedWorkspaceResult(
                workspace.Id,
                workspace.WorkspaceCode),
            new AuthenticatedCompanyResult(
                _currentIdentity.CompanyId,
                _currentIdentity.DefaultBranchId),
            new AuthenticatedDeviceResult(device.Id, device.Name),
            _currentIdentity.TokenFamilyId);
    }

    public async Task<DeviceActivationCodeResult>
        IssueDeviceActivationCodeAsync(
            Guid deviceId,
            string idempotencyKey,
            string correlationId,
            CancellationToken cancellationToken)
    {
        EnsureAuthenticatedContext();
        if (deviceId == Guid.Empty)
        {
            throw Problem(
                "DEVICE_NOT_FOUND",
                "The device was not found.",
                ApplicationErrorCategory.NotFound);
        }

        var validatedKey = RequiredBounded(
            idempotencyKey,
            200,
            "Idempotency-Key");
        var requestHash = IdentitySecretCryptography.Hash(
            $"{ActivationIssueCommand}\n{deviceId:D}");
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        await _sessionLock.AcquireActivationDeviceAsync(
            _currentIdentity.WorkspaceId,
            deviceId,
            cancellationToken);
        await _sessionLock.AcquireCommandIdempotencyAsync(
            _currentIdentity.WorkspaceId,
            ActivationIssueCommand,
            validatedKey,
            cancellationToken);
        await ClearExpiredActivationReplayMaterialAsync(
            deviceId,
            _clock.UtcNow,
            cancellationToken);
        var existing = await _dbContext.IdempotencyRecords
            .SingleOrDefaultAsync(
                item =>
                    item.CommandType == ActivationIssueCommand &&
                    item.IdempotencyKey == validatedKey,
                cancellationToken);
        if (existing is not null)
        {
            throw Problem(
                existing.RequestHash == requestHash
                    ? "DEVICE_ACTIVATION_CODE_RESPONSE_NOT_REPLAYABLE"
                    : "IDEMPOTENCY_PAYLOAD_CONFLICT",
                existing.RequestHash == requestHash
                    ? "The activation code was returned once and cannot be replayed; the original code remains valid until it is consumed, expires, or is replaced."
                    : "The idempotency key was already used for a different activation-code request.",
                ApplicationErrorCategory.Conflict,
                details: new Dictionary<string, object?>
                {
                    ["deviceId"] = deviceId.ToString("D"),
                });
        }

        var device = await _dbContext.Devices.SingleOrDefaultAsync(
            item => item.Id == deviceId,
            cancellationToken);
        if (device is null)
        {
            throw Problem(
                "DEVICE_NOT_FOUND",
                "The device was not found.",
                ApplicationErrorCategory.NotFound);
        }

        var record = IdempotencyRecord.Create(
            _currentIdentity.WorkspaceId,
            validatedKey,
            ActivationIssueCommand,
            requestHash,
            _clock.UtcNow);
        _dbContext.IdempotencyRecords.Add(record);
        var result = await IssueActivationCodeCoreAsync(
            device,
            _currentIdentity.UserId,
            correlationId,
            cancellationToken);
        record.Complete(
            JsonSerializer.Serialize(
                new
                {
                    result.DeviceId,
                    result.ExpiresAtUtc,
                }),
            (int)HttpStatusCode.Created,
            _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<DevelopmentIdentityBootstrapResult>
        BootstrapDevelopmentAsync(
            DevelopmentIdentityBootstrapRequest request,
            string correlationId,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var workspaceCode = string.IsNullOrWhiteSpace(request.WorkspaceCode)
            ? "TRADERPRO-DEMO"
            : NormalizeWorkspaceCode(request.WorkspaceCode);
        var ownerPassword = PasswordPolicy.Validate(request.OwnerPassword);
        var operatorPassword = PasswordPolicy.Validate(
            request.OperatorPassword);
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        await _sessionLock.AcquireDevelopmentBootstrapAsync(
            workspaceCode,
            cancellationToken);
        var workspace = await FindWorkspaceAsync(
            workspaceCode,
            cancellationToken);
        if (workspace is null)
        {
            var technicalCode =
                $"identity-{IdentitySecretCryptography.Hash(workspaceCode)[..20]}";
            workspace = Workspace.CreateCommercial(
                technicalCode,
                workspaceCode,
                "TraderPro Development Identity Workspace",
                _clock.UtcNow);
            _dbContext.Workspaces.Add(workspace);
        }

        if (workspace.Status is not WorkspaceStatus.Active)
        {
            throw Problem(
                "WORKSPACE_CONFIGURATION_INVALID",
                "The development identity workspace is not active.",
                ApplicationErrorCategory.Conflict);
        }

        _currentWorkspace.SetWorkspace(workspace.Id);
        var company = await _dbContext.Companies.SingleOrDefaultAsync(
            item => item.Code == "IDENTITY",
            cancellationToken);
        if (company is null)
        {
            company = Company.Create(
                workspace.Id,
                "IDENTITY",
                "TraderPro Development Company",
                null,
                null,
                _clock.UtcNow);
            _dbContext.Companies.Add(company);
        }

        var branch = await _dbContext.Branches.SingleOrDefaultAsync(
            item =>
                item.CompanyId == company.Id &&
                item.Code == "MAIN",
            cancellationToken);
        if (branch is null)
        {
            branch = Branch.Create(
                workspace.Id,
                company.Id,
                "MAIN",
                "Main Branch",
                true,
                _clock.UtcNow);
            _dbContext.Branches.Add(branch);
        }

        var owner = await EnsureUserAsync(
            workspace.Id,
            "owner",
            "Development Owner",
            TraderProRole.Owner,
            ownerPassword,
            cancellationToken);
        var operatorUser = await EnsureUserAsync(
            workspace.Id,
            "operator",
            "Development Operator",
            TraderProRole.Operator,
            operatorPassword,
            cancellationToken);
        var operatorDevice = await EnsureDeviceAsync(
            workspace.Id,
            "identity-operator-device",
            "Operator Device",
            cancellationToken);
        var ownerDevice = await EnsureDeviceAsync(
            workspace.Id,
            "identity-owner-device",
            "Owner Device",
            cancellationToken);
        foreach (var deviceId in new[]
                 {
                     operatorDevice.Id,
                     ownerDevice.Id,
                 }.OrderBy(id => id))
        {
            await _sessionLock.AcquireActivationDeviceAsync(
                workspace.Id,
                deviceId,
                cancellationToken);
        }

        var operatorActivation = await IssueActivationCodeCoreAsync(
            operatorDevice,
            owner.Id,
            correlationId,
            cancellationToken);
        var ownerActivation = await IssueActivationCodeCoreAsync(
            ownerDevice,
            owner.Id,
            correlationId,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DevelopmentIdentityBootstrapResult(
            workspace.Id,
            workspace.WorkspaceCode,
            company.Id,
            branch.Id,
            owner.Id,
            operatorUser.Id,
            operatorDevice.Id,
            ownerDevice.Id,
            operatorActivation,
            ownerActivation);
    }

    private async Task<PlatformUser> EnsureUserAsync(
        Guid workspaceId,
        string login,
        string displayName,
        TraderProRole role,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedLogin = LoginNormalizer.Normalize(login);
        var credential = await _dbContext.UserCredentials
            .SingleOrDefaultAsync(
                item => item.NormalizedLogin == normalizedLogin,
                cancellationToken);
        PlatformUser user;
        if (credential is null)
        {
            user = PlatformUser.Create(
                workspaceId,
                login,
                displayName,
                role,
                _clock.UtcNow);
            credential = UserCredential.Create(
                workspaceId,
                user.Id,
                normalizedLogin,
                _passwordHasher.HashPassword(user, password),
                _clock.UtcNow);
            _dbContext.Users.Add(user);
            _dbContext.UserCredentials.Add(credential);
            return user;
        }

        user = await _dbContext.Users.SingleAsync(
            item => item.Id == credential.UserId,
            cancellationToken);
        user.SetRole(role);
        if (_passwordHasher.VerifyHashedPassword(
                user,
                credential.PasswordHash,
                password) is PasswordVerificationResult.Failed)
        {
            credential.ChangePassword(
                _passwordHasher.HashPassword(user, password),
                _clock.UtcNow);
            var families = await _dbContext.RefreshTokenFamilies
                .Where(item =>
                    item.UserId == user.Id &&
                    item.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var family in families)
            {
                family.Revoke(_clock.UtcNow, "PasswordChanged");
            }
        }

        return user;
    }

    private async Task<Device> EnsureDeviceAsync(
        Guid workspaceId,
        string installationId,
        string name,
        CancellationToken cancellationToken)
    {
        var device = await _dbContext.Devices.SingleOrDefaultAsync(
            item => item.InstallationId == installationId,
            cancellationToken);
        if (device is not null)
        {
            return device;
        }

        device = Device.Create(
            workspaceId,
            installationId,
            name,
            "Android",
            _clock.UtcNow);
        _dbContext.Devices.Add(device);
        return device;
    }

    private async Task<DeviceActivationCodeResult>
        IssueActivationCodeCoreAsync(
            Device device,
            Guid issuedByUserId,
            string correlationId,
            CancellationToken cancellationToken)
    {
        if (device.Status is not DeviceStatus.Active)
        {
            throw Problem(
                "DEVICE_NOT_ACTIVE",
                "The device is not active.",
                ApplicationErrorCategory.Conflict);
        }

        var now = _clock.UtcNow;
        var olderCodes = await _dbContext.DeviceActivationCodes
            .Where(item =>
                item.DeviceId == device.Id &&
                item.UsedAtUtc == null &&
                item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var olderCode in olderCodes)
        {
            olderCode.Revoke(now);
        }

        var rawCode = IdentitySecretCryptography.GenerateUrlSafeToken();
        var expires = now.AddMinutes(_options.ActivationCodeMinutes);
        var code = DeviceActivationCode.Create(
            device.WorkspaceId,
            device.Id,
            IdentitySecretCryptography.Hash(rawCode),
            expires,
            issuedByUserId,
            now);
        _dbContext.DeviceActivationCodes.Add(code);
        _dbContext.AuditEvents.Add(
            CreateAudit(
                device.WorkspaceId,
                _currentIdentity.IsBound
                    ? _currentIdentity.CompanyId
                    : null,
                _currentIdentity.IsBound
                    ? _currentIdentity.DefaultBranchId
                    : null,
                issuedByUserId,
                _currentIdentity.IsBound
                    ? _currentIdentity.DeviceId
                    : null,
                "Platform.Identity.DeviceActivationCodeIssued",
                "Device",
                device.Id,
                correlationId,
                now));
        return new DeviceActivationCodeResult(
            device.Id,
            rawCode,
            expires);
    }

    private async Task RevokeDeviceFamiliesAsync(
        Guid deviceId,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
    {
        var families = await _dbContext.RefreshTokenFamilies
            .Where(item =>
                item.DeviceId == deviceId &&
                item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var family in families)
        {
            family.Revoke(now, reason);
        }
    }

    private async Task<RefreshState> LoadRefreshStateAsync(
        RefreshToken token,
        CancellationToken cancellationToken)
    {
        var family = await _dbContext.RefreshTokenFamilies
            .SingleOrDefaultAsync(
                item => item.Id == token.FamilyId,
                cancellationToken) ??
            throw Problem(
                "REFRESH_TOKEN_INVALID",
                "The refresh token is invalid.",
                ApplicationErrorCategory.Authentication);
        var workspace = await _dbContext.Workspaces.SingleOrDefaultAsync(
                item => item.Id == token.WorkspaceId,
                cancellationToken) ??
            throw Problem(
                "REFRESH_TOKEN_INVALID",
                "The refresh token is invalid.",
                ApplicationErrorCategory.Authentication);
        var user = await _dbContext.Users.SingleOrDefaultAsync(
                item => item.Id == family.UserId,
                cancellationToken) ??
            throw Problem(
                "USER_NOT_ACTIVE",
                "The user is not active.",
                ApplicationErrorCategory.Authentication);
        var device = await _dbContext.Devices.SingleOrDefaultAsync(
                item => item.Id == family.DeviceId,
                cancellationToken) ??
            throw Problem(
                "DEVICE_NOT_ACTIVE",
                "The device is not active.",
                ApplicationErrorCategory.Authentication);
        var userCredential = await _dbContext.UserCredentials
            .SingleAsync(
                item => item.UserId == user.Id,
                cancellationToken);
        var deviceCredential = await _dbContext.DeviceCredentials
            .SingleAsync(
                item => item.DeviceId == device.Id,
                cancellationToken);
        if (workspace.Status is not WorkspaceStatus.Active ||
            user.Status is not PlatformUserStatus.Active ||
            device.Status is not DeviceStatus.Active ||
            deviceCredential.RevokedAtUtc is not null ||
            family.WorkspaceId != workspace.Id ||
            user.WorkspaceId != workspace.Id ||
            device.WorkspaceId != workspace.Id)
        {
            throw Problem(
                "REFRESH_TOKEN_INVALID",
                "The refresh token is invalid.",
                ApplicationErrorCategory.Authentication);
        }

        var commercial = await LoadCommercialConfigurationAsync(
            workspace.Id,
            cancellationToken);
        return new RefreshState(
            workspace,
            user,
            device,
            userCredential,
            deviceCredential,
            family,
            commercial);
    }

    private static bool VersionsMatch(RefreshState state)
    {
        return state.Family.UserCredentialVersion ==
                state.UserCredential.CredentialVersion &&
            state.Family.DeviceSecretVersion ==
                state.DeviceCredential.SecretVersion;
    }

    private async Task<CommercialConfiguration>
        LoadCommercialConfigurationAsync(
            Guid workspaceId,
            CancellationToken cancellationToken)
    {
        var companies = await _dbContext.Companies
            .Where(item => item.Status == CompanyStatus.Active)
            .ToListAsync(cancellationToken);
        if (companies.Count != 1 ||
            companies[0].WorkspaceId != workspaceId)
        {
            throw WorkspaceConfigurationInvalid();
        }

        var company = companies[0];
        var branches = await _dbContext.Branches
            .Where(item =>
                item.CompanyId == company.Id &&
                item.IsDefault &&
                item.Status == BranchStatus.Active)
            .ToListAsync(cancellationToken);
        if (branches.Count != 1)
        {
            throw WorkspaceConfigurationInvalid();
        }

        return new CommercialConfiguration(company, branches[0]);
    }

    private AuthenticationTokenResult BuildAuthenticationResult(
        Workspace workspace,
        PlatformUser user,
        Device device,
        UserCredential userCredential,
        DeviceCredential deviceCredential,
        RefreshTokenFamily family,
        CommercialConfiguration commercial,
        string rawRefreshToken)
    {
        var accessToken = _accessTokenIssuer.Issue(
            workspace.Id,
            user.Id,
            device.Id,
            commercial.Company.Id,
            commercial.Branch.Id,
            user.Role,
            userCredential.CredentialVersion,
            deviceCredential.SecretVersion,
            family.Id);
        return new AuthenticationTokenResult(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            family.ExpiresAtUtc,
            new AuthenticatedUserResult(
                user.Id,
                user.DisplayName,
                user.Role.ToString()),
            new AuthenticatedWorkspaceResult(
                workspace.Id,
                workspace.WorkspaceCode),
            new AuthenticatedCompanyResult(
                commercial.Company.Id,
                commercial.Branch.Id),
            new AuthenticatedDeviceResult(device.Id, device.Name));
    }

    private async Task<Workspace?> FindWorkspaceAsync(
        string normalizedWorkspaceCode,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Workspaces.AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.NormalizedWorkspaceCode ==
                    normalizedWorkspaceCode,
                cancellationToken);
    }

    private async Task<bool> ClearExpiredReplayMaterialAsync(
        Guid familyId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var expired = await _dbContext.RefreshTokens
            .Where(item =>
                item.FamilyId == familyId &&
                item.ReplayProtectedToken != null &&
                item.ReplayAllowedUntilUtc < now)
            .ToListAsync(cancellationToken);
        foreach (var token in expired)
        {
            token.ClearReplayMaterial();
        }

        return expired.Count > 0;
    }

    private async Task ClearExpiredActivationReplayMaterialAsync(
        Guid deviceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var expired = await _dbContext.DeviceActivationCodes
            .Where(item =>
                item.DeviceId == deviceId &&
                item.ReplayProtectedResult != null &&
                item.ReplayAllowedUntilUtc <= now)
            .ToListAsync(cancellationToken);
        foreach (var code in expired)
        {
            code.ClearReplayMaterial();
        }
    }

    private async Task ClearActivationReplayMaterialAfterConfirmationAsync(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var confirmed = await _dbContext.DeviceActivationCodes
            .Where(item =>
                item.DeviceId == deviceId &&
                item.ReplayProtectedResult != null)
            .ToListAsync(cancellationToken);
        foreach (var code in confirmed)
        {
            code.ClearReplayMaterial();
        }
    }

    private async Task ClearSupersededActivationReplayMaterialAsync(
        Guid deviceId,
        Guid currentCodeId,
        CancellationToken cancellationToken)
    {
        var superseded = await _dbContext.DeviceActivationCodes
            .Where(item =>
                item.DeviceId == deviceId &&
                item.Id != currentCodeId &&
                item.ReplayProtectedResult != null)
            .ToListAsync(cancellationToken);
        foreach (var code in superseded)
        {
            code.ClearReplayMaterial();
        }
    }

    private async Task ClearPredecessorReplayMaterialAsync(
        Guid replacementTokenId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var predecessors = await _dbContext.RefreshTokens
            .Where(item =>
                item.FamilyId == familyId &&
                item.RotatedToTokenId == replacementTokenId &&
                item.ReplayProtectedToken != null)
            .ToListAsync(cancellationToken);
        foreach (var predecessor in predecessors)
        {
            predecessor.ClearReplayMaterial();
        }
    }

    private async Task RecordRefreshTokenReuseAsync(
        RefreshState state,
        string correlationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (state.Family.Revoke(
                now,
                "RefreshTokenReuseDetected"))
        {
            _dbContext.AuditEvents.Add(
                CreateAudit(
                    state.Workspace.Id,
                    state.Commercial.Company.Id,
                    state.Commercial.Branch.Id,
                    state.User.Id,
                    state.Device.Id,
                    "Platform.Identity.RefreshTokenReuseDetected",
                    "RefreshTokenFamily",
                    state.Family.Id,
                    correlationId,
                    now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private bool TryUnprotectReplayToken(
        string protectedToken,
        out string? rawToken)
    {
        try
        {
            rawToken = _refreshReplayProtector.Unprotect(protectedToken);
            return true;
        }
        catch (Exception exception) when (
            exception is CryptographicException or FormatException)
        {
            rawToken = null;
            return false;
        }
    }

    private bool TryUnprotectActivationReplayResult(
        string protectedResult,
        out ActivationReplayResult? result)
    {
        try
        {
            result = JsonSerializer.Deserialize<ActivationReplayResult>(
                _activationReplayProtector.Unprotect(
                    protectedResult,
                    out _));
            return result is not null;
        }
        catch (Exception exception) when (
            exception is CryptographicException or
                FormatException or
                JsonException)
        {
            result = null;
            return false;
        }
    }

    private static ApplicationProblemException RefreshTokenReuseDetected()
    {
        return Problem(
            "REFRESH_TOKEN_REUSE_DETECTED",
            "Refresh-token reuse was detected and the session was revoked.",
            ApplicationErrorCategory.Conflict);
    }

    private static ApplicationProblemException DeviceActivationAlreadyUsed()
    {
        return Problem(
            "DEVICE_ACTIVATION_ALREADY_USED",
            "The device activation code was already used.",
            ApplicationErrorCategory.Conflict);
    }

    private static ApplicationProblemException
        DeviceActivationRecoveryExpired()
    {
        return Problem(
            "DEVICE_ACTIVATION_RECOVERY_EXPIRED",
            "The activation-result recovery window expired; a new Owner-issued activation code is required.",
            ApplicationErrorCategory.Conflict);
    }

    private static DateTimeOffset ToPostgreSqlTimestampPrecision(
        DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        const long ticksPerMicrosecond = 10;
        return new DateTimeOffset(
            utc.Ticks - utc.Ticks % ticksPerMicrosecond,
            TimeSpan.Zero);
    }

    private static ApplicationProblemException IdempotencyPayloadConflict(
        Guid deviceId)
    {
        return Problem(
            "IDEMPOTENCY_PAYLOAD_CONFLICT",
            "The idempotency key was already used for a different device-activation request.",
            ApplicationErrorCategory.Conflict,
            details: new Dictionary<string, object?>
            {
                ["deviceId"] = deviceId.ToString("D"),
            });
    }

    private void VerifyDummyPassword(string password)
    {
        _ = _passwordHasher.VerifyHashedPassword(
            null!,
            _dummyPasswordHash,
            password);
    }

    private void EnsureAuthenticatedContext()
    {
        if (!_currentIdentity.IsBound)
        {
            throw Problem(
                "AUTHENTICATED_CONTEXT_INVALID",
                "The authenticated TraderPro context is invalid.",
                ApplicationErrorCategory.Authentication);
        }
    }

    private static string NormalizeWorkspaceCode(string? value)
    {
        try
        {
            return WorkspaceCodeNormalizer.Normalize(value);
        }
        catch (ArgumentException exception)
        {
            throw Problem(
                "WORKSPACE_CODE_INVALID",
                "Workspace code is invalid.",
                ApplicationErrorCategory.Validation,
                innerException: exception);
        }
    }

    private static string NormalizeLogin(string? value)
    {
        try
        {
            return LoginNormalizer.Normalize(value);
        }
        catch (ArgumentException exception)
        {
            throw Problem(
                "AUTHENTICATION_FAILED",
                "The workspace, login, password, or device credential is invalid.",
                ApplicationErrorCategory.Authentication,
                innerException: exception);
        }
    }

    private static string RequiredSecret(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaximumSecretLength)
        {
            throw Problem(
                "AUTHENTICATION_FAILED",
                "The supplied credential is invalid.",
                ApplicationErrorCategory.Authentication,
                fieldErrors:
                [
                    new ApplicationFieldError(
                        field,
                        "REQUIRED",
                        "A bounded non-empty value is required."),
                ]);
        }

        return value;
    }

    private static string RequiredBounded(
        string? value,
        int maximum,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Trim().Length > maximum)
        {
            throw Problem(
                "REQUEST_BODY_INVALID",
                "A required value is invalid.",
                ApplicationErrorCategory.Validation,
                fieldErrors:
                [
                    new ApplicationFieldError(
                        field,
                        "INVALID",
                        $"A non-empty value of at most {maximum} characters is required."),
                ]);
        }

        return value.Trim();
    }

    private static AuditEvent CreateAudit(
        Guid workspaceId,
        Guid? companyId,
        Guid? branchId,
        Guid? actorUserId,
        Guid? actorDeviceId,
        string action,
        string aggregateType,
        Guid aggregateId,
        string correlationId,
        DateTimeOffset now)
    {
        return AuditEvent.Create(
            workspaceId,
            companyId,
            branchId,
            actorUserId,
            actorDeviceId,
            action,
            aggregateType,
            aggregateId,
            null,
            null,
            null,
            null,
            correlationId,
            now);
    }

    private static ApplicationProblemException AuthenticationFailed()
    {
        return Problem(
            "AUTHENTICATION_FAILED",
            "The workspace, login, password, or device credential is invalid.",
            ApplicationErrorCategory.Authentication);
    }

    private static ApplicationProblemException DeviceActivationInvalid()
    {
        return Problem(
            "DEVICE_ACTIVATION_INVALID",
            "The device activation request is invalid.",
            ApplicationErrorCategory.Authentication);
    }

    private static ApplicationProblemException
        WorkspaceConfigurationInvalid()
    {
        return Problem(
            "WORKSPACE_CONFIGURATION_INVALID",
            "The workspace does not have one active commercial company and default branch.",
            ApplicationErrorCategory.Conflict);
    }

    private static ApplicationProblemException Problem(
        string code,
        string message,
        ApplicationErrorCategory category,
        bool retryable = false,
        IReadOnlyList<ApplicationFieldError>? fieldErrors = null,
        IReadOnlyDictionary<string, object?>? details = null,
        Exception? innerException = null)
    {
        return new ApplicationProblemException(
            code,
            message,
            category,
            retryable,
            fieldErrors,
            details,
            innerException: innerException);
    }

    private sealed record CommercialConfiguration(
        Company Company,
        Branch Branch);

    private sealed record RefreshState(
        Workspace Workspace,
        PlatformUser User,
        Device Device,
        UserCredential UserCredential,
        DeviceCredential DeviceCredential,
        RefreshTokenFamily Family,
        CommercialConfiguration Commercial);

    private sealed record ActivationReplayResult(
        Guid WorkspaceId,
        Guid ActivationCodeId,
        Guid DeviceId,
        string IdempotencyKeyHash,
        string RequestHash,
        string DeviceSecret,
        DateTimeOffset ActivatedAtUtc,
        int SecretVersion,
        DateTimeOffset ReplayAllowedUntilUtc);
}
