/// <summary>
/// 包装 <see cref="SocketsHttpHandler"/>，将请求的 <c>https://</c> scheme 透明重写为 <c>http://</c>，
/// 从而让 <see cref="SocketsHttpHandler"/> 的 <c>IsSecure</c> 判定为 <see langword="false"/>，
/// 跳过其内置 TLS 握手。实际的 TLS 由 <see cref="SocketsHttpHandler.ConnectCallback"/> 中
/// 的 <see cref="OpenSslStream"/> 完成。
/// </summary>
/// <remarks>
/// 调用方仍然使用 <c>https://</c> URL，重写仅发生在内部转发给 <see cref="SocketsHttpHandler"/> 时。
/// </remarks>
internal sealed class OpenSslSocketsHttpHandler : DelegatingHandler
{
    /// <summary>
    /// 使用指定的 <see cref="SocketsHttpHandler"/> 实例创建包装器。
    /// 该 handler 的 <see cref="SocketsHttpHandler.ConnectCallback"/> 应使用
    /// <see cref="OpenSslStream"/> 完成 TLS 握手。
    /// </summary>
    public OpenSslSocketsHttpHandler(SocketsHttpHandler innerHandler)
        : base(innerHandler)
    {
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null &&
            string.Equals(request.RequestUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            RewriteToHttp(request);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 将请求的 URI 从 https:// 重写为 http://。
    /// </summary>
    private static void RewriteToHttp(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        var builder = new UriBuilder(uri)
        {
            Scheme = "http",
            Port = -1 // 移除显式端口，让 ConnectCallback 自行决定连接 443
        };
        request.RequestUri = builder.Uri;
    }
}