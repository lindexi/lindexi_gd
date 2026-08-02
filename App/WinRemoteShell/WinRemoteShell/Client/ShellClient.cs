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
            var content = Encoding.UTF8.GetBytes(line);
            await webSocket.SendAsync(content, WebSocketMessageType.Text, true, cancellationToken);
            if (string.Equals(line.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }
    }

    private static async Task ReceiveAsync(ClientWebSocket webSocket, TextWriter output, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
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

    private static Uri ToWebSocketUri(Uri server, string path)
    {
        var builder = new UriBuilder(new Uri(server, path))
        {
            Scheme = server.Scheme == Uri.UriSchemeHttps ? "wss" : "ws"
        };
        return builder.Uri;
    }
}
