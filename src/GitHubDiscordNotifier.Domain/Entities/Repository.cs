namespace GitHubDiscordNotifier.Domain.Entities;

public class Repository
{
    public Guid Id { get; set; }
    public Guid SystemId { get; set; }
    public string GitHubRepositoryId { get; set; } = string.Empty;
    public string GitHubRepositoryName { get; set; } = string.Empty;
    public string GitHubRepositoryUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid AddedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public System System { get; set; } = null!;
    public User AddedByUser { get; set; } = null!;
    public ICollection<NotificationChannel> NotificationChannels { get; set; } = new List<NotificationChannel>();
}