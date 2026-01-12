using FluentValidation;
using GroupsService.Application.DTOs;

namespace GroupsService.Application.Validators;

public class JoinGroupDtoValidator : AbstractValidator<JoinGroupDto>
{
    public JoinGroupDtoValidator()
    {
        RuleFor(x => x.Password)
            .MinimumLength(3).WithMessage("Password must be at least 3 characters")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}
