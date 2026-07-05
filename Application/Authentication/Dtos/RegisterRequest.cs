namespace Application.Authentication.Dtos
{
    public sealed record RegisterRequest(
        string Email,
        string Password);
}
