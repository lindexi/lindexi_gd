using System.Runtime.Versioning;
using CPF.Linux;

namespace UnoInk.Inking.X11Platforms.Threading;

/// <summary>
/// 命名是从 Avalonia 抄的
/// </summary>
[SupportedOSPlatform("Linux")]
public class X11PlatformThreading
{
    public X11PlatformThreading(X11Application x11Application)
    {
        _x11Application = x11Application;
    }
    
    private readonly X11Application _x11Application;
    
    public void Run()
    {
        // 启动消息
        _eventsThread = new Thread(RunInner)
        {
            Name = $"X11InkWindow XEvents {Interlocked.Increment(ref _threadCount) - 1}",
            IsBackground = true
        };
        
        _eventsThread.Start();
    }
    
    private X11Info X11Info => _x11Application.X11Info;

    public bool HasThreadAccess => ReferenceEquals(Thread.CurrentThread, _eventsThread);

    private void RunInner()
    {
        var display = X11Info.Display;
        while (true)
        {
            var xNextEvent = XLib.XNextEvent(display, out var @event);
            //if (@event.type == XEventName.Expose)
            //{
            //    if (@event.ExposeEvent.window == X11InkProvider.InkWindow.X11InkWindowIntPtr)
            //    {
            //        X11InkProvider.InkWindow.Expose(@event.ExposeEvent);
            //    }
            //}
            //else 
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
                        action();
                    }

                    continue;
                }
            }
            
            _x11Application.DispatchEvent(@event);
        }
        // ReSharper disable once FunctionNeverReturns
    }
    
    private readonly List<Action> _invokeList = new List<Action>();
    private readonly IntPtr _invokeMessageId = new IntPtr(123123123);
    
    public async Task InvokeAsync(Action action, IntPtr x11WindowIntPtr)
    {
        var taskCompletionSource = new TaskCompletionSource();
        lock (_invokeList)
        {
            _invokeList.Add(() =>
            {
                action();
                taskCompletionSource.SetResult();
            });
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
                window = x11WindowIntPtr,
                message_type = 0,
                format = 32,
                ptr1 = _invokeMessageId,
                ptr2 = 0,
                ptr3 = 0,
                ptr4 = 0,
            }
        };
        XLib.XSendEvent(X11Info.Display, x11WindowIntPtr, false, 0, ref @event);
        
        XLib.XFlush(X11Info.Display);
        
        await taskCompletionSource.Task;
    }
    
    private Thread? _eventsThread;
    private static int _threadCount;
}
