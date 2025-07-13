using GitHubDiscordNotifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GitHubDiscordNotifier.Infrastructure.Persistence;

public class NotifierDbContext : DbContext
{
    public NotifierDbContext(DbContextOptions<NotifierDbContext> options) 
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Domain.Entities.System> Systems => Set<Domain.Entities.System>();
    public DbSet<SystemMember> SystemMembers => Set<SystemMember>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.GitHubId).HasMaxLength(255);
            entity.Property(e => e.GitHubUsername).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
        });

        // System configuration
        modelBuilder.Entity<Domain.Entities.System>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiscordServerId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.DiscordServerName).HasMaxLength(255);
            entity.Property(e => e.DiscordBotToken).HasMaxLength(500).IsRequired();
            entity.Property(e => e.WebhookSecret).HasMaxLength(255).IsRequired();
            
            entity.HasOne(e => e.Owner)
                .WithMany(u => u.OwnedSystems)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SystemMember configuration
        modelBuilder.Entity<SystemMember>(entity =>
        {
            entity.HasKey(e => new { e.SystemId, e.UserId });
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            
            entity.HasOne(e => e.System)
                .WithMany(s => s.Members)
                .HasForeignKey(e => e.SystemId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.SystemMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Repository configuration
        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GitHubRepositoryId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.GitHubRepositoryName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.GitHubRepositoryUrl).HasMaxLength(500).IsRequired();
            
            entity.HasOne(e => e.System)
                .WithMany(s => s.Repositories)
                .HasForeignKey(e => e.SystemId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.AddedByUser)
                .WithMany(u => u.AddedRepositories)
                .HasForeignKey(e => e.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // NotificationChannel configuration
        modelBuilder.Entity<NotificationChannel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiscordChannelId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.DiscordChannelName).HasMaxLength(255);
            entity.Property(e => e.EventTypes).IsRequired();
            
            entity.HasOne(e => e.System)
                .WithMany(s => s.NotificationChannels)
                .HasForeignKey(e => e.SystemId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Repository)
                .WithMany(r => r.NotificationChannels)
                .HasForeignKey(e => e.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is User || e.Entity is Domain.Entities.System || 
                       e.Entity is SystemMember || e.Entity is Repository || 
                       e.Entity is NotificationChannel);

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                if (entityEntry.Entity is User user)
                {
                    user.CreatedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else if (entityEntry.Entity is Domain.Entities.System system)
                {
                    system.CreatedAt = DateTime.UtcNow;
                    system.UpdatedAt = DateTime.UtcNow;
                }
                else if (entityEntry.Entity is SystemMember member)
                {
                    member.CreatedAt = DateTime.UtcNow;
                    member.UpdatedAt = DateTime.UtcNow;
                    member.JoinedAt = DateTime.UtcNow;
                }
                else if (entityEntry.Entity is Repository repo)
                {
                    repo.CreatedAt = DateTime.UtcNow;
                    repo.UpdatedAt = DateTime.UtcNow;
                }
                else if (entityEntry.Entity is NotificationChannel channel)
                {
                    channel.CreatedAt = DateTime.UtcNow;
                    channel.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                if (entityEntry.Entity is User user)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else if (entityEntry.Entity is Domain.Entities.System system)
                {
                    system.UpdatedAt = DateTime.UtcNow;
                }
                else if (entityEntry.Entity is SystemMember member)
                {
                    member.UpdatedAt = DateTime.UtcNow;
                }
                else if (entityEntry.Entity is Repository repo)
                {
                    repo.UpdatedAt = DateTime.UtcNow;
                }
                else if (entityEntry.Entity is NotificationChannel channel)
                {
                    channel.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}