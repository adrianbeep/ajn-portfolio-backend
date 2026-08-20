using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Contributions.Commands.RestoreContribution;

[AdminOnly]
public sealed record RestoreContributionCommand(string Slug) : IRequest<Result>;
