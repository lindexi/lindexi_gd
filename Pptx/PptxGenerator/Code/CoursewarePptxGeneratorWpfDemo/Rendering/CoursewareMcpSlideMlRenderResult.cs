namespace CoursewarePptxGeneratorWpfDemo.Rendering;

/// <summary>
/// MCP SlideML 渲染工具返回的结构化结果。
/// </summary>
internal sealed record CoursewareMcpSlideMlRenderResult
{
    /// <summary>
    /// 获取渲染回填后的 SlideML XML。
    /// </summary>
    public required string OutputXml { get; init; }

    /// <summary>
    /// 获取渲染过程中的警告信息。
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// 获取渲染过程中的错误信息。
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// 获取预览图保存的本地文件路径。
    /// </summary>
    public required string PreviewImageFilePath { get; init; }
}
