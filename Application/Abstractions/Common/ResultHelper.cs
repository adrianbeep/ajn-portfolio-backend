namespace Application.Abstractions.Common;

public static class ResultHelper
{
    public static TResponse FailureAs<TResponse>(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var tResponse = typeof(TResponse);
            
            var innerType = tResponse.GetGenericArguments()[0];
            
            var concreteResultType = typeof(Result<>).MakeGenericType(innerType);
            
            var failureMethod = concreteResultType.GetMethod("Failure", new[] { typeof(Error) });
            
            var failureInstance = failureMethod!.Invoke(null, new object[] { error });
            
            return (TResponse)failureInstance!;
        }
        
        return default!;
    }
}
