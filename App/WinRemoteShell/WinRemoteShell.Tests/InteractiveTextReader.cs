using System.Threading.Channels;

namespace WinRemoteShell.Tests;

internal sealed class InteractiveTextReader : TextReader
{
    private readonly Channel<string?> _lines = Channel.CreateUnbounded<string?>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask WriteLineAsync(string line, CancellationToken cancellationToken = default) =>
        _lines.Writer.WriteAsync(line, cancellationToken);

    public void Complete() => _lines.Writer.TryWrite(null);

    public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) =>
        _lines.Reader.ReadAsync(cancellationToken);
}
