namespace GitHubDiscordNotifier.Domain.Entities;

public class NotificationChannel
{
    public Guid Id { get; set; }
    public Guid SystemId { get; set; }
    public Guid RepositoryId { get; set; }
    public string DiscordChannelId { get; set; } = string.Empty;
    public string? DiscordChannelName { get; set; }
    public string EventTypes { get; set; } = "[]"; // JSON array
    public string? NotificationTemplate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public System System { get; set; } = null!;
    public Repository Repository { get; set; } = null!;
}