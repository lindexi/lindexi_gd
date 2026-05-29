namespace MiniMaxSdk.Music.Models;

/// <summary>
/// 表示一次 MiniMax 翻唱前处理请求的结果。
/// </summary>
/// <param name="CoverFeatureId">预处理后的音频特征唯一标识，有效期 24 小时。</param>
/// <param name="FormattedLyrics">通过 ASR 提取并格式化后的歌词。</param>
/// <param name="StructureResult">歌曲结构分析结果，使用 JSON 字符串表示。</param>
/// <param name="AudioDuration">参考音频时长，单位为秒。</param>
/// <param name="TraceId">请求追踪 ID。</param>
/// <param name="BaseResponse">接口返回的状态码及详情。</param>
public sealed record MiniMaxCoverPreprocessResult(
    string? CoverFeatureId,
    string? FormattedLyrics,
    string? StructureResult,
    double? AudioDuration,
    string? TraceId,
    MiniMaxBaseResponse BaseResponse);
