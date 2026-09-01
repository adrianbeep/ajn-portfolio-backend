using FluentValidation;
using Application.Contributions.Commands.DeleteContribution;

namespace Application.Contributions.Commands.DeleteContribution;

public class DeleteContributionCommandValidator : AbstractValidator<DeleteContributionCommand>
{
    public DeleteContributionCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.");
    }
}
