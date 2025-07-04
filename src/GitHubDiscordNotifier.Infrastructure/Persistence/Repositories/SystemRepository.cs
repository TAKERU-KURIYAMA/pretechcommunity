using GitHubDiscordNotifier.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GitHubDiscordNotifier.Infrastructure.Persistence.Repositories;

public class SystemRepository : ISystemRepository
{
    private readonly NotifierDbContext _context;

    public SystemRepository(NotifierDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Entities.System?> GetByIdAsync(Guid id)
    {
        return await _context.Systems
            .Include(s => s.Owner)
            .Include(s => s.Repositories)
            .Include(s => s.NotificationChannels)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Domain.Entities.System?> GetByIdWithMembersAsync(Guid id)
    {
        return await _context.Systems
            .Include(s => s.Owner)
            .Include(s => s.Members)
                .ThenInclude(m => m.User)
            .Include(s => s.Repositories)
            .Include(s => s.NotificationChannels)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Domain.Entities.System>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Systems
            .Include(s => s.Owner)
            .Include(s => s.Members)
            .Where(s => s.OwnerId == userId || s.Members.Any(m => m.UserId == userId))
            .ToListAsync();
    }

    public async Task<Domain.Entities.System> AddAsync(Domain.Entities.System system)
    {
        await _context.Systems.AddAsync(system);
        return system;
    }

    public Task UpdateAsync(Domain.Entities.System system)
    {
        _context.Systems.Update(system);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Domain.Entities.System system)
    {
        _context.Systems.Remove(system);
        return Task.CompletedTask;
    }

    public async Task<bool> IsUserMemberAsync(Guid systemId, Guid userId)
    {
        return await _context.SystemMembers
            .AnyAsync(sm => sm.SystemId == systemId && sm.UserId == userId);
    }

    public async Task<string?> GetUserRoleAsync(Guid systemId, Guid userId)
    {
        var member = await _context.SystemMembers
            .FirstOrDefaultAsync(sm => sm.SystemId == systemId && sm.UserId == userId);
        
        return member?.Role;
    }
}