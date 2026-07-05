namespace Application.Projects.Dtos
{
    public sealed record ProjectSummaryDto(
        Guid Id,
        string Slug,
        string Title,
        string UrlThumbnail,
        string Description);
}
