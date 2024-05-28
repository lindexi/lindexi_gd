using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace WicallbachercalLaicheljaihawwhallbem;

public class LogFileProvider : ILoggerProvider
{
    public LogFileProvider(DirectoryInfo logFolder)
    {
        _logFolder = logFolder;
        var logFile = Path.Join(logFolder.FullName, "Log.txt");
        var fileStream = new FileStream(logFile,FileMode.Append,FileAccess.Write,FileShare.Read);
        _fileStream = fileStream;
        var streamWriter = new StreamWriter(fileStream,Encoding.UTF8);
        _streamWriter = streamWriter;

        var channel = Channel.CreateUnbounded<string>();
        _channel = channel;

        Task.Run(WriteLogAsync);
    }

    private async Task? WriteLogAsync()
    {
        while (!_channel.Reader.Completion.IsCompleted)
        {
            var message = await _channel.Reader.ReadAsync();
            await _streamWriter.WriteLineAsync(message);
        }

        await _streamWriter.DisposeAsync();
        await _fileStream.DisposeAsync();
    }

    private readonly DirectoryInfo _logFolder;
    private readonly FileStream _fileStream;
    private readonly Channel<string> _channel;
    private readonly StreamWriter _streamWriter;

    public void Dispose()
    {
        ChannelWriter<string> channelWriter = _channel.Writer;
        channelWriter.TryComplete();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_channel.Writer);
    }

    class FileLogger : ILogger, IDisposable
    {
        public FileLogger(ChannelWriter<string> writer)
        {
            _writer = writer;
        }

        private readonly ChannelWriter<string> _writer;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = $"{DateTime.Now:yyyy.MM.dd HH:mm:ss,fff} [{logLevel}][{eventId}] {formatter(state, exception)}";
            _ = _writer.WriteAsync(message);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}