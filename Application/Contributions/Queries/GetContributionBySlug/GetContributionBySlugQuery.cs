using Application.Abstractions.Common;
using Application.Contributions.Dtos;
using MediatR;

namespace Application.Contributions.Queries.GetContributionBySlug;

public sealed record GetContributionBySlugQuery(string Slug) 
    : IRequest<Result<ContributionDetailDto>>;
