using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using MediatR;

namespace Application.Contributions.Commands.UpdateContribution;

public class UpdateContributionCommandHandler : IRequestHandler<UpdateContributionCommand, Result>
{
    private readonly IContributionRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateContributionCommandHandler(
        IContributionRepository repository,
        IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<Result> Handle(
        UpdateContributionCommand command,
        CancellationToken ct = default)
    {
        var contribution = await _repository.GetBySlugAsync(command.Slug, ct);

        if (contribution is null)
            return Result.Failure(
                new Error(
                    "ContributionNotFound",
                    $"Contribution with Slug '{command.Slug}' was not found."));
        
        contribution.Update(
            command.Title,
            command.Description,
            command.UrlThumbnail,
            command.UrlPrContribution,
            command.Content);

        var updateResult = await _repository.UpdateAsync(contribution, ct);
        
        if (!updateResult.IsSuccess)
            return Result.Failure(updateResult.Error);

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
