using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Projects.Dtos;
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Application.Projects.Queries.GetProjects;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, Result<ProjectListDto>>
{
    public readonly IProjectRepository _repository;

    public GetProjectsQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProjectListDto>> Handle(
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

        var total = await _repository.CountNotDeletedAsync(ct);
        var projects = await _repository.GetPagedNotDeletedAsync(page, pagesize, ct);

        var items = projects
            .Select(p => new ProjectSummaryDto(
                p.Id, p.Slug, p.Title, p.UrlThumbnail, p.Description,
                p.Technologies,
                p.State.ToString(), p.CreatedAt))
            .ToList();

        var dto = new ProjectListDto(items, total, page, pagesize);
        return Result<ProjectListDto>.Success(dto);
    }
}
