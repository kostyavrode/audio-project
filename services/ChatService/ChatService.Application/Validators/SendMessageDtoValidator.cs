using FluentValidation;
using ChatService.Application.DTOs;

namespace ChatService.Application.Validators;

public class SendMessageDtoValidator : AbstractValidator<SendMessageDto>
{
    public SendMessageDtoValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required");

        // Сообщение - либо текст, либо стикер
        When(x => x.StickerId == null, () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Message content is required")
                .MaximumLength(2000).WithMessage("Message content cannot exceed 2000 characters");
        }).Otherwise(() =>
        {
            RuleFor(x => x.StickerId)
                .GreaterThan(0).WithMessage("Sticker ID must be positive");
            RuleFor(x => x.Content)
                .Empty().WithMessage("Sticker message cannot contain text");
        });
    }
}
