using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GitHubDiscordNotifier.Common.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System;

namespace GitHubDiscordNotifier.Api.Health
{
    public class GetHealth
    {
        private readonly NotifierDbContext _dbContext;
        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<GetHealth> _logger;

        public GetHealth(NotifierDbContext dbContext, IConnectionMultiplexer? redis, ILogger<GetHealth> logger)
        {
            _dbContext = dbContext;
            _redis = redis;
            _logger = logger;
        }

        [FunctionName("GetHealth")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
        {
            try
            {
                var healthStatus = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    checks = new
                    {
                        database = await CheckDatabase(),
                        redis = CheckRedis()
                    }
                };

                return new OkObjectResult(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return new ObjectResult(new { status = "unhealthy", error = ex.Message })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
            }
        }

        private async Task<object> CheckDatabase()
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
                return new { status = "healthy" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return new { status = "unhealthy", error = ex.Message };
            }
        }

        private object CheckRedis()
        {
            try
            {
                if (_redis == null)
                {
                    return new { status = "not configured" };
                }

                var db = _redis.GetDatabase();
                db.Ping();
                return new { status = "healthy" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return new { status = "unhealthy", error = ex.Message };
            }
        }
    }
}