using System;
using System.Diagnostics;
using Avalonia.Threading;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Resources.Avalonia;

namespace LightTextEditorPlus.Platform;

/// <summary>
/// Avalonia 平台的基于 Skia 的文本编辑器平台提供器
/// </summary>
public class AvaloniaSkiaTextEditorPlatformProvider : SkiaTextEditorPlatformProvider
{
    /// <summary>
    /// 创建 Avalonia 平台的基于 Skia 的文本编辑器平台提供器
    /// </summary>
    /// <param name="avaloniaTextEditor"></param>
    public AvaloniaSkiaTextEditorPlatformProvider(TextEditor avaloniaTextEditor)
    {
        AvaloniaTextEditor = avaloniaTextEditor;
    }

    /// <summary>
    /// 文本编辑器
    /// </summary>
    public TextEditor AvaloniaTextEditor { get; }

    #region 可基类重写方法

    /// <inheritdoc />
    /// 如果需要自定义撤销恢复，可以获取文本编辑器重写的方法
    /// 默认文本库是独立的撤销恢复，每个文本编辑器都有自己的撤销恢复。如果想要全局的撤销恢复，可以自定义一个全局的撤销恢复
    public override ITextEditorUndoRedoProvider BuildTextEditorUndoRedoProvider()
    {
        return AvaloniaTextEditor.BuildCustomTextEditorUndoRedoProvider() ?? base.BuildTextEditorUndoRedoProvider();
    }

    /// <inheritdoc />
    public override ITextLogger? BuildTextLogger()
    {
        return AvaloniaTextEditor.BuildCustomTextLogger() ?? base.BuildTextLogger();
    }

    #endregion

    #region 文本布局

    private AvaloniaTextEditorDispatcherRequiring LayoutDispatcherRequiring
    {
        get
        {
            if (_layoutDispatcherRequiring is null)
            {
                _layoutDispatcherRequiring =
                    new AvaloniaTextEditorDispatcherRequiring(UpdateLayout, Dispatcher.UIThread);
            }

            return _layoutDispatcherRequiring;
        }
    }

    private AvaloniaTextEditorDispatcherRequiring? _layoutDispatcherRequiring;
    private Action? _layoutUpdateAction;

    /// <inheritdoc />
    public override void RequireDispatchUpdateLayout(Action updateLayoutAction)
    {
        if (_updatingLayout && AvaloniaTextEditor.TextEditorCore.IsInDebugMode)
        {
            AvaloniaTextEditor.Logger.LogDebug($"在执行布局的过程中，又再次被推送了布局委托。这可能是正常的，但会导致本次布局结果无法被正确应用");
        }

        _layoutUpdateAction = updateLayoutAction;
        LayoutDispatcherRequiring.Require();
    }

    /// <inheritdoc />
    public override void InvokeDispatchUpdateLayout(Action updateLayoutAction)
    {
        if (_updatingLayout && AvaloniaTextEditor.TextEditorCore.IsInDebugMode)
        {
            AvaloniaTextEditor.Logger.LogDebug($"在执行布局的过程中，又再次被推送了布局委托。这可能是正常的，但会导致本次布局结果无法被正确应用");
        }

        _layoutUpdateAction = updateLayoutAction;
        LayoutDispatcherRequiring.Invoke(withRequire: true);
    }

    private void UpdateLayout()
    {
        _updatingLayout = true;
        Debug.Assert(_layoutUpdateAction is not null);
        try
        {
            // Fixed : 这里有安全性问题。如果没有获取变量，则可能在 _layoutUpdateAction 执行过程中，调用进了 RequireDispatchUpdateLayout 或 InvokeDispatchUpdateLayout 方法，从而导致 _layoutUpdateAction 对象变更。即在进行禁止多次执行的 `_layoutUpdateAction = null` 方法时，将新的布局委托赋空
            // 立即将其赋空则是安全的
            Action? layoutUpdateAction = _layoutUpdateAction;

            // 禁止多次执行
            _layoutUpdateAction = null;

            layoutUpdateAction?.Invoke();
        }
        finally
        {
            _updatingLayout = false;
        }
    }

    private bool _updatingLayout;

    /// <summary>
    /// 立刻布局
    /// </summary>
    public bool EnsureLayoutUpdated()
    {
        if (_updatingLayout)
        {
            throw new InvalidOperationException(ExceptionMessages.AvaloniaSkiaTextEditorPlatformProvider_ReentrantEnsureLayoutUpdated);
        }

        if (_layoutUpdateAction is null)
        {
            // 无需布局
            Debug.Assert(
                !AvaloniaTextEditor.TextEditorCore.IsDirty ||
                AvaloniaTextEditor.TextEditorCore.DocumentManager.CharCount == 0,
                "无需布局的情况只有两个，要么文本不是脏的 IsDirty 为 false 值。要么是空文本，即 CharCount 为零");
            return false;
        }

        LayoutDispatcherRequiring.Invoke(withRequire: true);
        return true;
    }

    #endregion

    #region 字体管理

    /// <inheritdoc />
    protected override SkiaPlatformResourceManager GetSkiaPlatformResourceManager()
    {
        return _avaloniaTextEditorResourceManager ??=
            new AvaloniaTextEditorResourceManager(AvaloniaTextEditor, TextEditor);
    }

    private AvaloniaTextEditorResourceManager? _avaloniaTextEditorResourceManager;

    #endregion

    /// <summary>
    /// 获取文本编辑器的交互处理器
    /// </summary>
    /// <returns></returns>
    public virtual TextEditorHandler GetHandler()
    {
        return new TextEditorHandler(AvaloniaTextEditor);
    }

    /// <inheritdoc />
    public override IRenderManager GetRenderManager()
    {
        // 如果掉底层的 SkiaTextEditorPlatformProvider 的 GetRenderManager 方法，返回上层控件。如此即可在刷新渲染时，和 UI 框架同步，防止布局过程中出现额外的渲染。同时也能支持长文本只渲染可视区域
        return AvaloniaTextEditor.RenderManager;
    }
}