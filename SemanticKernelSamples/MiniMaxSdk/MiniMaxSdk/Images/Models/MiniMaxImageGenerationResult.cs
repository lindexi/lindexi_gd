namespace MiniMaxSdk.Images.Models;

/// <summary>
/// 表示一次 MiniMax 文生图请求的结果。
/// </summary>
/// <param name="TaskId">生成任务的 ID，可用于后续查询任务状态。</param>
/// <param name="Images">本次请求返回的图片集合。</param>
/// <param name="SuccessCount">成功生成的图片数量。</param>
/// <param name="FailedCount">因内容安全检查等原因未返回的图片数量。</param>
public sealed record MiniMaxImageGenerationResult(string? TaskId, IReadOnlyList<MiniMaxGeneratedImage> Images, int SuccessCount, int FailedCount);