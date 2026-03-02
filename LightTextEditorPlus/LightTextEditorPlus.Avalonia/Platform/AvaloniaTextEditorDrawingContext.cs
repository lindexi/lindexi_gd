using Avalonia.Media;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus;

/// <summary>
/// 文本的渲染上下文信息
/// </summary>
/// <param name="TextEditor"></param>
/// <param name="DrawingContext"></param>
public readonly record struct AvaloniaTextEditorDrawingContext(TextEditor TextEditor, DrawingContext DrawingContext)
{
    /// <summary>
    /// 可见范围。为空代表无法获取可见范围
    /// </summary>
    public required TextRect? Viewport { get; init; }

    /// <summary>
    /// 获取渲染信息
    /// </summary>
    /// <returns></returns>
    public RenderInfoProvider GetRenderInfo()
    {
        return TextEditor.GetRenderInfoImmediately();
    }
}