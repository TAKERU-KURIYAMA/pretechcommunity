using GitHubDiscordNotifier.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace GitHubDiscordNotifier.Common.Data
{
    public class NotifierDbContext : DbContext
    {
        public NotifierDbContext(DbContextOptions<NotifierDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Models.System> Systems { get; set; } = null!;
        public DbSet<SystemMember> SystemMembers { get; set; } = null!;
        public DbSet<Repository> Repositories { get; set; } = null!;
        public DbSet<NotificationChannel> NotificationChannels { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite key for SystemMember
            modelBuilder.Entity<SystemMember>()
                .HasKey(sm => new { sm.SystemId, sm.UserId });

            // Configure indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.GitHubId);

            modelBuilder.Entity<Models.System>()
                .HasIndex(s => s.DiscordServerId);

            modelBuilder.Entity<Models.System>()
                .HasIndex(s => s.OwnerId);

            modelBuilder.Entity<Repository>()
                .HasIndex(r => r.GitHubRepositoryId);

            modelBuilder.Entity<Repository>()
                .HasIndex(r => r.SystemId);

            modelBuilder.Entity<NotificationChannel>()
                .HasIndex(nc => nc.SystemId);

            modelBuilder.Entity<NotificationChannel>()
                .HasIndex(nc => nc.RepositoryId);

            // Configure default values
            modelBuilder.Entity<User>()
                .Property(u => u.Id)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Models.System>()
                .Property(s => s.Id)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<Models.System>()
                .Property(s => s.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Models.System>()
                .Property(s => s.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SystemMember>()
                .Property(sm => sm.JoinedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SystemMember>()
                .Property(sm => sm.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SystemMember>()
                .Property(sm => sm.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Repository>()
                .Property(r => r.Id)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<Repository>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Repository>()
                .Property(r => r.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<NotificationChannel>()
                .Property(nc => nc.Id)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<NotificationChannel>()
                .Property(nc => nc.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<NotificationChannel>()
                .Property(nc => nc.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Configure delete behavior
            modelBuilder.Entity<Models.System>()
                .HasOne(s => s.Owner)
                .WithMany()
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SystemMember>()
                .HasOne(sm => sm.System)
                .WithMany(s => s.SystemMembers)
                .HasForeignKey(sm => sm.SystemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SystemMember>()
                .HasOne(sm => sm.User)
                .WithMany(u => u.SystemMembers)
                .HasForeignKey(sm => sm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Repository>()
                .HasOne(r => r.System)
                .WithMany(s => s.Repositories)
                .HasForeignKey(r => r.SystemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Repository>()
                .HasOne(r => r.AddedByUser)
                .WithMany()
                .HasForeignKey(r => r.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NotificationChannel>()
                .HasOne(nc => nc.Repository)
                .WithMany(r => r.NotificationChannels)
                .HasForeignKey(nc => nc.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationChannel>()
                .HasOne(nc => nc.System)
                .WithMany()
                .HasForeignKey(nc => nc.SystemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}