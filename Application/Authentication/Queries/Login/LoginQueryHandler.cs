using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using Application.Abstractions.Persistence;
using Application.Authentication.Dtos;
using MediatR;

namespace Application.Authentication.Queries.Login;

public class LoginQueryHandler : IRequestHandler<LoginQuery, Result<AuthResponse>>
{
    private readonly IUserRepository _repository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginQueryHandler(
        IUserRepository repository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _repository = repository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginQuery query,
        CancellationToken ct = default)
    {
        var user = await _repository.GetByEmailAsync(query.Email, ct);

        if (user is null)
            return Result<AuthResponse>.Failure(
                new Error(
                    "Auth.InvalidCredentials", "Invalid email or password"));

        bool pass = await _repository.CheckPasswordAsync(user, query.Password, ct);

        if (!pass)
            return Result<AuthResponse>.Failure(
                new Error(
                    "Auth.InvalidCredentials", "Invalid email or password"));

        var token = _jwtTokenGenerator.GenerateToken(user, ct);

        return Result<AuthResponse>.Success(new AuthResponse(token.Token, token.ExpiresAt));
    }
}
