using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Application.Stickers;

namespace ChatService.Api.Controllers;

// Под /api/Messages, потому что nginx пробрасывает в ChatService именно этот префикс.
// Литеральный сегмент "stickers" приоритетнее, чем {groupId} в MessagesController.
[ApiController]
[Route("api/Messages/stickers")]
[AllowAnonymous]
public class StickersController : ControllerBase
{
    private readonly IStickerCatalog _stickerCatalog;

    public StickersController(IStickerCatalog stickerCatalog)
    {
        _stickerCatalog = stickerCatalog ?? throw new ArgumentNullException(nameof(stickerCatalog));
    }

    [HttpGet]
    [ProducesResponseType(typeof(StickerCatalogDto), StatusCodes.Status200OK)]
    public ActionResult<StickerCatalogDto> GetCatalog()
    {
        // Каталог меняется редко; короткий кеш, чтобы новый пак появился у всех за несколько минут
        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(_stickerCatalog.GetCatalog());
    }
}
