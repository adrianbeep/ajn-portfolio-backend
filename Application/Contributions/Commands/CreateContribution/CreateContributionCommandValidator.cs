using FluentValidation;
using Domain.Helpers;
using Application.Contributions.Commands.CreateContribution;

namespace Application.Contributions.Commands.CreateContribution;

public class CreateContributionCommandValidator : AbstractValidator<CreateContributionCommand>
{
    public CreateContributionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(Helpers.Messages.TitleRequired)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase letters, numbers, and hyphens only.")
            .MaximumLength(100).WithMessage("Slug must not exceed 100 characters.");

        RuleFor(x => x.UrlThumbnail)
            .NotEmpty().WithMessage("Thumbnail URL is required.")
            .MaximumLength(2000).WithMessage("Thumbnail URL must not exceed 2000 characters.");

        RuleFor(x => x.UrlPrContribution)
            .NotEmpty().WithMessage("PR URL is required.")
            .MaximumLength(2000).WithMessage("PR URL must not exceed 2000 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(50000).WithMessage("Content must not exceed 50000 characters.");
    }
}
