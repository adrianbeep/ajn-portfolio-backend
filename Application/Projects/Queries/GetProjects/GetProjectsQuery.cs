using Application.Abstractions.Common;
using Application.Projects.Dtos;
using MediatR;

namespace Application.Projects.Queries.GetProjects;

public sealed record GetProjectsQuery(
    int Page,
    int PageSize) : IRequest<Result<ProjectListDto>>;
