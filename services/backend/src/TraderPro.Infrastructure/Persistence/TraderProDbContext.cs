using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Tenancy;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.Poc;

namespace TraderPro.Infrastructure.Persistence;

public sealed class TraderProDbContext(
    DbContextOptions<TraderProDbContext> options,
    ICurrentWorkspaceAccessor currentWorkspace) : DbContext(options)
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<PlatformUser> Users => Set<PlatformUser>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<IdempotencyRecord> IdempotencyRecords =>
        Set<IdempotencyRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<CommandProbe> CommandProbes => Set<CommandProbe>();

    public DbSet<ReceivingSessionPoc> ReceivingSessionPocs =>
        Set<ReceivingSessionPoc>();

    public DbSet<ReceivingEntryPoc> ReceivingEntryPocs =>
        Set<ReceivingEntryPoc>();

    public DbSet<ReceivingFinalizationPoc> ReceivingFinalizationPocs =>
        Set<ReceivingFinalizationPoc>();

    internal Guid? ActiveWorkspaceId => currentWorkspace.WorkspaceId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("platform");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TraderProDbContext).Assembly);

        modelBuilder.Entity<Company>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<Branch>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<PlatformUser>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<Device>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<IdempotencyRecord>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<OutboxMessage>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<AuditEvent>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<CommandProbe>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<ReceivingSessionPoc>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<ReceivingEntryPoc>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
        modelBuilder.Entity<ReceivingFinalizationPoc>()
            .HasQueryFilter(entity =>
                entity.WorkspaceId == ActiveWorkspaceId);
    }
}
