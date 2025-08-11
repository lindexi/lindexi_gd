using System;

namespace X11ApplicationFramework.Apps.X11EventArgs;

public class X11DispatcherUnhandledExceptionEventArgs : EventArgs
{
    internal X11DispatcherUnhandledExceptionEventArgs(Exception exception)
    {
        Exception = exception;
    }

    public Exception Exception { get; }

    /// <summary>
    /// 这个 API 现在没啥用，先放着
    /// </summary>
    public bool Handled { get; set; } = true;
}
