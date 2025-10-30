using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using X11ApplicationFramework.Natives;

using static X11ApplicationFramework.Natives.XLib;

namespace X11ApplicationFramework.Apps.Threading;

[SupportedOSPlatform("Linux")]
public class X11PlatformDispatcher
{
    public X11PlatformDispatcher(X11Application x11Application)
    {
        _x11Application = x11Application;
        _currentThread = Thread.CurrentThread;
    }

    private readonly X11Application _x11Application;

    private readonly Thread _currentThread;

    private X11InfoManager X11Info => _x11Application.X11Info;

    public bool HasThreadAccess => ReferenceEquals(Thread.CurrentThread, _currentThread);

    public bool CheckAccess()
    {
        return HasThreadAccess;
    }

    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            //StaticDebugLogger.WriteLine($"[X11PlatformThreading] Fail VerifyAccess {Environment.StackTrace}\r\n\r\n");

            throw new InvalidOperationException("Access is not allowed from a different thread.");
        }
    }

    internal void Run()
    {
        if (_x11Application.WindowManager.Windows.Count == 0)
        {
            throw new InvalidOperationException($"一旦没有任何窗口，则不能开启消息");
        }

        var display = X11Info.Display;

        while (!_isShutdown)
        {
            var xNextEvent = XNextEvent(display, out var @event);

            // 判断是否私有 Invoke 的消息
            if (@event.type == XEventName.ClientMessage)
            {
                var clientMessageEvent = @event.ClientMessageEvent;
                if (clientMessageEvent.message_type == 0 && clientMessageEvent.ptr1 == _invokeMessageId)
                {
                    List<Action> tempList;
                    lock (_invokeList)
                    {
                        tempList = _invokeList.ToList();
                        _invokeList.Clear();
                    }

                    foreach (var action in tempList)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception e)
                        {
                            _x11Application.RaiseUnhandledException(e);
                        }
                    }

                    // 这是专门用来处理业务逻辑的，不需要处理其他的事件
                    continue;
                }
            }

            // 其他窗口消息
            if (_x11Application.WindowManager.TryGetWindow(@event.AnyEvent.window, out var window))
            {
                window.DispatchEvent(@event);
            }
        }
    }

    private bool _isShutdown;

    /// <summary>
    /// 内部 Invoke 的消息号，这是随便写的
    /// </summary>
    private readonly IntPtr _invokeMessageId = new IntPtr(123123123);

    /// <summary>
    /// 专门用来发送事件的 Display 用于修复线程安全
    /// </summary>
    private IntPtr _sendEventDisplay;

    internal void ShutdownDispose()
    {
        _isShutdown = true;

        if (_sendEventDisplay != 0)
        {
            XCloseDisplay(_sendEventDisplay);
        }
    }

    #region 消息调度

    public bool TryEnqueue(Action action)
    {
        var eventWindow = _x11Application.EventWindow;

        if (_isShutdown)
        {
            return false;
        }

        lock (_invokeList)
        {
            _invokeList.Add(action);

            if (_sendEventDisplay == IntPtr.Zero)
            {
                _sendEventDisplay = XLib.XOpenDisplay(IntPtr.Zero);
            }
        }

        // 在 Avalonia 里面，是通过循环读取的方式，通过 XPending 判断是否有消息
        // 如果没有消息就进入自旋判断是否有业务消息和判断是否有 XPending 消息
        // 核心使用 epoll_wait 进行等待
        // 在 UNO 里面，也是和 Avalonia 差不多的方法，加上 XConnectionNumber 的方式，用于进行等待
        // 如果有消息进入，则立刻可以返回
        // 只是差别只是 UNO 让 X11 线程作为非主线程，所有 UI 逻辑不和 X11 线程混淆。而 Avalonia 让 X11 线程作为主线程，所有处理逻辑都在相同的主线程处理，主线程同时执行所有 UI 逻辑
        // 整个逻辑比较复杂
        // 这里简单处理，只通过发送 ClientMessage 的方式，告诉消息循环需要处理业务逻辑
        // 发送 ClientMessage 是一个合理的方式，根据官方文档说明，可以看到这是没有明确定义的
        // https://www.x.org/releases/X11R7.5/doc/man/man3/XClientMessageEvent.3.html
        // https://tronche.com/gui/x/xlib/events/client-communication/client-message.html
        // The X server places no interpretation on the values in the window, message_type, or data members.
        // 在 cpf 里面，和 Avalonia 实现差不多，也是在判断 XPending 是否有消息，没消息则判断是否有业务逻辑
        // 最后再进入等待逻辑。似乎 CPF 这样的方式会导致 CPU 占用略微提升
        var @event = new XEvent
        {
            ClientMessageEvent =
            {
                type = XEventName.ClientMessage,
                send_event = true,
                window = eventWindow.X11WindowIntPtr,
                message_type = 0,
                format = 32,
                ptr1 = _invokeMessageId,
                ptr2 = 0,
                ptr3 = 0,
                ptr4 = 0,
            }
        };
        XLib.XSendEvent(_sendEventDisplay, eventWindow.X11WindowIntPtr, false, 0, ref @event);

        XLib.XFlush(_sendEventDisplay);
        return true;
    }
    private readonly List<Action> _invokeList = new List<Action>();

    public Task InvokeAsync(Action action)
    {
        var taskCompletionSource = new TaskCompletionSource();
        var success = TryEnqueue(() =>
        {
            try
            {
                action();
                taskCompletionSource.SetResult();
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        });

        if (!success)
        {
            taskCompletionSource.SetException(new Exception());
        }

        return taskCompletionSource.Task;
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();
        var success = TryEnqueue(() =>
        {
            try
            {
                var result = func();
                taskCompletionSource.SetResult(result);
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        });

        if (!success)
        {
            taskCompletionSource.SetException(new Exception());
        }

        return taskCompletionSource.Task;
    }

    #endregion
}