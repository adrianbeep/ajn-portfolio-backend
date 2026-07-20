using Application.Abstractions.Common;

namespace Application.Tests.Fakes;

public class FakeCurrentUser : ICurrentUser
{
    public Guid? Id { get; set; } = Guid.NewGuid();
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
}