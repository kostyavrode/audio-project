using AudioService.Domain.Entities;
using AudioService.Domain.Interfaces;
using AudioService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AudioService.Infrastructure.Repository;

public class AudioChannelRepository : IAudioChannelRepository
{
    private readonly AudioDbContext _context;

    public AudioChannelRepository(AudioDbContext context)
    {
        _context = context;
    }

    public async Task<AudioChannel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.AudioChannels
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AudioChannel>> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        return await _context.AudioChannels
            .Where(c => c.GroupId == groupId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AudioChannel channel, CancellationToken cancellationToken = default)
    {
        await _context.AudioChannels.AddAsync(channel, cancellationToken);
    }

    public Task UpdateAsync(AudioChannel channel, CancellationToken cancellationToken = default)
    {
        _context.AudioChannels.Update(channel);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AudioChannel channel, CancellationToken cancellationToken = default)
    {
        _context.AudioChannels.Remove(channel);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.AudioChannels
            .AnyAsync(c => c.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
