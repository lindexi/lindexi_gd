#if !USE_SKIA || USE_AllInOne

using System;

using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus;

// 此文件存放状态获取相关的方法
[APIConstraint("TextEditor.Status.Shared.txt")]
partial class TextEditor
{
    #region 光标和选择

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.CurrentCaretOffset"/>
    [TextEditorPublicAPI]
    public CaretOffset CurrentCaretOffset
    {
        set => TextEditorCore.CurrentCaretOffset = value;
        get => TextEditorCore.CurrentCaretOffset;
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.CurrentSelection"/>
    [TextEditorPublicAPI]
    public Selection CurrentSelection
    {
        set => TextEditorCore.CurrentSelection = value;
        get => TextEditorCore.CurrentSelection;
    }

    #region 光标事件

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.CurrentCaretOffsetChanging"/>
    public event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
        CurrentCaretOffsetChanging
    {
        add => TextEditorCore.CurrentCaretOffsetChanging += value;
        remove => TextEditorCore.CurrentCaretOffsetChanging -= value;
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.CurrentCaretOffsetChanged"/>
    public event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
        CurrentCaretOffsetChanged
    {
        add => TextEditorCore.CurrentCaretOffsetChanged += value;
        remove => TextEditorCore.CurrentCaretOffsetChanged -= value;
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.CurrentSelectionChanging"/>
    public event EventHandler<TextEditorValueChangeEventArgs<Selection>>? CurrentSelectionChanging
    {
        add => TextEditorCore.CurrentSelectionChanging += value;
        remove => TextEditorCore.CurrentSelectionChanging -= value;
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.CurrentSelectionChanged"/>
    public event EventHandler<TextEditorValueChangeEventArgs<Selection>>? CurrentSelectionChanged
    {
        add => TextEditorCore.CurrentSelectionChanged += value;
        remove => TextEditorCore.CurrentSelectionChanged -= value;
    }

    #endregion

    #endregion

    #region 文本状态

    /// <inheritdoc cref="P:LightTextEditorPlus.Core.TextEditorCore.IsDirty"/>
    [TextEditorPublicAPI]
    public bool IsDirty => TextEditorCore.IsDirty;

    /// <inheritdoc cref="P:LightTextEditorPlus.Core.TextEditorCore.IsUpdatingLayout"/>
    [TextEditorPublicAPI]
    public bool IsUpdatingLayout => TextEditorCore.IsUpdatingLayout;

    /// <inheritdoc cref="P:LightTextEditorPlus.Core.TextEditorCore.IsInDebugMode"/>
    [TextEditorPublicAPI]
    public bool IsInDebugMode => TextEditorCore.IsInDebugMode;

    /// <inheritdoc cref="P:LightTextEditorPlus.Core.TextEditorCore.SetInDebugMode"/>
    [TextEditorPublicAPI]
    public void SetInDebugMode() => TextEditorCore.SetInDebugMode();

    /// <inheritdoc cref="P:LightTextEditorPlus.Core.TextEditorCore.SetAllInDebugMode"/>
    [TextEditorPublicAPI]
    public static void SetAllInDebugMode() => LightTextEditorPlus.Core.TextEditorCore.SetAllInDebugMode();

    #endregion

    #region 渲染布局状态

#if USE_WPF || USE_AVALONIA

    /// <summary>
    /// 立即地获取渲染信息。等同于调用 <see cref="ForceLayout"/> 方法
    /// </summary>
    /// 不直接开放，防止业务方大量调用造成性能问题
    /// <returns></returns>
    internal RenderInfoProvider GetRenderInfoImmediately() 
        => ForceLayout();

    private partial RenderInfoProvider ForceLayout();
#endif

    #endregion

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.DebugName"/>
    public string? DebugName => TextEditorCore.DebugName;
}
#endif
