using Application.Abstractions.Common;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Authentication;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? Id
    {
        get
        {
            var raw = _accessor.HttpContext?.User?.FindFirst("sub")?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin =>
        _accessor.HttpContext?.User?.IsInRole("Admin") ?? false;
}