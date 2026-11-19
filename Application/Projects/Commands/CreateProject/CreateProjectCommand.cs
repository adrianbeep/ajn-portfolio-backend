using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Projects.Commands.CreateProject;

[AdminOnly]
public sealed record CreateProjectCommand(
    string Title,
    string Slug,
    string UrlThumbnail,
    string Description,
    string Content,
    IReadOnlyList<string>? Technologies = null) : IRequest<Result<Guid>>;
