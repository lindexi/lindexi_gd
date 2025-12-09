// See https://aka.ms/new-console-template for more information

using X11ApplicationFramework.Apps;

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
        var x11Info = x11Application.X11Info;

        var monitors = XRRGetMonitors(x11Info.Display, x11Info.RootWindow, true, out var count);

        Console.WriteLine($"获取显示器数量: {count}");

        for (int i = 0; i < count; i++)
        {
            var monitor = monitors[i];
            Console.WriteLine($"显示器 {i}:");
            //Console.WriteLine($"  名称: {new string(monitor.Name)}");
            Console.WriteLine($"  位置: ({monitor.X}, {monitor.Y})");
            Console.WriteLine($"  大小: {monitor.Width}x{monitor.Height}");
            Console.WriteLine($"  主显示器: {monitor.Primary}");
        }
    };
    x11Application.Run();
}

Console.WriteLine("Hello, World!");
