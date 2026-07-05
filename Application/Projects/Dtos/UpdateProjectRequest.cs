namespace Application.Projects.Dtos
{
    public sealed record UpdateProjectRequest(
        Guid Id,
        string Title,
        string Slug,
        string UrlThumbnail,
        string Description,
        string Content);
}
