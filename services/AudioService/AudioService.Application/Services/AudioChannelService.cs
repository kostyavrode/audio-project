using AudioService.Application.DTOs;
using AudioService.Domain.Entities;
using AudioService.Domain.Exceptions;
using AudioService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AudioService.Application.Services;

public class AudioChannelService : IAudioChannelService
{
    private readonly IAudioChannelRepository _channelRepository;
    private readonly IJanusGatewayClient _janusGatewayClient;
    private readonly IGroupAccessChecker _groupAccessChecker;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly ILogger<AudioChannelService> _logger;

    public AudioChannelService(
        IAudioChannelRepository channelRepository,
        IJanusGatewayClient janusGatewayClient,
        IGroupAccessChecker groupAccessChecker,
        IRabbitMQPublisher rabbitMQPublisher,
        ILogger<AudioChannelService> logger)
    {
        _channelRepository = channelRepository;
        _janusGatewayClient = janusGatewayClient;
        _groupAccessChecker = groupAccessChecker;
        _rabbitMQPublisher = rabbitMQPublisher;
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

        // Если у канала есть JanusRoomId, проверяем существование комнаты
        if (channel.JanusRoomId.HasValue)
        {
            try
            {
                var roomInfo = await _janusGatewayClient.GetRoomInfoAsync(channel.JanusRoomId.Value, cancellationToken);
                if (roomInfo == null)
                {
                    // Комната не существует, пытаемся пересоздать
                    _logger.LogWarning("Janus room {RoomId} for channel {ChannelId} does not exist, attempting to recreate", channel.JanusRoomId.Value, channelId);
                    try
                    {
                        await _janusGatewayClient.CreateRoomAsync(channel.JanusRoomId.Value, channel.Name, cancellationToken);
                        _logger.LogInformation("Successfully recreated Janus room {RoomId} for channel {ChannelId}", channel.JanusRoomId.Value, channelId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to recreate Janus room {RoomId} for channel {ChannelId}", channel.JanusRoomId.Value, channelId);
                        // Не выбрасываем исключение, просто логируем
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not verify Janus room {RoomId} for channel {ChannelId}, assuming it exists", channel.JanusRoomId.Value, channelId);
                // Не выбрасываем исключение, возможно комната существует, но временно недоступна
            }
        }

        return MapToAudioChannelDto(channel);
    }

    public async Task<IEnumerable<AudioChannelDto>> GetChannelsByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var channels = await _channelRepository.GetByGroupIdAsync(groupId, cancellationToken);

        // Проверяем и пересоздаем комнаты, если они не существуют
        foreach (var channel in channels)
        {
            if (channel.JanusRoomId.HasValue)
            {
                try
                {
                    var roomInfo = await _janusGatewayClient.GetRoomInfoAsync(channel.JanusRoomId.Value, cancellationToken);
                    if (roomInfo == null)
                    {
                        // Комната не существует, пытаемся пересоздать
                        _logger.LogWarning("Janus room {RoomId} for channel {ChannelId} does not exist, attempting to recreate", channel.JanusRoomId.Value, channel.Id);
                        try
                        {
                            await _janusGatewayClient.CreateRoomAsync(channel.JanusRoomId.Value, channel.Name, cancellationToken);
                            _logger.LogInformation("Successfully recreated Janus room {RoomId} for channel {ChannelId}", channel.JanusRoomId.Value, channel.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to recreate Janus room {RoomId} for channel {ChannelId}", channel.JanusRoomId.Value, channel.Id);
                            // Не выбрасываем исключение, просто логируем
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not verify Janus room {RoomId} for channel {ChannelId}, assuming it exists", channel.JanusRoomId.Value, channel.Id);
                    // Не выбрасываем исключение, возможно комната существует, но временно недоступна
                }
            }
        }

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

    public async Task<bool> RecreateJanusRoomAsync(string channelId, string userId, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel == null)
            throw new AudioChannelNotFoundException(channelId);

        var isMember = await _groupAccessChecker.IsGroupMemberAsync(channel.GroupId, userId, cancellationToken);

        if (!isMember)
            throw new UnauthorizedAccessException("Only group members can recreate Janus room");

        if (!channel.JanusRoomId.HasValue)
        {
            var janusRoomId = GetJanusRoomId(channelId);
            channel.SetJanusRoomId(janusRoomId);
            await _channelRepository.UpdateAsync(channel, cancellationToken);
            await _channelRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Generated JanusRoomId {RoomId} for channel {ChannelId}", janusRoomId, channelId);
        }

        try
        {
            try
            {
                await _janusGatewayClient.DeleteRoomAsync(channel.JanusRoomId.Value, cancellationToken);
            }
            catch { }

            await _janusGatewayClient.CreateRoomAsync(channel.JanusRoomId.Value, channel.Name, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recreate Janus room {RoomId} for channel {ChannelId}", channel.JanusRoomId.Value, channelId);
            throw;
        }
    }

    public async Task<IEnumerable<AudioParticipantDto>> GetChannelParticipantsAsync(string channelId, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel == null)
            throw new AudioChannelNotFoundException(channelId);

        if (!channel.JanusRoomId.HasValue)
            return Enumerable.Empty<AudioParticipantDto>();

        var participants = await _janusGatewayClient.GetRoomParticipantsAsync(channel.JanusRoomId.Value, cancellationToken);

        return participants.Select(p => new AudioParticipantDto
        {
            Id = p.Id,
            DisplayName = p.Display,
            IsMuted = p.Muted
        });
    }

    public async Task SetParticipantVolumeAsync(string channelId, long participantId, int volume, string userId, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel == null)
            throw new AudioChannelNotFoundException(channelId);

        var isMember = await _groupAccessChecker.IsGroupMemberAsync(channel.GroupId, userId, cancellationToken);

        if (!isMember)
            throw new UnauthorizedAccessException("Only group members can set participant volume");

        if (!channel.JanusRoomId.HasValue)
            throw new InvalidOperationException("Channel does not have a Janus room");

        if (volume < 0 || volume > 200)
            throw new DomainException("Volume must be between 0 and 200");

        await _janusGatewayClient.SetParticipantVolumeAsync(channel.JanusRoomId.Value, participantId, volume, cancellationToken);

        _logger.LogInformation("Set volume {Volume} for participant {ParticipantId} in channel {ChannelId} by user {UserId}", volume, participantId, channelId, userId);
    }

    public async Task RegisterParticipantJoinedAsync(string channelId, string userId, RegisterParticipantDto participantDto, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel == null)
            throw new AudioChannelNotFoundException(channelId);

        var isMember = await _groupAccessChecker.IsGroupMemberAsync(channel.GroupId, userId, cancellationToken);

        if (!isMember)
            throw new UnauthorizedAccessException("Only group members can register participant");

        var eventData = new
        {
            groupId = channel.GroupId,
            channelId = channelId,
            userId = userId,
            displayName = participantDto.DisplayName,
            participantId = participantDto.ParticipantId
        };

        var message = JsonSerializer.Serialize(eventData);
        await _rabbitMQPublisher.PublishAsync("audio-events", "AudioParticipantJoined", message, cancellationToken);

        _logger.LogInformation("Participant joined event published: User {UserId} joined channel {ChannelId} in group {GroupId}", 
            userId, channelId, channel.GroupId);
    }

    public async Task RegisterParticipantLeftAsync(string channelId, string userId, long participantId, CancellationToken cancellationToken = default)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken);

        if (channel == null)
            throw new AudioChannelNotFoundException(channelId);

        var isMember = await _groupAccessChecker.IsGroupMemberAsync(channel.GroupId, userId, cancellationToken);

        if (!isMember)
            throw new UnauthorizedAccessException("Only group members can register participant");

        var eventData = new
        {
            groupId = channel.GroupId,
            channelId = channelId,
            userId = userId,
            participantId = participantId
        };

        var message = JsonSerializer.Serialize(eventData);
        await _rabbitMQPublisher.PublishAsync("audio-events", "AudioParticipantLeft", message, cancellationToken);

        _logger.LogInformation("Participant left event published: User {UserId} left channel {ChannelId} in group {GroupId}", 
            userId, channelId, channel.GroupId);
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
