using Application.Abstractions.Common;
using Application.Behaviors;
using Application.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ResultResponse_LogsEntryAndExitWithIsSuccess()
    {
        var fakeLogger = new FakeLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(fakeLogger);
        var request = new TestCommand("hello");

        var response = await behavior.Handle(
            request,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        response.IsSuccess.Should().BeTrue();

        fakeLogger.Entries.Should().HaveCount(2);

        fakeLogger.Entries[0].Level.Should().Be(LogLevel.Information);
        fakeLogger.Entries[0].Message.Should().Contain("Handling TestCommand");

        fakeLogger.Entries[1].Level.Should().Be(LogLevel.Information);
        fakeLogger.Entries[1].Message.Should().Contain("Handled TestCommand");
        fakeLogger.Entries[1].Message.Should().Contain("IsSuccess=");
    }

    [Fact]
    public async Task Handle_HandlerThrows_LogsErrorAndRethrows()
    {
        var fakeLogger = new FakeLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(fakeLogger);
        var request = new TestCommand();

        var act = () => behavior.Handle(
            request,
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        fakeLogger.WithLevel(LogLevel.Error).Should().HaveCount(1);
        var errorEntry = fakeLogger.WithLevel(LogLevel.Error).Single();
        errorEntry.Exception.Should().BeOfType<InvalidOperationException>();
        errorEntry.Message.Should().Contain("Error handling TestCommand");
    }

    [Fact]
    public async Task Handle_NonResultResponse_LogsWithoutIsSuccess()
    {
        var fakeLogger = new FakeLogger<LoggingBehavior<TestCommand, string>>();
        var behavior = new LoggingBehavior<TestCommand, string>(fakeLogger);
        var request = new TestCommand();

        var response = await behavior.Handle(
            request,
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        response.Should().Be("ok");

        fakeLogger.Entries.Should().HaveCount(2);
        fakeLogger.Entries[1].Message.Should().Contain("Handled TestCommand");
        fakeLogger.Entries[1].Message.Should().NotContain("IsSuccess=");
    }

    [Fact]
    public async Task Handle_NextCalledOnce_WithCancellationToken()
    {
        var fakeLogger = new FakeLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(fakeLogger);
        var request = new TestCommand();
        var nextCallCount = 0;
        using var cts = new CancellationTokenSource();

        await behavior.Handle(
            request,
            ct =>
            {
                nextCallCount++;
                ct.Should().Be(cts.Token);
                return Task.FromResult(Result.Success());
            },
            cts.Token);

        nextCallCount.Should().Be(1);
    }
}