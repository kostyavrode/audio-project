using AuthService.Application.DTOs;
using FluentValidation;
using FluentValidation.Validators;

namespace AuthService.Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{

    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email required")
            .EmailAddress().WithMessage("Email should be valid")
            .MaximumLength(254).WithMessage("Email maximum length exceeded");
        
        RuleFor(x => x.NickName)
            .NotEmpty().WithMessage("NickName required")
            .MinimumLength(1).WithMessage("NickName minimum length exceeded")
            .MaximumLength(254).WithMessage("NickName maximum length exceeded");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password required")
            .MinimumLength(6).WithMessage("Password minimum 6 length exceeded")
            .MaximumLength(100).WithMessage("Password maximum 100 length exceeded");
        
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("ConfirmPassword required")
            .Equal(x => x.Password).WithMessage("ConfirmPassword should match");
    }
}