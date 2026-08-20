using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Projects.Commands.RestoreProject;

[AdminOnly]
public sealed record RestoreProjectCommand(string Slug) : IRequest<Result>;
