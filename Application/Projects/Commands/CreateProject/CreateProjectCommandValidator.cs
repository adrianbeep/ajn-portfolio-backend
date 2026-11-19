using FluentValidation;
using Application.Projects.Commands.CreateProject;
using Domain.Helpers;
using Domain.ValueObjects;

namespace Application.Projects.Commands.CreateProject;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(Helpers.Messages.TitleRequired)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage(Helpers.Messages.SlugRequired)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase letters, numbers, and hyphens only.")
            .MaximumLength(100).WithMessage("Slug must not exceed 100 characters.");

        RuleFor(x => x.UrlThumbnail)
            .NotEmpty().WithMessage("Thumbnail URL is required.")
            .MaximumLength(2000).WithMessage("Thumbnail URL must not exceed 2000 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(50000).WithMessage("Content must not exceed 50000 characters.");

        // Las reglas de las tecnologias viven en el value object: el validador
        // no las copia, las consulta. El dominio dice que viola y con que
        // indice, asi que un dato invalido es un 400 y nunca una excepcion de
        // dominio, y las dos caminos dicen exactamente lo mismo.
        RuleFor(x => x.Technologies)
            .Must((_, technologies, context) =>
            {
                foreach (var violation in TechnologySet.Validate(technologies))
                {
                    var propertyName = violation.Index is null
                        ? context.PropertyPath
                        : $"{context.PropertyPath}[{violation.Index}]";

                    context.AddFailure(propertyName, violation.Message);
                }

                return true;
            });
    }
}
