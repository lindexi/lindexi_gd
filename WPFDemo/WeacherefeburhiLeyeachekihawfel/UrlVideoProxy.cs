using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;

namespace WeacherefeburhiLeyeachekihawfel;

class UrlVideoProxy : IDisposable
{
    public UrlVideoProxy(Uri originVideoUrl)
    {
        _originVideoUrl = originVideoUrl;
        TcpService service = new TcpService();
        _tcpService = service;
        _tcpService.Connected += OnConnected;

        _tcpService.Received += OnReceived;
    }

    private async Task OnReceived(TcpSessionClient client, ReceivedDataEventArgs e)
    {
        var text = e.ByteBlock.Span.ToString(Encoding.UTF8);
        /*
           正常收到的 text 的内容大概如下
           GET / HTTP/1.1
           Accept: * /*
           User-Agent: Windows-Media-Player/12.0.26100.4202
           UA-CPU: AMD64
           Accept-Encoding: gzip, deflate
           Host: 127.0.0.1:55004
           Connection: Keep-Alive


         */

        using var socketsHttpHandler = new SocketsHttpHandler();
        socketsHttpHandler.SslOptions = new SslClientAuthenticationOptions()
        {
            RemoteCertificateValidationCallback = delegate
            {
                // 这里可以验证证书，或者直接返回 true 跳过验证
                return true;
            }
        };
        using var httpClient = new HttpClient(socketsHttpHandler);

        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);

        try
        {
            using var httpResponseMessage = await httpClient.GetAsync(_originVideoUrl, HttpCompletionOption.ResponseHeadersRead);
            var contentLength = httpResponseMessage.Content.Headers.ContentLength ??0;

            var responseText = $"""
                                HTTP/1.1 200 OK
                                Content-Length: {contentLength}
                                Date: Fri, 13 Jun 2025 08:05:06 GMT
                                ETag: "1d97660a4ebe020"
                                Last-Modified: Mon, 24 Apr 2023 03:55:44 GMT
                                Server: Kestrel


                                """;
            var responseBytes = Encoding.ASCII.GetBytes(responseText);
            await client.SendAsync(responseBytes);

            using var stream = await httpResponseMessage.Content.ReadAsStreamAsync();
            while (true)
            {
                var readLength = await stream.ReadAsync(buffer);
                if (readLength < 0)
                {
                    break;
                }

                try
                {
                    await client.SendAsync(buffer.AsMemory(0, readLength));
                }
                catch (SocketException socketException)
                {
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private readonly Uri _originVideoUrl;

    public async Task<string> StartAsync()
    {
        var port = GetIpV4LocalLoopbackAvailablePort();

        await _tcpService.StartAsync(new IPHost(IPAddress.Loopback, port));

        var url = $"http://127.0.0.1:{port}";
        return url;
    }

    private async Task OnConnected(TcpSessionClient client, ConnectedEventArgs e)
    {
        return;

        using var receiver = client.CreateReceiver();
        receiver.MaxCacheSize = 100;
        //var receiverResult = await receiver.ReadAsync(CancellationToken.None);
        //var text = receiverResult.ByteBlock.ReadString(FixedHeaderType.Byte);

        /*
           正常收到的 text 的内容大概如下
           ET / HTTP/1.1
           Accept: * /*
           User-Agent: Windows-Media-Player/12.0.26100
         */

        //client.Closed += (sessionClient, args) =>
        //{
        //    // 一般一次性就差不多了，如果会多次使用，那就不要这段代码了
        //    Dispose();
        //    return Task.CompletedTask;
        //};

        using var socketsHttpHandler = new SocketsHttpHandler();
        socketsHttpHandler.SslOptions = new SslClientAuthenticationOptions()
        {
            RemoteCertificateValidationCallback = delegate
            {
                // 这里可以验证证书，或者直接返回 true 跳过验证
                return true;
            }
        };
        using var httpClient = new HttpClient(socketsHttpHandler);

        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);

        try
        {
            var responseText = """
                               HTTP/1.1 200 OK
                               Accept-Ranges: bytes
                               Content-Length: 320000
                               Date: Fri, 13 Jun 2025 08:05:06 GMT
                               ETag: "1d97660a4ebe020"
                               Last-Modified: Mon, 24 Apr 2023 03:55:44 GMT
                               Server: Kestrel


                               """;
            var responseBytes = Encoding.ASCII.GetBytes(responseText);
            await client.SendAsync(responseBytes);

            using var stream = await httpClient.GetStreamAsync(_originVideoUrl);

            while (true)
            {
                var readLength = await stream.ReadAsync(buffer);
                if (readLength < 0)
                {
                    break;
                }

                try
                {
                    await client.SendAsync(buffer.AsMemory(0, readLength));
                }
                catch (SocketException socketException)
                {
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private readonly TcpService _tcpService;

    private static int GetIpV4LocalLoopbackAvailablePort() => GetAvailablePort(IPAddress.Loopback);

    private static int GetAvailablePort(IPAddress ip)
    {
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(ip, 0));
        socket.Listen(1);
        var ipEndPoint = (IPEndPoint) socket.LocalEndPoint!;
        var port = ipEndPoint.Port;
        return port;
    }

    public void Dispose()
    {
        _tcpService.Dispose();
    }
}