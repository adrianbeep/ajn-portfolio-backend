namespace Application.Contributions.Dtos;

public sealed record ContributionDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string UrlThumbnail,
    string UrlPrContribution,
    string Description,
    string Content,
    string State,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
