using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace WinRemoteShell.Server;

public sealed class CmdProcess : IAsyncDisposable
{
    private Process? _process;
    private Channel<string>? _output;

    public async IAsyncEnumerable<string> ExecuteAsync(
        IReadOnlyList<string> arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        var marker = $"__WINRS_END_{Guid.NewGuid():N}__";
        await WriteLineAsync(string.Join(' ', arguments), cancellationToken);
        await WriteLineAsync($"echo {marker}", cancellationToken);

        while (await _output!.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_output.Reader.TryRead(out var line))
            {
                if (line.EndsWith(marker, StringComparison.Ordinal))
                {
                    yield break;
                }

                yield return line;
            }
        }

        throw new InvalidOperationException("The command process exited before the command completed.");
    }

    public async Task WriteLineAsync(string line, CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        await _process!.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken);
    }

    public async IAsyncEnumerable<string> ReadOutputAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        while (await _output!.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_output.Reader.TryRead(out var line))
            {
                yield return line;
            }
        }
    }

    public async Task InterruptOrRestartAsync(CancellationToken cancellationToken = default)
    {
        EnsureStarted();
        await _process!.StandardInput.WriteAsync("\u0003".AsMemory(), cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        Restart();
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        return ValueTask.CompletedTask;
    }

    private void EnsureStarted()
    {
        if (_process is null || _process.HasExited)
        {
            Stop();
            Start();
        }
    }

    private void Start()
    {
        _output = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/D /Q /K chcp 65001>nul",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };
        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnOutputDataReceived;
        _process.Exited += OnExited;
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private void Stop()
    {
        if (_process is null)
        {
            return;
        }

        _process.OutputDataReceived -= OnOutputDataReceived;
        _process.ErrorDataReceived -= OnOutputDataReceived;
        _process.Exited -= OnExited;
        if (!_process.HasExited)
        {
            _process.Kill(true);
            _process.WaitForExit();
        }

        _process.Dispose();
        _process = null;
        _output?.Writer.TryComplete();
        _output = null;
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is not null)
        {
            _output?.Writer.TryWrite(e.Data);
        }
    }

    private void OnExited(object? sender, EventArgs e)
    {
        _output?.Writer.TryComplete();
    }
}
