using GroupsService.Application.DTOs;
using FluentValidation;

namespace GroupsService.Application.Validators;

public class CreateGroupDtoValidator : AbstractValidator<CreateGroupDto>
{
    public CreateGroupDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required")
            .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters");
        
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
        
        RuleFor(x => x.Password)
            .MinimumLength(3).WithMessage("Password must be at least 3 characters")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}
