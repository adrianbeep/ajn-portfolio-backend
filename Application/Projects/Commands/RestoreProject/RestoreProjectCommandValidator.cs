using FluentValidation;
using Application.Projects.Commands.RestoreProject;

namespace Application.Projects.Commands.RestoreProject;

public class RestoreProjectCommandValidator : AbstractValidator<RestoreProjectCommand>
{
    public RestoreProjectCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.");
    }
}
