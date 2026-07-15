using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageViewer;

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private const int Port = 38947;
    private const string Prefix = "ImageViewerPath:";

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private TcpListener? _listener;
    private Task? _listenTask;

    public static bool TrySendToExistingInstance(string[] args)
    {
        var path = GetStartupPath(args) ?? string.Empty;

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, Port);
            if (!connectTask.Wait(TimeSpan.FromMilliseconds(300)) || !client.Connected)
            {
                return false;
            }

            using var stream = client.GetStream();
            var payload = Encoding.UTF8.GetBytes(Prefix + path);
            stream.Write(payload, 0, payload.Length);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    public bool TryStart(Action<string> openPath)
    {
        ArgumentNullException.ThrowIfNull(openPath);

        try
        {
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();
            _listenTask = Task.Run(() => ListenAsync(openPath, _cancellationTokenSource.Token));
            return true;
        }
        catch (SocketException)
        {
            _listener = null;
            return false;
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _listener?.Stop();
        _cancellationTokenSource.Dispose();
    }

    private static string? GetStartupPath(string[] args)
    {
        foreach (var argument in args)
        {
            if (!string.IsNullOrWhiteSpace(argument))
            {
                return argument;
            }
        }

        return null;
    }

    private async Task ListenAsync(Action<string> openPath, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, openPath, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                client?.Dispose();
                break;
            }
            catch (ObjectDisposedException)
            {
                client?.Dispose();
                break;
            }
            catch (SocketException)
            {
                client?.Dispose();
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient client, Action<string> openPath, CancellationToken cancellationToken)
    {
        using (client)
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8, leaveOpen: false);
            var message = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            if (message.StartsWith(Prefix, StringComparison.Ordinal))
            {
                var path = message[Prefix.Length..];
                openPath(path);
            }
        }
    }
}
