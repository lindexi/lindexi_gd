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
    /// 日志
    /// </summary>
    internal ITextLogger Logger => TextEditorCore.Logger;

    public SkiaTextEditor SkiaTextEditor { get; }
    public TextEditorCore TextEditorCore => SkiaTextEditor.TextEditorCore;
}

