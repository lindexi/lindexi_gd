using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using BehairracercairJifelalihay;
using CPF.Linux;
using UnoInk.Inking.X11Platforms.Threading;
using static CPF.Linux.XLib;

namespace UnoInk.Inking.X11Platforms;

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
            return;
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
        X11PlatformThreading.RunInNewThread();
    }

    public X11PlatformThreading X11PlatformThreading { get; }

    internal virtual void DispatchEvent(XEvent @event)
    {

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
                StaticDebugLogger.WriteLine($"读取 Edid 成功 屏幕尺寸 {edidInfo.BasicDisplayParameters.MonitorPhysicalWidth} {edidInfo.BasicDisplayParameters.MonitorPhysicalHeight}");

                SetScreenPhysicalSize(edidInfo.BasicDisplayParameters.MonitorPhysicalWidth.Value, edidInfo.BasicDisplayParameters.MonitorPhysicalHeight.Value);
            }
            else
            {
                StaticDebugLogger.WriteLine($"读取 Edid 失败 {readEdidInfoResult.ErrorMessage}");
            }
        });
    }
}
