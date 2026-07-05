namespace Application.Abstractions.Common
{
    public interface ICurrentUser
    {
        Guid? Id { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
    }
}
