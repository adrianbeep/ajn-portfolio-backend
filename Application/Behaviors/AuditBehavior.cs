using Application.Abstractions.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(ICurrentUser currentUser, ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        if (!typeof(TRequest).Name.EndsWith("Command"))
            return await next(ct);

        if (!_currentUser.IsAuthenticated)
            return await next(ct);
        
        _logger.LogInformation(
            "Audit: user {UserId} attempted {RequestName}",
            _currentUser.Id,
            typeof(TRequest).Name);

        try
        {
            var response = await next(ct);
            
            var isSuccessProp = typeof(TResponse).GetProperty("IsSuccess");
            var errorProp = typeof(TResponse).GetProperty("Error");
            bool isSuccess = isSuccessProp?.PropertyType == typeof(bool) ? (bool)(isSuccessProp.GetValue(response) ?? false) : true;

            string? errorCode = null;
            if (!isSuccess && errorProp != null)
            {
                var errorValue = errorProp.GetValue(response);
                if (errorValue != null)
                {
                    var codeProp = errorValue.GetType().GetProperty("Code");
                    if (codeProp?.PropertyType == typeof(string))
                        errorCode = (string?)codeProp.GetValue(errorValue);
                }
            }

            if (isSuccess)
            {
                _logger.LogInformation(
                    "Audit: user {UserId} completed {RequestName} → success",
                    _currentUser.Id, typeof(TRequest).Name);
            }
            else
            {
                _logger.LogWarning(
                    "Audit: user {UserId} completed {RequestName} → failure ({ErrorCode})",
                    _currentUser.Id, typeof(TRequest).Name, errorCode);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Audit: user {UserId} attempted {RequestName} → exception",
                _currentUser.Id, typeof(TRequest).Name);
            
            throw;
        }
    }
}
