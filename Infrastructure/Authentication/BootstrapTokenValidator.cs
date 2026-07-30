using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authentication;

public class BootstrapTokenValidator : IBootstrapTokenValidator
{
    private readonly BootstrapSettings _settings;

    public BootstrapTokenValidator(IOptions<BootstrapSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<bool> IsValid(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(false);

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var expectedBytes = Encoding.UTF8.GetBytes(_settings.Token);

        // Comparación en tiempo constante para evitar timing attacks
        return Task.FromResult(
            CryptographicOperations.FixedTimeEquals(tokenBytes, expectedBytes)
        );
    }
}
