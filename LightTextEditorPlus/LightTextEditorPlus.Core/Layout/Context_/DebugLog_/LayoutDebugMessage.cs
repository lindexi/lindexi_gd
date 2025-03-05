namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 布局调试信息
/// </summary>
/// <param name="Category"></param>
/// <param name="Message"></param>
public readonly record struct LayoutDebugMessage(LayoutDebugCategory Category, string Message)
{
    /// <inheritdoc />
    public override string ToString() => $"[{Category}] {Message}";
}