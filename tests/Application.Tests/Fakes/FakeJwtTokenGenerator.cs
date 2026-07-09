using Application.Abstractions.Authentication;
using Domain.Entities;

namespace Application.Tests.Fakes;

public class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public int GenerateTokenCallCount { get; private set; }
    public string TokenToReturn { get; set; } = "fake-token";
    public DateTime ExpiresAtToReturn { get; set; } = DateTime.UtcNow.AddHours(1);

    public JwtTokenResult GenerateToken(ApplicationUser applicationUser, CancellationToken ct = default)
    {
        GenerateTokenCallCount++;
        return new JwtTokenResult(TokenToReturn, ExpiresAtToReturn);
    }
}
