// See https://aka.ms/new-console-template for more information

using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Security.Cryptography;
using Renci.SshNet.Security.Cryptography.Ciphers;


var socketsHttpHandler = new SocketsHttpHandler()
{
    SslOptions =
    {

    },
    ConnectCallback = async (context, token) =>
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        try
        {
            socket.NoDelay = true;

            // 这里可以自己偷偷改掉域名哦，也就是将原本请求的域名修改为一个奇怪的域名。这里偷偷改了，团队的其他伙伴可是很难调试出来的哦
            await socket.ConnectAsync(context.DnsEndPoint, token)
                // 配置异步等待后不需要回到原来的线程
                .ConfigureAwait(false);

            // 发送的超时时间，相当于请求的超时
            socket.SendTimeout = (int) TimeSpan.FromSeconds(10).TotalMilliseconds;
            // 接收的超时时间，相当于响应的超时
            socket.ReceiveTimeout = (int) TimeSpan.FromSeconds(5).TotalMilliseconds;
        }
        catch
        {
            socket.Dispose();
            throw;
        }

        // 在 NetworkStream 里，设置 ownsSocket 参数为 true 将会在 NetworkStream 被释放的时候，自动释放 Socket 资源
        var networkStream = new F(socket, ownsSocket: true);
        return networkStream;
    },
};



var sslClientAuthenticationOptions = socketsHttpHandler.SslOptions;

sslClientAuthenticationOptions.ApplicationProtocols = new List<SslApplicationProtocol>()
{
    SslApplicationProtocol.Http3,
    SslApplicationProtocol.Http2,
    SslApplicationProtocol.Http11,
};
sslClientAuthenticationOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
{
    return true;
};
//sslClientAuthenticationOptions.CipherSuitesPolicy = new CipherSuitesPolicy(new[]
//{
//    TlsCipherSuite.TLS_AES_128_GCM_SHA256
//});


var httpClient = new HttpClient(socketsHttpHandler);
var response = await httpClient.GetAsync("https://www.baidu.com");
var result = await response.Content.ReadAsStringAsync();
Console.WriteLine(result);

while (true)
{
    Console.Read();
}


class F: NetworkStream
{
    public F(Socket socket) : base(socket)
    {
    }

    public F(Socket socket, bool ownsSocket) : base(socket, ownsSocket)
    {
    }

    public F(Socket socket, FileAccess access) : base(socket, access)
    {
    }

    public F(Socket socket, FileAccess access, bool ownsSocket) : base(socket, access, ownsSocket)
    {
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return base.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return base.BeginWrite(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return base.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        base.EndWrite(asyncResult);
    }

    public override int ReadByte()
    {
        return base.ReadByte();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return base.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        return base.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return base.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await base.ReadAsync(buffer, cancellationToken);

        var s = Encoding.UTF8.GetString(buffer.Span[..result]);

        if (_pipeStream is null)
        {
            _pipeStream = new PipeStream();
            _task = Task.Run(async () =>
            {
                using var sslStream = new SslStream(_pipeStream, true, (sender, certificate, chain, errors) =>
                {
                    return true;
                });

                sslStream.AuthenticateAsClient("www.baidu.com");

                while (!_start)
                {
                    await Task.Delay(100);
                }

                var streamReader = new StreamReader(sslStream);
                s = streamReader.ReadToEnd();
            });
        }
        else
        {
            _pipeStream.Write(buffer.Span[..result]);
        }

        return result;
    }

    private PipeStream? _pipeStream;
    private Task? _task;
    private bool _start = false;
}