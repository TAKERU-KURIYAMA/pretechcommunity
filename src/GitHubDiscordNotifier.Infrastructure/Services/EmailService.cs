using GitHubDiscordNotifier.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GitHubDiscordNotifier.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        // TODO: Implement actual email sending logic
        // For now, just log the email
        _logger.LogInformation("Sending email to {To}: {Subject}", to, subject);
        await Task.CompletedTask;
    }

    public async Task SendWelcomeEmailAsync(string to, string userName)
    {
        var subject = "Welcome to GitHub Discord Notifier!";
        var body = $@"
            <h1>Welcome {userName}!</h1>
            <p>Thank you for registering with GitHub Discord Notifier.</p>
            <p>You can now create systems and start receiving GitHub notifications in your Discord server.</p>
            <p>Best regards,<br>The GitHub Discord Notifier Team</p>
        ";
        
        await SendAsync(to, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetLink)
    {
        var subject = "Password Reset Request";
        var body = $@"
            <h1>Password Reset</h1>
            <p>You have requested to reset your password.</p>
            <p>Please click the link below to reset your password:</p>
            <p><a href='{resetLink}'>Reset Password</a></p>
            <p>This link will expire in 1 hour.</p>
            <p>If you did not request this, please ignore this email.</p>
            <p>Best regards,<br>The GitHub Discord Notifier Team</p>
        ";
        
        await SendAsync(to, subject, body);
    }

    public async Task SendSystemInvitationEmailAsync(string to, string inviterName, string systemName)
    {
        var subject = $"You've been invited to join {systemName}";
        var body = $@"
            <h1>System Invitation</h1>
            <p>{inviterName} has invited you to join the system '{systemName}' on GitHub Discord Notifier.</p>
            <p>To accept this invitation, please log in to your account or create a new account if you don't have one.</p>
            <p>Best regards,<br>The GitHub Discord Notifier Team</p>
        ";
        
        await SendAsync(to, subject, body);
    }
}