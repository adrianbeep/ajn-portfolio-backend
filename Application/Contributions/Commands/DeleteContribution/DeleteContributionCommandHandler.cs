using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using MediatR;

namespace Application.Contributions.Commands.DeleteContribution;

public class DeleteContributionCommandHandler : IRequestHandler<DeleteContributionCommand, Result>
{
    private readonly IContributionRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteContributionCommandHandler(
        IContributionRepository repository,
        IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }
    
    public async Task<Result> Handle(
        DeleteContributionCommand command,
        CancellationToken ct = default)
    {
        var contribution = await _repository.GetBySlugAsync(command.Slug, ct);

        if (contribution is null)
            return Result.Failure(
                new Error(
                    "ContributionNotFound", $"Contribution with Slug '{command.Slug}' was not found."));

        var softDeleteResult = await _repository.SoftDeleteAsync(contribution, ct);

        if (!softDeleteResult.IsSuccess)
            return Result.Failure(softDeleteResult.Error);

        await _uow.SaveChangesAsync();
        return Result.Success();
    }
}
