using System.Diagnostics;

using X11ApplicationFramework.Apps;
using X11ApplicationFramework.Natives;
using X11ApplicationFramework.Primitive;
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


            var list = x11Info.EnumerateChildrenViaXQueryTree();
            Console.WriteLine($"ListCount={list.Count}");
            foreach (var xWindowId in list)
            {
                var name = x11Info.GetWindowName(xWindowId);
                Console.WriteLine($"XID={xWindowId.Handle:X} Name={name}");

                XWindowAttributes attributes = default;
                XGetWindowAttributes(x11Info.Display, xWindowId, ref attributes);

                Console.WriteLine($"  X={attributes.x};Y={attributes.y};Width={attributes.width};Height={attributes.height}");

                var childrenWindowList = x11Info.EnumerateChildrenViaXQueryTree(xWindowId);
                Console.WriteLine($"  Children Count={childrenWindowList.Count}");
                foreach (var childXWindowId in childrenWindowList)
                {
                    name = x11Info.GetWindowName(childXWindowId);
                    Console.WriteLine($"  XID={childXWindowId.Handle:X} Name={name}");
                }
            }
        });
    };
    x11Application.Run();
}

Console.WriteLine("Hello, World!");
