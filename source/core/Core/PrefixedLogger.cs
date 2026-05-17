using Microsoft.Extensions.Logging;

namespace Twf.Flow.Core;

/// <summary>Logger that prefixes all messages with a context string.</summary>
internal sealed class PrefixedLogger : ILogger
{
    private readonly ILogger _inner;
    private readonly string _prefix;

    public PrefixedLogger(ILogger inner, string prefix)
    {
        _inner = inner;
        _prefix = prefix;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        _inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _inner.Log(logLevel, eventId, state, exception,
            (s, ex) => $"{_prefix} {formatter(s, ex)}");
    }
}
