using FluentValidation;
using Application.Projects.Commands.UpdateProject;
using Domain.Helpers;
using Domain.ValueObjects;

namespace Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage(Helpers.Messages.SlugRequired);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(Helpers.Messages.TitleRequired)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.UrlThumbnail)
            .NotEmpty().WithMessage("Thumbnail URL is required.")
            .MaximumLength(2000).WithMessage("Thumbnail URL must not exceed 2000 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(50000).WithMessage("Content must not exceed 50000 characters.");

        // Las reglas de las tecnologias viven en el value object: el validador
        // las replica con las mismas constantes y la misma normalizacion para
        // que un dato invalido sea un 400 y nunca una excepcion de dominio.
        // Los mensajes son los del dominio: si las dos caminos dicen lo mismo,
        // una deriva entre ellos se ve como un 400 y no como un 500.
        RuleFor(x => x.Technologies)
            .Must(t => t is null || t.Count <= TechnologySet.MaxCount)
            .WithMessage(TechnologySet.Messages.TooManyItems);

        RuleForEach(x => x.Technologies)
            .Must(v => TechnologySet.Normalize(v).Length > 0)
            .WithMessage(TechnologySet.Messages.EmptyValue)
            .Must(v => TechnologySet.Normalize(v).Length <= TechnologySet.MaxLength)
            .WithMessage(TechnologySet.Messages.TooLong);

        RuleFor(x => x.Technologies)
            .Must(t => t is null || t.Select(TechnologySet.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).Count() == t.Count)
            .WithMessage(TechnologySet.Messages.Duplicated);
    }
}
