using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<Guid>>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateProjectCommandHandler(IProjectRepository repository, IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(
        CreateProjectCommand command, 
        CancellationToken ct = default)
    {
        var project = new Project(
            command.Title,
            command.Slug,
            command.UrlThumbnail,
            command.Description,
            command.Content,
            command.Technologies);
        
        var addResult = await _repository.AddAsync(project, ct);
        
        if (!addResult.IsSuccess)
            return Result<Guid>.Failure(addResult.Error);

        await _uow.SaveChangesAsync(ct);
        return Result<Guid>.Success(project.Id);
    }
}
