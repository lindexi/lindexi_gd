using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Logging;
using WhonurqaikarjurceLallchelceeqalbear;
using X11ApplicationFramework.Apps.Threading;
using X11ApplicationFramework.Apps.X11EventArgs;
using X11ApplicationFramework.Natives;

using static X11ApplicationFramework.Natives.XLib;
using UnhandledExceptionEventArgs = X11ApplicationFramework.Apps.Threading.UnhandledExceptionEventArgs;

namespace X11ApplicationFramework.Apps;

[SupportedOSPlatform("Linux")]
class X11Application
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

        UpdateScreenPhysicalSize();
        X11PlatformThreading = new X11PlatformThreading(this);
    }

    public X11InfoManager X11Info { get; }

    public void Start()
    {
        OnStart();
    }

    protected virtual void OnStart()
    {
        if (ShouldRunX11InNewThread)
        {
            X11PlatformThreading.RunInNewThread();
        }
        else
        {
            X11PlatformThreading.Run();
        }
    }

    public bool ShouldRunX11InNewThread { get; set; } = true;

    public X11PlatformThreading X11PlatformThreading { get; }

    internal void RegisterWindow(X11Window window)
    {
        WindowManager.RegisterWindow(window);
    }

    public X11WindowManager WindowManager { get; } = new X11WindowManager();
    public IReadOnlyCollection<X11Window> Windows => WindowManager.Windows;

    internal virtual unsafe void DispatchEvent(XEvent* @event)
    {
        if (WindowManager.TryGetWindow(@event->AnyEvent.window, out var window))
        {
            window.DispatchEvent(@event);
        }
    }

    /// <summary>
    /// 设置屏幕的物理尺寸
    /// </summary>
    /// <param name="widthCentimetre">宽度厘米</param>
    /// <param name="heightCentimetre">高度厘米</param>
    public void SetScreenPhysicalSize(double widthCentimetre, double heightCentimetre)
    {
        X11Info.ScreenPhysicalWidthCentimetre = widthCentimetre;
        X11Info.ScreenPhysicalHeightCentimetre = heightCentimetre;
    }

    private void UpdateScreenPhysicalSize()
    {
        // 获取屏幕物理尺寸
        // 从 Edid 读取的原因是 xrandr 读取不到真实物理尺寸，在 xrandr 即 X11 的各种 API 中都没有找到获取物理尺寸的方法，能获取到的写了的物理尺寸的值也只是通过 DPI 和分辨率进行计算的值，不是真实的物理尺寸
        // https://ubuntuforums.org/showthread.php?t=1461839
        Task.Run(() =>
        {
            var readEdidInfoResult = EdidInfo.ReadFormLinux();
            if (readEdidInfoResult.IsSuccess)
            {
                var edidInfo = readEdidInfoResult.EdidInfo;
                Log.Info(
                    $"[InkCore][ReadEdid] 读取 Edid 成功 屏幕尺寸 WH={edidInfo.BasicDisplayParameters.MonitorPhysicalWidth},{edidInfo.BasicDisplayParameters.MonitorPhysicalHeight}");

                SetScreenPhysicalSize(edidInfo.BasicDisplayParameters.MonitorPhysicalWidth.Value,
                    edidInfo.BasicDisplayParameters.MonitorPhysicalHeight.Value);
            }
            else
            {
                Log.Warn($"[InkCore][ReadEdid] 读取 Edid 失败 {readEdidInfoResult.ErrorMessage}");
            }
        });
    }

    public void ShutDown()
    {
        OnShutDown();

        Exit?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnShutDown()
    {
        X11PlatformThreading.Dispose();
    }

    public event EventHandler? Exit;

    public event EventHandler<UnhandledExceptionEventArgs>? UnhandledException;

    internal void RaiseUnhandledException(Exception exception)
    {
        UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(exception));
    }
}