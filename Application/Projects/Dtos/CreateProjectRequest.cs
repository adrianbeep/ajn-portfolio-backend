namespace Application.Projects.Dtos
{
    public sealed record CreateProjectRequest(
        string Title,
        string Slug,
        string UrlThumbnail,
        string Description,
        string Content);
}
