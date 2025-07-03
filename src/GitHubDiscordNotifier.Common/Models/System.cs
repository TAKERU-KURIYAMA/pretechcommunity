using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GitHubDiscordNotifier.Common.Models
{
    [Table("systems")]
    public class System
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Column("discord_server_id")]
        [Required]
        [MaxLength(255)]
        public string DiscordServerId { get; set; } = string.Empty;

        [Column("discord_server_name")]
        [MaxLength(255)]
        public string? DiscordServerName { get; set; }

        [Column("discord_bot_token")]
        [Required]
        [MaxLength(500)]
        public string DiscordBotToken { get; set; } = string.Empty;

        [Column("owner_id")]
        [Required]
        public Guid OwnerId { get; set; }

        [Column("webhook_secret")]
        [MaxLength(255)]
        public string? WebhookSecret { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public virtual User Owner { get; set; } = null!;

        public virtual ICollection<SystemMember> SystemMembers { get; set; } = new List<SystemMember>();
        public virtual ICollection<Repository> Repositories { get; set; } = new List<Repository>();
    }
}