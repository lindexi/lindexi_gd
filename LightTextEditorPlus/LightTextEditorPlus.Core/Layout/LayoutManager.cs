using System;
using System.Diagnostics;

using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 布局管理器
/// </summary>
/// 布局和渲染是拆开的，先进行布局再进行渲染
/// 布局的
// todo 文本公式混排 文本图片混排 文本和其他元素的混排多选 文本和其他可交互元素混排的光标策略
class LayoutManager
{
    public LayoutManager(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditor TextEditor { get; }
    public event EventHandler? InternalLayoutCompleted;

    [DebuggerStepThrough] // 别跳太多层
    public TextHitTestResult HitTest(in TextPoint point)
    {
        return ArrangingLayoutProvider.HitTest(point);
    }

    public void UpdateLayout()
    {
        TextEditor.Logger.LogDebug("===开始布局===");
        var result = ArrangingLayoutProvider.UpdateLayout();
        DocumentRenderData.DocumentBounds = result.DocumentBounds;
        //DocumentRenderData.IsDirty = false;
        TextEditor.Logger.LogDebug("===完成布局===");

        InternalLayoutCompleted?.Invoke(this, EventArgs.Empty);
    }

    private ArrangingLayoutProvider ArrangingLayoutProvider
    {
        get
        {
            if (_arrangingLayoutProvider?.ArrangingType != TextEditor.ArrangingType)
            {
                _arrangingLayoutProvider = TextEditor.ArrangingType switch
                {
                    ArrangingType.Horizontal => new HorizontalArrangingLayoutProvider(this),
                    // todo 支持竖排文本
                    _ => throw new NotSupportedException()
                };
            }

            return _arrangingLayoutProvider;
        }
    }

    private ArrangingLayoutProvider? _arrangingLayoutProvider;

    public DocumentRenderData DocumentRenderData { get; } = new DocumentRenderData();
}