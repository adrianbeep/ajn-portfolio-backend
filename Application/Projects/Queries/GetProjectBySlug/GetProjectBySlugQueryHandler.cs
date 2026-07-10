using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Projects.Dtos;
using MediatR;

namespace Application.Projects.Queries.GetProjectBySlug;

public class GetProjectBySlugQueryHandler : IRequestHandler<GetProjectBySlugQuery, Result<ProjectDetailDto>>
{
    private readonly IProjectRepository _repository;

    public GetProjectBySlugQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProjectDetailDto>> Handle(
        GetProjectBySlugQuery query,
        CancellationToken ct = default)
    {
        var project = await _repository.GetBySlugAsync(query.Slug, ct);

        if (project is null)
            return Result<ProjectDetailDto>.Failure(
                new Error("ProjectNotFound",
                $"Project with slug '{query.Slug}' was not found."));

        var dto = new ProjectDetailDto(
            project.Id,
            project.Title,
            project.Slug,
            project.UrlThumbnail,
            project.Description,
            project.Content,
            project.State.ToString(),
            project.CreatedAt,
            project.UpdatedAt);

        return Result<ProjectDetailDto>.Success(dto);
    }
}
