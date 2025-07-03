using GitHubDiscordNotifier.Common.Data;
using GitHubDiscordNotifier.Common.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;

[assembly: FunctionsStartup(typeof(GitHubDiscordNotifier.Api.Startup))]

namespace GitHubDiscordNotifier.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var context = builder.GetContext();
            var configuration = context.Configuration;

            // Configure Entity Framework
            builder.Services.AddDbContext<NotifierDbContext>(options =>
                options.UseSqlServer(configuration["SqlConnectionString"]));

            // Configure Redis
            var redisConnectionString = configuration["RedisConnectionString"];
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(redisConnectionString));
            }

            // Configure HttpClient
            builder.Services.AddHttpClient();

            // Configure Discord Service
            builder.Services.AddSingleton<DiscordService>();

            // Configure Application Insights
            var appInsightsConnectionString = configuration["ApplicationInsightsConnectionString"];
            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                builder.Services.AddApplicationInsightsTelemetry(appInsightsConnectionString);
            }

            // Add logging
            builder.Services.AddLogging();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }
    }
}