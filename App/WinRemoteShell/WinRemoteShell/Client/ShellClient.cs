using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace WinRemoteShell.Client;

public static class ShellClient
{
    public static async Task RunAsync(Uri server, TextReader input, TextWriter output, CancellationToken cancellationToken = default)
    {
        using var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(ToWebSocketUri(server, "shell"), cancellationToken);
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sendTask = SendAsync(webSocket, input, cancellationSource.Token);
        var receiveTask = ReceiveAsync(webSocket, output, cancellationSource.Token);
        await sendTask;
        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        await receiveTask;
    }

    private static async Task SendAsync(ClientWebSocket webSocket, TextReader input, CancellationToken cancellationToken)
    {
        while (await input.ReadLineAsync(cancellationToken) is { } line)
        {
            var byteCount = Encoding.UTF8.GetByteCount(line);
            var content = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var bytesWritten = Encoding.UTF8.GetBytes(line, content);
                await webSocket.SendAsync(content.AsMemory(0, bytesWritten), WebSocketMessageType.Text, true, cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(content);
            }

            if (string.Equals(line.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
    }

    private static async Task ReceiveAsync(ClientWebSocket webSocket, TextWriter output, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                await output.WriteAsync(Encoding.UTF8.GetString(buffer, 0, result.Count));
                await output.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static Uri ToWebSocketUri(Uri server, string path)
    {
        var builder = new UriBuilder(new Uri(server, path))
        {
            Scheme = server.Scheme == Uri.UriSchemeHttps ? "wss" : "ws"
        };
        return builder.Uri;
    }
}
