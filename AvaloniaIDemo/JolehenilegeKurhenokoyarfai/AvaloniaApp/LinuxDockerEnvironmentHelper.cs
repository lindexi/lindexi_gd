using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AvaloniaApp;

static class LinuxDockerEnvironmentHelper
{
    public static void EnsureX11Ready()
    {
        //Console.WriteLine($"EnsureX11Ready");

        if (OperatingSystem.IsLinux() && IsRunningInDocker())
        {
            var display = Environment.GetEnvironmentVariable("DISPLAY");
            if (display is null)
            {
                throw new InvalidOperationException($"Can not find DISPLAY Environment Variable. 找不到 DISPLAY 环境变量，即使后续启动 Xvfb 和调用 SetEnvironmentVariable 都不能让 XOpenDisplay 成功");
            }

            // 预期已经完成安装 xvfb 了
            // apt-get install -y xvfb
            // [xvfb 、xvnc、dummy、gdm、xrandr以及wayland的含义、概念 - mixyoung - 博客园](https://www.cnblogs.com/mixyoung/p/18503669/xvfb-xvnc-dummy-gdm-xrandr-and-wayland-s-meaning-and-concept-2ipotu )

            Process.Start("Xvfb",
            [
                display,
                "-screen", "0",
                "1920x1080x24"
            ]);

            //Console.WriteLine($"Start xvfb");

            var stopwatch = Stopwatch.StartNew();
            var spinWait = new SpinWait();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                var displayHandler = XOpenDisplay(IntPtr.Zero);
                //Console.WriteLine($"XOpenDisplay {displayHandler}");
                if (displayHandler == 0)
                {
                    spinWait.SpinOnce(10);
                }
                else
                {
                    Console.WriteLine($"Wait {stopwatch.Elapsed}");
                    XCloseDisplay(displayHandler);
                    return;
                }
            }

            throw new NotSupportedException($"Can not start X11");
        }
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    private static bool IsRunningInDocker()
    {
        // 这个变量也是做 docker 写的
        var environmentVariable = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        return string.Equals(environmentVariable, "true", StringComparison.OrdinalIgnoreCase);
    }
}