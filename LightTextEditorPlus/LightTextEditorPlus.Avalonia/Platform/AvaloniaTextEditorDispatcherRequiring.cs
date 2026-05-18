using System;
using Avalonia.Threading;

namespace LightTextEditorPlus.Platform;

class AvaloniaTextEditorDispatcherRequiring
{
    public AvaloniaTextEditorDispatcherRequiring
        (Action action, Dispatcher dispatcher, DispatcherPriority? priority = null)
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