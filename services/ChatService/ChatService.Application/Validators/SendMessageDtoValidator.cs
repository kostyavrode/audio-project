using FluentValidation;
using ChatService.Application.DTOs;

namespace ChatService.Application.Validators;

public class SendMessageDtoValidator : AbstractValidator<SendMessageDto>
{
    public SendMessageDtoValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required");
        
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(2000).WithMessage("Message content cannot exceed 2000 characters");
    }
}
