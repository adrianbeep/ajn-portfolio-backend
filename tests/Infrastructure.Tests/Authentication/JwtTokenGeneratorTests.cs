using System.Security.Claims;
using System.Text;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using SysJwt = System.IdentityModel.Tokens.Jwt;

namespace Infrastructure.Tests.Authentication;

public class JwtTokenGeneratorTests
{
    private readonly JwtSettings _validSettings;
    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        _validSettings = new JwtSettings
        {
            Secret = "this-is-a-super-secret-key-that-is-at-least-32-chars",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };

        _generator = new JwtTokenGenerator(Options.Create(_validSettings));
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsNonEmptyToken()
    {
        var user = CreateTestUser();

        var result = _generator.GenerateToken(user);

        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_SetsCorrectExpiration()
    {
        var user = CreateTestUser();
        var before = DateTime.UtcNow;

        var result = _generator.GenerateToken(user);

        result.ExpiresAt.Should().BeAfter(before.AddMinutes(59));
        result.ExpiresAt.Should().BeBefore(before.AddMinutes(61));
    }

    [Fact]
    public void GenerateToken_TokenContainsCorrectSubject()
    {
        var user = CreateTestUser();

        var result = _generator.GenerateToken(user);
        var handler = new SysJwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Subject.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_TokenContainsEmail()
    {
        var user = CreateTestUser();

        var result = _generator.GenerateToken(user);
        var handler = new SysJwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateToken_TokenContainsRoleClaim()
    {
        var user = CreateTestUser();

        var result = _generator.GenerateToken(user);
        var handler = new SysJwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public void GenerateToken_TokenContainsJti()
    {
        var user = CreateTestUser();

        var result = _generator.GenerateToken(user);
        var handler = new SysJwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectIssuer()
    {
        var user = CreateTestUser();

        var result = _generator.GenerateToken(user);
        var handler = new SysJwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Issuer.Should().Be(_validSettings.Issuer);
    }

    [Fact]
    public void GenerateToken_TokenHasCorrectAudience()
    {
        var user = CreateTestUser();

        var result = _generator.GenerateToken(user);
        var handler = new SysJwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Audiences.Should().Contain(_validSettings.Audience);
    }

    [Fact]
    public void GenerateToken_TokenIsSignedWithHmacSha256()
    {
        var user = CreateTestUser();

        var result = _generator.GenerateToken(user);
        var handler = new SysJwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.SignatureAlgorithm.Should().Be("HS256");
    }

    [Fact]
    public void GenerateToken_TwoTokensAreDifferent()
    {
        var user = CreateTestUser();

        var result1 = _generator.GenerateToken(user);
        var result2 = _generator.GenerateToken(user);

        result1.Token.Should().NotBe(result2.Token);
    }

    [Fact]
    public void GenerateToken_TokenCanBeValidated()
    {
        var user = CreateTestUser();
        var result = _generator.GenerateToken(user);

        var handler = new SysJwt.JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_validSettings.Secret));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _validSettings.Issuer,
            ValidAudience = _validSettings.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(result.Token, validationParameters, out _);

        principal.Should().NotBeNull();
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
    }

    private static ApplicationUser CreateTestUser()
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            NormalizedUserName = "TEST@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }
}
