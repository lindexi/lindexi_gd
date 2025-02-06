using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本
/// </summary>
public class ImmutableRun : TextRun
{
    /// <summary>
    /// 创建不可变的文本
    /// </summary>
    public ImmutableRun(string text, IRunProperty? runProperty = null) : base(text, runProperty)
    {
    }
}