using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Contributions.Commands.DeleteContribution;

[AdminOnly]
public sealed record DeleteContributionCommand(string Slug) : IRequest<Result>;
