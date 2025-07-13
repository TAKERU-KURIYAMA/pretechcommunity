using Discord;
using Discord.WebSocket;
using GitHubDiscordNotifier.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GitHubDiscordNotifier.Infrastructure.Services;

public class DiscordService : IDiscordService
{
    private readonly ILogger<DiscordService> _logger;

    public DiscordService(ILogger<DiscordService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ValidateBotTokenAsync(string token)
    {
        try
        {
            var client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            
            // Wait a bit to ensure connection is established
            await Task.Delay(2000);
            
            var isConnected = client.ConnectionState == ConnectionState.Connected;
            
            await client.StopAsync();
            await client.LogoutAsync();
            client.Dispose();
            
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Discord bot token");
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetChannelsAsync(string botToken, string serverId)
    {
        var channels = new Dictionary<string, string>();
        
        try
        {
            var client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, botToken);
            await client.StartAsync();
            
            // Wait for ready
            await Task.Delay(3000);
            
            if (ulong.TryParse(serverId, out var guildId))
            {
                var guild = client.GetGuild(guildId);
                if (guild != null)
                {
                    foreach (var channel in guild.TextChannels)
                    {
                        channels.Add(channel.Id.ToString(), channel.Name);
                    }
                }
            }
            
            await client.StopAsync();
            await client.LogoutAsync();
            client.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Discord channels");
        }
        
        return channels;
    }

    public async Task<bool> SendNotificationAsync(string botToken, string channelId, string message, object? embedData = null)
    {
        try
        {
            var client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, botToken);
            await client.StartAsync();
            
            // Wait for ready
            await Task.Delay(3000);
            
            if (ulong.TryParse(channelId, out var channelIdParsed))
            {
                var channel = client.GetChannel(channelIdParsed) as ITextChannel;
                if (channel != null)
                {
                    if (embedData != null)
                    {
                        // TODO: Build embed from embedData
                        var embed = new EmbedBuilder()
                            .WithTitle("GitHub Notification")
                            .WithDescription(message)
                            .WithColor(Color.Blue)
                            .WithTimestamp(DateTimeOffset.Now)
                            .Build();
                            
                        await channel.SendMessageAsync(embed: embed);
                    }
                    else
                    {
                        await channel.SendMessageAsync(message);
                    }
                    
                    await client.StopAsync();
                    await client.LogoutAsync();
                    client.Dispose();
                    
                    return true;
                }
            }
            
            await client.StopAsync();
            await client.LogoutAsync();
            client.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Discord notification");
        }
        
        return false;
    }
}