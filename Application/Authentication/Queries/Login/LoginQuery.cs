using Application.Abstractions.Common;
using Application.Authentication.Dtos;
using MediatR;

namespace Application.Authentication.Queries.Login;

public sealed record LoginQuery(string Email, string Password) : IRequest<Result<AuthResponse>>;

