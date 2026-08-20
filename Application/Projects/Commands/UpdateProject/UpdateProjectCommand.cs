using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Projects.Commands.UpdateProject;

[AdminOnly]
public sealed record UpdateProjectCommand(
    string Slug,
    string Title,
    string Description,
    string UrlThumbnail,
    string Content) : IRequest<Result>;
