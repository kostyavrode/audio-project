using AudioService.Application.Services;
using AudioService.Domain.Interfaces;
using Prometheus;

namespace AudioService.Api.Monitoring;

/// <summary>
/// Периодически спрашивает у Janus, кто реально сидит в аудиокомнатах, и публикует это в Prometheus.
/// Источник правды - сам Janus, а не события "вошёл/вышел" от клиентов: клиент может закрыть вкладку
/// или потерять сеть, не отправив "вышел", и счётчик на событиях со временем разъезжается.
/// </summary>
public sealed class RoomOccupancyMetricsService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private static readonly string[] RoomLabels = { "group_id", "channel_id", "channel_name" };

    private static readonly Gauge RoomParticipants = Metrics.CreateGauge(
        "audio_room_participants",
        "Participants currently connected to an audio room (as reported by Janus).",
        new GaugeConfiguration { LabelNames = RoomLabels });

    private static readonly Gauge UsersInRooms = Metrics.CreateGauge(
        "audio_rooms_users",
        "Distinct users currently connected to any audio room.");

    private static readonly Gauge OccupiedRooms = Metrics.CreateGauge(
        "audio_rooms_occupied",
        "Audio rooms with at least one participant.");

    private static readonly Gauge TotalRooms = Metrics.CreateGauge(
        "audio_rooms_total",
        "Audio rooms that exist in the database.");

    private static readonly Gauge JanusUp = Metrics.CreateGauge(
        "audio_rooms_janus_up",
        "1 if the last room occupancy poll of Janus succeeded, 0 otherwise.");

    private static readonly Gauge LastSuccess = Metrics.CreateGauge(
        "audio_rooms_last_poll_success_timestamp_seconds",
        "Unix time of the last successful room occupancy poll.");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RoomOccupancyMetricsService> _logger;
    private HashSet<(string GroupId, string ChannelId, string Name)> _publishedRooms = new();
    private bool _lastPollFailed;

    public RoomOccupancyMetricsService(IServiceScopeFactory scopeFactory, ILogger<RoomOccupancyMetricsService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await PollAsync(stoppingToken);
                JanusUp.Set(1);
                LastSuccess.SetToCurrentTimeUtc();
                _lastPollFailed = false;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                JanusUp.Set(0);
                // Не знаем, кто в комнатах - обнуляем, чтобы в Grafana не висели устаревшие цифры
                ResetOccupancy();
                if (!_lastPollFailed)
                {
                    _logger.LogWarning(ex, "Room occupancy poll failed");
                }
                _lastPollFailed = true;
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAudioChannelRepository>();
        var janus = scope.ServiceProvider.GetRequiredService<IJanusGatewayClient>();

        var channels = await repository.GetAllAsync(cancellationToken);
        var roomIds = channels
            .Where(c => c.JanusRoomId.HasValue)
            .Select(c => c.JanusRoomId!.Value)
            .Distinct()
            .ToList();

        var participantsByRoom = await janus.GetParticipantsForRoomsAsync(roomIds, cancellationToken);

        var currentRooms = new HashSet<(string GroupId, string ChannelId, string Name)>();
        var users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var occupied = 0;

        foreach (var channel in channels)
        {
            var count = 0;
            if (channel.JanusRoomId.HasValue &&
                participantsByRoom.TryGetValue(channel.JanusRoomId.Value, out var participants))
            {
                // Один человек при переподключении может ненадолго числиться под двумя id - считаем по нику
                var names = participants
                    .Select(p => string.IsNullOrWhiteSpace(p.Display) ? p.Id.ToString() : p.Display.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                count = names.Count;
                users.UnionWith(names);
            }

            if (count > 0)
            {
                occupied++;
            }

            var key = (channel.GroupId, channel.Id, channel.Name);
            currentRooms.Add(key);
            RoomParticipants.WithLabels(key.GroupId, key.Id, key.Name).Set(count);
        }

        // Удалённые или переименованные комнаты не должны оставаться в метриках
        foreach (var stale in _publishedRooms.Where(r => !currentRooms.Contains(r)))
        {
            RoomParticipants.RemoveLabelled(stale.GroupId, stale.ChannelId, stale.Name);
        }
        _publishedRooms = currentRooms;

        UsersInRooms.Set(users.Count);
        OccupiedRooms.Set(occupied);
        TotalRooms.Set(channels.Count);
    }

    private void ResetOccupancy()
    {
        foreach (var room in _publishedRooms)
        {
            RoomParticipants.WithLabels(room.GroupId, room.ChannelId, room.Name).Set(0);
        }
        UsersInRooms.Set(0);
        OccupiedRooms.Set(0);
    }
}
