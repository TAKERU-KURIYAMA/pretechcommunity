namespace GitHubDiscordNotifier.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateToken(Guid userId, string email);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
    Guid GetUserIdFromToken(string token);
}