using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Projects.Dtos;
using MediatR;

namespace Application.Projects.Queries.GetAllProjectsForAdmin;

public class GetAllProjectsForAdminQueryHandler
    : IRequestHandler<GetAllProjectsForAdminQuery, Result<IReadOnlyList<ProjectAdminListDto>>>
{
    private readonly IProjectRepository _repository;

    public GetAllProjectsForAdminQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ProjectAdminListDto>>> Handle(
        GetAllProjectsForAdminQuery query,
        CancellationToken ct)
    {
        var projects = await _repository.GetAllAndDeletedAsync(ct);

        var items = projects
            .Select(p => new ProjectAdminListDto(
                p.Id,
                p.Slug,
                p.Title,
                p.CreatedAt,
                p.DeletedAt))
            .ToList();

        return Result<IReadOnlyList<ProjectAdminListDto>>.Success(items);
    }
}