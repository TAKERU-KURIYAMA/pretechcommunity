namespace GitHubDiscordNotifier.Application.Common.Interfaces;

public interface IDiscordService
{
    Task<bool> ValidateBotTokenAsync(string token);
    Task<Dictionary<string, string>> GetChannelsAsync(string botToken, string serverId);
    Task<bool> SendNotificationAsync(string botToken, string channelId, string message, object? embedData = null);
}