using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Notifications.Models;
using TaskBridge.Api.Projects.Models;
using TaskBridge.Api.Projects.Repositories;

namespace TaskBridge.Api.Data;

public sealed class TaskBridgeDbContext(DbContextOptions<TaskBridgeDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(cancellationToken);

    public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAuditEntriesAreImmutable();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureAuditEntriesAreImmutable();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .HasIndex(project => new { project.OrganizationId, project.TeamId });

        modelBuilder.Entity<Project>()
            .Property(project => project.Name)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<Team>()
            .HasAlternateKey(team => new { team.OrganizationId, team.Id });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(member => new { member.OrganizationId, member.TeamId, member.UserId });
            entity.Property(member => member.UserId).HasMaxLength(200);
            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(member => new { member.OrganizationId, member.TeamId })
                .HasPrincipalKey(team => new { team.OrganizationId, team.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(member => new { member.OrganizationId, member.TeamId, member.UserId });
            entity.Property(member => member.UserId).HasMaxLength(200);
            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(member => new { member.OrganizationId, member.TeamId })
                .HasPrincipalKey(team => new { team.OrganizationId, team.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>()
            .Property(project => project.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<Project>()
            .HasOne<Team>()
            .WithMany()
            .HasForeignKey(project => new { project.OrganizationId, project.TeamId })
            .HasPrincipalKey(team => new { team.OrganizationId, team.Id })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.Property(audit => audit.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(audit => audit.EventType).HasMaxLength(100).IsRequired();
            entity.Property(audit => audit.ActorUserId).HasMaxLength(200).IsRequired();
            entity.Property(audit => audit.ActorIpAddress).HasMaxLength(45);
            entity.HasIndex(audit => new { audit.OrganizationId, audit.ProjectId, audit.Timestamp });
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.RecipientUserId).HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.EventType).HasMaxLength(100).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(1000).IsRequired();
            entity.HasIndex(notification => new
            {
                notification.OrganizationId,
                notification.RecipientUserId,
                notification.IsRead,
                notification.CreatedAt
            });
        });

        base.OnModelCreating(modelBuilder);
    }

    private void EnsureAuditEntriesAreImmutable()
    {
        if (ChangeTracker.Entries<AuditEntry>().Any(entry =>
                entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Audit entries cannot be updated or deleted.");
        }
    }
}