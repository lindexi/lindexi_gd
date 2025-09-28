using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本
/// </summary>
public class ImmutableTextRun : TextRun
{
    /// <summary>
    /// 创建不可变的文本
    /// </summary>
    public ImmutableTextRun(string text, RunProperty? runProperty = null) : base(text, runProperty)
    {
    }

    /// <summary>
    /// 定义的样式，可为空。空代表着没有定义特别的样式
    /// </summary>
    public new RunProperty? RunProperty => (RunProperty?) base.RunProperty;
}