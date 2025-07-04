namespace GitHubDiscordNotifier.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
    Task SendWelcomeEmailAsync(string to, string userName);
    Task SendPasswordResetEmailAsync(string to, string resetLink);
    Task SendSystemInvitationEmailAsync(string to, string inviterName, string systemName);
}