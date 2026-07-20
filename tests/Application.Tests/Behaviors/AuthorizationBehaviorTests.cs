using Application.Abstractions.Common;
using Application.Behaviors;
using Application.Tests.Fakes;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Behaviors;

public class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_NoAdminOnlyAttribute_PassesThrough()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = true, IsAdmin = true };
        var behavior = new AuthorizationBehavior<TestCommand, Result>(fakeUser);
        var request = new TestCommand();
        var nextCallCount = 0;

        var result = await behavior.Handle(
            request,
            _ =>
            {
                nextCallCount++;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        nextCallCount.Should().Be(1);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AdminOnlyNotAuthenticated_ReturnsUnauthorized()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = false, IsAdmin = false };
        var behavior = new AuthorizationBehavior<AdminOnlyTestCommand, Result>(fakeUser);
        var request = new AdminOnlyTestCommand();
        var nextCallCount = 0;

        var result = await behavior.Handle(
            request,
            _ =>
            {
                nextCallCount++;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        nextCallCount.Should().Be(0);
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Handle_AdminOnlyAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = true, IsAdmin = false };
        var behavior = new AuthorizationBehavior<AdminOnlyTestCommand, Result>(fakeUser);
        var request = new AdminOnlyTestCommand();
        var nextCallCount = 0;

        var result = await behavior.Handle(
            request,
            _ =>
            {
                nextCallCount++;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        nextCallCount.Should().Be(0);
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Forbidden");
    }

    [Fact]
    public async Task Handle_AdminOnlyAdmin_PassesThrough()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = true, IsAdmin = true };
        var behavior = new AuthorizationBehavior<AdminOnlyTestCommand, Result>(fakeUser);
        var request = new AdminOnlyTestCommand();
        var nextCallCount = 0;

        var result = await behavior.Handle(
            request,
            _ =>
            {
                nextCallCount++;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        nextCallCount.Should().Be(1);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToNext()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = true, IsAdmin = true };
        var behavior = new AuthorizationBehavior<TestCommand, Result>(fakeUser);
        var request = new TestCommand();
        using var cts = new CancellationTokenSource();
        var observedCt = CancellationToken.None;

        await behavior.Handle(
            request,
            ct =>
            {
                observedCt = ct;
                return Task.FromResult(Result.Success());
            },
            cts.Token);

        observedCt.Should().Be(cts.Token);
    }
}