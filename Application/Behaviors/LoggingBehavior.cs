using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next(ct);
            sw.Stop();
            var prop = typeof(TResponse).GetProperty("IsSuccess");

            if (prop?.PropertyType == typeof(bool))
            {
                var isSuccess = (bool)(prop.GetValue(response) ?? false);
                _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms (IsSuccess={isSuccess})",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds,
                isSuccess);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMs}ms",
                    typeof(TRequest).Name,
                    sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error handling {RequestName} after {ElapsedMs}ms",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}
