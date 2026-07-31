namespace Application.Contributions.Dtos;

public sealed record UpdateContributionRequest(
    Guid Id,
    string Title,
    string Slug,
    string UrlThumbnail,
    string UrlPrContribution,
    string Description,
    string Content);
