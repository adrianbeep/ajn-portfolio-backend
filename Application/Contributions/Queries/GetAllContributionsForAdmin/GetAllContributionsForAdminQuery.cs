using Application.Abstractions.Common;
using Application.Contributions.Dtos;
using MediatR;

namespace Application.Contributions.Queries.GetAllContributionsForAdmin;

public sealed record GetAllContributionsForAdminQuery
    : IRequest<Result<IReadOnlyList<ContributionAdminListDto>>>;
