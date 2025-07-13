using GitHubDiscordNotifier.Domain.Entities;

namespace GitHubDiscordNotifier.Domain.Interfaces;

public interface ISystemRepository
{
    Task<System?> GetByIdAsync(Guid id);
    Task<System?> GetByIdWithMembersAsync(Guid id);
    Task<IEnumerable<System>> GetByUserIdAsync(Guid userId);
    Task<System> AddAsync(System system);
    Task UpdateAsync(System system);
    Task DeleteAsync(System system);
    Task<bool> IsUserMemberAsync(Guid systemId, Guid userId);
    Task<string?> GetUserRoleAsync(Guid systemId, Guid userId);
}