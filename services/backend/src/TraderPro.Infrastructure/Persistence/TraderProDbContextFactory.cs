using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TraderPro.Application.Common.Tenancy;

namespace TraderPro.Infrastructure.Persistence;

public sealed class TraderProDbContextFactory :
    IDesignTimeDbContextFactory<TraderProDbContext>
{
    private const string ConnectionVariable =
        "ConnectionStrings__TraderPro";

    public TraderProDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set the {ConnectionVariable} environment variable before running dotnet-ef.");
        }

        var options = new DbContextOptionsBuilder<TraderProDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    "platform"))
            .Options;

        return new TraderProDbContext(
            options,
            NullCurrentWorkspaceAccessor.Instance);
    }

    private sealed class NullCurrentWorkspaceAccessor :
        ICurrentWorkspaceAccessor
    {
        public static NullCurrentWorkspaceAccessor Instance { get; } = new();

        public Guid? WorkspaceId => null;
    }
}
