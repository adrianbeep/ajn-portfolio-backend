using Application.Authentication.Queries.Login;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Authentication.Login;

public class LoginQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var user = new ApplicationUser { UserName = "user@example.com", Email = "user@example.com" };
        var fakeUserRepo = new FakeUserRepository();
        fakeUserRepo.Seed(user);
        fakeUserRepo.CheckPasswordReturnValue = true;
        var fakeTokenGen = new FakeJwtTokenGenerator();
        var handler = new LoginQueryHandler(fakeUserRepo, fakeTokenGen);
        var query = new LoginQuery("user@example.com", "Password1!");

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("fake-token");
        fakeUserRepo.CheckPasswordCallCount.Should().Be(1);
        fakeTokenGen.GenerateTokenCallCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCredentials_ReturnsFailure()
    {
        var fakeUserRepo = new FakeUserRepository();
        fakeUserRepo.CheckPasswordReturnValue = false;
        var fakeTokenGen = new FakeJwtTokenGenerator();
        var handler = new LoginQueryHandler(fakeUserRepo, fakeTokenGen);
        var query = new LoginQuery("user@example.com", "wrong-password");

        var result = await handler.HandleAsync(query);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidCredentials");
        fakeTokenGen.GenerateTokenCallCount.Should().Be(0);
    }
}
