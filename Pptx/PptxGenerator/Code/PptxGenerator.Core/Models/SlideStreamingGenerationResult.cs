namespace PptxGenerator.Models;

/// <summary>
/// 表示一次流式 SlideML 生成调用的最终结果。
/// </summary>
public sealed record SlideStreamingGenerationResult
{
    /// <summary>
    /// 获取最终一次尝试是否成功形成无错误的非空页面。
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// 获取本次调用实际执行的模型尝试次数。
    /// </summary>
    public required int AttemptCount { get; init; }

    /// <summary>
    /// 获取本次调用所有尝试中成功合并的新片段总数。
    /// </summary>
    public required int AcceptedFragmentCount { get; init; }

    /// <summary>
    /// 获取最终一次模型尝试中成功合并的新片段数。
    /// </summary>
    public required int FinalAttemptAcceptedFragmentCount { get; init; }

    /// <summary>
    /// 获取最终成功形成的完整 SlideML；失败时为空字符串。
    /// </summary>
    public required string FinalSlideXml { get; init; }

    /// <summary>
    /// 获取失败原因的可读摘要；成功时为 <see langword="null"/>。
    /// </summary>
    public string? ErrorMessage { get; init; }
}
