using FluentValidation;
using FluentValidation.Results;

namespace Application.Tests.Fakes;

public class FakeValidator<T> : AbstractValidator<T>
{
    public List<ValidationFailure> Errors { get; } = new();
    public int ValidateAsyncCallCount { get; private set; }

    public override Task<ValidationResult> ValidateAsync(
        ValidationContext<T> context,
        CancellationToken cancellationToken = default)
    {
        ValidateAsyncCallCount++;
        return Task.FromResult(new ValidationResult(Errors));
    }
}