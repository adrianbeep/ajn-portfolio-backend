namespace Application.Contributions.Dtos;

public sealed record CreateContributionRequest(
    string Title,
    string Slug,
    string UrlThumbnail,
    string UrlPrContribution,
    string Description,
    string Content);
