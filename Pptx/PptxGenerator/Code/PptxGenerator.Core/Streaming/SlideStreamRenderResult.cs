using PptxGenerator.Models;

namespace PptxGenerator.Streaming;

/// <summary>
/// 流式渲染结果，包含渲染后的 XML、警告、错误和预览图。
/// </summary>
public sealed class SlideStreamRenderResult
{
    /// <summary>
    /// 输入的 SlideML XML。
    /// </summary>
    public string InputXml { get; init; } = string.Empty;

    /// <summary>
    /// 渲染回填后的 SlideML XML。
    /// </summary>
    public string OutputXml { get; init; } = string.Empty;

    /// <summary>
    /// 渲染过程中的警告信息。
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 渲染错误信息。
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 渲染预览图片。
    /// </summary>
    public IPreviewImage? PreviewImage { get; init; }
}
