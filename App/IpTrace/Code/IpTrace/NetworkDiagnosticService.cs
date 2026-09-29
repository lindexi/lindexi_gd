using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IpTrace;

internal sealed partial class NetworkDiagnosticService : IDisposable
{
    private const int PingTimeoutMilliseconds = 3_000;
    private const int TraceTimeoutMilliseconds = 2_000;
    private const int MaximumHops = 100;
    private static readonly byte[] TracePayload = "IpTrace"u8.ToArray();

    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.ip.sb/"),
        Timeout = TimeSpan.FromSeconds(8)
    };

    internal static string NormalizeTarget(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("请输入域名或 IP 地址。", nameof(input));
        }

        var value = input.Trim();
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            value = $"http://{value}";
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        throw new ArgumentException("无法识别目标地址，请输入有效的域名或 IP 地址。", nameof(input));
    }

    internal async Task<string> QueryAsync(string target, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(target, cancellationToken).ConfigureAwait(false);
        if (addresses.Length == 0)
        {
            return $"未找到 {target} 的 DNS 记录。";
        }

        var lines = new List<string>
        {
            $"目标: {target}",
            $"解析时间: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}",
            string.Empty,
            "解析结果"
        };

        foreach (var address in addresses.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var location = await GetLocationAsync(address, cancellationToken).ConfigureAwait(false);
            lines.Add($"  {GetAddressType(address),-6}  {address,-39}  {location}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    internal async Task<string> PingAsync(string target, CancellationToken cancellationToken)
    {
        using var ping = new Ping();
        var lines = new List<string> { $"正在 Ping {target}，共 4 次：", string.Empty };
        var successfulRoundTrips = new List<long>();
        IPAddress? destination = null;

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reply = await ping.SendPingAsync
                    (target, TimeSpan.FromMilliseconds(PingTimeoutMilliseconds), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (reply.Status == IPStatus.Success)
            {
                destination = reply.Address;
                successfulRoundTrips.Add(reply.RoundtripTime);
                lines.Add
                (
                    $"来自 {reply.Address} 的回复: 时间={reply.RoundtripTime}ms TTL={reply.Options?.Ttl.ToString(CultureInfo.InvariantCulture) ?? "--"}"
                );
            }
            else
            {
                lines.Add($"第 {attempt} 次请求失败: {FormatStatus(reply.Status)}");
            }
        }

        lines.Add(string.Empty);
        lines.Add($"{destination ?? IPAddress.None} 的 Ping 统计信息:");
        lines.Add
        (
            $"  数据包: 已发送 = 4，已接收 = {successfulRoundTrips.Count}，丢失 = {4 - successfulRoundTrips.Count} ({(4 - successfulRoundTrips.Count) * 25}% 丢失)"
        );
        if (successfulRoundTrips.Count > 0)
        {
            lines.Add
            (
                $"  往返时间: 最短 = {successfulRoundTrips.Min()}ms，最长 = {successfulRoundTrips.Max()}ms，平均 = {successfulRoundTrips.Average():F0}ms"
            );
        }

        return string.Join(Environment.NewLine, lines);
    }

    internal async Task<string> TcpPingAsync(string target, int port, CancellationToken cancellationToken)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "端口必须介于 1 到 65535。 ");
        }

        var lines = new List<string> { $"正在 TCPing {target}:{port}，共 4 次：", string.Empty };
        var successfulRoundTrips = new List<double>();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var client = new TcpClient();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                await client.ConnectAsync(target, port, timeout.Token).ConfigureAwait(false);
                stopwatch.Stop();
                successfulRoundTrips.Add(stopwatch.Elapsed.TotalMilliseconds);
                lines.Add($"连接成功: {target}:{port}  时间={stopwatch.Elapsed.TotalMilliseconds:F1}ms");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lines.Add($"第 {attempt} 次连接超时（3000ms）");
            }
            catch (SocketException exception)
            {
                lines.Add($"第 {attempt} 次连接失败: {exception.SocketErrorCode}");
            }
        }

        lines.Add(string.Empty);
        lines.Add($"TCPing 统计: 成功 = {successfulRoundTrips.Count}，失败 = {4 - successfulRoundTrips.Count}");
        if (successfulRoundTrips.Count > 0)
        {
            lines.Add
            (
                $"往返时间: 最短 = {successfulRoundTrips.Min():F1}ms，最长 = {successfulRoundTrips.Max():F1}ms，平均 = {successfulRoundTrips.Average():F1}ms"
            );
        }

        return string.Join(Environment.NewLine, lines);
    }

    internal async Task TraceRouteAsync
    (
        string target,
        Func<string, Task> reportLineAsync,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(reportLineAsync);

        var addresses = await Dns.GetHostAddressesAsync(target, cancellationToken).ConfigureAwait(false);
        var destination = addresses.FirstOrDefault
                              (static address => address.AddressFamily == AddressFamily.InterNetwork)
                          ?? addresses.FirstOrDefault()
                          ?? throw new InvalidOperationException($"未找到 {target} 的 IP 地址。");

        await reportLineAsync($"通过最多 {MaximumHops} 个跃点跟踪到 {target} [{destination}] 的路由：").ConfigureAwait(false);
        await reportLineAsync(string.Empty).ConfigureAwait(false);
        await reportLineAsync("跃点  延迟       IP 地址                                  位置").ConfigureAwait(false);
        await reportLineAsync
            ("────  ─────────  ───────────────────────────────────────  ─────────────────────────").ConfigureAwait
            (false);

        using var ping = new Ping();
        for (var ttl = 1; ttl <= MaximumHops; ttl++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var options = new PingOptions(ttl, true);
            var stopwatch = Stopwatch.StartNew();
            PingReply reply;

            try
            {
                reply = await ping.SendPingAsync
                    (
                        destination, TimeSpan.FromMilliseconds(TraceTimeoutMilliseconds), TracePayload, options,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            catch (PingException exception)
            {
                await reportLineAsync
                    ($"{ttl,4}  {"*",-9}  {exception.InnerException?.Message ?? exception.Message}").ConfigureAwait
                    (false);
                continue;
            }

            stopwatch.Stop();
            if (reply.Status is IPStatus.Success or IPStatus.TtlExpired)
            {
                var location = reply.Address is null
                    ? "未知"
                    : await GetLocationAsync(reply.Address, cancellationToken).ConfigureAwait(false);
                var elapsed = $"{stopwatch.Elapsed.TotalMilliseconds:F0}ms";
                await reportLineAsync($"{ttl,4}  {elapsed,-9}  {reply.Address,-39}  {location}").ConfigureAwait(false);
            }
            else
            {
                await reportLineAsync($"{ttl,4}  {"*",-9}  请求超时").ConfigureAwait(false);
            }

            if (reply.Status == IPStatus.Success)
            {
                await reportLineAsync(string.Empty).ConfigureAwait(false);
                await reportLineAsync("跟踪完成。").ConfigureAwait(false);
                return;
            }
        }

        await reportLineAsync(string.Empty).ConfigureAwait(false);
        await reportLineAsync("已达到最大跃点数，跟踪结束。").ConfigureAwait(false);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<string> GetLocationAsync(IPAddress address, CancellationToken cancellationToken)
    {
        if (IPAddress.IsLoopback(address) || IsPrivateAddress(address))
        {
            return "本地/私有网络";
        }

        try
        {
            using var response = await _httpClient.GetAsync
                ($"geoip/{Uri.EscapeDataString(address.ToString())}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return "位置查询失败";
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var geo = await JsonSerializer.DeserializeAsync
                (stream, GeoIpJsonContext.Default.GeoIpResponse, cancellationToken).ConfigureAwait(false);
            if (geo is null)
            {
                return "未知";
            }

            var place = string.Join
            (
                " · ",
                new[] { geo.Country, geo.Region, geo.City }.Where
                    (static value => !string.IsNullOrWhiteSpace(value)).Distinct()
            );
            var organization = !string.IsNullOrWhiteSpace(geo.Organization) ? geo.Organization : geo.Isp;
            return string.IsNullOrWhiteSpace(organization) ? place : $"{place}  ({organization})";
        }
        catch (HttpRequestException)
        {
            return "位置查询失败";
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "位置查询超时";
        }
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.Equals(IPAddress.IPv6Loopback);
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
               || bytes[0] == 127
               || bytes[0] == 192 && bytes[1] == 168
               || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
               || bytes[0] == 169 && bytes[1] == 254;
    }

    private static string GetAddressType(IPAddress address) =>
        address.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6";

    private static string FormatStatus(IPStatus status) => status switch
    {
        IPStatus.TimedOut => "请求超时",
        IPStatus.DestinationHostUnreachable => "目标主机不可达",
        IPStatus.DestinationNetworkUnreachable => "目标网络不可达",
        _ => status.ToString()
    };

    [JsonSerializable(typeof(GeoIpResponse))]
    private partial class GeoIpJsonContext : JsonSerializerContext;

    private sealed record GeoIpResponse
    (
        [property: JsonPropertyName("region")] string? Region,
        [property: JsonPropertyName("organization")]
        string? Organization,
        [property: JsonPropertyName("isp")] string? Isp,
        [property: JsonPropertyName("city")] string? City,
        [property: JsonPropertyName("country")]
        string? Country
    );
}