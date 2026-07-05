using Domain.Entities;

namespace Application.Abstractions.Authentication
{
    public interface IJwtTokenGenerator
    {
        JwtTokenResult GenerateToken(ApplicationUser applicationUser, CancellationToken ct = default);
    }
}
