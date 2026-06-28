namespace PptxGenerator.Streaming;

/// <summary>
/// 流式中断控制器，管理错误分级、中断信号和重试逻辑。
/// </summary>
public sealed class SlideStreamInterruptionController
{
    private readonly int _maxConsecutiveErrors;
    private readonly int _maxRetries;
    private int _consecutiveErrorCount;
    private int _retryCount;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// 初始化 <see cref="SlideStreamInterruptionController"/> 的新实例。
    /// </summary>
    /// <param name="maxConsecutiveErrors">最大连续可容错错误数，达到此值时升级为不可容错。</param>
    /// <param name="maxRetries">最大重试次数。</param>
    public SlideStreamInterruptionController(int maxConsecutiveErrors = 3, int maxRetries = 2)
    {
        _maxConsecutiveErrors = maxConsecutiveErrors;
        _maxRetries = maxRetries;
    }

    /// <summary>
    /// 当前重试轮次（从 0 开始）。
    /// </summary>
    public int RetryRound => _retryCount;

    /// <summary>
    /// 是否已达到最大重试次数。
    /// </summary>
    public bool MaxRetriesReached => _retryCount >= _maxRetries;

    /// <summary>
    /// 是否已请求中断。
    /// </summary>
    public bool IsInterruptionRequested => _cts?.IsCancellationRequested ?? false;

    /// <summary>
    /// 关联的 CancellationToken，供 RunStreamingAsync 使用。
    /// </summary>
    public CancellationToken Token => _cts?.Token ?? CancellationToken.None;

    /// <summary>
    /// 开始新一轮流式请求。
    /// </summary>
    public void StartRound()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _consecutiveErrorCount = 0;
    }

    /// <summary>
    /// 重置连续错误计数（成功接收一个有效片段后调用）。
    /// </summary>
    public void ResetErrorCount()
    {
        _consecutiveErrorCount = 0;
    }

    /// <summary>
    /// 报告一个可容错错误。连续达到阈值时升级为不可容错。
    /// </summary>
    /// <param name="error">错误信息。</param>
    /// <returns>是否触发了中断。</returns>
    public bool ReportTolerableError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _consecutiveErrorCount++;
        if (_consecutiveErrorCount >= _maxConsecutiveErrors)
        {
            _cts?.Cancel();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 报告一个不可容错错误，立即触发中断。
    /// </summary>
    /// <param name="error">错误信息。</param>
    public void ReportFatalError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _cts?.Cancel();
    }

    /// <summary>
    /// 结束当前轮次，返回是否可以重试。
    /// </summary>
    /// <returns>是否可以重试。</returns>
    public bool CanRetry()
    {
        if (_retryCount >= _maxRetries)
        {
            return false;
        }

        _retryCount++;
        return true;
    }

    /// <summary>
    /// 取消当前轮次。
    /// </summary>
    public void Cancel()
    {
        _cts?.Cancel();
    }
}
