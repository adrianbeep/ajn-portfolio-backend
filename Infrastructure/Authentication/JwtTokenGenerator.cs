using Application.Abstractions.Authentication;
using Domain.Entities;

namespace Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    public JwtTokenResult GenerateToken(ApplicationUser applicationUser, CancellationToken ct = default)
        => new JwtTokenResult(string.Empty, DateTime.UtcNow);
}