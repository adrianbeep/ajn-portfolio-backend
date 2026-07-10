using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using MediatR;

namespace Application.Projects.Commands.DeleteProject;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Result>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<Result> Handle(
        DeleteProjectCommand command,
        CancellationToken ct = default)
    {
        var project = await _repository.GetBySlugAsync(command.Slug, ct);

        if (project is null)
            return Result.Failure(
            new Error(
                "ProjectNotFound", $"Project with Slug '{command.Slug}' was not found."));

        var softDeleteResult = await _repository.SoftDeleteAsync(project, ct);

        if (!softDeleteResult.IsSuccess)
            return Result.Failure(softDeleteResult.Error);
        
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
