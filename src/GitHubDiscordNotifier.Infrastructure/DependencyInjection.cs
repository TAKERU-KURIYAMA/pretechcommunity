using GitHubDiscordNotifier.Domain.Interfaces;
using GitHubDiscordNotifier.Infrastructure.Persistence;
using GitHubDiscordNotifier.Infrastructure.Persistence.Repositories;
using GitHubDiscordNotifier.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GitHubDiscordNotifier.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<NotifierDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(NotifierDbContext).Assembly.FullName)));

        // Add Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // Add repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISystemRepository, SystemRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IDiscordService, DiscordService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}