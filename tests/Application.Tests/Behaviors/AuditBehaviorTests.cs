using Application.Abstractions.Common;
using Application.Behaviors;
using Application.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Application.Tests.Behaviors;

public class AuditBehaviorTests
{
    [Fact]
    public async Task Handle_QueryRequest_PassesThroughWithoutLogging()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = true };
        var fakeLogger = new FakeLogger<AuditBehavior<TestQuery, Result>>();
        var behavior = new AuditBehavior<TestQuery, Result>(fakeUser, fakeLogger);
        var request = new TestQuery();
        var nextCallCount = 0;

        await behavior.Handle(
            request,
            _ =>
            {
                nextCallCount++;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        nextCallCount.Should().Be(1);
        fakeLogger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnauthenticatedCommand_PassesThroughWithoutLogging()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = false };
        var fakeLogger = new FakeLogger<AuditBehavior<TestCommand, Result>>();
        var behavior = new AuditBehavior<TestCommand, Result>(fakeUser, fakeLogger);
        var request = new TestCommand();
        var nextCallCount = 0;

        await behavior.Handle(
            request,
            _ =>
            {
                nextCallCount++;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        nextCallCount.Should().Be(1);
        fakeLogger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AuthenticatedCommandSuccess_LogsIntentAndSuccess()
    {
        var userId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser { Id = userId, IsAuthenticated = true };
        var fakeLogger = new FakeLogger<AuditBehavior<TestCommand, Result>>();
        var behavior = new AuditBehavior<TestCommand, Result>(fakeUser, fakeLogger);
        var request = new TestCommand();

        await behavior.Handle(
            request,
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        fakeLogger.Entries.Should().HaveCount(2);

        var intent = fakeLogger.Entries[0];
        intent.Level.Should().Be(LogLevel.Information);
        intent.Message.Should().Contain("user");
        intent.Message.Should().Contain(userId.ToString());
        intent.Message.Should().Contain("attempted TestCommand");

        var success = fakeLogger.Entries[1];
        success.Level.Should().Be(LogLevel.Information);
        success.Message.Should().Contain("completed TestCommand");
        success.Message.Should().Contain("success");
    }

    [Fact]
    public async Task Handle_AuthenticatedCommandFailure_LogsIntentAndWarningWithErrorCode()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = true };
        var fakeLogger = new FakeLogger<AuditBehavior<TestCommand, Result>>();
        var behavior = new AuditBehavior<TestCommand, Result>(fakeUser, fakeLogger);
        var request = new TestCommand();

        await behavior.Handle(
            request,
            _ => Task.FromResult(Result.Failure(new Error("Conflict", "Slug already exists"))),
            CancellationToken.None);

        fakeLogger.Entries.Should().HaveCount(2);

        fakeLogger.Entries[0].Level.Should().Be(LogLevel.Information);
        fakeLogger.Entries[0].Message.Should().Contain("attempted TestCommand");

        var failure = fakeLogger.Entries[1];
        failure.Level.Should().Be(LogLevel.Warning);
        failure.Message.Should().Contain("completed TestCommand");
        failure.Message.Should().Contain("failure");
        failure.Message.Should().Contain("Conflict");
    }

    [Fact]
    public async Task Handle_AuthenticatedCommandHandlerThrows_LogsIntentAndErrorAndRethrows()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = true };
        var fakeLogger = new FakeLogger<AuditBehavior<TestCommand, Result>>();
        var behavior = new AuditBehavior<TestCommand, Result>(fakeUser, fakeLogger);
        var request = new TestCommand();

        var act = () => behavior.Handle(
            request,
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        fakeLogger.WithLevel(LogLevel.Information).Should().HaveCount(1);
        fakeLogger.WithLevel(LogLevel.Information).Single().Message.Should().Contain("attempted TestCommand");

        var errorEntry = fakeLogger.WithLevel(LogLevel.Error).Single();
        errorEntry.Exception.Should().BeOfType<InvalidOperationException>();
        errorEntry.Message.Should().Contain("attempted TestCommand");
        errorEntry.Message.Should().Contain("exception");
    }

    [Fact]
    public async Task Handle_GenericResultResponse_LogsErrorCodeCorrectly()
    {
        var fakeUser = new FakeCurrentUser { IsAuthenticated = true };
        var fakeLogger = new FakeLogger<AuditBehavior<TestValueCommand, Result<string>>>();
        var behavior = new AuditBehavior<TestValueCommand, Result<string>>(fakeUser, fakeLogger);
        var request = new TestValueCommand();

        await behavior.Handle(
            request,
            _ => Task.FromResult(Result<string>.Failure(new Error("ValidationFailed", "x"))),
            CancellationToken.None);

        fakeLogger.WithLevel(LogLevel.Warning).Should().HaveCount(1);
        fakeLogger.WithLevel(LogLevel.Warning).Single().Message.Should().Contain("ValidationFailed");
    }
}