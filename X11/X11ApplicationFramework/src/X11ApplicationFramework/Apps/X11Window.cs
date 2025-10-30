using DotNetCampus.Logging;

using System;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using X11ApplicationFramework.Natives;

using static X11ApplicationFramework.Natives.XLib;

namespace X11ApplicationFramework.Apps;

[SupportedOSPlatform("Linux")]
public class X11Window : X11WindowNativeInterop
{
    public X11Window(X11Application application, X11WindowCreateInfo createInfo) : this(application, CreateX11Window(application, createInfo))
    {
        AppendPid();
        SetNetWmWindowTypeNormal();
    }

    internal X11Window(X11Application application, IntPtr x11WindowIntPtr) : base(application.X11Info,
        x11WindowIntPtr)
    {
        Application = application;
        application.RegisterWindow(this);
    }

    public X11Application Application { get; }

    public IntPtr GC => _gc ??= XCreateGC(Display, X11WindowIntPtr, 0, 0);

    private IntPtr? _gc;

    private static IntPtr CreateX11Window(X11Application application, X11WindowCreateInfo createInfo)
    {
        var x11Info = application.X11Info;
        var display = x11Info.Display;
        var rootWindow = x11Info.RootWindow;
        var screen = x11Info.Screen;

        var width = createInfo.Width;
        var height = createInfo.Height;

        if (createInfo.IsFullScreen)
        {
            width = x11Info.XDisplayWidth;
            height = x11Info.XDisplayHeight;
        }

        //StaticDebugLogger.WriteLine($"创建窗口 {width},{height}");

        XLib.XMatchVisualInfo(display, screen, 32, 4, out var info);
        var visual = info.visual;

        var valueMask =
                //SetWindowValuemask.BackPixmap
                0
                | SetWindowValuemask.BackPixel
                | SetWindowValuemask.BorderPixel
                | SetWindowValuemask.BitGravity
                | SetWindowValuemask.WinGravity
                | SetWindowValuemask.BackingStore
                | SetWindowValuemask.ColorMap
            //| SetWindowValuemask.OverrideRedirect
            ;
        var xSetWindowAttributes = new XSetWindowAttributes
        {
            backing_store = 1,
            bit_gravity = Gravity.NorthWestGravity,
            win_gravity = Gravity.NorthWestGravity,
            //override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
            colormap = XLib.XCreateColormap(display, rootWindow, visual, 0),
            border_pixel = 0,
            background_pixel = IntPtr.Zero,
        };

        var x11Window = XLib.XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref xSetWindowAttributes);

        Log.Info($"[InkCore][X11Apps][X11Window] 创建窗口 Visual={visual} WH={width},{height} XID={x11Window}");

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XLib.XSelectInput(display, x11Window, mask);

        Log.Info($"[InkCore][X11Apps][X11Window] 完成创建窗口 X11Window={x11Window}");

        return x11Window;
    }

    internal IDispatcher GetDispatcher()
        => new X11InkWindowDispatcher(this);

    internal unsafe void DispatchEvent(XEvent* @event)
    {
        OnDispatchEvent(@event);
    }

    protected virtual unsafe void OnDispatchEvent(XEvent* @event)
    {
        OnReceiveEvent(@event);
    }
}

[SupportedOSPlatform("Linux")]
file class X11InkWindowDispatcher : IDispatcher
{
    public X11InkWindowDispatcher(X11Window x11InkWindow)
    {
        _x11InkWindow = x11InkWindow;
    }

    private readonly X11Window _x11InkWindow;

    public bool TryEnqueue(Action action)
    {
        return _x11InkWindow.Application.X11PlatformThreading.TryEnqueue(action, _x11InkWindow.X11WindowIntPtr);
    }

    public async ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> action, CancellationToken cancellation)
    {
        var taskCompletionSource = new TaskCompletionSource<TResult>();

        // 以下是兼容实现
        TryEnqueue(async () =>
        {
            try
            {
                var result = await action(cancellation);
                // 其实不支持同步上下文返回
                taskCompletionSource.SetResult(result);
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
        });

        return await taskCompletionSource.Task;
    }

    public bool HasThreadAccess => _x11InkWindow.Application.X11PlatformThreading?.HasThreadAccess ?? false;
}

interface IDispatcher
{
    /// <summary>
    ///  Gets a value that specifies whether the current execution context is on the UI thread.
    /// </summary>
    bool HasThreadAccess { get; }

    /// <summary>
    /// Adds a task to the queue which will be executed on the thread associated with the dispatcher.
    /// </summary>
    /// <remarks>This is the raw version which allows to interact with the native dispatcher the fewest overhead possible.</remarks>
    /// <param name="action">The task to execute.</param>
    /// <returns>True indicates that the task was added to the queue; false, otherwise.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    bool TryEnqueue(Action action);

    /// <summary>
    /// Asynchronously executes an operation on the UI thread.
    /// </summary>
    /// <typeparam name="TResult">Type of the result of the operation.</typeparam>
    /// <param name="action">The async operation to execute.</param>
    /// <param name="cancellation">An cancellation token to cancel the async operation.</param>
    /// <returns>A ValueTask to asynchronously get the result of the operation.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        AsyncFunc<TResult> action,
        CancellationToken cancellation);
}

delegate ValueTask<TResult> AsyncFunc<TResult>(CancellationToken ct);