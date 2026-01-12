using FluentValidation;
using ChatService.Application.DTOs;

namespace ChatService.Application.Validators;

public class GetMessagesDtoValidator : AbstractValidator<GetMessagesDto>
{
    public GetMessagesDtoValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required");
        
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");
        
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }
}
