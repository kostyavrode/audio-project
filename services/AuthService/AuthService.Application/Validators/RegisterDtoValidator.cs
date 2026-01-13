using AuthService.Application.DTOs;
using FluentValidation;

namespace AuthService.Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email should be valid")
            .MaximumLength(254).WithMessage("Email maximum length exceeded")
            .When(x => !string.IsNullOrEmpty(x.Email));
        
        RuleFor(x => x.NickName)
            .NotEmpty().WithMessage("NickName required")
            .MinimumLength(1).WithMessage("NickName minimum 1 character")
            .MaximumLength(30).WithMessage("NickName maximum 30 characters");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password required")
            .MinimumLength(6).WithMessage("Password minimum 6 characters")
            .MaximumLength(100).WithMessage("Password maximum 100 characters");
        
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("ConfirmPassword required")
            .Equal(x => x.Password).WithMessage("Passwords should match");
    }
}