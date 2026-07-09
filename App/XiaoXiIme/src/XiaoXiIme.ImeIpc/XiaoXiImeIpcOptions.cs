namespace XiaoXiIme.ImeIpc;

public sealed record XiaoXiImeIpcOptions(
    string ServerName,
    TimeSpan? ConnectTimeout = null,
    TimeSpan? RequestTimeout = null,
    int RetryCount = 0)
{
    public const string DefaultServerName = "XiaoXiIme_ImeHost";

    public static TimeSpan DefaultConnectTimeout { get; } = TimeSpan.FromMilliseconds(500);

    public static TimeSpan DefaultRequestTimeout { get; } = TimeSpan.FromMilliseconds(500);

    public static XiaoXiImeIpcOptions Default { get; } = new(DefaultServerName);

    public TimeSpan EffectiveConnectTimeout => ConnectTimeout ?? DefaultConnectTimeout;

    public TimeSpan EffectiveRequestTimeout => RequestTimeout ?? DefaultRequestTimeout;

    public int EffectiveRetryCount => Math.Max(0, RetryCount);
}