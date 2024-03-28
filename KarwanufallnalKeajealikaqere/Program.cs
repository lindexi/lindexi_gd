// See https://aka.ms/new-console-template for more information

using System.Buffers;
using Pipelines.Sockets.Unofficial;

using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

var availablePort = GetAvailablePort(IPAddress.Loopback);
var endPoint = new IPEndPoint(IPAddress.Loopback, availablePort);

// 创建服务端
using var fooSocketServer = new FooSocketServer();
fooSocketServer.Listen(endPoint);

Console.Read();

var socketConnection = await SocketConnection.ConnectAsync(endPoint);

// 发送消息
// 从 PipeWriter 里面获取一个 Memory 对象，用来写入数据
var memory = socketConnection.Output.GetMemory(1024);
// 将字符串编码成字节，写入 Memory 对象
var length = Encoding.UTF8.GetBytes("这是来自客户端的消息".AsSpan(), memory.Span);
// 标记已写入的数据的长度
socketConnection.Output.Advance(length);
// 将写入的数据发送出去
await socketConnection.Output.FlushAsync();

// 从 PipeReader 里面读取数据
var readResult = await socketConnection.Input.ReadAsync();
Console.WriteLine($"[客户端] 收到服务端端回复的 {Encoding.UTF8.GetString(readResult.Buffer)}");
// 标记已处理的数据的长度，下次读取的时候会跳过这些数据
socketConnection.Input.AdvanceTo(readResult.Buffer.End);

while (true)
{
    // 从控制台读取输入，将输入的内容发送给服务端
    var line = Console.ReadLine();
    // 重新从 PipeWriter 里面获取一个 Memory 对象，用来写入数据。不能用之前的 Memory 对象，因为之前的 Memory 对象已经被标记为已写入的数据
    // https://learn.microsoft.com/en-us/dotnet/api/system.io.pipelines.pipewriter.getmemory
    // You must request a new buffer after calling Advance to continue writing more data; you cannot write to a previously acquired buffer.
    memory = socketConnection.Output.GetMemory(1024);
    length = Encoding.UTF8.GetBytes(line.AsSpan(), memory.Span);
    socketConnection.Output.Advance(length);
    var flushResult = await socketConnection.Output.FlushAsync();
    if (flushResult.IsCompleted)
    {
        break;
    }

    readResult = await socketConnection.Input.ReadAsync();
    Console.WriteLine($"[客户端] 收到服务端端回复的 {Encoding.UTF8.GetString(readResult.Buffer)}");
    socketConnection.Input.AdvanceTo(readResult.Buffer.End);
}

Console.Read();

static int GetAvailablePort(IPAddress ip)
{
    using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
    socket.Bind(new IPEndPoint(ip, 0));
    socket.Listen(1);
    var ipEndPoint = (IPEndPoint) socket.LocalEndPoint!;
    var port = ipEndPoint.Port;
    return port;
}

class FooSocketServer : SocketServer
{
    protected override Task OnClientConnectedAsync(in ClientConnection client)
    {
        Console.WriteLine($"收到客户端 {client.RemoteEndPoint} 连接");
        return DoFooAsync(client);
    }

    private async Task DoFooAsync(ClientConnection client)
    {
        for (int i = 0; i < int.MaxValue; i++)
        {
            var readResult = await client.Transport.Input.ReadAsync();

            var inputText = Encoding.UTF8.GetString(readResult.Buffer);
            Console.WriteLine($"[服务端] 收到客户端发送的 {inputText}");

            var memory = client.Transport.Output.GetMemory(1024);
            var length = Encoding.UTF8.GetBytes($"{i} 这是来自服务端的消息".AsSpan(), memory.Span);
            client.Transport.Output.Advance(length);
            await client.Transport.Output.FlushAsync();

            // 标记已处理的数据
            client.Transport.Input.AdvanceTo(readResult.Buffer.End);

            if (readResult.IsCompleted)
            {
                break;
            }
        }
    }
}