using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Projects.Dtos;
using System.ComponentModel.DataAnnotations;

namespace Application.Projects.Queries.GetProjects;

public class GetProjectsQueryHandler
{
    public readonly IProjectRepository _repository;

    public GetProjectsQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProjectListDto>> HandleAsync(
        GetProjectsQuery query,
        CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pagesize = query.PageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _     => query.PageSize
        };

        var projects = await _repository.GetAllNotDeletedAsync(ct);

        var total = projects.Count();

        var items = projects
            .Skip((page - 1) * pagesize)
            .Take(pagesize)
            .Select(p => new ProjectSummaryDto(
                p.Id, p.Slug, p.Title, p.UrlThumbnail, p.Description))
            .ToList();

        var dto = new ProjectListDto(items, total, page, pagesize);
        return Result<ProjectListDto>.Success(dto);
    }
}
