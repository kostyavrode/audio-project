namespace GroupsService.Application.DTOs;

public class JoinGroupDto
{
    public string? Password { get; set; }

    // Ключ из ссылки-приглашения: с ним пароль группы не нужен
    public string? InviteCode { get; set; }
}
