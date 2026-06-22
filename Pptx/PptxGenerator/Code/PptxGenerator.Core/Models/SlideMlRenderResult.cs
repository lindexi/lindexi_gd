namespace PptxGenerator.Models;

/// <summary>
/// SlideML 渲染结果，包含输入/输出 XML、警告列表和预览图片。
/// </summary>
public sealed class SlideMlRenderResult
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
    public IReadOnlyList<string> Warnings { get; init; } = System.Array.Empty<string>();

    /// <summary>
    /// 渲染错误信息。
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = System.Array.Empty<string>();

    /// <summary>
    /// 渲染预览图片。
    /// </summary>
    public IPreviewImage? PreviewImage { get; init; }
}
