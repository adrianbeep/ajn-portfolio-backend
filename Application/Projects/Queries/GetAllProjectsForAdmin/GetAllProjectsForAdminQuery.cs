using Application.Abstractions.Common;
using Application.Projects.Dtos;
using MediatR;

namespace Application.Projects.Queries.GetAllProjectsForAdmin;

public sealed record GetAllProjectsForAdminQuery
    : IRequest<Result<IReadOnlyList<ProjectAdminListDto>>>;