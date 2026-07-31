using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Contributions.Dtos;
using MediatR;

namespace Application.Contributions.Queries.GetContributionBySlug;

public class GetContributionBySlugQueryHandler
    : IRequestHandler<GetContributionBySlugQuery, Result<ContributionDetailDto>>
{
    private readonly IContributionRepository _repository;

    public GetContributionBySlugQueryHandler(
        IContributionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ContributionDetailDto>> Handle(
        GetContributionBySlugQuery query,
        CancellationToken ct = default)
    {
        var contribution = await _repository.GetBySlugAsync(query.Slug, ct);

        if (contribution is null)
            return Result<ContributionDetailDto>.Failure(
                new Error("ContributionNotFound",
                    $"Contribution with slug '{query.Slug}' was not found."));

        var dto = new ContributionDetailDto(
            contribution.Id,
            contribution.Title,
            contribution.Slug,
            contribution.UrlThumbnail,
            contribution.UrlPrContribution,
            contribution.Description,
            contribution.Content,
            contribution.State.ToString(),
            contribution.CreatedAt,
            contribution.UpdatedAt);

        return Result<ContributionDetailDto>.Success(dto);
    }
}
