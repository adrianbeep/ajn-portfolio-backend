using Application.Abstractions.Common;
using Application.Contributions.Dtos;
using MediatR;

namespace Application.Contributions.Queries.GetContributions;

public sealed record GetContributionsQuery(
    int Page,
    int PageSize) : IRequest<Result<ContributionListDto>>;
