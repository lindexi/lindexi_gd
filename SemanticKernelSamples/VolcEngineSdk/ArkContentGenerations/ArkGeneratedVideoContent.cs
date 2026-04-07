namespace VolcEngineSdk;

/// <summary>
/// 生成视频的 URL，格式为 mp4。为保障信息安全，生成的视频会在24小时后被清理，请及时转存
/// </summary>
/// <param name="VideoUrl"></param>
public record ArkGeneratedVideoContent(string VideoUrl)
{
    /// <summary>
    /// 视频的尾帧图像 URL。有效期为 24小时，请及时转存。
    /// </summary>
    /// <remarks>
    /// 创建视频生成任务 时设置 "return_last_frame": true 时，会返回该参数
    /// </remarks>
    public string? LastFrameUrl { get; init; }
}