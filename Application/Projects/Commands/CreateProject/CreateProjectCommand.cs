using Application.Abstractions.Common;
using MediatR;

namespace Application.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Title,
    string Slug,
    string UrlThumbnail,
    string Description,
    string Content) : IRequest<Result<Guid>>;
