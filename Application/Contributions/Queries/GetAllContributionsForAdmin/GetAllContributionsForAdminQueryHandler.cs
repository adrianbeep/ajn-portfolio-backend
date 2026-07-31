using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Contributions.Dtos;
using MediatR;

namespace Application.Contributions.Queries.GetAllContributionsForAdmin;

public class GetAllContributionsForAdminQueryHandler
    : IRequestHandler<GetAllContributionsForAdminQuery, Result<IReadOnlyList<ContributionAdminListDto>>>
{
    private readonly IContributionRepository _repository;
    
    // TODO: add pagination when contribution count grows. See GetProjects pattern.
    public GetAllContributionsForAdminQueryHandler(IContributionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ContributionAdminListDto>>> Handle(
        GetAllContributionsForAdminQuery query,
        CancellationToken ct)
    {
        var contributions = await _repository.GetAllAndDeletedAsync(ct);

        var items = contributions
            .Select(c => new ContributionAdminListDto(
                c.Id,
                c.Slug,
                c.Title,
                c.CreatedAt,
                c.DeletedAt))
            .ToList();

        return Result<IReadOnlyList<ContributionAdminListDto>>.Success(items);
    }
}
