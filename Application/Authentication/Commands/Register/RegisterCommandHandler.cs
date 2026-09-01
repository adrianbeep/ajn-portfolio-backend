using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Authentication.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.Authentication.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _repository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IUserRepository repository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _repository = repository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(
        RegisterCommand command,
        CancellationToken ct = default)
    {
        var user = new ApplicationUser { UserName = command.Email, Email = command.Email};

        var userResult = await _repository.AddAsync(user, command.Password, ct);

        if (!userResult.IsSuccess)
            return Result<AuthResponse>.Failure(userResult.Error);

        var roleResult = await _repository.AddToRoleAsync(user, "Admin", ct);
        if (!roleResult.IsSuccess)
            return Result<AuthResponse>.Failure(roleResult.Error);

        var token = _jwtTokenGenerator.GenerateToken(user, ct);

        return Result<AuthResponse>.Success(new AuthResponse(token.Token, token.ExpiresAt));
    }
}
