using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GitHubDiscordNotifier.Common.Models
{
    [Table("system_members")]
    public class SystemMember
    {
        [Column("system_id")]
        public Guid SystemId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("role")]
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "member"; // owner, admin, member

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(SystemId))]
        public virtual System System { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}