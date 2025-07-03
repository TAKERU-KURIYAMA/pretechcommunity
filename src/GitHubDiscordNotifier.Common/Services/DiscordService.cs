using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GitHubDiscordNotifier.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GitHubDiscordNotifier.Common.Services
{
    public class DiscordService
    {
        private readonly Dictionary<string, DiscordSocketClient> _clients = new();
        private readonly ILogger<DiscordService> _logger;

        public DiscordService(ILogger<DiscordService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> InitializeBotAsync(string botToken, string systemId)
        {
            try
            {
                if (_clients.ContainsKey(systemId))
                {
                    await _clients[systemId].LogoutAsync();
                    _clients.Remove(systemId);
                }

                var client = new DiscordSocketClient();
                
                client.Log += (message) =>
                {
                    _logger.LogInformation($"Discord Bot {systemId}: {message}");
                    return Task.CompletedTask;
                };

                await client.LoginAsync(TokenType.Bot, botToken);
                await client.StartAsync();

                _clients[systemId] = client;
                
                _logger.LogInformation($"Discord bot initialized for system {systemId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to initialize Discord bot for system {systemId}");
                return false;
            }
        }

        public async Task<bool> SendNotificationAsync(string systemId, ulong channelId, string eventType, JObject webhookData, string? customTemplate = null)
        {
            try
            {
                if (!_clients.TryGetValue(systemId, out var client))
                {
                    _logger.LogError($"Discord client not found for system {systemId}");
                    return false;
                }

                var channel = client.GetChannel(channelId) as IMessageChannel;
                if (channel == null)
                {
                    _logger.LogError($"Discord channel {channelId} not found for system {systemId}");
                    return false;
                }

                var embed = CreateEmbedFromWebhook(eventType, webhookData, customTemplate);
                await channel.SendMessageAsync(embed: embed);

                _logger.LogInformation($"Discord notification sent to channel {channelId} for system {systemId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send Discord notification for system {systemId}");
                return false;
            }
        }

        private Embed CreateEmbedFromWebhook(string eventType, JObject webhookData, string? customTemplate)
        {
            var embedBuilder = new EmbedBuilder();

            var repository = webhookData["repository"];
            var repoName = repository?["full_name"]?.ToString() ?? "Unknown Repository";
            var repoUrl = repository?["html_url"]?.ToString();

            // Set color based on event type
            var color = eventType switch
            {
                "push" => Color.Green,
                "pull_request" => Color.Blue,
                "issues" => Color.Orange,
                "release" => Color.Purple,
                _ => Color.Default
            };

            embedBuilder.WithColor(color);

            // Process different event types
            switch (eventType)
            {
                case "push":
                    ProcessPushEvent(embedBuilder, webhookData, repoName, repoUrl);
                    break;
                case "pull_request":
                    ProcessPullRequestEvent(embedBuilder, webhookData, repoName, repoUrl);
                    break;
                case "issues":
                    ProcessIssuesEvent(embedBuilder, webhookData, repoName, repoUrl);
                    break;
                case "release":
                    ProcessReleaseEvent(embedBuilder, webhookData, repoName, repoUrl);
                    break;
                default:
                    ProcessGenericEvent(embedBuilder, webhookData, eventType, repoName, repoUrl);
                    break;
            }

            embedBuilder.WithTimestamp(DateTimeOffset.UtcNow);
            return embedBuilder.Build();
        }

        private void ProcessPushEvent(EmbedBuilder embedBuilder, JObject webhookData, string repoName, string? repoUrl)
        {
            var pusher = webhookData["pusher"]?["name"]?.ToString();
            var refName = webhookData["ref"]?.ToString()?.Replace("refs/heads/", "");
            var commits = webhookData["commits"] as JArray;
            var commitCount = commits?.Count ?? 0;

            embedBuilder.WithTitle($"🚀 Push to {repoName}");
            embedBuilder.WithDescription($"**{pusher}** pushed {commitCount} commit(s) to `{refName}`");

            if (commits != null && commits.Count > 0)
            {
                var commitList = commits.Take(5).Select(c => 
                    $"[`{c["id"]?.ToString()?[..7]}`]({c["url"]}) {c["message"]?.ToString()?.Split('\n')[0]}"
                ).ToList();

                if (commits.Count > 5)
                    commitList.Add($"... and {commits.Count - 5} more commit(s)");

                embedBuilder.AddField("Commits", string.Join("\n", commitList));
            }

            if (!string.IsNullOrEmpty(repoUrl))
                embedBuilder.WithUrl(repoUrl);
        }

        private void ProcessPullRequestEvent(EmbedBuilder embedBuilder, JObject webhookData, string repoName, string? repoUrl)
        {
            var action = webhookData["action"]?.ToString();
            var pr = webhookData["pull_request"];
            var title = pr?["title"]?.ToString();
            var user = pr?["user"]?["login"]?.ToString();
            var prUrl = pr?["html_url"]?.ToString();
            var number = pr?["number"]?.ToString();

            var actionEmoji = action switch
            {
                "opened" => "🔵",
                "closed" => "🔴",
                "merged" => "🟣",
                "reopened" => "🟡",
                _ => "🔵"
            };

            embedBuilder.WithTitle($"{actionEmoji} Pull Request #{number} {action}");
            embedBuilder.WithDescription($"**{user}** {action} a pull request in {repoName}");
            embedBuilder.AddField("Title", title ?? "No title");

            if (!string.IsNullOrEmpty(prUrl))
                embedBuilder.WithUrl(prUrl);
        }

        private void ProcessIssuesEvent(EmbedBuilder embedBuilder, JObject webhookData, string repoName, string? repoUrl)
        {
            var action = webhookData["action"]?.ToString();
            var issue = webhookData["issue"];
            var title = issue?["title"]?.ToString();
            var user = issue?["user"]?["login"]?.ToString();
            var issueUrl = issue?["html_url"]?.ToString();
            var number = issue?["number"]?.ToString();

            var actionEmoji = action switch
            {
                "opened" => "🟢",
                "closed" => "🔴",
                "reopened" => "🟡",
                _ => "🟢"
            };

            embedBuilder.WithTitle($"{actionEmoji} Issue #{number} {action}");
            embedBuilder.WithDescription($"**{user}** {action} an issue in {repoName}");
            embedBuilder.AddField("Title", title ?? "No title");

            if (!string.IsNullOrEmpty(issueUrl))
                embedBuilder.WithUrl(issueUrl);
        }

        private void ProcessReleaseEvent(EmbedBuilder embedBuilder, JObject webhookData, string repoName, string? repoUrl)
        {
            var action = webhookData["action"]?.ToString();
            var release = webhookData["release"];
            var tagName = release?["tag_name"]?.ToString();
            var releaseName = release?["name"]?.ToString();
            var author = release?["author"]?["login"]?.ToString();
            var releaseUrl = release?["html_url"]?.ToString();

            embedBuilder.WithTitle($"🎉 Release {tagName} {action}");
            embedBuilder.WithDescription($"**{author}** {action} a release in {repoName}");
            
            if (!string.IsNullOrEmpty(releaseName))
                embedBuilder.AddField("Release Name", releaseName);

            if (!string.IsNullOrEmpty(releaseUrl))
                embedBuilder.WithUrl(releaseUrl);
        }

        private void ProcessGenericEvent(EmbedBuilder embedBuilder, JObject webhookData, string eventType, string repoName, string? repoUrl)
        {
            embedBuilder.WithTitle($"📬 {eventType} event in {repoName}");
            embedBuilder.WithDescription($"A {eventType} event occurred in {repoName}");

            if (!string.IsNullOrEmpty(repoUrl))
                embedBuilder.WithUrl(repoUrl);
        }

        public async Task DisposeBotAsync(string systemId)
        {
            if (_clients.TryGetValue(systemId, out var client))
            {
                await client.LogoutAsync();
                await client.DisposeAsync();
                _clients.Remove(systemId);
                _logger.LogInformation($"Discord bot disposed for system {systemId}");
            }
        }

        public async Task DisposeAllAsync()
        {
            foreach (var kvp in _clients)
            {
                await kvp.Value.LogoutAsync();
                await kvp.Value.DisposeAsync();
            }
            _clients.Clear();
        }
    }
}