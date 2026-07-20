using Application.Abstractions.Authentication;

namespace Infrastructure.Authentication;

public class BootstrapTokenValidator : IBootstrapTokenValidator
{
    public Task<bool> IsValid(string token, CancellationToken ct = default)
        => Task.FromResult(false);
}