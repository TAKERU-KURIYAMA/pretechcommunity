namespace GitHubDiscordNotifier.Domain.Entities;

public class System
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscordServerId { get; set; } = string.Empty;
    public string? DiscordServerName { get; set; }
    public string DiscordBotToken { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string WebhookSecret { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<SystemMember> Members { get; set; } = new List<SystemMember>();
    public ICollection<Repository> Repositories { get; set; } = new List<Repository>();
    public ICollection<NotificationChannel> NotificationChannels { get; set; } = new List<NotificationChannel>();
}