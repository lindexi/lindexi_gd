
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using JucojocuNeficawhurholee;

var url = "https://www.baidu.com";
var uri = new Uri(url);
var host = uri.Host;

Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 开始测试，目标: {url}");

// ===== SocketsHttpHandler + OpenSslStream + OpenSslSocketsHttpHandler 包装器 =====
// 包装器将 https:// 重写为 http://，使 SocketsHttpHandler.IsSecure = false，跳过二次握手。
// ConnectCallback 中 OpenSslStream 完成真实的 TLS，连接 443 端口。
Console.WriteLine($"\n--- SocketsHttpHandler + OpenSslStream (https:// 无重复握手) ---");

var innerHandler = new SocketsHttpHandler
{
    ConnectCallback = async (context, cancellationToken) =>
    {
        var innerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            // 始终连接 443（因为 scheme 已被重写为 http，context.DnsEndPoint.Port 会是 80）
            var endpoint = new DnsEndPoint(context.DnsEndPoint.Host, 443);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ConnectCallback: 开始连接 {endpoint}");
            await innerSocket.ConnectAsync(endpoint, cancellationToken);

            var openSslStream = new OpenSslStream(innerSocket, ownsSocket: true);

            var authSw = Stopwatch.StartNew();
            await openSslStream.AuthenticateAsClientAsync(new OpenSslClientAuthenticationOptions
            {
                TargetHost = context.DnsEndPoint.Host
            }, cancellationToken);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ConnectCallback: TLS 握手完成，耗时: {authSw.ElapsedMilliseconds}ms");

            return openSslStream;
        }
        catch
        {
            innerSocket.Dispose();
            throw;
        }
    }
};

using var handler = new OpenSslSocketsHttpHandler(innerHandler);
using var httpClient = new HttpClient(handler);

var sw = Stopwatch.StartNew();
var result = await httpClient.GetStringAsync(url);
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] HttpClient 请求完成，响应长度: {result.Length} 字符，耗时: {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 响应前 500 字符:\n{result[..Math.Min(500, result.Length)]}");

Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] 测试完成。");
