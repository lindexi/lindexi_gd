
using System.Diagnostics;
using JucojocuNeficawhurholee;

var url = "https://www.baidu.com";

Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 开始测试，目标: {url}");

// ===== OpenSslSocketsHttpHandler：https:// 无重复握手 =====
// 内部将 https:// 重写为 http:// 并用 HttpRequestMessage.Options 标记，
// ConnectCallback 根据标记决定是否走 OpenSslStream TLS。
Console.WriteLine($"\n--- OpenSslSocketsHttpHandler (https:// 无重复握手) ---");

using var handler = new OpenSslSocketsHttpHandler();
using var httpClient = new HttpClient(handler);

var sw = Stopwatch.StartNew();
var result = await httpClient.GetStringAsync(url);
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] HttpClient 请求完成，响应长度: {result.Length} 字符，耗时: {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 响应前 500 字符:\n{result[..Math.Min(500, result.Length)]}");

Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] 测试完成。");
