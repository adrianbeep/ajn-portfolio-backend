namespace Application.Projects.Dtos
{
    public sealed record ProjectDetailDto(
        Guid Id,
        string Title,
        string Slug,
        string UrlThumbnail,
        string Description,
        IReadOnlyList<string> Technologies,
        string Content,
        string State,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
}
