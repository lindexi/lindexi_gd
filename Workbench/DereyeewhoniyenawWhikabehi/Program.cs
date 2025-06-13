// See https://aka.ms/new-console-template for more information

using System.Net;

// 实验，域名的 IP 顺序是不保证的
// 我在 godaddy 配置我的 foo.lindexi.com 域名包含 5 个 IP 地址
// 在经过 ipconfig /flushdns 之后，拿到的 Dns.GetHostAddressesAsync 返回的值是随机的

var ipAddresses = await Dns.GetHostAddressesAsync("foo.lindexi.com");
foreach (var ipAddress in ipAddresses)
{
    Console.WriteLine(ipAddress);
}

// 调用 ipconfig /flushdns 之后会清空 DNS 缓存，重新解析域名。此时返回的顺序可能会不同

Console.WriteLine("Hello, World!");

// 这就意味着如果做 CDN 网络通讯的话，访问的 IP 的顺序是不保证的，也许程序底层自己做，也许靠 ipconfig /flushdns 刷新，都可以

var socketsHttpHandler = new SocketsHttpHandler()
{
    EnableMultipleHttp2Connections = true,
    ConnectCallback = (context, token) =>
    {
        throw null;
    }
};

var httpClient = new HttpClient(socketsHttpHandler);
await httpClient.GetAsync("https://foo.lindexi.com");