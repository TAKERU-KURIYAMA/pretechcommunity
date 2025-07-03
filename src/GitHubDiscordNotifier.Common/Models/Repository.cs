using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GitHubDiscordNotifier.Common.Models
{
    [Table("repositories")]
    public class Repository
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("system_id")]
        [Required]
        public Guid SystemId { get; set; }

        [Column("github_repository_id")]
        [Required]
        [MaxLength(255)]
        public string GitHubRepositoryId { get; set; } = string.Empty;

        [Column("github_repository_name")]
        [Required]
        [MaxLength(255)]
        public string GitHubRepositoryName { get; set; } = string.Empty;

        [Column("github_repository_url")]
        [Required]
        [MaxLength(500)]
        public string GitHubRepositoryUrl { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("added_by_user_id")]
        [Required]
        public Guid AddedByUserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(SystemId))]
        public virtual System System { get; set; } = null!;

        [ForeignKey(nameof(AddedByUserId))]
        public virtual User AddedByUser { get; set; } = null!;

        public virtual ICollection<NotificationChannel> NotificationChannels { get; set; } = new List<NotificationChannel>();
    }
}