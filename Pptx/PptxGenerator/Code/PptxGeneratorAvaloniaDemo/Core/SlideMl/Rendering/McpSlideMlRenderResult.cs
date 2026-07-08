using System.Collections.Generic;

namespace PptxGenerator;

/// <summary>
/// SlideML MCP 渲染工具的结构化返回结果。
/// </summary>
internal sealed record McpSlideMlRenderResult
{
    /// <summary>
    /// 渲染回填后的 SlideML XML。
    /// </summary>
    public required string OutputXml { get; init; }

    /// <summary>
    /// 渲染过程中的警告信息。
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// 渲染过程中的错误信息。
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// 预览图保存的本地文件路径。
    /// </summary>
    public required string PreviewImageFilePath { get; init; }
}
