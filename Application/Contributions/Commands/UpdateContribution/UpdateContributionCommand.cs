using MediatR;
using Application.Abstractions.Common;

namespace Application.Contributions.Commands.UpdateContribution;

public sealed record UpdateContributionCommand(
    string Slug,
    string Title,
    string Description,
    string UrlThumbnail,
    string UrlPrContribution,
    string Content) : IRequest<Result>;
