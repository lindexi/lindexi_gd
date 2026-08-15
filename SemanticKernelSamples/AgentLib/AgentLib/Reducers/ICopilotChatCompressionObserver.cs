namespace AgentLib.Reducers;

/// <summary>
/// 观察自动对话压缩过程。
/// </summary>
public interface ICopilotChatCompressionObserver
{
    /// <summary>
    /// 压缩器达到阈值并开始生成摘要。
    /// </summary>
    Task CompressionStartedAsync(
        CopilotChatCompressionStatistics statistics,
        CancellationToken cancellationToken);

    /// <summary>
    /// 压缩器完成摘要生成和历史压缩。
    /// </summary>
    Task CompressionCompletedAsync(
        CopilotChatCompressionResult result,
        CancellationToken cancellationToken);

    /// <summary>
    /// 压缩失败且原始历史保持不变。
    /// </summary>
    Task CompressionFailedAsync(
        CopilotChatCompressionStatistics statistics,
        Exception exception,
        CancellationToken cancellationToken);
}
