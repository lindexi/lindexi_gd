using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace RdpProxyService;

internal sealed class RdpProxyWorker(ILogger<RdpProxyWorker> logger) : BackgroundService
{
    private const int ListenPort = 33890;
    private const int TargetPort = 3389;
    private readonly ConcurrentDictionary<int, Task> _connections = [];
    private int _connectionId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, ListenPort);
        listener.Start();
        logger.LogInformation("RDP 代理已启动，正在将 0.0.0.0:{ListenPort} 转发至 127.0.0.1:{TargetPort}", ListenPort, TargetPort);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                var connectionId = Interlocked.Increment(ref _connectionId);
                var connectionTask = HandleConnectionAsync(connectionId, client, stoppingToken);
                _connections.TryAdd(connectionId, connectionTask);
                _ = connectionTask.ContinueWith
                (
                    static (_, state) =>
                    {
                        var (connections, id) = ((ConcurrentDictionary<int, Task>, int))state!;
                        connections.TryRemove(id, out _);
                    },
                    (_connections, connectionId),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                );
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            listener.Stop();
            await Task.WhenAll(_connections.Values).ConfigureAwait(false);
        }
    }

    private async Task HandleConnectionAsync(int connectionId, TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        using (var target = new TcpClient())
        {
            try
            {
                await target.ConnectAsync(IPAddress.Loopback, TargetPort, cancellationToken).ConfigureAwait(false);
                logger.LogInformation
                    ("连接 {ConnectionId} 已建立：{RemoteEndpoint}", connectionId, client.Client.RemoteEndPoint);

                await using var clientStream = client.GetStream();
                await using var targetStream = target.GetStream();
                using var connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var clientToTarget = CopyStreamAsync(clientStream, targetStream, connectionCancellation.Token);
                var targetToClient = CopyStreamAsync(targetStream, clientStream, connectionCancellation.Token);
                await Task.WhenAny(clientToTarget, targetToClient).ConfigureAwait(false);
                await connectionCancellation.CancelAsync().ConfigureAwait(false);

                try
                {
                    await Task.WhenAll(clientToTarget, targetToClient).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (connectionCancellation.IsCancellationRequested)
                {
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (SocketException exception)
            {
                logger.LogWarning(exception, "连接 {ConnectionId} 的 RDP 转发失败", connectionId);
            }
            catch (IOException exception)
            {
                logger.LogDebug(exception, "连接 {ConnectionId} 已断开", connectionId);
            }
        }
    }

    private static async Task CopyStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}