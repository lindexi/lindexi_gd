namespace NeerecairwarLemwemgallkaga;

public class YubacemhiHurhewebeardalLogger: ILogger, IDisposable
{
    public YubacemhiHurhewebeardalLogger(string categoryName)
    {
        CategoryName = categoryName;
    }

    public string CategoryName { get; }

    public IDisposable BeginScope<TState>(TState state)
    {
        return this;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state,exception);
        Console.WriteLine($"[{CategoryName}] {message}");
    }

    public void Dispose()
    {
    }
}

public class LoggerProvider : ILoggerProvider
{
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new YubacemhiHurhewebeardalLogger(categoryName);
    }
}