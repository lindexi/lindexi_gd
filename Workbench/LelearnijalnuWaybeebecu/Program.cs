using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

// 用法：dotnet run -- <域名> [端口]
// 示例：dotnet run -- model.server.lindexi.com 443
string host = args.Length >= 1 ? args[0] : "model.server.lindexi.com";
int port = args.Length >= 2 && int.TryParse(args[1], out var parsedPort) ? parsedPort : 443;

Console.WriteLine($"正在连接 {host}:{port} ...");

using var tcp = new TcpClient();
await tcp.ConnectAsync(host, port);

// 回调固定返回 true：先忽略证书校验，只为拿到服务器返回的证书
using var ssl = new SslStream
(
    tcp.GetStream(),
    leaveInnerStreamOpen: false,
    userCertificateValidationCallback: static (_, _, _, _) => true
);

await ssl.AuthenticateAsClientAsync(host);

var remote = ssl.RemoteCertificate
    ?? throw new InvalidOperationException("服务端没有返回证书。");

using var cert = new X509Certificate2(remote);

Console.WriteLine();
Console.WriteLine("===== 服务端返回的证书 =====");
Console.WriteLine($"Subject    : {cert.Subject}");
Console.WriteLine($"Issuer     : {cert.Issuer}");
Console.WriteLine($"NotBefore  : {cert.NotBefore:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"NotAfter   : {cert.NotAfter:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Serial     : {cert.SerialNumber}");
Console.WriteLine($"Thumbprint : {cert.Thumbprint}");

// Subject Alternative Name（证书里声明的域名）
var san = cert.Extensions["2.5.29.17"];
if (san is not null)
{
    Console.WriteLine($"SAN        : {san.Format(multiLine: false)}");
}

Console.WriteLine();
Console.WriteLine("===== 对比当前时间 =====");
var now = DateTime.Now;
Console.WriteLine($"当前时间   : {now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"是否已过期 : {cert.NotAfter < now}");

// 导出成 .cer 文件（DER 格式），方便双击查看或用 certutil 分析
var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
var outPath = Path.Combine(desktop, "server.cer");
File.WriteAllBytes(outPath, cert.Export(X509ContentType.Cert));
Console.WriteLine();
Console.WriteLine($"已导出证书文件: {outPath}");

// 顺便构建证书链，检查每一级的时间有效性
using var chain = new X509Chain();
chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
bool built = chain.Build(cert);

Console.WriteLine();
Console.WriteLine($"===== 证书链（构建成功: {built}） =====");
int i = 0;
foreach (var element in chain.ChainElements)
{
    var c = element.Certificate;
    Console.WriteLine($"  [{i}] {c.Subject}");
    Console.WriteLine($"        NotBefore : {c.NotBefore:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"        NotAfter  : {c.NotAfter:yyyy-MM-dd HH:mm:ss}");

    foreach (var status in element.ChainElementStatus)
    {
        Console.WriteLine($"        Status    : {status.Status}");
    }

    i++;
}