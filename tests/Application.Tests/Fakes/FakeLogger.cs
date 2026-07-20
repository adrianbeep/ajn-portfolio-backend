using Microsoft.Extensions.Logging;

namespace Application.Tests.Fakes;

public class FakeLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, state?.ToString() ?? string.Empty, exception));
    }

    public IEnumerable<LogEntry> WithLevel(LogLevel level) =>
        Entries.Where(e => e.Level == level);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);