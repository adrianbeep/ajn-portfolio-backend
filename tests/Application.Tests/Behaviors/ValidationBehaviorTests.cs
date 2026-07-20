using Application.Abstractions.Common;
using Application.Behaviors;
using Application.Tests.Fakes;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Xunit;

namespace Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_NoValidators_PassesThroughAndCallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>(
            Enumerable.Empty<IValidator<TestCommand>>());
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
    public async Task Handle_AllValidatorsPass_CallsNext()
    {
        var validator = new FakeValidator<TestCommand>();
        var behavior = new ValidationBehavior<TestCommand, Result>(new[] { validator });
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

        validator.ValidateAsyncCallCount.Should().Be(1);
        nextCallCount.Should().Be(1);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidatorFails_ReturnsFailureAndDoesNotCallNext()
    {
        var validator = new FakeValidator<TestCommand>();
        validator.Errors.Add(new ValidationFailure("Payload", "is required"));
        var behavior = new ValidationBehavior<TestCommand, Result>(new[] { validator });
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

        nextCallCount.Should().Be(0);
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ValidationFailed");
        result.Error.Message.Should().Contain("is required");
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>(
            Enumerable.Empty<IValidator<TestCommand>>());
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