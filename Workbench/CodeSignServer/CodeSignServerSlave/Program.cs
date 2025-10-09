// See https://aka.ms/new-console-template for more information

using System.Net.WebSockets;

var httpClient = new HttpClient();

//WebSocket.CreateFromStream()

var clientWebSocket = new ClientWebSocket();
await clientWebSocket.ConnectAsync(new Uri("ws://127.0.0.1:5073/sign"),CancellationToken.None);

ReadOnlyMemory<byte> t = new byte[] { 0x12, 0x13 };
await clientWebSocket.SendAsync(t, WebSocketMessageType.Binary, true, CancellationToken.None);

Console.WriteLine("Hello, World!");
