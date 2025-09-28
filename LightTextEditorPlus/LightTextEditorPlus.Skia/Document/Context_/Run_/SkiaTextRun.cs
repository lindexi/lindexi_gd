using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 字符串文本片段
/// </summary>
public class SkiaTextRun : TextRun
{
    /// <summary>
    /// 创建字符串文本片段
    /// </summary>
    /// <param name="text"></param>
    /// <param name="runProperty"></param>
    public SkiaTextRun(string text, SkiaTextRunProperty? runProperty = null) : base(text, runProperty)
    {
    }
}