using AudioService.Application.DTOs;
using AudioService.Domain.Entities;
using AudioService.Domain.Exceptions;
using AudioService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AudioService.Application.Services;

public class AudioChannelService : IAudioChannelService
{
    private readonly IAudioChannelRepository _channelRepository;
    private readonly IJanusGatewayClient _janusGatewayClient;
    private readonly IGroupAccessChecker _groupAccessChecker;
    private readonly ILogger<AudioChannelService> _logger;

    public AudioChannelService(
        IAudioChannelRepository channelRepository,
        IJanusGatewayClient janusGatewayClient,
        IGroupAccessChecker groupAccessChecker,
        ILogger<AudioChannelService> logger)
    {
        _channelRepository = channelRepository;
        _janusGatewayClient = janusGatewayClient;
        _groupAccessChecker = groupAccessChecker;
        _logger = logger;
    }

    public async Task<AudioChannelDto> CreateAudioChannelAsync(CreateAudioChannelDto createDto, string userId, CancellationToken cancellationToken = default)
    {
        var isOwner = await _groupAccessChecker.IsGroupOwnerAsync(createDto.GroupId, userId, cancellationToken);

        if (!isOwner)
        {
            throw new UnauthorizedToCreateChannelException(createDto.GroupId, userId);
        }

        var channelId = Guid.NewGuid().ToString();
        var channel = AudioChannel.Create(channelId, createDto.GroupId, createDto.Name);

        var janusRoomId = GetJanusRoomId(channelId);

        try
        {
            await _janusGatewayClient.CreateRoomAsync(janusRoomId, createDto.Name, cancellationToken);
            channel.SetJanusRoomId(janusRoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Janus room for channel {ChannelId}", channelId);
            throw;
        }

        await _channelRepository.AddAsync(channel, cancellationToken);
        await _channelRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audio channel {ChannelId} created in group {GroupId} by user {UserId}", channelId, createDto.GroupId, userId);

        return MapToAudioChannelDto(channel);
    }

    public async Task<AudioChannelDto?> GetAudioChannelByIdAsync(string channelId, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel == null)
        {
            return null;
        }

        return MapToAudioChannelDto(channel);
    }

    public async Task<IEnumerable<AudioChannelDto>> GetChannelsByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var channels = await _channelRepository.GetByGroupIdAsync(groupId, cancellationToken);

        return channels.Select(MapToAudioChannelDto);
    }

    public async Task<AudioChannelDto> UpdateAudioChannelAsync(string channelId, UpdateAudioChannelDto updateDto, string userId, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel == null)
        {
            throw new AudioChannelNotFoundException(channelId);
        }

        var isOwner = await _groupAccessChecker.IsGroupOwnerAsync(channel.GroupId, userId, cancellationToken);

        if (!isOwner)
        {
            throw new UnauthorizedAccessException("Only group owner can update audio channels");
        }

        if (!string.IsNullOrEmpty(updateDto.Name))
        {
            channel.UpdateName(updateDto.Name);
        }

        await _channelRepository.UpdateAsync(channel, cancellationToken);
        await _channelRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audio channel {ChannelId} updated by user {UserId}", channelId, userId);

        return MapToAudioChannelDto(channel);
    }

    public async Task DeleteAudioChannelAsync(string channelId, string userId, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel == null)
        {
            throw new AudioChannelNotFoundException(channelId);
        }

        var isOwner = await _groupAccessChecker.IsGroupOwnerAsync(channel.GroupId, userId, cancellationToken);

        if (!isOwner)
        {
            throw new UnauthorizedAccessException("Only group owner can delete audio channels");
        }

        if (channel.JanusRoomId.HasValue)
        {
            try
            {
                await _janusGatewayClient.DeleteRoomAsync(channel.JanusRoomId.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Janus room {RoomId} for channel {ChannelId}", channel.JanusRoomId.Value, channelId);
            }
        }

        channel.MarkForDeletion();

        await _channelRepository.DeleteAsync(channel, cancellationToken);
        await _channelRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audio channel {ChannelId} deleted by user {UserId}", channelId, userId);
    }

    private static long GetJanusRoomId(string channelId)
    {
        var hash = channelId.GetHashCode();
        return Math.Abs((long)hash);
    }

    private static AudioChannelDto MapToAudioChannelDto(AudioChannel channel)
    {
        return new AudioChannelDto
        {
            Id = channel.Id,
            GroupId = channel.GroupId,
            Name = channel.Name,
            JanusRoomId = channel.JanusRoomId,
            CreatedAt = channel.CreatedAt
        };
    }
}
