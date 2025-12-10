using System.Diagnostics;

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
        Debug.Assert(OperatingSystem.IsLinux());
        x11Application.Dispatcher.InvokeAsync(() =>
        {
            Debug.Assert(OperatingSystem.IsLinux());

            var x11Info = x11Application.X11Info;

            var list = x11Info.EnumerateTopLevelWindowsViaNetClientList();
            Console.WriteLine($"ListCount={list.Count}");
            foreach (var xWindowId in list)
            {
                Console.WriteLine(xWindowId);
            }
        });
    };
    x11Application.Run();
}

Console.WriteLine("Hello, World!");
