using System.Text.Json;
using ChatService.Application.Stickers;
using Microsoft.Extensions.Logging;

namespace ChatService.Infrastructure.Stickers;

public class StickerCatalogSettings
{
    public const string SectionName = "Stickers";

    // Абсолютный путь или путь от ContentRoot сервиса
    public string CatalogPath { get; set; } = "Stickers/catalog.json";
}

/// <summary>
/// Каталог стикеров из JSON-файла. Файл перечитывается, если изменился (проверка раз в 30 секунд),
/// так что новый пак подхватывается без перезапуска. Битый или пропавший файл не ломает чат:
/// остаётся последний успешно загруженный каталог (при старте - пустой).
/// </summary>
public sealed class JsonStickerCatalog : IStickerCatalog
{
    private static readonly TimeSpan RecheckInterval = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _path;
    private readonly ILogger<JsonStickerCatalog> _logger;
    private readonly object _lock = new();
    // Пустой снимок до первой успешной загрузки
    private Snapshot _snapshot = new(new StickerCatalogDto(), new HashSet<int>());
    private DateTime? _loadedWriteTimeUtc;
    private DateTime _nextCheckUtc = DateTime.MinValue;
    private bool _missingLogged;

    public JsonStickerCatalog(string path, ILogger<JsonStickerCatalog> logger)
    {
        _path = path;
        _logger = logger;
    }

    public StickerCatalogDto GetCatalog() => Current().Catalog;

    public bool Exists(int stickerId) => Current().Ids.Contains(stickerId);

    private Snapshot Current()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now >= _nextCheckUtc)
            {
                _nextCheckUtc = now + RecheckInterval;
                ReloadIfChanged();
            }
            return _snapshot;
        }
    }

    private void ReloadIfChanged()
    {
        if (!File.Exists(_path))
        {
            if (!_missingLogged)
            {
                _logger.LogWarning("Sticker catalog {Path} not found, stickers are unavailable", _path);
                _missingLogged = true;
            }
            return;
        }
        _missingLogged = false;

        var writeTime = File.GetLastWriteTimeUtc(_path);
        if (writeTime == _loadedWriteTimeUtc)
        {
            return;
        }
        // Даже при ошибке запоминаем время, чтобы не писать одну и ту же ошибку каждые 30 секунд
        _loadedWriteTimeUtc = writeTime;

        try
        {
            var catalog = JsonSerializer.Deserialize<StickerCatalogDto>(File.ReadAllText(_path), JsonOptions)
                ?? throw new InvalidDataException("Catalog is empty");
            _snapshot = Normalize(catalog);
            _logger.LogInformation("Sticker catalog v{Version} loaded: {Packs} packs, {Stickers} stickers",
                _snapshot.Catalog.Version, _snapshot.Catalog.Packs.Count, _snapshot.Ids.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sticker catalog {Path}, keeping the previous one", _path);
        }
    }

    // Отбрасываем некорректные записи, а не весь каталог: одна опечатка не должна выключать все стикеры
    private Snapshot Normalize(StickerCatalogDto catalog)
    {
        var ids = new HashSet<int>();
        var packs = new List<StickerPackDto>();

        foreach (var pack in catalog.Packs ?? new List<StickerPackDto>())
        {
            if (pack == null || string.IsNullOrWhiteSpace(pack.Id))
            {
                _logger.LogWarning("Sticker pack without id skipped");
                continue;
            }

            var seen = new HashSet<int>();
            var stickers = new List<StickerDto>();
            foreach (var sticker in pack.Stickers ?? new List<StickerDto>())
            {
                if (sticker == null || sticker.Id <= 0 || !seen.Add(sticker.Id))
                {
                    _logger.LogWarning("Invalid or duplicate sticker {StickerId} skipped in pack {PackId}", sticker?.Id, pack.Id);
                    continue;
                }
                stickers.Add(new StickerDto { Id = sticker.Id, Emoji = sticker.Emoji ?? string.Empty });
            }

            if (stickers.Count == 0)
            {
                _logger.LogWarning("Sticker pack {PackId} has no stickers, skipped", pack.Id);
                continue;
            }

            ids.UnionWith(seen);
            packs.Add(new StickerPackDto
            {
                Id = pack.Id,
                Title = string.IsNullOrWhiteSpace(pack.Title) ? pack.Id : pack.Title,
                IconStickerId = seen.Contains(pack.IconStickerId) ? pack.IconStickerId : stickers[0].Id,
                Stickers = stickers
            });
        }

        return new Snapshot(new StickerCatalogDto { Version = catalog.Version, Packs = packs }, ids);
    }

    private sealed record Snapshot(StickerCatalogDto Catalog, HashSet<int> Ids);
}
