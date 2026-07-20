using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Tests.Behaviors;

public sealed record TestCommand(string Payload = "") : IRequest<Result>;

[AdminOnly]
public sealed record AdminOnlyTestCommand(string Payload = "") : IRequest<Result>;

public sealed record TestQuery(string Payload = "") : IRequest<Result>;

public sealed record TestValueCommand(string Payload = "") : IRequest<Result<string>>;