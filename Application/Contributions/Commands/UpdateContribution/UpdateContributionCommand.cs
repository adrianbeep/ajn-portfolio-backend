using MediatR;
using Application.Abstractions.Authentication;
using Application.Abstractions.Common;

namespace Application.Contributions.Commands.UpdateContribution;

[AdminOnly]
public sealed record UpdateContributionCommand(
    string Slug,
    string Title,
    string Description,
    string UrlThumbnail,
    string UrlPrContribution,
    string Content) : IRequest<Result>;
