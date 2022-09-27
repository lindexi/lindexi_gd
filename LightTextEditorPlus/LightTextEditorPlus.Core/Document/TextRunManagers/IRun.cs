namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一段文本，具有相同的属性定义
/// </summary>
public interface IRun
{
    IReadOnlyRunProperty? RunProperty { get; }
}