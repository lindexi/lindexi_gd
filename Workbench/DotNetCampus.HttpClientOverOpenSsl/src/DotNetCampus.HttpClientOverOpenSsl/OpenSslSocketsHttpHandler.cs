using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace DotNetCampus.HttpClientOverOpenSsl;

/// <summary>
/// 基于 <see cref="SocketsHttpHandler"/> + <see cref="OpenSslStream"/> 的 HttpMessageHandler。
/// 将 <c>https://</c> 请求透明重写为 <c>http://</c>，使 <see cref="SocketsHttpHandler"/> 的
/// <c>IsSecure</c> 判定为 <see langword="false"/>，跳过其内置 TLS 握手。
/// 实际的 TLS 由 <see cref="SocketsHttpHandler.ConnectCallback"/> 中的 <see cref="OpenSslStream"/> 完成。
/// </summary>
/// <remarks>
/// 调用方正常使用 <c>https://</c> URL。原本就是 <c>http://</c> 的请求不受影响，走默认 TCP 连接。
/// </remarks>
public sealed class OpenSslSocketsHttpHandler : HttpMessageHandler
{
    /// <summary>
    /// 初始化 <see cref="OpenSslSocketsHttpHandler"/> 的新实例。
    /// </summary>
    public OpenSslSocketsHttpHandler(SocketsHttpHandler? socketsHttpHandler = null)
    {
        if (socketsHttpHandler?.ConnectCallback is not null)
        {
            throw new ArgumentException("The provided SocketsHttpHandler already has a ConnectCallback set.", nameof(socketsHttpHandler));
        }

        var innerHandler = socketsHttpHandler ?? new SocketsHttpHandler();

        innerHandler.ConnectCallback = ConnectCallbackAsync;
        _invoker = new HttpMessageInvoker(innerHandler, disposeHandler: true);
    }

    /// <summary>
    /// 标记在 <see cref="HttpRequestMessage.Options"/> 中的键，指示该请求原本是 https，
    /// ConnectCallback 据此决定是否走 TLS 并连接 443 端口。
    /// </summary>
    private static readonly HttpRequestOptionsKey<bool> IsOriginallyHttpsKey = new("OpenSslSocketsHttpHandler.IsOriginallyHttps");

    private readonly HttpMessageInvoker _invoker;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null &&
            string.Equals(request.RequestUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            RewriteToHttp(request);
        }

        return await _invoker.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 将请求的 URI 从 https:// 重写为 http://，并通过 <see cref="HttpRequestMessage.Options"/>
    /// 标记原始 scheme，供 <see cref="ConnectCallbackAsync"/> 区分处理。
    /// </summary>
    private static void RewriteToHttp(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        var builder = new UriBuilder(uri)
        {
            Scheme = "http",
            Port = -1
        };
        request.RequestUri = builder.Uri;
        request.Options.Set(IsOriginallyHttpsKey, true);
    }

    /// <summary>
    /// <see cref="SocketsHttpHandler.ConnectCallback"/> 的实现。
    /// 根据请求是否原本为 https 决定是否使用 <see cref="OpenSslStream"/> 建立 TLS 连接。
    /// </summary>
    private static async ValueTask<Stream> ConnectCallbackAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var isOriginallyHttps = context.InitialRequestMessage.Options.TryGetValue(IsOriginallyHttpsKey, out var value) && value;

        var port = isOriginallyHttps ? 443 : context.DnsEndPoint.Port;
        var host = context.DnsEndPoint.Host;

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            var endpoint = new DnsEndPoint(host, port);
            await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);

            if (!isOriginallyHttps)
            {
                return new NetworkStream(socket, ownsSocket: true);
            }

            var openSslStream = new OpenSslAsyncStream(socket, ownsSocket: true);
            await openSslStream.AuthenticateAsClientAsync(new OpenSslClientAuthenticationOptions
            {
                TargetHost = host
            }, cancellationToken).ConfigureAwait(false);

            return openSslStream;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _invoker.Dispose();
        }

        base.Dispose(disposing);
    }
}