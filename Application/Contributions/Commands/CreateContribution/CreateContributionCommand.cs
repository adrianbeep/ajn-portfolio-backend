using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Contributions.Commands.CreateContribution;

[AdminOnly]
public sealed record CreateContributionCommand(
    string Title,
    string Slug,
    string UrlThumbnail,
    string UrlPrContribution,
    string Description,
    string Content) : IRequest<Result<Guid>>;
