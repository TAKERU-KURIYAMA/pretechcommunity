using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GitHubDiscordNotifier.Common.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("email")]
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("github_id")]
        [MaxLength(255)]
        public string? GitHubId { get; set; }

        [Column("github_username")]
        [MaxLength(255)]
        public string? GitHubUsername { get; set; }

        [Column("refresh_token")]
        [MaxLength(500)]
        public string? RefreshToken { get; set; }

        [Column("refresh_token_expiry")]
        public DateTime? RefreshTokenExpiry { get; set; }

        [Column("avatar_url")]
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<SystemMember> SystemMembers { get; set; } = new List<SystemMember>();
    }
}