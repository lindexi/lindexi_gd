using X11ApplicationFramework.Apps;
using X11ApplicationFramework.Natives;
using X11ApplicationFramework.Utils;

using static X11ApplicationFramework.Natives.XLib;

if (!OperatingSystem.IsLinux())
{
    Console.WriteLine($"以下代码只能在 Linux 运行");
    return;
}

unsafe
{
    var x11Application = new X11Application();
    x11Application.Startup += (_, _) =>
    {
        x11Application.Dispatcher.InvokeAsync(() =>
        {
            var x11Info = x11Application.X11Info;
            var displayInfos = MultiScreensDisplayInfoHelper.GetDisplayInfos(x11Info);
            Console.WriteLine($"显示器数量: {displayInfos.Count}");

            foreach (var displayInfo in displayInfos)
            {
                Console.WriteLine($"显示器信息: 宽度={displayInfo.Width}, 高度={displayInfo.Height}, EDID名称={displayInfo.EDIDName}, 是否主显示器={displayInfo.IsPrimary}");
            }
        });
    };
    x11Application.Run();
}

Console.WriteLine("Hello, World!");
