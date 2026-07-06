using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Projects.Dtos;
using Domain.Entities;

namespace Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateProjectCommandHandler(IProjectRepository repository, IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateProjectRequest query, 
        CancellationToken ct = default)
    {
        var project = await _repository.AddAsync(
            new Project(query.Title,
            query.Slug,
            query.UrlThumbnail,
            query.Description,
            query.Content));

        if (project is null)
            return Result<Guid>.Failure(
                new Error("ProjectNotCreated",
                $"Project with title '{query.Title}' was not created"));

        _uow.SaveChangesAsync(ct);
        return Result<project.Id>.Success(
            );
    }
}
