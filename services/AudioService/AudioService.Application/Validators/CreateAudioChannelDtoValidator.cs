using FluentValidation;
using AudioService.Application.DTOs;

namespace AudioService.Application.Validators;

public class CreateAudioChannelDtoValidator : AbstractValidator<CreateAudioChannelDto>
{
    public CreateAudioChannelDtoValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("Group ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Channel name is required")
            .MaximumLength(100).WithMessage("Channel name cannot exceed 100 characters");
    }
}
