using AudioService.Domain.Entities;

namespace AudioService.Domain.Interfaces;

public interface IAudioChannelRepository
{
    Task<AudioChannel?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AudioChannel>> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task AddAsync(AudioChannel channel, CancellationToken cancellationToken = default);
    Task UpdateAsync(AudioChannel channel, CancellationToken cancellationToken = default);
    Task DeleteAsync(AudioChannel channel, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
