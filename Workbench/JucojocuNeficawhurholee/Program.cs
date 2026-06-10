
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using JucojocuNeficawhurholee;

var url = "https://www.baidu.com";
var uri = new Uri(url);
var host = uri.Host;
var port = uri.Port > 0 ? uri.Port : 443;

Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 开始测试，目标: {url}");

//// ===== 方案 A：直接使用 OpenSslStream（Socket 模式）=====
//Console.WriteLine($"\n--- 方案 A: OpenSslStream (Socket 模式) ---");

//var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

var sw = Stopwatch.StartNew();
//await socket.ConnectAsync(host, port);
//Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] TCP 连接完成，耗时: {sw.ElapsedMilliseconds}ms");

//await using var sslStream = new OpenSslStream(socket, ownsSocket: true);

//sw.Restart();
//await sslStream.AuthenticateAsClientAsync(new OpenSslClientAuthenticationOptions
//{
//    TargetHost = host
//});
//Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] TLS 握手完成，耗时: {sw.ElapsedMilliseconds}ms");

//// 构造 HTTP/1.1 请求
//var httpRequest = $"GET / HTTP/1.1\r\nHost: {host}\r\nConnection: close\r\n\r\n";
//var requestBytes = Encoding.UTF8.GetBytes(httpRequest);

//sw.Restart();
//await sslStream.WriteAsync(requestBytes, 0, requestBytes.Length);

//// 读取响应
//using var responseStream = new MemoryStream();
//var readBuffer = new byte[4096];
//int bytesRead;
//while ((bytesRead = await sslStream.ReadAsync(readBuffer, 0, readBuffer.Length)) > 0)
//{
//    responseStream.Write(readBuffer, 0, bytesRead);
//}

//var responseBytes = responseStream.ToArray();
//Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 响应读取完成，共 {responseBytes.Length} 字节，耗时: {sw.ElapsedMilliseconds}ms");

//var responseText = Encoding.UTF8.GetString(responseBytes);
//Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 响应前 500 字符:\n{responseText[..Math.Min(500, responseText.Length)]}");

// ===== 方案 B：通过 SocketsHttpHandler.ConnectCallback 使用 OpenSslStream =====
// 注意：SocketsHttpHandler 对 https:// URL 会在 ConnectCallback 返回的 Stream 上再次进行 TLS 握手。
// 因此这里使用 http:// URL，在 ConnectCallback 中手动完成 TLS 连接。
Console.WriteLine($"\n--- 方案 B: SocketsHttpHandler + OpenSslStream ---");

var handler = new SocketsHttpHandler
{
    ConnectCallback = async (context, cancellationToken) =>
    {
        var innerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            // 手动指定 HTTPS 端口 443
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

// 使用 http:// URL 避免 SocketsHttpHandler 重复 TLS 握手
var httpClient = new HttpClient(handler);
sw.Restart();
var result = await httpClient.GetStringAsync($"http://{host}/");
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] HttpClient 请求完成，响应长度: {result.Length} 字符，耗时: {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 响应前 500 字符:\n{result[..Math.Min(500, result.Length)]}");

Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] 测试完成。");