using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Domain.Entities;
using MediatR;

namespace Application.Contributions.Commands.CreateContribution;

public class CreateContributionCommandHandler 
    : IRequestHandler<CreateContributionCommand, Result<Guid>>
{
    private readonly IContributionRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateContributionCommandHandler(IContributionRepository repository,
        IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(
        CreateContributionCommand command,
        CancellationToken ct = default)
    {
        var contribution = new Contribution(
            command.Title,
            command.Slug,
            command.UrlThumbnail,
            command.UrlPrContribution,
            command.Description,
            command.Content);

        var addResult = await _repository.AddAsync(contribution, ct);
        
        if (!addResult.IsSuccess)
            return Result<Guid>.Failure(addResult.Error);

        await _uow.SaveChangesAsync(ct);
        return Result<Guid>.Success(contribution.Id);
    }
}
