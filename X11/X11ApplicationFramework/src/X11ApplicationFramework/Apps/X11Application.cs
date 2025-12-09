using System;
using System.Runtime.Versioning;
using System.Text;

using X11ApplicationFramework.Apps.Threading;
using X11ApplicationFramework.Apps.X11EventArgs;
using X11ApplicationFramework.Natives;

using static X11ApplicationFramework.Natives.XLib;

namespace X11ApplicationFramework.Apps;

[SupportedOSPlatform("Linux")]
public class X11Application
{
    public X11Application()
    {
        var display = XOpenDisplay(IntPtr.Zero);
        var screen = XDefaultScreen(display);

        if (XCompositeQueryExtension(display, out var eventBase, out var errorBase) == 0)
        {
            Console.WriteLine("Error: Composite extension is not supported");
            XCloseDisplay(display);
            throw new NotSupportedException("Error: Composite extension is not supported");
        }
        else
        {
            //Console.WriteLine("XCompositeQueryExtension");
        }

        var rootWindow = XDefaultRootWindow(display);

        var x11Info = new X11InfoManager(display, screen, rootWindow);

        X11Info = x11Info;

        Dispatcher = new X11PlatformDispatcher(this);
    }

    public X11InfoManager X11Info { get; }

    internal void RegisterWindow(X11Window window)
    {
        WindowManager.RegisterWindow(window);
    }

    public event EventHandler? Startup;

    internal X11WindowManager WindowManager { get; } = new X11WindowManager();

    public X11PlatformDispatcher Dispatcher { get; }

    internal X11Window EventWindow
    {
        get
        {
            if (_eventWindow is null)
            {
                var eventWindow = XLib.CreateEventWindow(X11Info.Display, X11Info.RootWindow);
                _eventWindow = new X11Window(this, eventWindow);
                //_eventWindow.AppendPid();
            }

            return _eventWindow;
        }
    }
    private X11Window? _eventWindow;

    public void Run()
    {
        Startup?.Invoke(this, EventArgs.Empty);

        Dispatcher.Run();
    }

    internal void RaiseUnhandledException(Exception exception)
    {
        UnhandledException?.Invoke(this, new X11DispatcherUnhandledExceptionEventArgs(exception));
    }
    public event EventHandler<X11DispatcherUnhandledExceptionEventArgs>? UnhandledException;

    internal void OnShutdown()
    {
        if (_eventWindow is not null)
        {
            _eventWindow.Close();
        }
        Dispatcher.ShutdownDispose();
        X11Info.ShutdownDispose();
    }
}