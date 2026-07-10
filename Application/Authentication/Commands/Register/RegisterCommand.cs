using Application.Abstractions.Common;
using Application.Authentication.Dtos;
using MediatR;

namespace Application.Authentication.Commands.Register;

public sealed record RegisterCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
