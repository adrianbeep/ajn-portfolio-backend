using Application.Abstractions.Common;
using MediatR;

namespace Application.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(string Slug) : IRequest<Result>;
