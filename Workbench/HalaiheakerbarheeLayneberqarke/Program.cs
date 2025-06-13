// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

// 一个域名可以有多个 IP 地址，利用此特性，可以实现 IP 级域名备份，也能利用此特性实现寻找距离自己最近的 IP 地址
IPAddress[] ipAddresses = await Dns.GetHostAddressesAsync("www.baidu.com");

// 从共享池中租用一个字节数组，大小为 1MB
// 网络请求过程中也是可以做到低分配的，合理使用内存池可以减少 GC 压力
var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);

try
{
    foreach (var ipAddress in ipAddresses)
    {
        Console.WriteLine($"开始连接 IP:{ipAddress}");

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(ipAddress, 80);

        using var networkStream = new NetworkStream(socket);

        Console.WriteLine($"连接完成，开始发送请求");

        // 这里的请求内容是一个简单的 HTTP GET 请求，注：后面要带两个换行哦
        ReadOnlySpan<byte> content = """
                                     GET https://www.baidu.com HTTP/1.1
                                     Host: www.baidu.com


                                     """u8;
        content.CopyTo(buffer); // 将请求内容复制到租用的字节数组中。异步请求不能传入 Span 类型，只能传入 Memory 类型。将 Span 转换为 Memory 的方式是先写入到 buffer 中，然后再将其当成 ReadOnlyMemory 或 Memory 类型
        ReadOnlyMemory<byte> writeBuffer = buffer.AsMemory(0, content.Length);
        await networkStream.WriteAsync(writeBuffer);

        // 读取响应内容
        var length = await networkStream.ReadAsync(buffer); // 注： 很多时候，这里都是没有完全读取到完整的响应内容的，可能需要多次读取才能获取完整的响应内容。读取多长的数据需要从返回的 Header 里面获取 Content-Length 字段的值
        var text = Encoding.UTF8.GetString(buffer, 0, length);

        Console.WriteLine($"收到百度的响应内容。内容长度 {length}，内容摘要：{text.Substring(0, Math.Min(50, text.Length))}...");

        Console.WriteLine();
    }
}
finally
{
    // 别忘了归还共享池中的字节数组
    ArrayPool<byte>.Shared.Return(buffer);
}

// 以下是进行 https 请求的代码
// 上面的缓存已经被归还了，为了继续使用，就开始重新申请好了
buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);

try
{
    foreach (var ipAddress in ipAddresses)
    {
        Console.WriteLine($"开始连接 IP:{ipAddress}");

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(ipAddress, 443); // 443 是 HTTPS 的默认端口

        using var networkStream = new NetworkStream(socket);

        Console.WriteLine($"连接完成，开始进行 https 通讯");

        // 进行 SSL/TLS 握手，使用 SslStream 包装 NetworkStream 然后进行认证
        // 接下来后续的所有读写操作都将通过 SslStream 进行
        using var sslStream = new SslStream(networkStream);
        await sslStream.AuthenticateAsClientAsync("www.baidu.com");

        Console.WriteLine($"开始发送请求");

        // 这里的请求内容是一个简单的 HTTP GET 请求，注：后面要带两个换行哦
        ReadOnlySpan<byte> content = """
                                     GET https://www.baidu.com HTTP/1.1
                                     Host: www.baidu.com


                                     """u8;
        content.CopyTo(buffer); // 将请求内容复制到租用的字节数组中。异步请求不能传入 Span 类型，只能传入 Memory 类型。将 Span 转换为 Memory 的方式是先写入到 buffer 中，然后再将其当成 ReadOnlyMemory 或 Memory 类型
        ReadOnlyMemory<byte> writeBuffer = buffer.AsMemory(0, content.Length);
        await sslStream.WriteAsync(writeBuffer); // 这里要用 SslStream 来发送请求内容

        // 读取响应内容
        var length = await sslStream.ReadAsync(buffer); // 这里要用 SslStream 来读取响应内容
        var text = Encoding.UTF8.GetString(buffer, 0, length);

        Console.WriteLine($"收到百度的响应内容。内容长度 {length}，内容摘要：{text.Substring(0, Math.Min(50, text.Length))}...");

        Console.WriteLine();
    }
}
finally
{
    // 别忘了归还共享池中的字节数组
    ArrayPool<byte>.Shared.Return(buffer);
}