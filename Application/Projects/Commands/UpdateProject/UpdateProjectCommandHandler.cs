using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<Result> Handle(
        UpdateProjectCommand command,
        CancellationToken ct = default)
    {
        var project = await _repository.GetBySlugAsync(command.Slug, ct);

        if (project is null)
            return Result.Failure(
                new Error(
                    "ProjectNotFound", $"Project with Slug '{command.Slug}' was not found."));
        
        project.Update(
            command.Title,
            command.Description,
            command.UrlThumbnail,
            command.Content);
        
        var updateResult = await _repository.UpdateAsync(project, ct);
        
        if (!updateResult.IsSuccess)
            return Result.Failure(updateResult.Error);

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
