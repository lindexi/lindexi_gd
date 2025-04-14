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

    /// <summary>
    /// 下一次更新布局时的配置
    /// </summary>
    public UpdateLayoutConfiguration NextUpdateLayoutConfiguration { get; set; } = default;

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
        var currentConfiguration = NextUpdateLayoutConfiguration;
        NextUpdateLayoutConfiguration = default; // 清空配置

        TextEditor.Logger.Log(new StartLayoutLogInfo());
        var arrangingLayoutProvider = ArrangingLayoutProvider;
        var updateLayoutContext = new UpdateLayoutContext(this, arrangingLayoutProvider, currentConfiguration);

        updateLayoutContext.RecordDebugLayoutInfo($"开始布局", LayoutDebugCategory.Document);

        var result = arrangingLayoutProvider.UpdateLayout(updateLayoutContext);
        DocumentLayoutBounds = result.LayoutBounds;

        updateLayoutContext.RecordDebugLayoutInfo($"完成布局", LayoutDebugCategory.Document);
        updateLayoutContext.SetLayoutCompleted();
        TextEditor.Logger.Log(new LayoutCompletedLogInfo(result, updateLayoutContext));

        //InternalLayoutCompleted?.Invoke(this, EventArgs.Empty);
    }

    private ArrangingLayoutProvider ArrangingLayoutProvider
    {
        get
        {
            if (_arrangingLayoutProvider?.ArrangingType != TextEditor.ArrangingType)
            {
                if (TextEditor.ArrangingType.IsHorizontal)
                {
                    _arrangingLayoutProvider = new HorizontalArrangingLayoutProvider(this);
                }
                else if (TextEditor.ArrangingType.IsVertical)
                {
                    _arrangingLayoutProvider = new VerticalArrangingLayoutProvider(this);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            return _arrangingLayoutProvider;
        }
    }

    private ArrangingLayoutProvider? _arrangingLayoutProvider;

    public DocumentLayoutBoundsInHorizontalArrangingCoordinateSystem DocumentLayoutBounds { get; private set; }
}
