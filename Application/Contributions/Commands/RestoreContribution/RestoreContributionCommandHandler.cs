using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using MediatR;

namespace Application.Contributions.Commands.RestoreContribution;

public class RestoreContributionCommandHandler : IRequestHandler<RestoreContributionCommand, Result>
{
    private readonly IContributionRepository _repository;
    private readonly IUnitOfWork _uow;

    public RestoreContributionCommandHandler(
        IContributionRepository repository,
        IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<Result> Handle(
        RestoreContributionCommand command,
        CancellationToken ct = default)
    {
        var contribution = await _repository.GetBySlugAsync(command.Slug, ct);

        if (contribution is null)
            return Result.Failure(
                new Error(
                    "ContributionNotFound", $"Contribution with Slug '{command.Slug}' was not found.'"));

        var restoreResult = await _repository.RestoreAsync(contribution, ct);

        if (!restoreResult.IsSuccess)
            return Result.Failure(restoreResult.Error);

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
