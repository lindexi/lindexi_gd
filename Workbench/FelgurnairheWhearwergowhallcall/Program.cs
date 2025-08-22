// See https://aka.ms/new-console-template for more information

using System.Net.Sockets;

var unixDomainSocketEndPoint = new UnixDomainSocketEndPoint("\0lindexi");

_ = Task.Run(async () =>
{
    Console.WriteLine($"按下回车，开始发送内容");
    Console.ReadLine();

    var socket2 = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
    var buffer2 = new byte[150];
    buffer2.AsSpan().Fill(0x02);
    // 尝试发送 150 长度过去，看能否做两次读取
    await socket2.SendToAsync(buffer2, unixDomainSocketEndPoint);

    await Task.Delay(1000); // 等待一段时间，确保数据发送完成
    Console.WriteLine($"第一段发送完成，按下回车开始发送第二段内容");
    Console.ReadLine();
    // 测试中，每次只能读取 100 长度的数据。如果在按下第一次回车之后发送，只能读取到 100 长度的数据，则证明是一发一收，超过则丢失。第二次回车之后，发现能够继续读取到数据，但数据内容是 0x06 即第二次发送的内容，第一次发送内容剩余的 50 长度没有被读取，这证明了 Dgram 是一收一发的
    buffer2.AsSpan().Fill(0x06);
    await socket2.SendToAsync(buffer2, unixDomainSocketEndPoint);
});

var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
socket.Bind(unixDomainSocketEndPoint);

var buffer = new byte[100];
// 开始接收时，炸掉
/*
Unhandled exception. System.Net.Sockets.SocketException (22): Invalid argument
   at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.CreateException(SocketError error, Boolean forAsyncThrow)
   at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.ReceiveAsync(Socket socket, CancellationToken cancellationToken)
   at System.Net.Sockets.Socket.ReceiveAsync(Memory`1 buffer, SocketFlags socketFlags, Boolean fromNetworkStream, CancellationToken cancellationToken)
   at System.Net.Sockets.Socket.ReceiveAsync(ArraySegment`1 buffer, SocketFlags socketFlags, Boolean fromNetworkStream)
 */
var length = await socket.ReceiveAsync(buffer);

Console.WriteLine($"读取到的长度 {length} {buffer[0]:X2}");

// 测试 Dgram 是否真的是一收一发的，再次读取看能否读取到数据
length = await socket.ReceiveAsync(buffer);
Console.WriteLine($"二次读取的内容 {length} {buffer[0]:X2}");

Console.WriteLine("Hello, World!");
