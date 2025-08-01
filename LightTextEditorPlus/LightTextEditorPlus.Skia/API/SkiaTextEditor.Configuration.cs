using System;
using LightTextEditorPlus.Configurations;

namespace LightTextEditorPlus;

// 此文件存放配置相关的方法
[APIConstraint("TextEditor.Configuration.txt")]
partial class SkiaTextEditor
{
    /// <summary>
    /// 光标的配置
    /// </summary>
    public SkiaCaretConfiguration CaretConfiguration { get; set; } = new SkiaCaretConfiguration();

    /// <summary>
    /// 渲染的配置
    /// </summary>
    public SkiaTextEditorRenderConfiguration RenderConfiguration { get; set; } =
        new SkiaTextEditorRenderConfiguration();
}
