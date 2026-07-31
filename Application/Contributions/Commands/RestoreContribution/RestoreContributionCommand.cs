using Application.Abstractions.Common;
using MediatR;

namespace Application.Contributions.Commands.RestoreContribution;

public sealed record RestoreContributionCommand(string Slug) : IRequest<Result>;
