namespace ChatService.Application.Stickers;

/// <summary>
/// Каталог стикеров (паки и ID). Картинки лежат на сайте как статика: stickers/{id}.webp.
/// </summary>
public interface IStickerCatalog
{
    StickerCatalogDto GetCatalog();

    /// <summary>Есть ли стикер в каталоге - отправлять можно только такие.</summary>
    bool Exists(int stickerId);
}

public class StickerCatalogDto
{
    // Растёт при каждом изменении каталога
    public int Version { get; set; }
    public List<StickerPackDto> Packs { get; set; } = new();
}

public class StickerPackDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    // Стикер, который показывается на вкладке пака
    public int IconStickerId { get; set; }
    public List<StickerDto> Stickers { get; set; } = new();
}

public class StickerDto
{
    // Глобальный и никогда не переиспользуемый: по нему строится адрес картинки и он хранится в сообщениях
    public int Id { get; set; }
    public string Emoji { get; set; } = string.Empty;
}
