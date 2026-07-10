using Application.Abstractions.Common;
using MediatR;

namespace Application.Projects.Commands.RestoreProject;

public sealed record RestoreProjectCommand(string Slug) : IRequest<Result>;
