using System;
using System.Windows.Threading;

namespace LightTextEditorPlus.Utils.Threading;

/// <summary>
/// 表示一种调度模型。在这种模型中，任务只能通过申请来执行，一系列申请结束后，任务会按照预定的优先级调度执行。
/// 这种模型适用于在频繁触发的事件中执行一个无需每次执行的逻辑。（类似于 InvalidateArrange 等。）
/// </summary>
internal class DispatcherRequiring : DispatcherObject
{
    private bool _isTaskRequired;
    private readonly Action _action;
    private readonly DispatcherPriority _priority = DispatcherPriority.Normal;

    /// <summary>
    /// 创建按照 <see cref="DispatcherPriority.Normal"/> 优先级调度执行 <paramref name="action"/> 的 <see cref="DispatcherRequiring"/> 的新实例。
    /// </summary>
    /// <param name="action">要进行的调度。</param>
    public DispatcherRequiring(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// 创建按照 <paramref name="priority"/> 优先级调度执行 <paramref name="action"/> 的 <see cref="DispatcherRequiring"/> 的新实例。
    /// </summary>
    /// <param name="action">要进行的调度。</param>
    /// <param name="priority">调度采用的优先级。</param>
    public DispatcherRequiring(Action action, DispatcherPriority priority)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _priority = priority;
    }

    /// <summary>
    /// 请求执行任务，以便在调度发生时开始执行。
    /// </summary>
    public void Require()
    {
        if (_isTaskRequired)
        {
            return;
        }
        _isTaskRequired = true;

        Dispatcher.InvokeAsync(InvokeAction, _priority);
    }

    /// <summary>
    /// 立即执行任务，执行完后，将清空所有之前对执行任务的请求。<para/>
    /// 默认情况下，如果此前没有申请执行过（没有调用 <see cref="Require"/> 方法），调用此方法将不会执行任务。
    /// 要更改此默认行为，请指定参数 <paramref name="withRequire"/> 决定是否立即申请一次，以便确保一定执行。
    /// </summary>
    /// <param name="withRequire">是否立即开始一次申请，如果开始，则无论此前是否申请过，都会开始执行任务。</param>
    public void Invoke(bool withRequire = false)
    {
        _isTaskRequired = _isTaskRequired || withRequire;
        InvokeAction();
    }

    /// <summary>
    /// 取消任务的执行。这样，即便调度开始，也不会执行指定的任务。
    /// </summary>
    public void Cancel()
    {
        _isTaskRequired = false;
    }

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