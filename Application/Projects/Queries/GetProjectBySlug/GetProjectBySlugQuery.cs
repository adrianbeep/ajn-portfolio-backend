using Application.Abstractions.Common;
using Application.Projects.Dtos;
using MediatR;

namespace Application.Projects.Queries.GetProjectBySlug;

public sealed record GetProjectBySlugQuery(string Slug) : IRequest<Result<ProjectDetailDto>>;
