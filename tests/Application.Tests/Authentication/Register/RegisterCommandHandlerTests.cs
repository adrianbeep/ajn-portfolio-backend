using Application.Abstractions.Common;
using Application.Authentication.Commands.Register;
using Application.Tests.Fakes;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Authentication.Register;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsSuccessWithToken()
    {
        var fakeUserRepo = new FakeUserRepository();
        var fakeTokenGen = new FakeJwtTokenGenerator();
        var handler = new RegisterCommandHandler(fakeUserRepo, fakeTokenGen);
        var command = new RegisterCommand("user@example.com", "Password1!");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("fake-token");
        fakeUserRepo.AddCallCount.Should().Be(1);
        fakeUserRepo.Users.Should().HaveCount(1);
        fakeTokenGen.GenerateTokenCallCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAlreadyExists_ReturnsFailure()
    {
        var fakeUserRepo = new FakeUserRepository();
        fakeUserRepo.AddShouldFail = true;
        var fakeTokenGen = new FakeJwtTokenGenerator();
        var handler = new RegisterCommandHandler(fakeUserRepo, fakeTokenGen);
        var command = new RegisterCommand("user@example.com", "Password1!");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EmailAlreadyExists");
        fakeUserRepo.Users.Should().BeEmpty();
        fakeTokenGen.GenerateTokenCallCount.Should().Be(0);
    }
}
