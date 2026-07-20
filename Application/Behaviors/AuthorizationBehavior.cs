using System.Reflection;
using Application.Abstractions.Authentication;
using Application.Abstractions.Common;
using MediatR;

namespace Application.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ICurrentUser _currentUser;

    public AuthorizationBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        var adminOnly = typeof(TRequest).GetCustomAttribute<AdminOnlyAttribute>();

        if (adminOnly is null)
            return await next(ct);

        if (!_currentUser.IsAuthenticated)
        {
            var error = new Error("Unauthorized", "User unauthorized");

            return ResultHelper.FailureAs<TResponse>(error);
        }

        if (!_currentUser.IsAdmin)
        {
            var error = new Error("Forbidden", "Admin role required");

            return ResultHelper.FailureAs<TResponse>(error);
        }
            
        return await next(ct);
    }
}
