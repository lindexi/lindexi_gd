namespace JucojocuNeficawhurholee;

/// <summary>
/// OpenSSL 客户端认证配置选项。
/// </summary>
internal sealed class OpenSslClientAuthenticationOptions
{
    /// <summary>
    /// 目标主机名，用于 TLS SNI（Server Name Indication）和证书验证。
    /// </summary>
    public required string TargetHost { get; init; }
}
