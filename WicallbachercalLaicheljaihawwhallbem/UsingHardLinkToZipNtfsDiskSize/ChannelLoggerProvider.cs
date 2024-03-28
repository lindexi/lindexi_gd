using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace UsingHardLinkToZipNtfsDiskSize;

public class ChannelLoggerProvider : ILoggerProvider
{
    public ChannelLoggerProvider(params IStringLoggerWriter[] stringLoggerWriterList)
    {
        _stringLoggerWriterList = stringLoggerWriterList;
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
        {
            SingleReader = true
        });
        _channel = channel;

        Task.Run(WriteLogAsync);
    }

    private readonly IStringLoggerWriter[] _stringLoggerWriterList;

    private async Task? WriteLogAsync()
    {
        while (!_channel.Reader.Completion.IsCompleted)
        {
            try
            {
                var message = await _channel.Reader.ReadAsync();
                foreach (var stringLoggerWriter in _stringLoggerWriterList)
                {
                    await stringLoggerWriter.WriteAsync(message);
                }
            }
            catch (ChannelClosedException)
            {
                // 结束
            }
        }

        foreach (var stringLoggerWriter in _stringLoggerWriterList)
        {
            await stringLoggerWriter.DisposeAsync();
        }
    }

    private readonly Channel<string> _channel;
    public void Dispose()
    {
        ChannelWriter<string> channelWriter = _channel.Writer;
        channelWriter.TryComplete();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ChannelLogger(_channel.Writer);
    }

    class ChannelLogger : ILogger, IDisposable
    {
        public ChannelLogger(ChannelWriter<string> writer)
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