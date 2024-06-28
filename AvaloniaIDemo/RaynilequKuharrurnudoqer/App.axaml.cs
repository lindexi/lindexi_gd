using System;
using System.Threading;
using CPF.Linux;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using static CPF.Linux.XLib; // CPF: https://gitee.com/csharpui/CPF

namespace RaynilequKuharrurnudoqer;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _ = RunX11Async();
    }

    private async Task RunX11Async()
    {
        await Task.CompletedTask;
        //await Task.Delay(10);

        var thread = new Thread(() =>
        {
            var display = XOpenDisplay(IntPtr.Zero);
            var screen = XDefaultScreen(display);
            var rootWindow = XDefaultRootWindow(display);

            var win1 = new X11Window(display);
            var x11WindowNativeInterop = new X11WindowNativeInterop(display,win1.Window, rootWindow);
            x11WindowNativeInterop.ShowActive();
            x11WindowNativeInterop.EnterFullScreen(true);

            while (true)
            {
                var xNextEvent = XNextEvent(display, out var @event);
            }
        });

        thread.IsBackground = true;
        thread.Start();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}