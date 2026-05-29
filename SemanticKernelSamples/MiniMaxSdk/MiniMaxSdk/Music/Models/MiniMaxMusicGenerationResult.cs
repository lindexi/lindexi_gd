namespace MiniMaxSdk.Music.Models;

/// <summary>
/// 表示一次 MiniMax 音乐生成请求的结果。
/// </summary>
/// <param name="Status">音乐合成状态，取值参考 <see cref="MiniMaxMusicGenerationStatuses"/>。</param>
/// <param name="Audio">当输出格式为 <c>hex</c> 时返回的音频十六进制编码字符串。</param>
/// <param name="BaseResponse">接口返回的状态码及详情。</param>
public sealed record MiniMaxMusicGenerationResult(int? Status, string? Audio, MiniMaxBaseResponse BaseResponse);
