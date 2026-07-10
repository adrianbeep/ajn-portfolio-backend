using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using MediatR;

namespace Application.Projects.Commands.RestoreProject;

public class RestoreProjectCommandHandler : IRequestHandler<RestoreProjectCommand, Result>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _uow;

    public RestoreProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<Result> Handle(
        RestoreProjectCommand command,
        CancellationToken ct = default)
    {
        var project = await _repository.GetBySlugAsync(command.Slug, ct);

        if (project is null)
            return Result.Failure(
            new Error(
                "ProjectNotFound", $"Project with Slug '{command.Slug}' was not found."));

        var restoreResult = await _repository.RestoreAsync(project, ct);
        
        if (!restoreResult.IsSuccess)
            return Result.Failure(restoreResult.Error);
        
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
