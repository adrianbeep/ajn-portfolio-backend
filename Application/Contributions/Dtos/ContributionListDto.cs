namespace Application.Contributions.Dtos;

public sealed record ContributionListDto(
    IReadOnlyList<ContributionSummaryDto> Items,
    int Total,
    int Page,
    int PageSize);
