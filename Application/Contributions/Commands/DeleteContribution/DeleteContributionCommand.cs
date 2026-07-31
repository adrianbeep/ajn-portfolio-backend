using Application.Abstractions.Common;
using MediatR;

namespace Application.Contributions.Commands.DeleteContribution;

public sealed record DeleteContributionCommand(string Slug) : IRequest<Result>;
