// See https://aka.ms/new-console-template for more information

using CodeSignServerMaster.Contexts;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

{
    Task.Run(async () =>
    {
        using var httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(100)
        };

        var signTaskRequest = new SignTaskRequest("abc", "http://www.lindexi.com/ChachawferceFalacerewhine", "lindexi");
        using var httpResponseMessage = await httpClient.PostAsJsonAsync($"http://{host}/signapp", signTaskRequest);

        var signTaskResponse = await httpResponseMessage.Content.ReadFromJsonAsync<SignTaskResponse>();
        Debug.Assert(signTaskResponse is not null, nameof(signTaskResponse) + " != null");
        Console.WriteLine($"TraceId:{signTaskResponse.TraceId} FileUrl:{signTaskResponse.FileUrl} Message:{signTaskResponse.Message}");
    });

    while (true)
    {
        try
        {
            using var clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
            await clientWebSocket.ConnectAsync(new Uri($"ws://{host}/fetch"), CancellationToken.None);

            var signSlaveInfo = new SignSlaveInfo($"签名服务器 {GetCurrentIp()}");
            var signSlaveInfoContent = JsonSerializer.SerializeToUtf8Bytes(signSlaveInfo);
            await clientWebSocket.SendAsync(signSlaveInfoContent, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);

            var buffer = ArrayPool<byte>.Shared.Rent(102400);

            try
            {
                var webSocketReceiveResult = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (webSocketReceiveResult.EndOfMessage)
                {
                    // 处理完整消息
                    var content = buffer.AsSpan(0, webSocketReceiveResult.Count);
                    var fetchSignTaskRequest = JsonSerializer.Deserialize<FetchSignTaskRequest>(content);

                    SignTaskResponse? response = null;
                    if (fetchSignTaskRequest?.SignTaskRequest is not null)
                    {
                        Console.WriteLine($"TraceId:{fetchSignTaskRequest.SignTaskRequest.TraceId} FileUrl:{fetchSignTaskRequest.SignTaskRequest.FileUrl} SignName:{fetchSignTaskRequest.SignTaskRequest.SignName} Message:{fetchSignTaskRequest.Message}");

                        response = new SignTaskResponse(fetchSignTaskRequest.SignTaskRequest.TraceId, "http://blog.lindexi.com/Sign", $"签名成功，由 {GetCurrentIp()} 服务器签名");
                    }
                    else
                    {
                        // 空任务，回复空白礼貌
                    }

                    var fetchSignTaskResponse = new FetchSignTaskResponse(response);
                    var responseContent = JsonSerializer.SerializeToUtf8Bytes(fetchSignTaskResponse);
                    await clientWebSocket.SendAsync(responseContent, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            break;
        }
    }
}

{

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

}

Console.WriteLine("Hello, World!");

string GetCurrentIp()
{
    var stringBuilder = new StringBuilder();
    foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
    {
        if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
        {
            continue;
        }

        if (networkInterface.Supports(NetworkInterfaceComponent.IPv4))
        {
            var ipInterfaceProperties = networkInterface.
                GetIPProperties();
            foreach (var unicastIpAddressInformation in ipInterfaceProperties.UnicastAddresses)
            {
                if (unicastIpAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var address = unicastIpAddressInformation.Address.ToString();
                    stringBuilder.Append(address)
                        .Append(';');
                }
            }
        }
    }

    return stringBuilder.ToString();
}