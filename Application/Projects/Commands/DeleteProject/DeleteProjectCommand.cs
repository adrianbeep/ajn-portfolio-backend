using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Projects.Commands.DeleteProject;

[AdminOnly]
public sealed record DeleteProjectCommand(string Slug) : IRequest<Result>;
