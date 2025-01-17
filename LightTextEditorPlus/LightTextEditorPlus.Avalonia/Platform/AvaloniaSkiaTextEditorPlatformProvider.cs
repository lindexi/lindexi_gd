using System;
using System.Diagnostics;

using Avalonia.Threading;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Platform;

public class AvaloniaSkiaTextEditorPlatformProvider : SkiaTextEditorPlatformProvider
{
    public TextEditor AvaloniaTextEditor { get; internal set; } = null!;

    #region 可基类重写方法

    /// <inheritdoc />
    /// 如果需要自定义撤销恢复，可以获取文本编辑器重写的方法
    /// 默认文本库是独立的撤销恢复，每个文本编辑器都有自己的撤销恢复。如果想要全局的撤销恢复，可以自定义一个全局的撤销恢复
    public override ITextEditorUndoRedoProvider BuildTextEditorUndoRedoProvider()
    {
        return AvaloniaTextEditor.BuildCustomTextEditorUndoRedoProvider() ?? base.BuildTextEditorUndoRedoProvider();
    }

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

    public override void RequireDispatchUpdateLayout(Action updateLayoutAction)
    {
        _layoutUpdateAction = updateLayoutAction;
        LayoutDispatcherRequiring.Require();
    }

    public override void InvokeDispatchUpdateLayout(Action updateLayoutAction)
    {
        _layoutUpdateAction = updateLayoutAction;
        LayoutDispatcherRequiring.Invoke(withRequire: true);
    }

    private void UpdateLayout()
    {
        _updatingLayout = true;
        try
        {
            _layoutUpdateAction?.Invoke();
            // 禁止多次执行
            _layoutUpdateAction = null;
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
    public void EnsureLayoutUpdated()
    {
        if (_updatingLayout)
        {
            throw new InvalidOperationException($"正在布局的过程中触发立刻布局，可能出现无限递归炸堆栈");
        }

        if (_layoutUpdateAction is null)
        {
            // 无需布局
            Debug.Assert(!AvaloniaTextEditor.TextEditorCore.IsDirty || AvaloniaTextEditor.TextEditorCore.DocumentManager.CharCount == 0, "无需布局的情况只有两个，要么文本不是脏的 IsDirty 为 false 值。要么是空文本，即 CharCount 为零");
            return;
        }

        LayoutDispatcherRequiring.Invoke(withRequire: false);
    }

    #endregion

    #region 字体管理

    protected override SkiaPlatformResourceManager GetSkiaPlatformResourceManager()
    {
        return _avaloniaTextEditorResourceManager ??=
            new AvaloniaTextEditorResourceManager(AvaloniaTextEditor, TextEditor);
    }

    private AvaloniaTextEditorResourceManager? _avaloniaTextEditorResourceManager;

    #endregion
}


class AvaloniaTextEditorDispatcherRequiring
{
    public AvaloniaTextEditorDispatcherRequiring(Action action, Dispatcher dispatcher, DispatcherPriority? priority = null)
    {
        _action = action;
        _dispatcher = dispatcher;
        _priority = priority ?? DispatcherPriority.Render;
    }

    /// <summary>
    /// 请求执行任务，以便在调度发生时开始执行。
    /// </summary>
    public void Require()
    {
        _dispatcher.VerifyAccess();

        if (_isTaskRequired)
        {
            return;
        }
        _isTaskRequired = true;

        _dispatcher.InvokeAsync(InvokeAction, _priority);
    }

    /// <summary>
    /// 立即执行任务，执行完后，将清空所有之前对执行任务的请求。<para/>
    /// 默认情况下，如果此前没有申请执行过（没有调用 <see cref="Require"/> 方法），调用此方法将不会执行任务。
    /// 要更改此默认行为，请指定参数 <paramref name="withRequire"/> 决定是否立即申请一次，以便确保一定执行。
    /// </summary>
    /// <param name="withRequire">是否立即开始一次申请，如果开始，则无论此前是否申请过，都会开始执行任务。</param>
    public void Invoke(bool withRequire = false)
    {
        _dispatcher.VerifyAccess();

        _isTaskRequired = _isTaskRequired || withRequire;
        InvokeAction();
    }

    ///// <summary>
    ///// 放弃任务的执行，如果任务已经开始执行，将无法放弃
    ///// </summary>
    ///// <returns>
    ///// true: 任务还没执行，成功放弃；
    ///// false: 任务已经执行，无法放弃；
    ///// </returns>
    //public bool GiveUp()
    //{

    //}

    private bool _isTaskRequired;
    private readonly Action _action;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherPriority _priority;

    private void InvokeAction()
    {
        if (!_isTaskRequired)
        {
            return;
        }
        try
        {
            _action.Invoke();
        }
        finally
        {
            _isTaskRequired = false;
        }
    }
}
