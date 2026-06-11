namespace DotNetCampus.HttpClientOverOpenSsl;

/// <summary>
/// OpenSSL 操作异常，用于包装 libssl 返回的错误信息。
/// </summary>
internal sealed class OpenSslException : IOException
{
    /// <summary>
    /// 使用指定的错误信息、SSL 错误码和 OpenSSL 错误码创建 <see cref="OpenSslException"/> 实例。
    /// </summary>
    public OpenSslException(string message, int sslErrorCode, ulong openSslErrorCode = 0)
        : base(message)
    {
        SslErrorCode = sslErrorCode;
        OpenSslErrorCode = openSslErrorCode;
    }

    /// <summary>
    /// SSL 错误码，来自 <c>SSL_get_error</c>。
    /// </summary>
    public int SslErrorCode { get; }

    /// <summary>
    /// OpenSSL 错误队列中的错误码，来自 <c>ERR_get_error</c>。为 0 表示无额外错误信息。
    /// </summary>
    public ulong OpenSslErrorCode { get; }
}
