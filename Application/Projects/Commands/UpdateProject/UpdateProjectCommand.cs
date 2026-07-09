namespace Application.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    string Slug,
    string Title,
    string Description,
    string UrlThumbnail,
    string Content);
