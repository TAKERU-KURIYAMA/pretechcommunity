using System;
using System.Linq;
using System.Threading.Tasks;
using GitHubDiscordNotifier.Common.Data;
using GitHubDiscordNotifier.Common.Services;
using GitHubDiscordNotifier.Common.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHubDiscordNotifier.Api.Webhook
{
    public class PostWebhook
    {
        private readonly NotifierDbContext _dbContext;
        private readonly DiscordService _discordService;
        private readonly ILogger<PostWebhook> _logger;

        public PostWebhook(NotifierDbContext dbContext, DiscordService discordService, ILogger<PostWebhook> logger)
        {
            _dbContext = dbContext;
            _discordService = discordService;
            _logger = logger;
        }

        [FunctionName("PostWebhook")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhook/{systemId}")] HttpRequest req,
            string systemId)
        {
            try
            {
                // Parse system ID
                if (!Guid.TryParse(systemId, out var systemGuid))
                {
                    return new BadRequestObjectResult(new { error = "Invalid system ID" });
                }

                // Get system
                var system = await _dbContext.Systems
                    .FirstOrDefaultAsync(s => s.Id == systemGuid);

                if (system == null)
                {
                    return new NotFoundObjectResult(new { error = "System not found" });
                }

                // Read payload
                var payload = await req.ReadAsStringAsync();
                if (string.IsNullOrEmpty(payload))
                {
                    return new BadRequestObjectResult(new { error = "Empty payload" });
                }

                // Verify webhook signature
                var signature = req.Headers["X-Hub-Signature-256"].ToString();
                if (!string.IsNullOrEmpty(system.WebhookSecret) && 
                    !WebhookHelper.VerifyGitHubWebhookSignature(payload, signature, system.WebhookSecret))
                {
                    _logger.LogWarning($"Invalid webhook signature for system {systemId}");
                    return new UnauthorizedObjectResult(new { error = "Invalid signature" });
                }

                // Parse GitHub event
                var eventType = req.Headers["X-GitHub-Event"].ToString();
                var webhookData = JsonConvert.DeserializeObject<JObject>(payload);

                if (webhookData == null)
                {
                    return new BadRequestObjectResult(new { error = "Invalid JSON payload" });
                }

                _logger.LogInformation($"Received GitHub webhook: {eventType} for system {systemId}");

                // Process webhook based on event type
                await ProcessWebhookEvent(system, eventType, webhookData);

                return new OkObjectResult(new { message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing webhook for system {systemId}");
                return new ObjectResult(new { error = "Internal server error" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        private async Task ProcessWebhookEvent(Common.Models.System system, string eventType, JObject webhookData)
        {
            try
            {
                // Get repository from webhook data
                var repository = webhookData["repository"];
                if (repository == null) return;

                var repoId = repository["id"]?.ToString();
                var repoName = repository["full_name"]?.ToString();

                // Find matching repository in our system
                var systemRepo = await _dbContext.Repositories
                    .FirstOrDefaultAsync(r => r.SystemId == system.Id && 
                                            r.GitHubRepositoryId == repoId);

                if (systemRepo == null)
                {
                    _logger.LogInformation($"Repository {repoName} not configured for system {system.Id}");
                    return;
                }

                // Get notification channels for this repository
                var notificationChannels = await _dbContext.NotificationChannels
                    .Where(nc => nc.RepositoryId == systemRepo.Id && nc.IsActive)
                    .ToListAsync();

                foreach (var channel in notificationChannels)
                {
                    // Check if this event type is enabled for this channel
                    var eventTypes = JsonConvert.DeserializeObject<string[]>(channel.EventTypes);
                    if (eventTypes?.Contains(eventType) == true)
                    {
                        await SendDiscordNotification(system, channel, eventType, webhookData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing webhook event {eventType}");
            }
        }

        private async Task SendDiscordNotification(Common.Models.System system, Common.Models.NotificationChannel channel, string eventType, JObject webhookData)
        {
            try
            {
                // Initialize Discord bot for this system if not already done
                await _discordService.InitializeBotAsync(system.DiscordBotToken, system.Id.ToString());

                // Parse channel ID
                if (!ulong.TryParse(channel.DiscordChannelId, out var channelId))
                {
                    _logger.LogError($"Invalid Discord channel ID: {channel.DiscordChannelId}");
                    return;
                }

                // Send notification
                var success = await _discordService.SendNotificationAsync(
                    system.Id.ToString(), 
                    channelId, 
                    eventType, 
                    webhookData, 
                    channel.NotificationTemplate);

                if (success)
                {
                    _logger.LogInformation($"Discord notification sent for {eventType} to channel {channel.DiscordChannelId}");
                }
                else
                {
                    _logger.LogError($"Failed to send Discord notification for {eventType} to channel {channel.DiscordChannelId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending Discord notification for {eventType}");
            }
        }
    }
}