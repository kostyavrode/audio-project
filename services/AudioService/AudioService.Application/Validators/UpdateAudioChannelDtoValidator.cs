using FluentValidation;
using AudioService.Application.DTOs;

namespace AudioService.Application.Validators;

public class UpdateAudioChannelDtoValidator : AbstractValidator<UpdateAudioChannelDto>
{
    public UpdateAudioChannelDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Channel name cannot be empty")
            .When(x => x.Name != null)
            .MaximumLength(100).WithMessage("Channel name cannot exceed 100 characters")
            .When(x => x.Name != null);

        RuleFor(x => x)
            .Must(x => x.Name != null)
            .WithMessage("At least one field must be provided");
    }
}
