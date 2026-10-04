using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;

namespace NotificationService.Api.Security;

/// <summary>
/// Проверяет членство пользователя в группе у GroupsService - первоисточника данных о группах.
/// Запрос идёт с токеном самого пользователя, поэтому сервис не может узнать больше, чем сам пользователь.
/// </summary>
public sealed class GroupMembershipVerifier
{
    private static readonly TimeSpan PositiveCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly string _groupsServiceUrl;
    private readonly ILogger<GroupMembershipVerifier> _logger;

    public GroupMembershipVerifier(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<GroupMembershipVerifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _groupsServiceUrl = (configuration.GetValue<string>("GroupsServiceUrl") ?? "http://groups-service:8080").TrimEnd('/');
        _logger = logger;
    }

    public async Task<bool> IsMemberAsync(string groupId, string userId, string? accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var cacheKey = $"group-member:{groupId}:{userId}";
        if (_cache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_groupsServiceUrl}/api/Groups/{Uri.EscapeDataString(groupId)}/members");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Кэшируем только положительный ответ и ненадолго: исключённый из группы
                // теряет доступ не позже чем через минуту
                _cache.Set(cacheKey, true, PositiveCacheTtl);
                return true;
            }

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Не смогли проверить - доступ не даём
            _logger.LogWarning(ex, "Failed to verify membership of user {UserId} in group {GroupId}", userId, groupId);
            return false;
        }
    }

    public static string? ExtractToken(HttpRequest request)
    {
        var token = request.Cookies["access_token"];
        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        var header = request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return header["Bearer ".Length..].Trim();
        }

        return null;
    }
}
