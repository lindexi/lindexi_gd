// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using CodeSignServerMaster.Contexts;

Task.Run(async () =>
{
    var httpClient = new HttpClient();
    var file = @"C:\lindexi\文本库编辑保存卡住 devenv.DMP";
    using var fileStream = File.OpenRead(file);
    var httpRequestMessage = new HttpRequestMessage()
    {
        Method = HttpMethod.Post,
        Content = new StreamContent(fileStream),
        RequestUri = new Uri("http://127.0.0.1:5073/sign")
    };
    try
    {
        using var httpResponseMessage =
            await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
        await using var responseStream = await httpResponseMessage.Content.ReadAsStreamAsync();
        var outputFile = "output";
        await using var outputStream = File.Create(outputFile);
        await responseStream.CopyToAsync(outputStream);
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
await clientWebSocket.ConnectAsync(new Uri("ws://127.0.0.1:5073/task"), CancellationToken.None);

byte[] t = "Hello, World!"u8.ToArray();
await clientWebSocket.SendAsync(t, WebSocketMessageType.Binary, true, CancellationToken.None);

var buffer = ArrayPool<byte>.Shared.Rent(1024);

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
            };

            var memory = buffer.AsMemory(0, responseMessageType.HeadLength);
            memory.Span.Clear();
            MemoryMarshal.Write(memory.Span, in responseMessageType);

            await clientWebSocket.SendAsync(memory, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
        }

        break;
    }
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}


Console.WriteLine("Hello, World!");
