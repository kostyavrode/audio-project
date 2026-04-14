using Prometheus;

namespace Common.Monitoring;

/// <summary>
/// Tracks SignalR connections per hub for Prometheus (distinct users and open connections).
/// </summary>
public sealed class SignalRPresenceMetrics
{
    private const string HubLabel = "hub";

    private static readonly Gauge DistinctUsers = Metrics.CreateGauge(
        "audio_signalr_distinct_users",
        "Authenticated users with at least one open SignalR connection on this instance.",
        new GaugeConfiguration { LabelNames = new[] { HubLabel } });

    private static readonly Gauge OpenConnections = Metrics.CreateGauge(
        "audio_signalr_open_connections",
        "Number of open SignalR connections on this instance.",
        new GaugeConfiguration { LabelNames = new[] { HubLabel } });

    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, int>> _connectionsByHubUser = new();

    public void OnConnected(string hub, string userId)
    {
        lock (_gate)
        {
            if (!_connectionsByHubUser.TryGetValue(hub, out var byUser))
            {
                byUser = new Dictionary<string, int>(StringComparer.Ordinal);
                _connectionsByHubUser[hub] = byUser;
            }

            byUser.TryGetValue(userId, out var n);
            if (n == 0)
                DistinctUsers.WithLabels(hub).Inc();

            byUser[userId] = n + 1;
            OpenConnections.WithLabels(hub).Inc();
        }
    }

    public void OnDisconnected(string hub, string userId)
    {
        lock (_gate)
        {
            if (!_connectionsByHubUser.TryGetValue(hub, out var byUser))
                return;

            if (!byUser.TryGetValue(userId, out var n) || n < 1)
                return;

            if (n == 1)
            {
                byUser.Remove(userId);
                if (byUser.Count == 0)
                    _connectionsByHubUser.Remove(hub);
                DistinctUsers.WithLabels(hub).Dec();
            }
            else
            {
                byUser[userId] = n - 1;
            }

            OpenConnections.WithLabels(hub).Dec();
        }
    }

    public int GetOpenConnections(string hub)
    {
        lock (_gate)
        {
            if (!_connectionsByHubUser.TryGetValue(hub, out var byUser))
                return 0;
            return byUser.Values.Sum();
        }
    }

    public int GetDistinctUsers(string hub)
    {
        lock (_gate)
        {
            if (!_connectionsByHubUser.TryGetValue(hub, out var byUser))
                return 0;
            return byUser.Count;
        }
    }
}
