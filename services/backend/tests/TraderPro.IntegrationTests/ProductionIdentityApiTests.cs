using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Platform.Identity;
using TraderPro.Infrastructure.Modules.Platform.Identity;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ProductionIdentityApiTests(PostgreSqlFixture fixture)
{
    private const string OwnerPassword = "owner development passphrase";
    private const string OperatorPassword =
        "operator development passphrase";
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 2, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migration_creates_identity_controls_and_upgrades_task6_data()
    {
        const string task6Migration =
            "20260729163015_HardenTwoDeviceProcurementPocContracts";
        await using var database = await fixture.CreateDatabaseAsync(
            task6Migration);
        var workspaceId = Uuid7.NewGuid();
        var deviceId = Uuid7.NewGuid();
        var sessionId = Uuid7.NewGuid();
        var outboxId = Uuid7.NewGuid();
        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                INSERT INTO platform.workspaces (
                    id, code, display_name, status,
                    created_at_utc, updated_at_utc, version)
                VALUES (
                    @workspace_id, 'task7-upgrade',
                    'Task 7 Upgrade Workspace', 1,
                    @now, @now, 1);

                INSERT INTO platform.devices (
                    id, workspace_id, installation_id, name, platform,
                    status, last_seen_at_utc,
                    created_at_utc, updated_at_utc, version)
                VALUES (
                    @device_id, @workspace_id, 'upgrade-device',
                    'Upgrade Device', 'Testing', 1, NULL,
                    @now, @now, 1);

                INSERT INTO procurement.receiving_session_pocs (
                    id, workspace_id, status, editor_device_id,
                    lease_id, lease_expires_at_utc,
                    last_lease_heartbeat_at_utc,
                    next_expected_local_sequence, entry_count,
                    processed_total_weight_kg,
                    submitted_at_utc, approved_at_utc,
                    approved_by_device_id,
                    created_at_utc, updated_at_utc, version)
                VALUES (
                    @session_id, @workspace_id, 1, @device_id,
                    @lease_id, @lease_expires, @now,
                    2, 0, 0.000000,
                    NULL, NULL, NULL,
                    @now, @now, 1);

                INSERT INTO platform.outbox_messages (
                    id, workspace_id, event_stream, event_type,
                    event_version, aggregate_type, aggregate_id,
                    aggregate_version, payload_json, correlation_id,
                    occurred_at_utc, status, attempt_count)
                VALUES (
                    @outbox_id, @workspace_id, 1, 'UpgradeEvidence',
                    1, 'Upgrade', @session_id,
                    1, '{}'::jsonb, @correlation,
                    @now, 1, 0);
                """;
            command.Parameters.AddWithValue("workspace_id", workspaceId);
            command.Parameters.AddWithValue("device_id", deviceId);
            command.Parameters.AddWithValue("session_id", sessionId);
            command.Parameters.AddWithValue("lease_id", Uuid7.NewGuid());
            command.Parameters.AddWithValue(
                "lease_expires",
                UtcNow.AddMinutes(5));
            command.Parameters.AddWithValue("outbox_id", outboxId);
            command.Parameters.AddWithValue(
                "correlation",
                Uuid7.NewGuid().ToString("D"));
            command.Parameters.AddWithValue("now", UtcNow);
            await command.ExecuteNonQueryAsync(CancellationToken);
        }

        await database.ApplyMigrationsAsync();
        await using var verification =
            await database.OpenConnectionAsync();
        Assert.Equal(
            5,
            await ScalarAsync<long>(
                verification,
                """
                SELECT count(*)
                FROM information_schema.tables
                WHERE table_schema = 'platform'
                  AND table_name IN (
                    'user_credentials',
                    'device_credentials',
                    'device_activation_codes',
                    'refresh_token_families',
                    'refresh_tokens')
                """));
        Assert.True(
            await ScalarAsync<long>(
                verification,
                """
                SELECT count(*)
                FROM pg_trigger t
                JOIN pg_class c ON c.oid = t.tgrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'platform'
                  AND NOT t.tgisinternal
                  AND c.relname IN (
                    'workspaces',
                    'user_credentials',
                    'device_credentials',
                    'device_activation_codes',
                    'refresh_token_families',
                    'refresh_tokens')
                """) >= 11);
        Assert.Equal(
            4,
            await ScalarAsync<long>(
                verification,
                """
                SELECT count(*)
                FROM information_schema.columns
                WHERE table_schema = 'platform'
                  AND table_name = 'device_activation_codes'
                  AND column_name IN (
                    'redemption_idempotency_key_hash',
                    'redemption_request_hash',
                    'replay_protected_result',
                    'replay_allowed_until_utc')
                """));
        Assert.Equal(
            1,
            await ScalarAsync<long>(
                verification,
                """
                SELECT count(*)
                FROM pg_indexes
                WHERE schemaname = 'platform'
                  AND tablename = 'device_activation_codes'
                  AND indexname =
                    'ux_device_activation_codes_workspace_redemption_key'
                """));
        Assert.Equal(
            1,
            await ScalarAsync<long>(
                verification,
                """
                SELECT count(*)
                FROM pg_constraint
                WHERE conname =
                    'ck_device_activation_codes_replay_state'
                """));
        Assert.Equal(
            1,
            await ScalarAsync<long>(
                verification,
                """
                SELECT count(*)
                FROM platform.__ef_migrations_history
                WHERE "MigrationId" =
                    '20260809090000_CrashSafeDeviceActivation'
                """));
        var expectedCode =
            $"TP-{workspaceId.ToString("N").ToUpperInvariant()}";
        Assert.Equal(
            expectedCode,
            await ScalarAsync<string>(
                verification,
                """
                SELECT normalized_workspace_code
                FROM platform.workspaces
                WHERE id = @workspace_id
                """,
                ("workspace_id", workspaceId)));
        Assert.Equal(
            1L,
            await ScalarAsync<long>(
                verification,
                """
                SELECT count(*)
                FROM procurement.receiving_session_pocs
                WHERE id = @session_id
                """,
                ("session_id", sessionId)));
        Assert.Equal(
            1L,
            await ScalarAsync<long>(
                verification,
                """
                SELECT count(*)
                FROM platform.outbox_messages
                WHERE id = @outbox_id
                """,
                ("outbox_id", outboxId)));
    }

    [Fact]
    public async Task Bootstrap_activation_login_and_database_revalidated_me_work()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        setup = await BootstrapAsync(factory);
        var ownerSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var operatorSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OperatorDeviceId,
            setup.OperatorActivationCode);
        var owner = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            ownerSecret);
        var operatorLogin = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "operator",
            OperatorPassword,
            setup.OperatorDeviceId,
            operatorSecret);

        using var meRequest = BearerRequest(
            HttpMethod.Get,
            "/api/v1/auth/me",
            owner.AccessToken);
        meRequest.Headers.Add(
            "X-TraderPro-Workspace-ID",
            Uuid7.NewGuid().ToString("D"));
        meRequest.Headers.Add(
            "X-TraderPro-Device-ID",
            Uuid7.NewGuid().ToString("D"));
        using var client = factory.CreateClient();
        using var me = await client.SendAsync(
            meRequest,
            CancellationToken);
        using var meJson = await ReadJsonAsync(me);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(
            setup.WorkspaceId,
            meJson.RootElement
                .GetProperty("workspace")
                .GetProperty("workspaceId")
                .GetGuid());
        Assert.Equal(
            "Owner",
            meJson.RootElement
                .GetProperty("user")
                .GetProperty("role")
                .GetString());
        Assert.Equal("Operator", operatorLogin.Role);

        await using var context = database.CreateContext(
            setup.WorkspaceId,
            UtcNow);
        Assert.Equal(1, await context.Companies.CountAsync(CancellationToken));
        Assert.Equal(1, await context.Branches.CountAsync(CancellationToken));
        Assert.Equal(2, await context.Users.CountAsync(CancellationToken));
        Assert.Equal(2, await context.Devices.CountAsync(CancellationToken));
        Assert.Equal(
            2,
            await context.UserCredentials.CountAsync(CancellationToken));
        Assert.DoesNotContain(
            OwnerPassword,
            (await context.UserCredentials.ToListAsync(CancellationToken))
                .Select(item => item.PasswordHash));
        Assert.All(
            await context.DeviceCredentials.ToListAsync(CancellationToken),
            item => Assert.Equal(64, item.DeviceSecretHash.Length));
        Assert.All(
            await context.DeviceActivationCodes.ToListAsync(CancellationToken),
            item =>
            {
                Assert.Equal(64, item.CodeHash.Length);
                Assert.NotEqual(
                    setup.OwnerActivationCode,
                    item.CodeHash);
            });
        Assert.All(
            await context.RefreshTokens.ToListAsync(CancellationToken),
            item =>
            {
                Assert.Equal(64, item.TokenHash.Length);
                Assert.NotEqual(owner.RefreshToken, item.TokenHash);
            });
    }

    [Fact]
    public async Task Endpoint_metadata_binds_commercial_context_outside_identity_prefixes()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var login = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        using var request = BearerRequest(
            HttpMethod.Get,
            "/api/v1/testing/commercial-context",
            login.AccessToken);
        request.Headers.Add(
            "X-TraderPro-Workspace-ID",
            Uuid7.NewGuid().ToString("D"));
        request.Headers.Add(
            "X-TraderPro-Device-ID",
            Uuid7.NewGuid().ToString("D"));
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(
            request,
            CancellationToken);
        using var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            setup.WorkspaceId,
            json.RootElement.GetProperty("workspaceId").GetGuid());
        Assert.Equal(
            setup.OwnerDeviceId,
            json.RootElement.GetProperty("deviceId").GetGuid());
    }

    [Fact]
    public async Task Poc_route_ignores_authorization_header_and_keeps_temporary_context()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: true,
            identityBootstrapEnabled: true);
        var setup = await BootstrapAsync(factory);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/spikes/command-probes/");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        request.Headers.Add(
            "X-TraderPro-Workspace-ID",
            setup.WorkspaceId.ToString("D"));
        request.Headers.Add("Idempotency-Key", "poc-commercial-isolation");
        request.Content = JsonContent.Create(new { name = "isolated-poc" });
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(
            request,
            CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Access_token_validation_rejects_invalid_authority()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var login = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        var companyId = ReadGuidClaim(login.AccessToken, "cid");
        var branchId = ReadGuidClaim(login.AccessToken, "bid");

        var invalidTokens = new[]
        {
            IssueAccessToken(
                factory.SigningKey,
                "TraderPro.WrongIssuer",
                TraderProApiFactory.TestAudience,
                UtcNow,
                setup,
                companyId,
                branchId,
                login.FamilyId),
            IssueAccessToken(
                factory.SigningKey,
                TraderProApiFactory.TestIssuer,
                "TraderPro.WrongAudience",
                UtcNow,
                setup,
                companyId,
                branchId,
                login.FamilyId),
            IssueAccessToken(
                Convert.ToBase64String(
                    System.Security.Cryptography.RandomNumberGenerator
                        .GetBytes(32)),
                TraderProApiFactory.TestIssuer,
                TraderProApiFactory.TestAudience,
                UtcNow,
                setup,
                companyId,
                branchId,
                login.FamilyId),
            IssueAccessToken(
                factory.SigningKey,
                TraderProApiFactory.TestIssuer,
                TraderProApiFactory.TestAudience,
                UtcNow.AddMinutes(-16),
                setup,
                companyId,
                branchId,
                login.FamilyId),
            IssueAccessToken(
                factory.SigningKey,
                TraderProApiFactory.TestIssuer,
                TraderProApiFactory.TestAudience,
                UtcNow,
                setup with { WorkspaceId = Uuid7.NewGuid() },
                companyId,
                branchId,
                login.FamilyId),
        };

        foreach (var token in invalidTokens)
        {
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                await MeStatusAsync(factory, token));
        }
    }

    [Fact]
    public async Task Lockout_is_atomic_and_unlocks_using_server_clock()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var response = await SendLoginAsync(
                factory,
                setup.WorkspaceCode,
                "owner",
                "incorrect passphrase",
                setup.OwnerDeviceId,
                secret);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using (var locked = await SendLoginAsync(
                   factory,
                   setup.WorkspaceCode,
                   "owner",
                   OwnerPassword,
                   setup.OwnerDeviceId,
                   secret))
        using (var lockedJson = await ReadJsonAsync(locked))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
            Assert.Equal(
                "AUTHENTICATION_TEMPORARILY_LOCKED",
                ErrorCode(lockedJson));
        }

        await using var laterFactory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false,
            utcNow: UtcNow.AddMinutes(16));
        var login = await LoginAsync(
            laterFactory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        await using var context = database.CreateContext(
            setup.WorkspaceId,
            UtcNow.AddMinutes(16));
        Assert.Equal(
            0,
            (await context.UserCredentials.SingleAsync(
                item => item.UserId == setup.OwnerUserId,
                CancellationToken)).FailedSignInCount);
    }

    [Fact]
    public async Task Refresh_rotates_replays_concurrently_and_revokes_reuse()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false,
            identityBootstrapEnabled: true,
            refreshReplaySeconds: 10);
        Assert.Equal(
            10,
            factory.Services
                .GetRequiredService<TraderProAuthenticationOptions>()
                .RefreshReplaySeconds);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var login = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        var first = await RefreshAsync(factory, login.RefreshToken);
        var retry = await RefreshAsync(factory, login.RefreshToken);
        Assert.Equal(first.RefreshToken, retry.RefreshToken);

        var concurrentLogin = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        var concurrent = await Task.WhenAll(
            RefreshAsync(factory, concurrentLogin.RefreshToken),
            RefreshAsync(factory, concurrentLogin.RefreshToken));
        Assert.Equal(
            concurrent[0].RefreshToken,
            concurrent[1].RefreshToken);

        DateTimeOffset replayDeadline;
        await using (var replayContext = database.CreateContext(
                         setup.WorkspaceId))
        {
            replayDeadline = (await replayContext.RefreshTokens
                    .SingleAsync(
                        item =>
                            item.FamilyId == login.FamilyId &&
                            item.ConsumedAtUtc != null,
                        CancellationToken))
                .ReplayAllowedUntilUtc!.Value;
        }

        var waitUntilOutsideWindow =
            replayDeadline - DateTimeOffset.UtcNow +
            TimeSpan.FromSeconds(1);
        if (waitUntilOutsideWindow > TimeSpan.Zero)
        {
            await Task.Delay(
                waitUntilOutsideWindow,
                CancellationToken);
        }

        using var reused = await SendRefreshAsync(
            factory,
            login.RefreshToken);
        using var reusedJson = await ReadJsonAsync(reused);
        Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
        Assert.Equal(
            "REFRESH_TOKEN_REUSE_DETECTED",
            ErrorCode(reusedJson));

        await using var context = database.CreateContext(setup.WorkspaceId);
        var family = await context.RefreshTokenFamilies.SingleAsync(
            item => item.Id == login.FamilyId,
            CancellationToken);
        Assert.NotNull(family.RevokedAtUtc);
        Assert.Equal(
            2,
            await context.RefreshTokens.CountAsync(
                item => item.FamilyId == family.Id,
                CancellationToken));
        var consumed = await context.RefreshTokens.SingleAsync(
            item =>
                item.FamilyId == family.Id &&
                item.ConsumedAtUtc != null,
            CancellationToken);
        Assert.NotEqual(first.RefreshToken, consumed.ReplayProtectedToken);
        Assert.Equal(
            1,
            await context.AuditEvents.CountAsync(
                item =>
                    item.Action ==
                    "Platform.Identity.RefreshTokenReuseDetected",
                CancellationToken));
    }

    [Fact]
    public async Task Obsolete_predecessor_after_successor_rotation_revokes_family_once()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var tokenA = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        var tokenB = await RefreshAsync(factory, tokenA.RefreshToken);
        var tokenC = await RefreshAsync(factory, tokenB.RefreshToken);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var reused = await SendRefreshAsync(
                factory,
                tokenA.RefreshToken);
            using var reusedJson = await ReadJsonAsync(reused);
            Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
            Assert.Equal(
                "REFRESH_TOKEN_REUSE_DETECTED",
                ErrorCode(reusedJson));
        }

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await MeStatusAsync(factory, tokenC.AccessToken));
        await using var context = database.CreateContext(setup.WorkspaceId);
        var predecessorHash = IdentitySecretCryptography.Hash(
            tokenA.RefreshToken);
        var predecessor = await context.RefreshTokens.SingleAsync(
            item => item.TokenHash == predecessorHash,
            CancellationToken);
        Assert.Null(predecessor.ReplayProtectedToken);
        Assert.Null(predecessor.ReplayAllowedUntilUtc);
        Assert.Equal(
            1,
            await context.AuditEvents.CountAsync(
                item =>
                    item.Action ==
                    "Platform.Identity.RefreshTokenReuseDetected",
                CancellationToken));
    }

    [Theory]
    [InlineData("/api/v1/auth/logout")]
    [InlineData("/api/v1/auth/logout-all")]
    public async Task Refresh_racing_logout_leaves_no_usable_rotation(
        string logoutPath)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var login = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);

        var refreshTask = SendRefreshAsync(factory, login.RefreshToken);
        var logoutTask = SendAuthenticatedPostAsync(
            factory,
            logoutPath,
            login.AccessToken);
        var responses = await Task.WhenAll(refreshTask, logoutTask);
        using var refresh = responses[0];
        using var logout = responses[1];
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        if (refresh.IsSuccessStatusCode)
        {
            using var refreshJson = await ReadJsonAsync(refresh);
            var accessToken = refreshJson.RootElement
                .GetProperty("accessToken")
                .GetString()!;
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                await MeStatusAsync(factory, accessToken));
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.NotNull(
            (await context.RefreshTokenFamilies.SingleAsync(
                item => item.Id == login.FamilyId,
                CancellationToken)).RevokedAtUtc);
    }

    [Fact]
    public async Task Login_blocked_on_device_is_observed_by_logout_all()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var existing = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        await using var blocker = await database.OpenConnectionAsync();
        await using var blockerTransaction =
            await blocker.BeginTransactionAsync(CancellationToken);
        await AcquireAdvisoryLockAsync(
            blocker,
            $"30:device-credential:{setup.WorkspaceId:D}:{setup.OwnerDeviceId:D}");

        var loginTask = SendLoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        await WaitForAdvisoryWaiterAsync(database);
        var logoutAllTask = SendAuthenticatedPostAsync(
            factory,
            "/api/v1/auth/logout-all",
            existing.AccessToken);
        await blockerTransaction.CommitAsync(CancellationToken);
        using var loginResponse = await loginTask;
        using var logoutAll = await logoutAllTask;
        using var loginJson = await ReadJsonAsync(loginResponse);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, logoutAll.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await MeStatusAsync(
                factory,
                loginJson.RootElement
                    .GetProperty("accessToken")
                    .GetString()!));
    }

    [Fact]
    public async Task Current_logout_is_idempotent_and_invalidates_access()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var login = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        using var client = factory.CreateClient();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = BearerRequest(
                HttpMethod.Post,
                attempt == 0
                    ? "/api/v1/auth/logout"
                    : "/api/v1/auth/logout/",
                login.AccessToken);
            using var response = await client.SendAsync(
                request,
                CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await MeStatusAsync(factory, login.AccessToken));
        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(
            1,
            await context.AuditEvents.CountAsync(
                item =>
                    item.Action ==
                    "Platform.Identity.SessionLoggedOut",
                CancellationToken));
    }

    [Fact]
    public async Task Logout_all_revokes_only_the_current_user()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var ownerSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var operatorSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OperatorDeviceId,
            setup.OperatorActivationCode);
        var ownerOne = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            ownerSecret);
        var ownerTwo = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            ownerSecret);
        var operatorLogin = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "operator",
            OperatorPassword,
            setup.OperatorDeviceId,
            operatorSecret);

        using var logout = BearerRequest(
            HttpMethod.Post,
            "/api/v1/auth/logout-all",
            ownerOne.AccessToken);
        using var client = factory.CreateClient();
        using var logoutResponse = await client.SendAsync(
            logout,
            CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await MeStatusAsync(factory, ownerOne.AccessToken));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await MeStatusAsync(factory, ownerTwo.AccessToken));
        Assert.Equal(
            HttpStatusCode.OK,
            await MeStatusAsync(factory, operatorLogin.AccessToken));
    }

    [Fact]
    public async Task Database_role_and_credential_version_override_token_claims()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var login = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);

        await using (var context = database.CreateContext(
                         setup.WorkspaceId,
                         UtcNow.AddMinutes(1)))
        {
            var owner = await context.Users.SingleAsync(
                item => item.Id == setup.OwnerUserId,
                CancellationToken);
            owner.SetRole(TraderProRole.Operator);
            await context.SaveChangesAsync(CancellationToken);
        }

        using var client = factory.CreateClient();
        using (var meRequest = BearerRequest(
                   HttpMethod.Get,
                   "/api/v1/auth/me",
                   login.AccessToken))
        using (var me = await client.SendAsync(
                   meRequest,
                   CancellationToken))
        using (var meJson = await ReadJsonAsync(me))
        {
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            Assert.Equal(
                "Operator",
                meJson.RootElement
                    .GetProperty("user")
                    .GetProperty("role")
                    .GetString());
        }

        using (var issue = BearerRequest(
                   HttpMethod.Post,
                   $"/api/v1/devices/{setup.OwnerDeviceId:D}/activation-codes",
                   login.AccessToken))
        {
            issue.Headers.Add("Idempotency-Key", "stale-owner-role");
            using var forbidden = await client.SendAsync(
                issue,
                CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }

        await using (var context = database.CreateContext(
                         setup.WorkspaceId,
                         UtcNow.AddMinutes(2)))
        {
            var credential = await context.UserCredentials.SingleAsync(
                item => item.UserId == setup.OwnerUserId,
                CancellationToken);
            credential.ChangePassword(
                "changed-password-hash",
                UtcNow.AddMinutes(2));
            await context.SaveChangesAsync(CancellationToken);
        }

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await MeStatusAsync(factory, login.AccessToken));
    }

    [Fact]
    public async Task Reactivation_reuses_device_row_and_revokes_old_sessions()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var oldSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var login = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            oldSecret);
        using var issue = BearerRequest(
            HttpMethod.Post,
            $"/api/v1/devices/{setup.OwnerDeviceId:D}/activation-codes",
            login.AccessToken);
        issue.Headers.Add("Idempotency-Key", "reactivate-owner-device");
        using var client = factory.CreateClient();
        using var issueResponse = await client.SendAsync(
            issue,
            CancellationToken);
        using var issueJson = await ReadJsonAsync(issueResponse);
        Assert.Equal(HttpStatusCode.Created, issueResponse.StatusCode);
        var code = issueJson.RootElement
            .GetProperty("activationCode")
            .GetString()!;
        var newSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            code);

        Assert.NotEqual(oldSecret, newSecret);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await MeStatusAsync(factory, login.AccessToken));
        using (var oldLogin = await SendLoginAsync(
                   factory,
                   setup.WorkspaceCode,
                   "owner",
                   OwnerPassword,
                   setup.OwnerDeviceId,
                   oldSecret))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        }

        var newLogin = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            newSecret);
        Assert.False(string.IsNullOrWhiteSpace(newLogin.AccessToken));
        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(2, await context.Devices.CountAsync(CancellationToken));
        var credential = await context.DeviceCredentials.SingleAsync(
            item => item.DeviceId == setup.OwnerDeviceId,
            CancellationToken);
        Assert.Equal(2, credential.SecretVersion);
        Assert.True(
            IdentitySecretCryptography.VerifyHash(
                newSecret,
                credential.DeviceSecretHash));
    }

    [Fact]
    public async Task Login_racing_device_reactivation_cannot_leave_old_session()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var oldSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var owner = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            oldSecret);
        var activationCode = await IssueActivationCodeAsync(
            factory,
            setup.OwnerDeviceId,
            owner.AccessToken,
            "login-reactivation-race");

        var loginTask = SendLoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            oldSecret);
        var activationTask = SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            activationCode);
        var responses = await Task.WhenAll(loginTask, activationTask);
        using var loginResponse = responses[0];
        using var activationResponse = responses[1];
        using var activationJson = await ReadJsonAsync(activationResponse);

        Assert.Equal(HttpStatusCode.OK, activationResponse.StatusCode);
        if (loginResponse.IsSuccessStatusCode)
        {
            using var loginJson = await ReadJsonAsync(loginResponse);
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                await MeStatusAsync(
                    factory,
                    loginJson.RootElement
                        .GetProperty("accessToken")
                        .GetString()!));
        }

        var currentSecret = activationJson.RootElement
            .GetProperty("deviceSecret")
            .GetString()!;
        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(2, await context.Devices.CountAsync(CancellationToken));
        var credential = await context.DeviceCredentials.SingleAsync(
            item => item.DeviceId == setup.OwnerDeviceId,
            CancellationToken);
        Assert.True(
            IdentitySecretCryptography.VerifyHash(
                currentSecret,
                credential.DeviceSecretHash));
    }

    [Fact]
    public async Task Refresh_racing_device_reactivation_cannot_survive_rotation()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var oldSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var login = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            oldSecret);
        var activationCode = await IssueActivationCodeAsync(
            factory,
            setup.OwnerDeviceId,
            login.AccessToken,
            "refresh-reactivation-race");

        var refreshTask = SendRefreshAsync(factory, login.RefreshToken);
        var activationTask = SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            activationCode);
        var responses = await Task.WhenAll(refreshTask, activationTask);
        using var refreshResponse = responses[0];
        using var activationResponse = responses[1];

        Assert.Equal(HttpStatusCode.OK, activationResponse.StatusCode);
        if (refreshResponse.IsSuccessStatusCode)
        {
            using var refreshJson = await ReadJsonAsync(refreshResponse);
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                await MeStatusAsync(
                    factory,
                    refreshJson.RootElement
                        .GetProperty("accessToken")
                        .GetString()!));
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.NotNull(
            (await context.RefreshTokenFamilies.SingleAsync(
                item => item.Id == login.FamilyId,
                CancellationToken)).RevokedAtUtc);
    }

    [Fact]
    public async Task Owner_can_issue_but_operator_and_cross_workspace_cannot()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var ownerSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var operatorSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OperatorDeviceId,
            setup.OperatorActivationCode);
        var owner = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            ownerSecret);
        var operatorLogin = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "operator",
            OperatorPassword,
            setup.OperatorDeviceId,
            operatorSecret);
        using var operatorIssue = BearerRequest(
            HttpMethod.Post,
            $"/api/v1/devices/{setup.OwnerDeviceId:D}/activation-codes",
            operatorLogin.AccessToken);
        operatorIssue.Headers.Add("Idempotency-Key", "operator-forbidden");
        using var client = factory.CreateClient();
        using var forbidden = await client.SendAsync(
            operatorIssue,
            CancellationToken);
        using var forbiddenJson = await ReadJsonAsync(forbidden);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("OWNER_ROLE_REQUIRED", ErrorCode(forbiddenJson));

        using var genericDenied = BearerRequest(
            HttpMethod.Get,
            "/api/v1/testing/commercial-context/denied",
            owner.AccessToken);
        using var genericDeniedResponse = await client.SendAsync(
            genericDenied,
            CancellationToken);
        using var genericDeniedJson = await ReadJsonAsync(
            genericDeniedResponse);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            genericDeniedResponse.StatusCode);
        Assert.Equal(
            "AUTHORIZATION_DENIED",
            ErrorCode(genericDeniedJson));

        var otherWorkspace = Workspace.Create(
            $"other-{Guid.NewGuid():N}",
            "Other Workspace",
            UtcNow);
        await using (var unscoped = database.CreateContext(null, UtcNow))
        {
            unscoped.Workspaces.Add(otherWorkspace);
            await unscoped.SaveChangesAsync(CancellationToken);
        }

        var otherDevice = Device.Create(
            otherWorkspace.Id,
            "other-device",
            "Other Device",
            "Testing",
            UtcNow);
        await using (var otherContext = database.CreateContext(
                         otherWorkspace.Id,
                         UtcNow))
        {
            otherContext.Devices.Add(otherDevice);
            await otherContext.SaveChangesAsync(CancellationToken);
        }

        using var crossWorkspace = BearerRequest(
            HttpMethod.Post,
            $"/api/v1/devices/{otherDevice.Id:D}/activation-codes",
            owner.AccessToken);
        crossWorkspace.Headers.Add("Idempotency-Key", "cross-workspace");
        using var hidden = await client.SendAsync(
            crossWorkspace,
            CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task Different_key_activation_issuance_leaves_one_active_code_and_retry_is_precise()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var ownerSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var owner = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            ownerSecret);

        var firstTask = SendIssueActivationCodeAsync(
            factory,
            setup.OperatorDeviceId,
            owner.AccessToken,
            "different-key-one");
        var secondTask = SendIssueActivationCodeAsync(
            factory,
            setup.OperatorDeviceId,
            owner.AccessToken,
            "different-key-two");
        var responses = await Task.WhenAll(firstTask, secondTask);
        using var first = responses[0];
        using var second = responses[1];
        using var firstJson = await ReadJsonAsync(first);
        using var secondJson = await ReadJsonAsync(second);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        AssertNoStore(first);
        AssertNoStore(second);
        var issued = new[]
        {
            (
                Key: "different-key-one",
                Code: firstJson.RootElement
                    .GetProperty("activationCode")
                    .GetString()!),
            (
                Key: "different-key-two",
                Code: secondJson.RootElement
                    .GetProperty("activationCode")
                    .GetString()!),
        };

        string activeHash;
        await using (var context = database.CreateContext(setup.WorkspaceId))
        {
            var active = await context.DeviceActivationCodes
                .Where(item =>
                    item.DeviceId == setup.OperatorDeviceId &&
                    item.UsedAtUtc == null &&
                    item.RevokedAtUtc == null)
                .ToListAsync(CancellationToken);
            Assert.Single(active);
            activeHash = active[0].CodeHash;
        }

        var activeIssue = Assert.Single(
            issued,
            item =>
                IdentitySecretCryptography.Hash(item.Code) == activeHash);
        using var retry = await SendIssueActivationCodeAsync(
            factory,
            setup.OperatorDeviceId,
            owner.AccessToken,
            activeIssue.Key);
        using var retryJson = await ReadJsonAsync(retry);
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Equal(
            "DEVICE_ACTIVATION_CODE_RESPONSE_NOT_REPLAYABLE",
            ErrorCode(retryJson));
        Assert.False(
            retryJson.RootElement
                .GetProperty("error")
                .TryGetProperty("activationCode", out _));
        AssertNoStore(retry);

        var redeemed = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OperatorDeviceId,
            activeIssue.Code);
        Assert.False(string.IsNullOrWhiteSpace(redeemed));
    }

    [Fact]
    public async Task Initial_activation_replay_normalizes_PostgreSql_timestamp_precision()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(
            database,
            UtcNow.AddTicks(7));
        var setup = await BootstrapAsync(factory);
        const string idempotencyKey = "initial-activation-precision-replay";

        using var committed = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);
        using var committedJson = await ReadJsonAsync(committed);
        Assert.Equal(HttpStatusCode.OK, committed.StatusCode);

        using var replayed = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);
        using var replayedJson = await ReadJsonAsync(replayed);
        Assert.Equal(HttpStatusCode.OK, replayed.StatusCode);
        Assert.Equal(
            committedJson.RootElement
                .GetProperty("deviceSecret")
                .GetString(),
            replayedJson.RootElement
                .GetProperty("deviceSecret")
                .GetString());
        Assert.Equal(
            committedJson.RootElement
                .GetProperty("secretVersion")
                .GetInt32(),
            replayedJson.RootElement
                .GetProperty("secretVersion")
                .GetInt32());
    }

    [Fact]
    public async Task Committed_activation_with_lost_response_is_recovered_without_rotation_or_duplicate_audit()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var previousSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var previousLogin = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            previousSecret);
        using var issue = BearerRequest(
            HttpMethod.Post,
            $"/api/v1/devices/{setup.OwnerDeviceId:D}/activation-codes",
            previousLogin.AccessToken);
        issue.Headers.Add(
            "Idempotency-Key",
            "lost-response-reactivation-code");
        using var client = factory.CreateClient();
        using var issueResponse = await client.SendAsync(
            issue,
            CancellationToken);
        using var issueJson = await ReadJsonAsync(issueResponse);
        Assert.Equal(HttpStatusCode.Created, issueResponse.StatusCode);
        var reactivationCode = issueJson.RootElement
            .GetProperty("activationCode")
            .GetString()!;
        const string idempotencyKey = "lost-activation-response";

        using (var lostResponse = await SendActivationAsync(
                   factory,
                   setup.WorkspaceCode,
                   setup.OwnerDeviceId,
                   reactivationCode,
                   idempotencyKey))
        {
            Assert.Equal(HttpStatusCode.OK, lostResponse.StatusCode);
        }

        using var recovered = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            reactivationCode,
            idempotencyKey);
        using var recoveredJson = await ReadJsonAsync(recovered);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        AssertNoStore(recovered);
        var recoveredSecret = recoveredJson.RootElement
            .GetProperty("deviceSecret")
            .GetString()!;

        await using var context = database.CreateContext(setup.WorkspaceId);
        var credential = await context.DeviceCredentials.SingleAsync(
            item => item.DeviceId == setup.OwnerDeviceId,
            CancellationToken);
        var code = await context.DeviceActivationCodes.SingleAsync(
            item => item.RedemptionIdempotencyKeyHash ==
                IdentitySecretCryptography.Hash(idempotencyKey),
            CancellationToken);
        Assert.Equal(2, credential.SecretVersion);
        Assert.True(
            IdentitySecretCryptography.VerifyHash(
                recoveredSecret,
                credential.DeviceSecretHash));
        Assert.Equal(
            IdentitySecretCryptography.Hash(idempotencyKey),
            code.RedemptionIdempotencyKeyHash);
        Assert.Equal(64, code.RedemptionRequestHash!.Length);
        Assert.NotEqual(reactivationCode, code.CodeHash);
        Assert.NotNull(code.ReplayProtectedResult);
        Assert.DoesNotContain(
            recoveredSecret,
            code.ReplayProtectedResult!,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            reactivationCode,
            code.ReplayProtectedResult!,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            idempotencyKey,
            code.ReplayProtectedResult!,
            StringComparison.Ordinal);
        var reactivationAudit = await context.AuditEvents.SingleAsync(
            item =>
                item.AggregateId == setup.OwnerDeviceId &&
                item.Action == "Platform.Identity.DeviceReactivated",
            CancellationToken);
        var persistedAudit = JsonSerializer.Serialize(reactivationAudit);
        Assert.DoesNotContain(
            recoveredSecret,
            persistedAudit,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            reactivationCode,
            persistedAudit,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            idempotencyKey,
            persistedAudit,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            code.ReplayProtectedResult!,
            persistedAudit,
            StringComparison.Ordinal);
        var predecessorFamily = await context.RefreshTokenFamilies.SingleAsync(
            item => item.DeviceId == setup.OwnerDeviceId,
            CancellationToken);
        Assert.NotNull(predecessorFamily.RevokedAtUtc);
        Assert.Equal("DeviceReactivated", predecessorFamily.RevocationReason);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            await MeStatusAsync(factory, previousLogin.AccessToken));
        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE platform.device_activation_codes
            SET redemption_request_hash = repeat('0', 64),
                version = version + 1
            WHERE id = @id
            """,
            ("id", code.Id));
        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE platform.device_activation_codes
            SET replay_protected_result = 'replacement-ciphertext',
                version = version + 1
            WHERE id = @id
            """,
            ("id", code.Id));
        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE platform.device_activation_codes
            SET replay_allowed_until_utc =
                    replay_allowed_until_utc + interval '1 second',
                version = version + 1
            WHERE id = @id
            """,
            ("id", code.Id));
        await AssertSqlRejectedAsync(
            database,
            """
            INSERT INTO platform.device_activation_codes (
                id, workspace_id, device_id, code_hash, expires_at_utc,
                used_at_utc, revoked_at_utc,
                redemption_idempotency_key_hash,
                redemption_request_hash, replay_protected_result,
                replay_allowed_until_utc, issued_by_user_id,
                created_at_utc, version)
            SELECT
                @new_id, workspace_id, device_id, repeat('1', 64),
                expires_at_utc, used_at_utc, NULL, repeat('2', 64),
                NULL, 'ciphertext', replay_allowed_until_utc,
                issued_by_user_id, created_at_utc, 1
            FROM platform.device_activation_codes
            WHERE id = @id
            """,
            ("new_id", Uuid7.NewGuid()),
            ("id", code.Id));
        await AssertSqlRejectedAsync(
            database,
            """
            INSERT INTO platform.device_activation_codes (
                id, workspace_id, device_id, code_hash, expires_at_utc,
                used_at_utc, revoked_at_utc,
                redemption_idempotency_key_hash,
                redemption_request_hash, replay_protected_result,
                replay_allowed_until_utc, issued_by_user_id,
                created_at_utc, version)
            SELECT
                @new_id, workspace_id, device_id, repeat('3', 64),
                expires_at_utc, used_at_utc, NULL, repeat('4', 64),
                repeat('5', 64), 'ciphertext',
                used_at_utc + interval '2 days', issued_by_user_id,
                created_at_utc, 1
            FROM platform.device_activation_codes
            WHERE id = @id
            """,
            ("new_id", Uuid7.NewGuid()),
            ("id", code.Id));
        await AssertSqlRejectedAsync(
            database,
            """
            INSERT INTO platform.device_activation_codes (
                id, workspace_id, device_id, code_hash, expires_at_utc,
                used_at_utc, revoked_at_utc,
                redemption_idempotency_key_hash,
                redemption_request_hash, replay_protected_result,
                replay_allowed_until_utc, issued_by_user_id,
                created_at_utc, version)
            SELECT
                @new_id, workspace_id, device_id, repeat('6', 64),
                expires_at_utc, used_at_utc, NULL,
                redemption_idempotency_key_hash, repeat('7', 64),
                'ciphertext', replay_allowed_until_utc,
                issued_by_user_id, created_at_utc, 1
            FROM platform.device_activation_codes
            WHERE id = @id
            """,
            ("new_id", Uuid7.NewGuid()),
            ("id", code.Id));
    }

    [Fact]
    public async Task Activation_exact_retry_survives_delay_and_server_restart()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var clock = new TestClock(UtcNow);
        var firstFactory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false,
            identityBootstrapEnabled: true,
            testClock: clock);
        var setup = await BootstrapAsync(firstFactory);
        const string idempotencyKey = "restart-safe-activation";
        using var first = await SendActivationAsync(
            firstFactory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);
        using var firstJson = await ReadJsonAsync(first);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstSecret = firstJson.RootElement
            .GetProperty("deviceSecret")
            .GetString()!;
        var keyRingPath = firstFactory.DataProtectionKeyRingPath;
        var signingKey = firstFactory.SigningKey;
        clock.Advance(TimeSpan.FromMinutes(5));
        await firstFactory.DisposeAsync();

        await using (var wrongKeyFactory = new TraderProApiFactory(
                         database.ConnectionString,
                         spikesEnabled: false,
                         identityBootstrapEnabled: true,
                         testClock: clock,
                         signingKey: signingKey))
        using (var unavailable = await SendActivationAsync(
                   wrongKeyFactory,
                   setup.WorkspaceCode,
                   setup.OwnerDeviceId,
                   setup.OwnerActivationCode,
                   idempotencyKey))
        using (var unavailableJson = await ReadJsonAsync(unavailable))
        {
            Assert.Equal(
                HttpStatusCode.ServiceUnavailable,
                unavailable.StatusCode);
            Assert.Equal(
                "DEVICE_ACTIVATION_RECOVERY_UNAVAILABLE",
                ErrorCode(unavailableJson));
        }

        await using var restartedFactory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false,
            identityBootstrapEnabled: true,
            testClock: clock,
            signingKey: signingKey,
            dataProtectionKeyRingPath: keyRingPath);
        using var replay = await SendActivationAsync(
            restartedFactory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);
        using var replayJson = await ReadJsonAsync(replay);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(
            firstSecret,
            replayJson.RootElement.GetProperty("deviceSecret").GetString());
        Assert.Equal(
            firstJson.RootElement.GetProperty("activatedAtUtc").GetString(),
            replayJson.RootElement.GetProperty("activatedAtUtc").GetString());
        Assert.Equal(
            firstJson.RootElement.GetProperty("secretVersion").GetInt32(),
            replayJson.RootElement.GetProperty("secretVersion").GetInt32());
    }

    [Fact]
    public async Task Activation_attempt_conflicts_are_precise_and_do_not_rotate()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        const string idempotencyKey = "activation-attempt-one";
        var firstSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);

        using var differentAttempt = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            "activation-attempt-two");
        using var differentAttemptJson = await ReadJsonAsync(
            differentAttempt);
        Assert.Equal(HttpStatusCode.Conflict, differentAttempt.StatusCode);
        Assert.Equal(
            "DEVICE_ACTIVATION_ALREADY_USED",
            ErrorCode(differentAttemptJson));

        using var changedPayload = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey,
            deviceLabel: "Changed device label");
        using var changedPayloadJson = await ReadJsonAsync(changedPayload);
        Assert.Equal(HttpStatusCode.Conflict, changedPayload.StatusCode);
        Assert.Equal(
            "IDEMPOTENCY_PAYLOAD_CONFLICT",
            ErrorCode(changedPayloadJson));

        using var reusedForAnotherCode = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OperatorDeviceId,
            setup.OperatorActivationCode,
            idempotencyKey);
        using var reusedForAnotherCodeJson = await ReadJsonAsync(
            reusedForAnotherCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            reusedForAnotherCode.StatusCode);
        Assert.Equal(
            "IDEMPOTENCY_PAYLOAD_CONFLICT",
            ErrorCode(reusedForAnotherCodeJson));

        await using var context = database.CreateContext(setup.WorkspaceId);
        var credential = await context.DeviceCredentials.SingleAsync(
            item => item.DeviceId == setup.OwnerDeviceId,
            CancellationToken);
        Assert.Equal(1, credential.SecretVersion);
        Assert.True(
            IdentitySecretCryptography.VerifyHash(
                firstSecret,
                credential.DeviceSecretHash));
    }

    [Fact]
    public async Task Expired_activation_recovery_requires_new_owner_code_and_reactivation_rotates_once()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var clock = new TestClock(UtcNow);
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false,
            identityBootstrapEnabled: true,
            testClock: clock);
        var setup = await BootstrapAsync(factory);
        var ownerSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        const string operatorAttempt = "operator-expiring-recovery";
        var oldOperatorSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OperatorDeviceId,
            setup.OperatorActivationCode,
            operatorAttempt);
        clock.Advance(TimeSpan.FromMinutes(16));

        using var expired = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OperatorDeviceId,
            setup.OperatorActivationCode,
            operatorAttempt);
        using var expiredJson = await ReadJsonAsync(expired);
        Assert.Equal(HttpStatusCode.Conflict, expired.StatusCode);
        Assert.Equal(
            "DEVICE_ACTIVATION_RECOVERY_EXPIRED",
            ErrorCode(expiredJson));

        var owner = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            ownerSecret);
        var replacementCode = await IssueActivationCodeAsync(
            factory,
            setup.OperatorDeviceId,
            owner.AccessToken,
            "operator-recovery-replacement-code");
        var replacementSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OperatorDeviceId,
            replacementCode,
            "operator-recovery-replacement-redemption");

        Assert.NotEqual(oldOperatorSecret, replacementSecret);
        await using var context = database.CreateContext(setup.WorkspaceId);
        var credential = await context.DeviceCredentials.SingleAsync(
            item => item.DeviceId == setup.OperatorDeviceId,
            CancellationToken);
        var oldCode = await context.DeviceActivationCodes.SingleAsync(
            item => item.CodeHash == IdentitySecretCryptography.Hash(
                setup.OperatorActivationCode),
            CancellationToken);
        Assert.Equal(2, credential.SecretVersion);
        Assert.Null(oldCode.ReplayProtectedResult);
        Assert.True(
            IdentitySecretCryptography.VerifyHash(
                replacementSecret,
                credential.DeviceSecretHash));
        Assert.Equal(
            1,
            await context.AuditEvents.CountAsync(
                item =>
                    item.AggregateId == setup.OperatorDeviceId &&
                    item.Action == "Platform.Identity.DeviceReactivated",
                CancellationToken));
    }

    [Fact]
    public async Task Disabled_device_blocks_activation_without_consuming_code()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        await using (var context = database.CreateContext(setup.WorkspaceId))
        {
            var device = await context.Devices.SingleAsync(
                item => item.Id == setup.OwnerDeviceId,
                CancellationToken);
            device.SetStatus(DeviceStatus.Disabled);
            await context.SaveChangesAsync(CancellationToken);
        }

        using var response = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            "disabled-device-activation");
        using var json = await ReadJsonAsync(response);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("DEVICE_NOT_ACTIVE", ErrorCode(json));
        await using var verification = database.CreateContext(
            setup.WorkspaceId);
        Assert.Null(
            (await verification.DeviceActivationCodes.SingleAsync(
                item => item.DeviceId == setup.OwnerDeviceId,
                CancellationToken)).UsedAtUtc);
        Assert.False(
            await verification.DeviceCredentials.AnyAsync(
                item => item.DeviceId == setup.OwnerDeviceId,
                CancellationToken));
    }

    [Fact]
    public async Task Wrong_and_expired_activation_codes_have_frozen_errors()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var clock = new TestClock(UtcNow);
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false,
            identityBootstrapEnabled: true,
            testClock: clock);
        var setup = await BootstrapAsync(factory);

        using var wrong = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            "synthetic-wrong-activation-code",
            "wrong-activation-code-attempt");
        using var wrongJson = await ReadJsonAsync(wrong);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal("DEVICE_ACTIVATION_INVALID", ErrorCode(wrongJson));

        clock.Advance(TimeSpan.FromMinutes(16));
        using var expired = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            "expired-unused-activation-code-attempt");
        using var expiredJson = await ReadJsonAsync(expired);
        Assert.Equal(HttpStatusCode.Conflict, expired.StatusCode);
        Assert.Equal("DEVICE_ACTIVATION_EXPIRED", ErrorCode(expiredJson));
    }

    [Fact]
    public async Task Temporary_database_failure_rolls_back_activation_and_same_attempt_retries_safely()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                CREATE FUNCTION platform.fail_activation_once()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    IF OLD.used_at_utc IS NULL AND
                       NEW.used_at_utc IS NOT NULL THEN
                        RAISE EXCEPTION 'synthetic activation failure';
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                CREATE TRIGGER synthetic_activation_failure
                BEFORE UPDATE ON platform.device_activation_codes
                FOR EACH ROW EXECUTE FUNCTION
                    platform.fail_activation_once();
                """;
            await command.ExecuteNonQueryAsync(CancellationToken);
        }

        const string idempotencyKey = "database-failure-activation";
        using var failed = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);
        using var failedJson = await ReadJsonAsync(failed);

        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                DROP TRIGGER synthetic_activation_failure
                    ON platform.device_activation_codes;
                DROP FUNCTION platform.fail_activation_once();
                """;
            await command.ExecuteNonQueryAsync(CancellationToken);
        }

        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal("TEMPORARY_COMMAND_FAILURE", ErrorCode(failedJson));
        using var retried = await SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);
        using var retriedJson = await ReadJsonAsync(retried);
        Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
        var secret = retriedJson.RootElement
            .GetProperty("deviceSecret")
            .GetString()!;
        await using var context = database.CreateContext(setup.WorkspaceId);
        var credential = await context.DeviceCredentials.SingleAsync(
            item => item.DeviceId == setup.OwnerDeviceId,
            CancellationToken);
        Assert.Equal(1, credential.SecretVersion);
        Assert.True(
            IdentitySecretCryptography.VerifyHash(
                secret,
                credential.DeviceSecretHash));
    }

    [Fact]
    public async Task Concurrent_identical_activation_redemption_has_one_logical_result()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        const string idempotencyKey =
            "concurrent-identical-activation-redemption";

        var firstTask = SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);
        var secondTask = SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode,
            idempotencyKey);
        var responses = await Task.WhenAll(firstTask, secondTask);
        using var first = responses[0];
        using var second = responses[1];
        Assert.All(
            responses,
            response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        using var firstJson = await ReadJsonAsync(first);
        using var secondJson = await ReadJsonAsync(second);
        var winningSecret = firstJson.RootElement
            .GetProperty("deviceSecret")
            .GetString()!;
        Assert.Equal(
            winningSecret,
            secondJson.RootElement.GetProperty("deviceSecret").GetString());
        Assert.Equal(
            firstJson.RootElement.GetProperty("secretVersion").GetInt32(),
            secondJson.RootElement.GetProperty("secretVersion").GetInt32());

        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(2, await context.Devices.CountAsync(CancellationToken));
        var credential = await context.DeviceCredentials.SingleAsync(
            item => item.DeviceId == setup.OwnerDeviceId,
            CancellationToken);
        Assert.True(
            IdentitySecretCryptography.VerifyHash(
                winningSecret,
                credential.DeviceSecretHash));
    }

    [Fact]
    public async Task Activation_issuance_racing_redemption_preserves_single_device_state()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var firstSecret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var owner = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            firstSecret);
        var oldCode = await IssueActivationCodeAsync(
            factory,
            setup.OwnerDeviceId,
            owner.AccessToken,
            "issuance-redemption-old");

        var redemptionTask = SendActivationAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            oldCode);
        var issuanceTask = SendIssueActivationCodeAsync(
            factory,
            setup.OwnerDeviceId,
            owner.AccessToken,
            "issuance-redemption-new");
        var responses = await Task.WhenAll(
            redemptionTask,
            issuanceTask);
        using var redemption = responses[0];
        using var issuance = responses[1];
        string? returnedSecret = null;
        if (redemption.IsSuccessStatusCode)
        {
            using var redemptionJson = await ReadJsonAsync(redemption);
            returnedSecret = redemptionJson.RootElement
                .GetProperty("deviceSecret")
                .GetString();
        }

        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(2, await context.Devices.CountAsync(CancellationToken));
        var credential = await context.DeviceCredentials.SingleAsync(
            item => item.DeviceId == setup.OwnerDeviceId,
            CancellationToken);
        if (returnedSecret is not null)
        {
            Assert.True(
                IdentitySecretCryptography.VerifyHash(
                    returnedSecret,
                    credential.DeviceSecretHash));
        }

        Assert.True(
            await context.DeviceActivationCodes.CountAsync(
                item =>
                    item.DeviceId == setup.OwnerDeviceId &&
                    item.UsedAtUtc == null &&
                    item.RevokedAtUtc == null,
                CancellationToken) <= 1);
    }

    [Fact]
    public async Task Commercial_correlation_is_preserved_on_success_and_errors()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var correlationId = Uuid7.NewGuid().ToString("D");
        using var successfulLogin = await SendLoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret,
            correlationId);
        Assert.Equal(HttpStatusCode.OK, successfulLogin.StatusCode);
        Assert.Equal(
            correlationId,
            successfulLogin.Headers.GetValues("X-Correlation-ID").Single());
        AssertNoStore(successfulLogin);

        var errorCorrelationId = Uuid7.NewGuid().ToString("D");
        using var failedLogin = await SendLoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            "incorrect passphrase",
            setup.OwnerDeviceId,
            secret,
            errorCorrelationId);
        using var failedJson = await ReadJsonAsync(failedLogin);
        Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);
        Assert.Equal(
            errorCorrelationId,
            failedLogin.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(
            errorCorrelationId,
            failedJson.RootElement
                .GetProperty("meta")
                .GetProperty("correlationId")
                .GetString());
        AssertNoStore(failedLogin);

        using var invalidCorrelation = await SendLoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret,
            "raw-invalid-correlation");
        using var invalidJson = await ReadJsonAsync(invalidCorrelation);
        var safeId = invalidJson.RootElement
            .GetProperty("meta")
            .GetProperty("correlationId")
            .GetString();
        Assert.True(Guid.TryParseExact(safeId, "D", out _));
        Assert.NotEqual("raw-invalid-correlation", safeId);
        Assert.DoesNotContain(
            "raw-invalid-correlation",
            invalidJson.RootElement.GetRawText(),
            StringComparison.Ordinal);
        AssertNoStore(invalidCorrelation);

        await using var context = database.CreateContext(setup.WorkspaceId);
        Assert.Equal(
            correlationId,
            (await context.AuditEvents.SingleAsync(
                item =>
                    item.Action == "Platform.Identity.LoginSucceeded",
                CancellationToken)).CorrelationId);
    }

    [Fact]
    public async Task PostgreSql_rejects_invalid_refresh_chain_mutations()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = IdentityFactory(database, UtcNow);
        var setup = await BootstrapAsync(factory);
        var secret = await ActivateAsync(
            factory,
            setup.WorkspaceCode,
            setup.OwnerDeviceId,
            setup.OwnerActivationCode);
        var firstLogin = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        var secondLogin = await LoginAsync(
            factory,
            setup.WorkspaceCode,
            "owner",
            OwnerPassword,
            setup.OwnerDeviceId,
            secret);
        var otherSetup = await BootstrapAsync(
            factory,
            "TRADERPRO-OTHER");
        var otherSecret = await ActivateAsync(
            factory,
            otherSetup.WorkspaceCode,
            otherSetup.OwnerDeviceId,
            otherSetup.OwnerActivationCode);
        var otherLogin = await LoginAsync(
            factory,
            otherSetup.WorkspaceCode,
            "owner",
            OwnerPassword,
            otherSetup.OwnerDeviceId,
            otherSecret);

        Guid firstTargetId;
        Guid secondTargetId;
        Guid otherTargetId;
        await using (var unscoped = database.CreateContext(null))
        {
            firstTargetId = (await unscoped.RefreshTokens
                    .IgnoreQueryFilters()
                    .SingleAsync(
                        item => item.FamilyId == firstLogin.FamilyId,
                        CancellationToken))
                .Id;
            secondTargetId = (await unscoped.RefreshTokens
                    .IgnoreQueryFilters()
                    .SingleAsync(
                        item => item.FamilyId == secondLogin.FamilyId,
                        CancellationToken))
                .Id;
            otherTargetId = (await unscoped.RefreshTokens
                    .IgnoreQueryFilters()
                    .SingleAsync(
                        item => item.FamilyId == otherLogin.FamilyId,
                        CancellationToken))
                .Id;
        }

        await AssertRefreshInsertRejectedAsync(
            database,
            setup.WorkspaceId,
            firstLogin.FamilyId,
            secondTargetId,
            UtcNow,
            "cross-family");
        await AssertRefreshInsertRejectedAsync(
            database,
            setup.WorkspaceId,
            firstLogin.FamilyId,
            otherTargetId,
            UtcNow,
            "cross-workspace");
        await AssertRefreshInsertRejectedAsync(
            database,
            setup.WorkspaceId,
            firstLogin.FamilyId,
            firstTargetId,
            UtcNow.AddMinutes(1),
            "replacement-before-consumption");

        var validPredecessorId = Uuid7.NewGuid();
        await InsertRefreshPredecessorAsync(
            database,
            validPredecessorId,
            setup.WorkspaceId,
            firstLogin.FamilyId,
            firstTargetId,
            UtcNow,
            "valid-predecessor");
        await AssertRefreshInsertRejectedAsync(
            database,
            setup.WorkspaceId,
            firstLogin.FamilyId,
            firstTargetId,
            UtcNow,
            "duplicate-predecessor");

        var selfId = Uuid7.NewGuid();
        await AssertSqlRejectedAsync(
            database,
            """
            INSERT INTO platform.refresh_tokens (
                id, workspace_id, family_id, token_hash,
                created_at_utc, expires_at_utc, consumed_at_utc,
                rotated_to_token_id, replay_protected_token,
                replay_allowed_until_utc, version)
            VALUES (
                @id, @workspace_id, @family_id, @token_hash,
                @created, @expires, @consumed,
                @id, 'ciphertext', @replay_until, 1)
            """,
            ("id", selfId),
            ("workspace_id", setup.WorkspaceId),
            ("family_id", firstLogin.FamilyId),
            ("token_hash", IdentitySecretCryptography.Hash("self-link")),
            ("created", UtcNow.AddMinutes(-1)),
            ("expires", UtcNow.AddDays(1)),
            ("consumed", UtcNow),
            ("replay_until", UtcNow.AddMinutes(1)));
        await AssertSqlRejectedAsync(
            database,
            """
            INSERT INTO platform.refresh_tokens (
                id, workspace_id, family_id, token_hash,
                created_at_utc, expires_at_utc, consumed_at_utc,
                rotated_to_token_id, replay_protected_token,
                replay_allowed_until_utc, version)
            VALUES (
                @id, @workspace_id, @family_id, @token_hash,
                @created, @expires, @consumed,
                @target_id, 'ciphertext', NULL, 1)
            """,
            ("id", Uuid7.NewGuid()),
            ("workspace_id", setup.WorkspaceId),
            ("family_id", firstLogin.FamilyId),
            ("token_hash", IdentitySecretCryptography.Hash("invalid-replay")),
            ("created", UtcNow.AddMinutes(-1)),
            ("expires", UtcNow.AddDays(1)),
            ("consumed", UtcNow),
            ("target_id", firstTargetId));
        await AssertSqlRejectedAsync(
            database,
            """
            UPDATE platform.refresh_tokens
            SET token_hash = @token_hash,
                version = version + 1
            WHERE id = @id
            """,
            ("token_hash", IdentitySecretCryptography.Hash("mutated-hash")),
            ("id", firstTargetId));
    }

    [Fact]
    public async Task Login_rate_limit_returns_standard_429_envelope()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var factory = new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false,
            rateLimitingEnabled: true);
        HttpResponseMessage? last = null;
        for (var request = 0; request < 11; request++)
        {
            last?.Dispose();
            last = await SendLoginAsync(
                factory,
                "UNKNOWN-WORKSPACE",
                "owner",
                OwnerPassword,
                Uuid7.NewGuid(),
                "invalid-device-secret");
        }

        using (last)
        using (var json = await ReadJsonAsync(last!))
        {
            Assert.Equal(
                HttpStatusCode.TooManyRequests,
                last!.StatusCode);
            Assert.Equal("RATE_LIMIT_EXCEEDED", ErrorCode(json));
            AssertNoStore(last);
        }
    }

    private static TraderProApiFactory IdentityFactory(
        IsolatedPostgreSqlDatabase database,
        DateTimeOffset utcNow)
    {
        return new TraderProApiFactory(
            database.ConnectionString,
            spikesEnabled: false,
            identityBootstrapEnabled: true,
            utcNow: utcNow);
    }

    private static async Task<IdentitySetup> BootstrapAsync(
        TraderProApiFactory factory,
        string workspaceCode = "TRADERPRO-DEMO")
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/spikes/identity/bootstrap",
            new
            {
                workspaceCode,
                ownerPassword = OwnerPassword,
                operatorPassword = OperatorPassword,
            },
            CancellationToken);
        using var json = await ReadJsonAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(json.RootElement.GetRawText());
        }
        AssertNoStore(response);

        return new IdentitySetup(
            json.RootElement.GetProperty("workspaceId").GetGuid(),
            json.RootElement.GetProperty("workspaceCode").GetString()!,
            json.RootElement.GetProperty("ownerUserId").GetGuid(),
            json.RootElement.GetProperty("operatorUserId").GetGuid(),
            json.RootElement.GetProperty("ownerDeviceId").GetGuid(),
            json.RootElement.GetProperty("operatorDeviceId").GetGuid(),
            json.RootElement
                .GetProperty("ownerActivation")
                .GetProperty("activationCode")
                .GetString()!,
            json.RootElement
                .GetProperty("operatorActivation")
                .GetProperty("activationCode")
                .GetString()!);
    }

    private static async Task<string> ActivateAsync(
        TraderProApiFactory factory,
        string workspaceCode,
        Guid deviceId,
        string activationCode,
        string? idempotencyKey = null)
    {
        using var response = await SendActivationAsync(
            factory,
            workspaceCode,
            deviceId,
            activationCode,
            idempotencyKey);
        using var json = await ReadJsonAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(json.RootElement.GetRawText());
        }

        Assert.Equal(
            deviceId,
            json.RootElement.GetProperty("deviceId").GetGuid());
        AssertNoStore(response);
        return json.RootElement.GetProperty("deviceSecret").GetString()!;
    }

    private static Task<HttpResponseMessage> SendActivationAsync(
        TraderProApiFactory factory,
        string workspaceCode,
        Guid deviceId,
        string activationCode,
        string? idempotencyKey = null,
        string? deviceLabel = null,
        string? clientInstallationReference = null)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/device-activations/redeem")
        {
            Content = JsonContent.Create(
                new
                {
                    workspaceCode,
                    activationCode,
                    clientInstallationReference =
                        clientInstallationReference ??
                        $"test-installation-{deviceId:D}",
                    deviceLabel = deviceLabel ?? $"Device {deviceId:D}",
                    platform = "Android",
                }),
        };
        request.Headers.Add(
            "Idempotency-Key",
            idempotencyKey ?? $"test-activation-{Guid.NewGuid():N}");
        return client.SendAsync(
            request,
            CancellationToken);
    }

    private static async Task<LoginResult> LoginAsync(
        TraderProApiFactory factory,
        string workspaceCode,
        string login,
        string password,
        Guid deviceId,
        string deviceSecret)
    {
        using var response = await SendLoginAsync(
            factory,
            workspaceCode,
            login,
            password,
            deviceId,
            deviceSecret);
        using var json = await ReadJsonAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(json.RootElement.GetRawText());
        }
        AssertNoStore(response);

        return new LoginResult(
            json.RootElement.GetProperty("accessToken").GetString()!,
            json.RootElement.GetProperty("refreshToken").GetString()!,
            json.RootElement
                .GetProperty("user")
                .GetProperty("role")
                .GetString()!,
            ReadFamilyId(
                json.RootElement.GetProperty("accessToken").GetString()!));
    }

    private static Task<HttpResponseMessage> SendLoginAsync(
        TraderProApiFactory factory,
        string workspaceCode,
        string login,
        string password,
        Guid deviceId,
        string deviceSecret,
        string? correlationId = null)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/login")
        {
            Content = JsonContent.Create(
                new
                {
                    workspaceCode,
                    login,
                    password,
                    deviceId,
                    deviceSecret,
                }),
        };
        if (correlationId is not null)
        {
            request.Headers.Add("X-Correlation-ID", correlationId);
        }

        return client.SendAsync(request, CancellationToken);
    }

    private static async Task<LoginResult> RefreshAsync(
        TraderProApiFactory factory,
        string refreshToken)
    {
        using var response = await SendRefreshAsync(factory, refreshToken);
        using var json = await ReadJsonAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(json.RootElement.GetRawText());
        }
        AssertNoStore(response);

        return new LoginResult(
            json.RootElement.GetProperty("accessToken").GetString()!,
            json.RootElement.GetProperty("refreshToken").GetString()!,
            json.RootElement
                .GetProperty("user")
                .GetProperty("role")
                .GetString()!,
            ReadFamilyId(
                json.RootElement.GetProperty("accessToken").GetString()!));
    }

    private static Task<HttpResponseMessage> SendRefreshAsync(
        TraderProApiFactory factory,
        string refreshToken)
    {
        var client = factory.CreateClient();
        return client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken },
            CancellationToken);
    }

    private static Task<HttpResponseMessage> SendAuthenticatedPostAsync(
        TraderProApiFactory factory,
        string path,
        string accessToken)
    {
        var client = factory.CreateClient();
        var request = BearerRequest(HttpMethod.Post, path, accessToken);
        return client.SendAsync(request, CancellationToken);
    }

    private static async Task<string> IssueActivationCodeAsync(
        TraderProApiFactory factory,
        Guid deviceId,
        string accessToken,
        string idempotencyKey)
    {
        using var response = await SendIssueActivationCodeAsync(
            factory,
            deviceId,
            accessToken,
            idempotencyKey);
        using var json = await ReadJsonAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                json.RootElement.GetRawText());
        }

        AssertNoStore(response);
        return json.RootElement
            .GetProperty("activationCode")
            .GetString()!;
    }

    private static Task<HttpResponseMessage> SendIssueActivationCodeAsync(
        TraderProApiFactory factory,
        Guid deviceId,
        string accessToken,
        string idempotencyKey)
    {
        var client = factory.CreateClient();
        var request = BearerRequest(
            HttpMethod.Post,
            $"/api/v1/devices/{deviceId:D}/activation-codes",
            accessToken);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request, CancellationToken);
    }

    private static async Task<HttpStatusCode> MeStatusAsync(
        TraderProApiFactory factory,
        string accessToken)
    {
        using var client = factory.CreateClient();
        using var request = BearerRequest(
            HttpMethod.Get,
            "/api/v1/auth/me",
            accessToken);
        using var response = await client.SendAsync(
            request,
            CancellationToken);
        return response.StatusCode;
    }

    private static HttpRequestMessage BearerRequest(
        HttpMethod method,
        string uri,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static Guid ReadFamilyId(string accessToken)
    {
        var payload = accessToken.Split('.')[1]
            .Replace('-', '+')
            .Replace('_', '/');
        payload = payload.PadRight(
            payload.Length + ((4 - payload.Length % 4) % 4),
            '=');
        using var json = JsonDocument.Parse(
            Convert.FromBase64String(payload));
        return Guid.ParseExact(
            json.RootElement.GetProperty("sid").GetString()!,
            "D");
    }

    private static Guid ReadGuidClaim(
        string accessToken,
        string claim)
    {
        var payload = accessToken.Split('.')[1]
            .Replace('-', '+')
            .Replace('_', '/');
        payload = payload.PadRight(
            payload.Length + ((4 - payload.Length % 4) % 4),
            '=');
        using var json = JsonDocument.Parse(
            Convert.FromBase64String(payload));
        return Guid.ParseExact(
            json.RootElement.GetProperty(claim).GetString()!,
            "D");
    }

    private static string IssueAccessToken(
        string signingKey,
        string issuer,
        string audience,
        DateTimeOffset issuedAtUtc,
        IdentitySetup setup,
        Guid companyId,
        Guid branchId,
        Guid familyId)
    {
        var options = new TraderProAuthenticationOptions(
            issuer,
            audience,
            15,
            30,
            30,
            signingKey,
            Path.GetTempPath(),
            5,
            15,
            15,
            false,
            false,
            false,
            []);
        return new JwtAccessTokenIssuer(
                options,
                new TestClock(issuedAtUtc))
            .Issue(
                setup.WorkspaceId,
                setup.OwnerUserId,
                setup.OwnerDeviceId,
                companyId,
                branchId,
                TraderProRole.Owner,
                1,
                1,
                familyId)
            .Token;
    }

    private static string? ErrorCode(JsonDocument json)
    {
        return json.RootElement
            .GetProperty("error")
            .GetProperty("code")
            .GetString();
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync(
            CancellationToken);
        return await JsonDocument.ParseAsync(
            stream,
            cancellationToken: CancellationToken);
    }

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(
                parameter.Name,
                parameter.Value);
        }

        return (T)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static Task AssertRefreshInsertRejectedAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        Guid familyId,
        Guid targetId,
        DateTimeOffset consumedAtUtc,
        string uniqueValue)
    {
        return AssertSqlRejectedAsync(
            database,
            """
            INSERT INTO platform.refresh_tokens (
                id, workspace_id, family_id, token_hash,
                created_at_utc, expires_at_utc, consumed_at_utc,
                rotated_to_token_id, replay_protected_token,
                replay_allowed_until_utc, version)
            VALUES (
                @id, @workspace_id, @family_id, @token_hash,
                @created, @expires, @consumed,
                @target_id, 'ciphertext', @replay_until, 1)
            """,
            ("id", Uuid7.NewGuid()),
            ("workspace_id", workspaceId),
            ("family_id", familyId),
            ("token_hash", IdentitySecretCryptography.Hash(uniqueValue)),
            ("created", UtcNow.AddMinutes(-2)),
            ("expires", UtcNow.AddDays(1)),
            ("consumed", consumedAtUtc),
            ("target_id", targetId),
            ("replay_until", consumedAtUtc.AddMinutes(1)));
    }

    private static async Task InsertRefreshPredecessorAsync(
        IsolatedPostgreSqlDatabase database,
        Guid id,
        Guid workspaceId,
        Guid familyId,
        Guid targetId,
        DateTimeOffset consumedAtUtc,
        string uniqueValue)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO platform.refresh_tokens (
                id, workspace_id, family_id, token_hash,
                created_at_utc, expires_at_utc, consumed_at_utc,
                rotated_to_token_id, replay_protected_token,
                replay_allowed_until_utc, version)
            VALUES (
                @id, @workspace_id, @family_id, @token_hash,
                @created, @expires, @consumed,
                @target_id, 'ciphertext', @replay_until, 1)
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("workspace_id", workspaceId);
        command.Parameters.AddWithValue("family_id", familyId);
        command.Parameters.AddWithValue(
            "token_hash",
            IdentitySecretCryptography.Hash(uniqueValue));
        command.Parameters.AddWithValue(
            "created",
            UtcNow.AddMinutes(-2));
        command.Parameters.AddWithValue("expires", UtcNow.AddDays(1));
        command.Parameters.AddWithValue("consumed", consumedAtUtc);
        command.Parameters.AddWithValue("target_id", targetId);
        command.Parameters.AddWithValue(
            "replay_until",
            consumedAtUtc.AddMinutes(1));
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task AssertSqlRejectedAsync(
        IsolatedPostgreSqlDatabase database,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(
                parameter.Name,
                parameter.Value);
        }

        await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(CancellationToken));
    }

    private static async Task AcquireAdvisoryLockAsync(
        NpgsqlConnection connection,
        string scope)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT pg_advisory_xact_lock(hashtextextended(@scope, 0))";
        command.Parameters.AddWithValue("scope", scope);
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task WaitForAdvisoryWaiterAsync(
        IsolatedPostgreSqlDatabase database)
    {
        await using var connection = await database.OpenConnectionAsync();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (await ScalarAsync<long>(
                    connection,
                    """
                    SELECT count(*)
                    FROM pg_locks
                    WHERE locktype = 'advisory'
                      AND NOT granted
                    """) > 0)
            {
                return;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(20),
                CancellationToken);
        }

        throw new TimeoutException(
            "The expected advisory-lock waiter was not observed.");
    }

    private static void AssertNoStore(HttpResponseMessage response)
    {
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            response.Headers.Pragma,
            value => string.Equals(
                value.Name,
                "no-cache",
                StringComparison.OrdinalIgnoreCase));
    }

    private sealed record IdentitySetup(
        Guid WorkspaceId,
        string WorkspaceCode,
        Guid OwnerUserId,
        Guid OperatorUserId,
        Guid OwnerDeviceId,
        Guid OperatorDeviceId,
        string OwnerActivationCode,
        string OperatorActivationCode);

    private sealed record LoginResult(
        string AccessToken,
        string RefreshToken,
        string Role,
        Guid FamilyId);

    private sealed class TestClock(DateTimeOffset utcNow) :
        TraderPro.Application.Common.Time.IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration)
        {
            UtcNow = UtcNow.Add(duration);
        }
    }
}
