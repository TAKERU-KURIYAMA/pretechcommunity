namespace GitHubDiscordNotifier.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? GitHubId { get; set; }
    public string? GitHubUsername { get; set; }
    public string? AvatarUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<System> OwnedSystems { get; set; } = new List<System>();
    public ICollection<SystemMember> SystemMemberships { get; set; } = new List<SystemMember>();
    public ICollection<Repository> AddedRepositories { get; set; } = new List<Repository>();
}