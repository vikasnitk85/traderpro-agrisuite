using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using TraderPro.Application.Common.Tenancy;
using TraderPro.Application.Common.Time;
using TraderPro.Infrastructure.Persistence;
using TraderPro.Infrastructure.Persistence.Interceptors;

namespace TraderPro.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18")
            .WithDatabase("traderpro_admin")
            .WithUsername("traderpro_test")
            .WithPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(24)))
            .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task<IsolatedPostgreSqlDatabase> CreateDatabaseAsync(
        string? targetMigration = null)
    {
        var databaseName = $"traderpro_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(
                         _container.GetConnectionString()))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync(
                TestContext.Current.CancellationToken);
        }

        var connectionBuilder = new NpgsqlConnectionStringBuilder(
            _container.GetConnectionString())
        {
            Database = databaseName,
            Pooling = false,
        };
        var database = new IsolatedPostgreSqlDatabase(
            _container.GetConnectionString(),
            connectionBuilder.ConnectionString,
            databaseName);
        await database.ApplyMigrationsAsync(targetMigration);
        return database;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection :
    ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL 18 integration";
}

public sealed class IsolatedPostgreSqlDatabase(
    string adminConnectionString,
    string connectionString,
    string databaseName) : IAsyncDisposable
{
    public static readonly string[] PlatformTables =
    [
        "audit_events",
        "branches",
        "command_probes",
        "commercial_outbox_audiences",
        "companies",
        "device_activation_codes",
        "device_credentials",
        "devices",
        "idempotency_records",
        "outbox_messages",
        "refresh_token_families",
        "refresh_tokens",
        "user_credentials",
        "users",
        "workspaces",
    ];

    public string ConnectionString { get; } = connectionString;

    public TraderProDbContext CreateContext(
        Guid? workspaceId,
        DateTimeOffset? utcNow = null)
    {
        var clock = new FixedClock(
            utcNow ??
            new DateTimeOffset(2026, 7, 29, 4, 0, 0, TimeSpan.Zero));
        var workspace = new FixedWorkspaceAccessor(workspaceId);
        var options = new DbContextOptionsBuilder<TraderProDbContext>()
            .UseNpgsql(
                ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    "platform"))
            .AddInterceptors(
                new WorkspaceOwnershipInterceptor(workspace),
                new UtcTimestampInterceptor(clock),
                new VersioningInterceptor())
            .Options;

        return new TraderProDbContext(options, workspace);
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE \"{databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync(
            TestContext.Current.CancellationToken);
    }

    internal async Task ApplyMigrationsAsync(string? targetMigration = null)
    {
        await using var context = CreateContext(null);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(
            targetMigration,
            TestContext.Current.CancellationToken);
    }

    private sealed class FixedWorkspaceAccessor(Guid? workspaceId) :
        ICurrentWorkspaceAccessor
    {
        public Guid? WorkspaceId { get; } = workspaceId;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

}
