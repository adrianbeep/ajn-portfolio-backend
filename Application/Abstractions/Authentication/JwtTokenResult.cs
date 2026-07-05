namespace Application.Abstractions.Authentication
{
    public sealed record JwtTokenResult(
        string Token,
        DateTime ExpiresAt);
}
