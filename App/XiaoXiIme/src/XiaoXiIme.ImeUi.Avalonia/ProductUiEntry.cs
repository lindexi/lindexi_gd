using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Threading;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class ProductUiEntry : IDisposable
{
    private readonly Mutex _mutex;
    private readonly string _pipe;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _listener;
    private Action<bool>? _open;
    private Action? _exit;
    private bool? _pending;
    private bool _pendingExit;
    private bool _disposed;
    internal bool IsOwner { get; }

    internal ProductUiEntry(string[] args, string? profileIdentity = null)
    {
        var positional = args.Where(arg => arg is not "--settings" and not "--exit").ToArray();
        var server = positional.Length > 0 ? positional[0] : XiaoXiImeIpcOptions.Default.ServerName;
        var identity = Convert.ToHexString
            (SHA256.HashData(Encoding.UTF8.GetBytes(Environment.UserName + "\0" + (profileIdentity ?? server))));
        _pipe = "XiaoXiIme_ProductUi_" + identity;
        _mutex = new Mutex(false, "Local\\" + _pipe);
        try
        {
            IsOwner = _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            IsOwner = true;
        }

        if (IsOwner)
        {
            _listener = ListenAsync(_shutdown.Token);
        }
        else
        {
            try
            {
                using var client = new NamedPipeClientStream
                    (".", _pipe, PipeDirection.Out, PipeOptions.CurrentUserOnly);
                client.Connect(3000);
                client.WriteByte(args.Contains("--exit") ? (byte)2 : args.Contains("--settings") ? (byte)1 : (byte)0);
            }
            finally
            {
                _disposed = true;
                _mutex.Dispose();
                _shutdown.Dispose();
            }
        }
    }

    internal void Attach(Action<bool> open, Action exit)
    {
        if (_disposed)
        {
            return;
        }

        _exit = exit;
        _open = open;
        if (_pendingExit)
        {
            _pendingExit = false;
            exit();
            return;
        }

        if (_pending is { } pending)
        {
            _pending = null;
            open(pending);
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await using var server = new NamedPipeServerStream
                (
                    _pipe, PipeDirection.In, 1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly
                );
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                var buffer = new byte[1];
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                try
                {
                    if (await server.ReadAsync(buffer, timeout.Token).ConfigureAwait(false) == 1)
                    {
                        Dispatcher.UIThread.Post
                        (() =>
                            {
                                if (_disposed)
                                {
                                    return;
                                }

                                if (buffer[0] == 2)
                                {
                                    if (_exit is { } exit)
                                    {
                                        exit();
                                    }
                                    else
                                    {
                                        _pendingExit = true;
                                    }

                                    return;
                                }

                                if (_open is { } open)
                                {
                                    open(buffer[0] == 1);
                                }
                                else
                                {
                                    _pending = buffer[0] == 1;
                                }
                            }
                        );
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException exception)
        {
            System.Diagnostics.Trace.WriteLine(exception.GetType().Name);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _open = null;
        _exit = null;
        _pending = null;
        _shutdown.Cancel();
        // The listener never waits on the UI thread. Release the pipe before ownership.
        _listener?.GetAwaiter().GetResult();
        if (IsOwner)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
        _shutdown.Dispose();
    }
}