using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using CPF.Linux;
using UnoInk.X11Ink;
using UnoInk.X11Platforms.Threading;
using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;

namespace UnoInk.X11Platforms;

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
        
        var x11Info = new X11Info(display, screen, rootWindow);
        X11Info = x11Info;
    }

    public X11Info X11Info { get; }

    [MemberNotNull(nameof(X11PlatformThreading))]
    public void Start()
    {
        X11PlatformThreading = new X11PlatformThreading(this);
        X11PlatformThreading.Run();
    }
    
    [MemberNotNull(nameof(X11PlatformThreading))]
    public void EnsureStart()
    {
        if (X11PlatformThreading is null)
        {
            throw new InvalidOperationException();
        }
    }

    public X11PlatformThreading? X11PlatformThreading { get; set; }
    
    internal virtual void DispatchEvent(XEvent @event)
    {
        
    }
}
