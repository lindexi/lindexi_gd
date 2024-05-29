namespace UnoInk.Inking.X11Platforms.Threading;

public class DispatcherRequiring
{
    public DispatcherRequiring(Action action, IDispatcher dispatcher)
    {
        _action = action;
        _dispatcher = dispatcher;
    }

    private readonly Action _action;
    private readonly IDispatcher _dispatcher;
    private bool _isTaskRequired;

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

        _dispatcher.TryEnqueue(InvokeAction);
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
