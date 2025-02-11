// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Sockets;

Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
listener.Listen(int.MaxValue);
var endPoint = (IPEndPoint) listener.LocalEndPoint!;
var uri = new Uri($"http://{endPoint.Address}:{endPoint.Port}/");
Console.WriteLine(uri);

while (true)
{
    Socket socket = await listener.AcceptAsync();
    await using var networkStream = new NetworkStream(socket, true);
    byte[] buffer = new byte[1024];
    int totalRead = 0;
    while (true)
    {
        int read = networkStream.Read(buffer, totalRead, buffer.Length - totalRead);
        if (read == 0) return;
        totalRead += read;
        if (buffer.AsSpan(0, totalRead).IndexOf("\r\n\r\n"u8) == -1)
        {
            if (totalRead == buffer.Length) Array.Resize(ref buffer, buffer.Length * 2);
            continue;
        }

        networkStream.Write("HTTP/1.1 200 OK\r\nDate: Sun, 05 Jul 2020 12:00:00 GMT \r\nServer: Example\r\nContent-Length: 5\r\n\r\nHello"u8);

        totalRead = 0;
    }
}

