using Microsoft.Extensions.Logging;

namespace DeepSeekWpf.Services;

public sealed class AppLoggerProvider : ILoggerProvider
{
    private readonly IAppLogger _appLogger;

    public AppLoggerProvider(IAppLogger appLogger)
    {
        _appLogger = appLogger;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new AppLoggerAdapter(_appLogger, categoryName);
    }

    public void Dispose()
    {
    }

    private sealed class AppLoggerAdapter : ILogger
    {
        private readonly IAppLogger _appLogger;
        private readonly string _categoryName;

        public AppLoggerAdapter(IAppLogger appLogger, string categoryName)
        {
            _appLogger = appLogger;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            _ = _appLogger.LogAsync(logLevel, $"[{_categoryName}] {message}", exception);
        }
    }
}