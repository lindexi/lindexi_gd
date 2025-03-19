using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus;

/// <summary>
/// 文本编辑器
/// </summary>
/// 这是入口文件，这是一个分部类
/// - 平台实现相关代码： Platform\TextEditor.Platform.ava.cs
/// - API 定义层： API\TextEditor.*.cs
public partial class TextEditor
{
    /// <summary>
    /// 文本核心
    /// </summary>
    public TextEditorCore TextEditorCore => SkiaTextEditor.TextEditorCore;

    /// <summary>
    /// 使用 Skia 渲染承载的文本编辑器
    /// </summary>
    public SkiaTextEditor SkiaTextEditor { get; }
}

