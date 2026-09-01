using FluentValidation;
using Application.Contributions.Commands.RestoreContribution;

namespace Application.Contributions.Commands.RestoreContribution;

public class RestoreContributionCommandValidator : AbstractValidator<RestoreContributionCommand>
{
    public RestoreContributionCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.");
    }
}
