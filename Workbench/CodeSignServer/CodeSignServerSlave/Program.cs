// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Net.WebSockets;

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

var webSocketReceiveResult = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);

Console.WriteLine("Hello, World!");
