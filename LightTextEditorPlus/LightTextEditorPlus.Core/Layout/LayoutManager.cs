using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Diagnostics.LogInfos;
using LightTextEditorPlus.Core.Layout.HitTests;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 布局管理器
/// </summary>
/// 布局和渲染是拆开的，先进行布局再进行渲染
/// 布局管理器实际上啥都没有做，都是委托给具体的 <see cref="ArrangingLayoutProvider"/> 执行
// todo 文本公式混排 文本图片混排 文本和其他元素的混排多选 文本和其他可交互元素混排的光标策略
class LayoutManager
{
    /// <summary>
    /// 创建布局管理器
    /// </summary>
    public LayoutManager(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditorCore TextEditor { get; }
    // 由于 InternalLayoutCompleted 触发太快了，导致无法正确处理状态，因此决定将此事件干掉，换成在 TextEditorCore 进行统一处理
    //public event EventHandler? InternalLayoutCompleted;

    private LayoutHitTestProvider? _layoutHitTestProvider;

    /// <summary>
    /// 命中测试
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    [DebuggerStepThrough] // 别跳太多层
    public TextHitTestResult HitTest(in TextPoint point)
    {
        _layoutHitTestProvider ??= new LayoutHitTestProvider(ArrangingLayoutProvider);
        return _layoutHitTestProvider.HitTest(in point);
    }

    /// <summary>
    /// 更新布局
    /// </summary>
    public void UpdateLayout()
    {
        TextEditor.Logger.Log(new StartLayoutLogInfo());
        var result = ArrangingLayoutProvider.UpdateLayout();
        DocumentLayoutBounds = result.LayoutBounds;
        TextEditor.Logger.Log(new LayoutCompletedLogInfo(result));

        //InternalLayoutCompleted?.Invoke(this, EventArgs.Empty);
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
                    ArrangingType.Vertical => new VerticalArrangingLayoutProvider(this),
                    // todo 支持竖排文本
                    _ => throw new NotSupportedException()
                };
            }

            return _arrangingLayoutProvider;
        }
    }

    private ArrangingLayoutProvider? _arrangingLayoutProvider;

    public DocumentLayoutBoundsInHorizontalArrangingCoordinateSystem DocumentLayoutBounds { get; private set; }
}
