using AudioService.Application.DTOs;

namespace AudioService.Application.Services;

public interface IAudioChannelService
{
    Task<AudioChannelDto> CreateAudioChannelAsync(CreateAudioChannelDto createDto, string userId, CancellationToken cancellationToken = default);
    Task<AudioChannelDto?> GetAudioChannelByIdAsync(string channelId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AudioChannelDto>> GetChannelsByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task<AudioChannelDto> UpdateAudioChannelAsync(string channelId, UpdateAudioChannelDto updateDto, string userId, CancellationToken cancellationToken = default);
    Task DeleteAudioChannelAsync(string channelId, string userId, CancellationToken cancellationToken = default);
    Task<bool> RecreateJanusRoomAsync(string channelId, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AudioParticipantDto>> GetChannelParticipantsAsync(string channelId, CancellationToken cancellationToken = default);
}
