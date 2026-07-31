using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Contributions.Dtos;
using MediatR;

namespace Application.Contributions.Queries.GetContributions;

public class GetContributionsQueryHandler 
    : IRequestHandler<GetContributionsQuery, Result<ContributionListDto>>
{
    public readonly IContributionRepository _repository;

    public GetContributionsQueryHandler(
        IContributionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ContributionListDto>> Handle(
        GetContributionsQuery query,
        CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pagesize = query.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => query.PageSize
        };

        var total = await _repository.CountNotDeletedAsync(ct);
        var contributions = await _repository.GetPagedNotDeletedAsync(page, pagesize, ct);

        var items = contributions
            .Select(c => new ContributionSummaryDto(
                c.Id, c.Slug, c.Title, c.UrlThumbnail, c.UrlPrContribution, c.Description))
            .ToList();

        var dto = new ContributionListDto(items, total, page, pagesize);
        return Result<ContributionListDto>.Success(dto);
    }
}
