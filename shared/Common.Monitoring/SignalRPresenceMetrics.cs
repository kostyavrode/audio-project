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

    private static readonly Gauge GroupViewers = Metrics.CreateGauge(
        "audio_site_group_viewers",
        "Distinct users who currently have the page of this group open.",
        new GaugeConfiguration { LabelNames = new[] { "group_id" } });

    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, int>> _connectionsByHubUser = new();

    // connectionId -> (userId, groups this connection has joined)
    private readonly Dictionary<string, (string UserId, HashSet<string> Groups)> _groupsByConnection = new(StringComparer.Ordinal);
    // groupId -> userId -> number of that user's connections in the group
    private readonly Dictionary<string, Dictionary<string, int>> _usersByGroup = new(StringComparer.Ordinal);

    public void OnGroupJoined(string connectionId, string userId, string groupId)
    {
        lock (_gate)
        {
            if (!_groupsByConnection.TryGetValue(connectionId, out var entry))
            {
                entry = (userId, new HashSet<string>(StringComparer.Ordinal));
                _groupsByConnection[connectionId] = entry;
            }

            if (!entry.Groups.Add(groupId))
                return;

            if (!_usersByGroup.TryGetValue(groupId, out var byUser))
            {
                byUser = new Dictionary<string, int>(StringComparer.Ordinal);
                _usersByGroup[groupId] = byUser;
            }

            byUser.TryGetValue(entry.UserId, out var n);
            byUser[entry.UserId] = n + 1;
            GroupViewers.WithLabels(groupId).Set(byUser.Count);
        }
    }

    public void OnGroupLeft(string connectionId, string groupId)
    {
        lock (_gate)
        {
            if (!_groupsByConnection.TryGetValue(connectionId, out var entry) || !entry.Groups.Remove(groupId))
                return;

            RemoveUserFromGroup(groupId, entry.UserId);
            if (entry.Groups.Count == 0)
                _groupsByConnection.Remove(connectionId);
        }
    }

    /// <summary>
    /// Must be called when a connection closes: SignalR drops its group memberships without notifying the hub.
    /// </summary>
    public void OnConnectionClosed(string connectionId)
    {
        lock (_gate)
        {
            if (!_groupsByConnection.Remove(connectionId, out var entry))
                return;

            foreach (var groupId in entry.Groups)
                RemoveUserFromGroup(groupId, entry.UserId);
        }
    }

    private void RemoveUserFromGroup(string groupId, string userId)
    {
        if (!_usersByGroup.TryGetValue(groupId, out var byUser) || !byUser.TryGetValue(userId, out var n))
            return;

        if (n <= 1)
            byUser.Remove(userId);
        else
            byUser[userId] = n - 1;

        if (byUser.Count == 0)
        {
            _usersByGroup.Remove(groupId);
            GroupViewers.RemoveLabelled(groupId);
        }
        else
        {
            GroupViewers.WithLabels(groupId).Set(byUser.Count);
        }
    }

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
