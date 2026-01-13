using AuthService.Application.DTOs;
using FluentValidation;

namespace AuthService.Application.Validators;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.NickName)
            .NotEmpty().WithMessage("NickName is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}