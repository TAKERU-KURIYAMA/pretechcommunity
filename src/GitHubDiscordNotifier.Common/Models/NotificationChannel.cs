using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GitHubDiscordNotifier.Common.Models
{
    [Table("notification_channels")]
    public class NotificationChannel
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("system_id")]
        [Required]
        public Guid SystemId { get; set; }

        [Column("repository_id")]
        [Required]
        public Guid RepositoryId { get; set; }

        [Column("discord_channel_id")]
        [Required]
        [MaxLength(255)]
        public string DiscordChannelId { get; set; } = string.Empty;

        [Column("discord_channel_name")]
        [MaxLength(255)]
        public string? DiscordChannelName { get; set; }

        [Column("event_types")]
        [Required]
        [MaxLength(1000)]
        public string EventTypes { get; set; } = string.Empty; // JSON array of event types

        [Column("notification_template")]
        [MaxLength(4000)]
        public string? NotificationTemplate { get; set; } // Custom notification template

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(SystemId))]
        public virtual System System { get; set; } = null!;

        [ForeignKey(nameof(RepositoryId))]
        public virtual Repository Repository { get; set; } = null!;
    }
}