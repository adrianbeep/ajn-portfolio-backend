namespace Application.Authentication.Dtos
{
    public sealed record AuthResponse(
        string Token,
        DateTime ExpiresAt);
}
