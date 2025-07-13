namespace GitHubDiscordNotifier.Domain.Entities;

public class SystemMember
{
    public Guid SystemId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "member"; // owner, admin, member
    public DateTime JoinedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public System System { get; set; } = null!;
    public User User { get; set; } = null!;
}