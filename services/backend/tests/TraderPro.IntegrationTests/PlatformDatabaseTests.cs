using Microsoft.EntityFrameworkCore;
using Npgsql;
using TraderPro.Domain.Common;
using TraderPro.Domain.Platform;

namespace TraderPro.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class PlatformDatabaseTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 29, 4, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task Fresh_database_accepts_migration_and_has_all_platform_tables()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'platform'
              AND table_type = 'BASE TABLE'
              AND table_name <> '__ef_migrations_history'
            ORDER BY table_name;
            """;
        await using var reader = await command.ExecuteReaderAsync(
            CancellationToken);
        var tables = new List<string>();
        while (await reader.ReadAsync(CancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Equal(IsolatedPostgreSqlDatabase.PlatformTables, tables);
    }

    [Fact]
    public async Task Persisted_platform_records_keep_uuid_version_7_ids()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "uuid");
        var company = Company.Create(
            workspace.Id,
            "company",
            "UUID Company",
            null,
            null,
            UtcNow);

        await using (var context = database.CreateContext(workspace.Id))
        {
            context.Companies.Add(company);
            await context.SaveChangesAsync(CancellationToken);
        }

        Assert.Equal(7, workspace.Id.Version);
        Assert.Equal(7, company.Id.Version);
    }

    [Fact]
    public async Task Foreign_keys_require_existing_and_workspace_matching_parents()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var missingWorkspaceId = Uuid7.NewGuid();

        await using (var context = database.CreateContext(missingWorkspaceId))
        {
            context.Companies.Add(
                Company.Create(
                    missingWorkspaceId,
                    "missing",
                    "Missing Workspace",
                    null,
                    null,
                    UtcNow));
            await AssertForeignKeyViolationAsync(context);
        }

        await using (var context = database.CreateContext(missingWorkspaceId))
        {
            context.Users.Add(
                PlatformUser.Create(
                    missingWorkspaceId,
                    "missing-user",
                    "Missing User",
                    UtcNow));
            await AssertForeignKeyViolationAsync(context);
        }

        await using (var context = database.CreateContext(missingWorkspaceId))
        {
            context.Devices.Add(
                Device.Create(
                    missingWorkspaceId,
                    "missing-device",
                    "Missing Device",
                    "android",
                    UtcNow));
            await AssertForeignKeyViolationAsync(context);
        }

        var workspaceA = await CreateWorkspaceAsync(database, "fk-a");
        var workspaceB = await CreateWorkspaceAsync(database, "fk-b");
        var companyA = await CreateCompanyAsync(
            database,
            workspaceA.Id,
            "company-a");

        await using (var context = database.CreateContext(workspaceB.Id))
        {
            context.Branches.Add(
                Branch.Create(
                    workspaceB.Id,
                    companyA.Id,
                    "mismatch",
                    "Mismatched Branch",
                    false,
                    UtcNow));
            await AssertForeignKeyViolationAsync(context);
        }
    }

    [Fact]
    public async Task Normal_queries_are_isolated_for_every_workspace_owned_table()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspaceA = await CreateWorkspaceAsync(database, "isolation-a");
        var workspaceB = await CreateWorkspaceAsync(database, "isolation-b");
        await SeedWorkspaceOwnedRecordsAsync(database, workspaceA, "a");
        await SeedWorkspaceOwnedRecordsAsync(database, workspaceB, "b");

        await using var contextA = database.CreateContext(workspaceA.Id);
        await using var contextB = database.CreateContext(workspaceB.Id);

        var countsA = await GetWorkspaceOwnedCountsAsync(contextA);
        var countsB = await GetWorkspaceOwnedCountsAsync(contextB);
        var companyWorkspaceIdsA = await contextA.Companies
            .Select(company => company.WorkspaceId)
            .ToArrayAsync(CancellationToken);
        var companyWorkspaceIdsB = await contextB.Companies
            .Select(company => company.WorkspaceId)
            .ToArrayAsync(CancellationToken);

        Assert.All(countsA, count => Assert.Equal(1, count));
        Assert.All(countsB, count => Assert.Equal(1, count));
        Assert.All(
            companyWorkspaceIdsA,
            id => Assert.Equal(workspaceA.Id, id));
        Assert.All(
            companyWorkspaceIdsB,
            id => Assert.Equal(workspaceB.Id, id));
    }

    [Fact]
    public async Task No_active_workspace_filters_every_workspace_owned_table()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "fail-closed-query");
        await SeedWorkspaceOwnedRecordsAsync(database, workspace, "closed");

        await using var context = database.CreateContext(null);
        var counts = await GetWorkspaceOwnedCountsAsync(context);

        Assert.All(counts, count => Assert.Equal(0, count));
    }

    [Fact]
    public async Task No_active_workspace_rejects_owned_inserts_updates_and_deletes()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "fail-closed-write");
        var company = await CreateCompanyAsync(
            database,
            workspace.Id,
            "protected-company");

        await using (var context = database.CreateContext(null))
        {
            context.Companies.Add(
                Company.Create(
                    workspace.Id,
                    "blocked-insert",
                    "Blocked Insert",
                    null,
                    null,
                    UtcNow));
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.SaveChangesAsync(CancellationToken));
        }

        await using (var context = database.CreateContext(null))
        {
            var tracked = await context.Companies
                .IgnoreQueryFilters()
                .SingleAsync(
                    candidate => candidate.Id == company.Id,
                    CancellationToken);
            tracked.UpdateNames("Blocked Update", null);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.SaveChangesAsync(CancellationToken));
        }

        await using (var context = database.CreateContext(null))
        {
            var tracked = await context.Companies
                .IgnoreQueryFilters()
                .SingleAsync(
                    candidate => candidate.Id == company.Id,
                    CancellationToken);
            context.Companies.Remove(tracked);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.SaveChangesAsync(CancellationToken));
        }
    }

    [Fact]
    public async Task Empty_workspace_id_is_assigned_from_the_active_workspace()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "auto-assignment");
        var company = Company.Create(
            Guid.Empty,
            "assigned-company",
            "Assigned Company",
            null,
            null,
            UtcNow);

        await using (var context = database.CreateContext(workspace.Id))
        {
            context.Companies.Add(company);
            await context.SaveChangesAsync(CancellationToken);
        }

        Assert.Equal(workspace.Id, company.WorkspaceId);
        await using var verification = database.CreateContext(workspace.Id);
        var persisted = await verification.Companies
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == company.Id,
                CancellationToken);
        Assert.Equal(workspace.Id, persisted.WorkspaceId);
    }

    [Fact]
    public async Task Workspace_write_controls_reject_cross_workspace_mutations()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspaceA = await CreateWorkspaceAsync(database, "write-a");
        var workspaceB = await CreateWorkspaceAsync(database, "write-b");
        var companyA = await CreateCompanyAsync(
            database,
            workspaceA.Id,
            "company-a");
        var companyB = await CreateCompanyAsync(
            database,
            workspaceB.Id,
            "company-b");

        await using (var context = database.CreateContext(workspaceA.Id))
        {
            context.Companies.Add(
                Company.Create(
                    workspaceB.Id,
                    "cross-insert",
                    "Cross Insert",
                    null,
                    null,
                    UtcNow));
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.SaveChangesAsync(CancellationToken));
        }

        await using (var context = database.CreateContext(workspaceA.Id))
        {
            var tracked = await context.Companies.SingleAsync(
                company => company.Id == companyA.Id,
                CancellationToken);
            Assert.Throws<InvalidOperationException>(
                () => context.Entry(tracked)
                    .Property(company => company.WorkspaceId)
                    .CurrentValue = workspaceB.Id);
        }

        await using (var context = database.CreateContext(workspaceA.Id))
        {
            var tracked = await context.Companies
                .IgnoreQueryFilters()
                .SingleAsync(
                    company => company.Id == companyB.Id,
                    CancellationToken);
            tracked.UpdateNames("Forbidden Update", null);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.SaveChangesAsync(CancellationToken));
        }

        await using (var context = database.CreateContext(workspaceA.Id))
        {
            var tracked = await context.Companies
                .IgnoreQueryFilters()
                .SingleAsync(
                    company => company.Id == companyB.Id,
                    CancellationToken);
            context.Companies.Remove(tracked);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.SaveChangesAsync(CancellationToken));
        }
    }

    [Fact]
    public async Task Stale_company_update_produces_a_concurrency_failure()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "concurrency");
        var company = await CreateCompanyAsync(
            database,
            workspace.Id,
            "company");

        await using var context1 = database.CreateContext(workspace.Id);
        await using var context2 = database.CreateContext(workspace.Id);
        var firstCopy = await context1.Companies.SingleAsync(
            candidate => candidate.Id == company.Id,
            CancellationToken);
        var staleCopy = await context2.Companies.SingleAsync(
            candidate => candidate.Id == company.Id,
            CancellationToken);

        firstCopy.UpdateNames("First Update", null);
        await context1.SaveChangesAsync(CancellationToken);
        staleCopy.UpdateNames("Stale Update", null);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => context2.SaveChangesAsync(CancellationToken));

        await using var verification = database.CreateContext(workspace.Id);
        var persisted = await verification.Companies
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == company.Id,
                CancellationToken);
        Assert.Equal("First Update", persisted.LegalName);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task Idempotency_uniqueness_is_scoped_by_workspace_and_command()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspaceA = await CreateWorkspaceAsync(database, "idempotency-a");
        var workspaceB = await CreateWorkspaceAsync(database, "idempotency-b");
        await InsertIdempotencyAsync(
            database,
            workspaceA.Id,
            "CommandA",
            "same-key");

        await using (var duplicate = database.CreateContext(workspaceA.Id))
        {
            duplicate.IdempotencyRecords.Add(
                NewIdempotency(
                    workspaceA.Id,
                    "CommandA",
                    "same-key"));
            await AssertUniqueViolationAsync(duplicate);
        }

        await InsertIdempotencyAsync(
            database,
            workspaceB.Id,
            "CommandA",
            "same-key");
        await InsertIdempotencyAsync(
            database,
            workspaceA.Id,
            "CommandB",
            "same-key");
    }

    [Fact]
    public async Task Each_company_can_have_only_one_default_branch()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "defaults");
        var companyA = await CreateCompanyAsync(
            database,
            workspace.Id,
            "company-a");
        var companyB = await CreateCompanyAsync(
            database,
            workspace.Id,
            "company-b");
        await InsertBranchAsync(
            database,
            workspace.Id,
            companyA.Id,
            "default-a",
            true);
        await InsertBranchAsync(
            database,
            workspace.Id,
            companyB.Id,
            "default-b",
            true);

        await using var context = database.CreateContext(workspace.Id);
        context.Branches.Add(
            Branch.Create(
                workspace.Id,
                companyA.Id,
                "second-default",
                "Second Default",
                true,
                UtcNow));

        await AssertUniqueViolationAsync(context);
    }

    [Fact]
    public async Task PostgreSql_rejects_audit_updates_and_deletes()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "audit");
        var auditEvent = AuditEvent.Create(
            workspace.Id,
            null,
            null,
            null,
            null,
            "Created",
            "Company",
            Uuid7.NewGuid(),
            null,
            null,
            null,
            "{}",
            "audit-correlation",
            UtcNow);
        await using (var context = database.CreateContext(workspace.Id))
        {
            context.AuditEvents.Add(auditEvent);
            await context.SaveChangesAsync(CancellationToken);
        }

        await AssertAuditMutationRejectedAsync(
            database,
            "UPDATE platform.audit_events SET action = 'Changed' WHERE id = @id",
            auditEvent.Id);
        await AssertAuditMutationRejectedAsync(
            database,
            "DELETE FROM platform.audit_events WHERE id = @id",
            auditEvent.Id);

        await using var verification = database.CreateContext(workspace.Id);
        Assert.True(
            await verification.AuditEvents.AnyAsync(
                candidate => candidate.Id == auditEvent.Id,
                CancellationToken));
    }

    [Fact]
    public async Task Audit_branch_must_match_its_company()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "audit-branch");
        var companyA = await CreateCompanyAsync(
            database,
            workspace.Id,
            "audit-company-a");
        var companyB = await CreateCompanyAsync(
            database,
            workspace.Id,
            "audit-company-b");
        await InsertBranchAsync(
            database,
            workspace.Id,
            companyA.Id,
            "audit-branch-a",
            false);

        Branch branchA;
        await using (var context = database.CreateContext(workspace.Id))
        {
            branchA = await context.Branches
                .AsNoTracking()
                .SingleAsync(
                    branch => branch.CompanyId == companyA.Id,
                    CancellationToken);
        }

        var validAuditEvent = NewAuditEvent(
            workspace.Id,
            companyA.Id,
            branchA.Id,
            "valid-audit-branch");
        await using (var context = database.CreateContext(workspace.Id))
        {
            context.AuditEvents.Add(validAuditEvent);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using (var context = database.CreateContext(workspace.Id))
        {
            var missingCompany = NewAuditEvent(
                workspace.Id,
                companyA.Id,
                branchA.Id,
                "missing-company");
            context.AuditEvents.Add(missingCompany);
            context.Entry(missingCompany)
                .Property(auditEvent => auditEvent.CompanyId)
                .CurrentValue = null;
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(CancellationToken));
            var postgresException = GetPostgresException(exception);
            Assert.Equal(
                PostgresErrorCodes.CheckViolation,
                postgresException.SqlState);
            Assert.Equal(
                "ck_audit_events_branch_requires_company",
                postgresException.ConstraintName);
        }

        await using (var context = database.CreateContext(workspace.Id))
        {
            context.AuditEvents.Add(
                NewAuditEvent(
                    workspace.Id,
                    companyB.Id,
                    branchA.Id,
                    "mismatched-company"));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(CancellationToken));
            var postgresException = GetPostgresException(exception);
            Assert.Equal(
                PostgresErrorCodes.ForeignKeyViolation,
                postgresException.SqlState);
            Assert.Equal(
                "fk_audit_events_branches_workspace_company_branch",
                postgresException.ConstraintName);
        }

        await using var verification = database.CreateContext(workspace.Id);
        Assert.True(
            await verification.AuditEvents.AnyAsync(
                auditEvent => auditEvent.Id == validAuditEvent.Id,
                CancellationToken));
    }

    [Fact]
    public async Task Outbox_status_and_attempts_can_change_but_cannot_be_negative()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "outbox");
        var message = OutboxMessage.Create(
            workspace.Id,
            "CompanyCreated",
            1,
            "Company",
            Uuid7.NewGuid(),
            1,
            "{}",
            "outbox-correlation",
            UtcNow);

        await using (var context = database.CreateContext(workspace.Id))
        {
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using (var context = database.CreateContext(workspace.Id))
        {
            var tracked = await context.OutboxMessages.SingleAsync(
                candidate => candidate.Id == message.Id,
                CancellationToken);
            tracked.StartProcessing();
            await context.SaveChangesAsync(CancellationToken);
            tracked.MarkProcessed(UtcNow.AddMinutes(1));
            await context.SaveChangesAsync(CancellationToken);
        }

        await using (var connection = await database.OpenConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                UPDATE platform.outbox_messages
                SET attempt_count = -1
                WHERE id = @id
                """;
            command.Parameters.AddWithValue("id", message.Id);
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => command.ExecuteNonQueryAsync(CancellationToken));
            Assert.Equal(
                PostgresErrorCodes.CheckViolation,
                exception.SqlState);
        }

        await using var verification = database.CreateContext(workspace.Id);
        var persisted = await verification.OutboxMessages
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == message.Id,
                CancellationToken);
        Assert.Equal(OutboxMessageStatus.Processed, persisted.Status);
        Assert.Equal(1, persisted.AttemptCount);
    }

    [Fact]
    public async Task Utc_timestamp_and_version_interceptors_apply_exactly_once()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var workspace = await CreateWorkspaceAsync(database, "utc-version");
        var insertedAt = UtcNow.AddHours(1);
        var updatedAt = UtcNow.AddHours(2);
        var company = Company.Create(
            workspace.Id,
            "company",
            "Original",
            null,
            null,
            UtcNow.AddDays(-1));

        await using (var context = database.CreateContext(
                         workspace.Id,
                         insertedAt))
        {
            context.Companies.Add(company);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using (var context = database.CreateContext(
                         workspace.Id,
                         updatedAt))
        {
            var tracked = await context.Companies.SingleAsync(
                candidate => candidate.Id == company.Id,
                CancellationToken);
            Assert.Equal(1, tracked.Version);
            Assert.Equal(insertedAt, tracked.CreatedAtUtc);
            Assert.Equal(TimeSpan.Zero, tracked.CreatedAtUtc.Offset);
            tracked.UpdateNames("Updated", null);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var verification = database.CreateContext(workspace.Id);
        var persisted = await verification.Companies
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == company.Id,
                CancellationToken);
        Assert.Equal(2, persisted.Version);
        Assert.Equal(insertedAt, persisted.CreatedAtUtc);
        Assert.Equal(updatedAt, persisted.UpdatedAtUtc);
        Assert.Equal(TimeSpan.Zero, persisted.UpdatedAtUtc.Offset);
    }

    private static async Task<Workspace> CreateWorkspaceAsync(
        IsolatedPostgreSqlDatabase database,
        string code)
    {
        var workspace = Workspace.Create(code, $"{code} Workspace", UtcNow);
        await using var context = database.CreateContext(null);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync(CancellationToken);
        return workspace;
    }

    private static async Task<Company> CreateCompanyAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        string code)
    {
        var company = Company.Create(
            workspaceId,
            code,
            $"{code} Legal Name",
            null,
            null,
            UtcNow);
        await using var context = database.CreateContext(workspaceId);
        context.Companies.Add(company);
        await context.SaveChangesAsync(CancellationToken);
        return company;
    }

    private static async Task InsertBranchAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        Guid companyId,
        string code,
        bool isDefault)
    {
        await using var context = database.CreateContext(workspaceId);
        context.Branches.Add(
            Branch.Create(
                workspaceId,
                companyId,
                code,
                $"{code} Branch",
                isDefault,
                UtcNow));
        await context.SaveChangesAsync(CancellationToken);
    }

    private static async Task InsertIdempotencyAsync(
        IsolatedPostgreSqlDatabase database,
        Guid workspaceId,
        string commandType,
        string key)
    {
        await using var context = database.CreateContext(workspaceId);
        context.IdempotencyRecords.Add(
            NewIdempotency(workspaceId, commandType, key));
        await context.SaveChangesAsync(CancellationToken);
    }

    private static IdempotencyRecord NewIdempotency(
        Guid workspaceId,
        string commandType,
        string key)
    {
        return IdempotencyRecord.Create(
            workspaceId,
            key,
            commandType,
            $"hash-{workspaceId:N}-{commandType}",
            UtcNow);
    }

    private static AuditEvent NewAuditEvent(
        Guid workspaceId,
        Guid companyId,
        Guid branchId,
        string correlationId)
    {
        return AuditEvent.Create(
            workspaceId,
            companyId,
            branchId,
            null,
            null,
            "Viewed",
            "Document",
            Uuid7.NewGuid(),
            null,
            null,
            null,
            "{}",
            correlationId,
            UtcNow);
    }

    private static async Task SeedWorkspaceOwnedRecordsAsync(
        IsolatedPostgreSqlDatabase database,
        Workspace workspace,
        string suffix)
    {
        await using var context = database.CreateContext(workspace.Id);
        var company = Company.Create(
            workspace.Id,
            $"company-{suffix}",
            $"Company {suffix}",
            null,
            null,
            UtcNow);
        context.AddRange(
            company,
            Branch.Create(
                workspace.Id,
                company.Id,
                $"branch-{suffix}",
                $"Branch {suffix}",
                true,
                UtcNow),
            PlatformUser.Create(
                workspace.Id,
                $"user-{suffix}",
                $"User {suffix}",
                UtcNow),
            Device.Create(
                workspace.Id,
                $"installation-{suffix}",
                $"Device {suffix}",
                "android",
                UtcNow),
            NewIdempotency(
                workspace.Id,
                $"Command{suffix}",
                $"key-{suffix}"),
            OutboxMessage.Create(
                workspace.Id,
                $"Event{suffix}",
                1,
                "Company",
                company.Id,
                1,
                "{}",
                $"outbox-{suffix}",
                UtcNow),
            AuditEvent.Create(
                workspace.Id,
                company.Id,
                null,
                null,
                null,
                "Created",
                "Company",
                company.Id,
                null,
                null,
                null,
                "{}",
                $"audit-{suffix}",
                UtcNow));
        await context.SaveChangesAsync(CancellationToken);
    }

    private static async Task<int[]> GetWorkspaceOwnedCountsAsync(
        TraderPro.Infrastructure.Persistence.TraderProDbContext context)
    {
        return
        [
            await context.Companies.CountAsync(CancellationToken),
            await context.Branches.CountAsync(CancellationToken),
            await context.Users.CountAsync(CancellationToken),
            await context.Devices.CountAsync(CancellationToken),
            await context.IdempotencyRecords.CountAsync(CancellationToken),
            await context.OutboxMessages.CountAsync(CancellationToken),
            await context.AuditEvents.CountAsync(CancellationToken),
        ];
    }

    private static async Task AssertForeignKeyViolationAsync(
        TraderPro.Infrastructure.Persistence.TraderProDbContext context)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync(CancellationToken));
        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            GetPostgresException(exception).SqlState);
    }

    private static async Task AssertUniqueViolationAsync(
        TraderPro.Infrastructure.Persistence.TraderProDbContext context)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync(CancellationToken));
        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            GetPostgresException(exception).SqlState);
    }

    private static PostgresException GetPostgresException(Exception exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

            current = current.InnerException;
        }

        throw new Xunit.Sdk.XunitException(
            "The failure did not contain a PostgreSQL exception.");
    }

    private static async Task AssertAuditMutationRejectedAsync(
        IsolatedPostgreSqlDatabase database,
        string sql,
        Guid auditEventId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("id", auditEventId);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(CancellationToken));

        Assert.Equal("55000", exception.SqlState);
    }
}
