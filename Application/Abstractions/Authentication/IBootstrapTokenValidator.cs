namespace Application.Abstractions.Authentication
{
    public interface IBootstrapTokenValidator
    {
        Task<bool> IsValid(string token, CancellationToken ct = default);
    }
}
