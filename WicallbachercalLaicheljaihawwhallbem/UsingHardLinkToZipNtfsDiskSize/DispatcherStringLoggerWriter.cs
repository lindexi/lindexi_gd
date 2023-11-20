using System.Windows.Controls;

namespace UsingHardLinkToZipNtfsDiskSize;

class DispatcherStringLoggerWriter : IStringLoggerWriter
{
    public DispatcherStringLoggerWriter(TextBlock logTextBlock)
    {
        _logTextBlock = logTextBlock;
    }

    private readonly TextBlock _logTextBlock;

    public async ValueTask WriteAsync(string message)
    {
        await _logTextBlock.Dispatcher.InvokeAsync(() =>
        {
            _logTextBlock.Text = message;
        });
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}