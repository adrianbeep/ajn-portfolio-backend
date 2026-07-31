namespace Application.Contributions.Dtos;

public sealed record ContributionSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string UrlThumbnail,
    string UrlPrContribution,
    string Description);
