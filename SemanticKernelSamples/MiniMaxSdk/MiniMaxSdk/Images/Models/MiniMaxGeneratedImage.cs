namespace MiniMaxSdk.Images.Models;

/// <summary>
/// 表示 MiniMax 文生图接口返回的一张图片。
/// </summary>
/// <param name="Url">当返回格式为 <c>url</c> 时的图片链接。</param>
/// <param name="Bytes">当返回格式为 <c>base64</c> 时解码得到的图片二进制内容。</param>
/// <param name="SuggestedFileExtension">根据图片内容或链接推断得到的建议文件扩展名。</param>
public sealed record MiniMaxGeneratedImage(string? Url, byte[]? Bytes, string SuggestedFileExtension)
{
    /// <summary>
    /// 获取当前图片是否包含二进制内容。
    /// </summary>
    public bool HasBinaryContent => Bytes is { Length: > 0 };
}