// See https://aka.ms/new-console-template for more information

using CodeSignServerMaster.Contexts;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

var host = "127.0.0.1:57562";
var configurationFile = @"C:\lindexi\Sign.coin";
if (File.Exists(configurationFile))
{
    var configurationHost = File.ReadAllText(configurationFile).Trim();
    if (!string.IsNullOrEmpty(configurationHost))
    {
        host = configurationHost;
    }
}

var manualResetEventSlim = new ManualResetEventSlim(false);

Task.Run(async () =>
{
    manualResetEventSlim.Wait();

    using var httpClient = new HttpClient()
    {
        Timeout = TimeSpan.FromMinutes(100)
    };
    var file = @"C:\lindexi\App.dmp";
    using var fileStream = File.OpenRead(file);
    var httpRequestMessage = new HttpRequestMessage()
    {
        Method = HttpMethod.Post,
        Content = new StreamContent(fileStream),
        RequestUri = new Uri($"http://{host}/sign")
    };

    try
    {
        using var httpResponseMessage =
            await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            await using var responseStream = await httpResponseMessage.Content.ReadAsStreamAsync();
            var outputFile = "output";
            await using var outputStream = File.Create(outputFile);
            await responseStream.CopyToAsync(outputStream);
        }
        else
        {
            var errorMessage = await httpResponseMessage.Content.ReadAsStringAsync();
            Console.WriteLine($"请求签名失败 StatusCode:{httpResponseMessage.StatusCode} ErrorMessage:{errorMessage}");
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
});

//WebSocket.CreateFromStream()

var clientWebSocket = new ClientWebSocket();
clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
await clientWebSocket.ConnectAsync(new Uri($"ws://{host}/task"), CancellationToken.None);

manualResetEventSlim.Set();

byte[] t = "Hello, World!"u8.ToArray();
await clientWebSocket.SendAsync(t, WebSocketMessageType.Binary, true, CancellationToken.None);

var buffer = ArrayPool<byte>.Shared.Rent(102400);

try
{
    while (clientWebSocket.State == WebSocketState.Open)
    {
        var webSocketReceiveResult = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
        var content = buffer.AsSpan(0, webSocketReceiveResult.Count);
        var messageType = MemoryMarshal.Read<MessageType>(content);

        if (messageType.Type == 1)
        {
            // 心跳消息
            MessageType responseMessageType = new MessageType()
            {
                Type = 3,
                Header = MessageType.DefaultHeader
            };

            var memory = buffer.AsMemory(0, responseMessageType.HeadLength);
            memory.Span.Clear();
            MemoryMarshal.Write(memory.Span, in responseMessageType);

            await clientWebSocket.SendAsync(memory, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
        }
        else if (messageType.Type == 2)
        {
            // 完全读取
            while (true)
            {
                webSocketReceiveResult = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                content = buffer.AsSpan(0, webSocketReceiveResult.Count);
                if (webSocketReceiveResult.EndOfMessage)
                {
                    break;
                }
            }

            await clientWebSocket.SendAsync(buffer, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
        }
    }
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}


Console.WriteLine("Hello, World!");
