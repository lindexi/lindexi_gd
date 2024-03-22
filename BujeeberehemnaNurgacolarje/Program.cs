using System.Runtime.Loader;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Sia;

using Uno.Foundation.Logging;
using Uno.WinUI.Runtime.Skia.X11;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using static CPF.Linux.XLib;
using CPF.Linux;
using XEvent = CPF.Linux.XEvent;
using XKeySym = CPF.Linux.XKeySym;
using System.Reflection.Metadata;
using System.Net;
using System;
using ColorFlags = CPF.Linux.ColorFlags;
using EventMask = CPF.Linux.EventMask;
using XColor = CPF.Linux.XColor;
using XEventMask = CPF.Linux.XEventMask;
using XEventName = CPF.Linux.XEventName;
using XSetWindowAttributes = CPF.Linux.XSetWindowAttributes;

namespace BujeeberehemnaNurgacolarje;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        AssemblyLoadContext.Default.Resolving += Default_Resolving;

        StartX11App();

        return;

        var x11ApplicationHost = new X11ApplicationHost(() =>
        {
            var application = new Sia.App();

            var factory = LoggerFactory.Create(builder =>
            {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
                builder.AddConsole();
#endif

                // Exclude logs below this level
                builder.SetMinimumLevel(LogLevel.Information);

                // Default filters for Uno Platform namespaces
                builder.AddFilter("Uno", LogLevel.Information);
                builder.AddFilter("Windows", LogLevel.Information);
                builder.AddFilter("Microsoft", LogLevel.Information);

                // Generic Xaml events
                // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

                // Layouter specific messages
                // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

                // builder.AddFilter("Windows.Storage", LogLevel.Debug );

                // Binding related messages
                // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

                // Binder memory references tracking
                // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

                // DevServer and HotReload related
                // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

                // Debug JS interop
                // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
            });

            global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
            global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif

            return application;
        });

        x11ApplicationHost.Run();
    }

    private static System.Reflection.Assembly? Default_Resolving(AssemblyLoadContext context, System.Reflection.AssemblyName assemblyName)
    {
        var file = $"{assemblyName.Name}.dll";
        file = Path.Join(AppContext.BaseDirectory, file);

        if (File.Exists(file))
        {
            return context.LoadFromAssemblyPath(file);
        }

        return null;
    }

    private static void StartX11App()
    {
        var app = new App();
        app.Run();
    }
}

class App
{
    public App()
    {
        XInitThreads();
        Display = XOpenDisplay(IntPtr.Zero);
        XError.Init();
        Info = new X11Info(Display, DeferredDisplay);
        Console.WriteLine("XInputVersion=" + Info.XInputVersion);
        var screen = XDefaultScreen(Display);
        Console.WriteLine($"Screen = {screen}");
        Screen = screen;
        var white = XWhitePixel(Display, screen);
        var black = XBlackPixel(Display, screen);
        Window = XCreateSimpleWindow(Display, XDefaultRootWindow(Display), 0, 0, 300, 300, 5, white, black);
        
        Console.WriteLine($"Window={Window}");
        //XSetStandardProperties(dis, win, "My Window", "HI!", None, NULL, 0, NULL);
        //var protocols = new[]
        //{
        //    Info.Atoms.WM_DELETE_WINDOW
        //};
        //XSetWMProtocols(Display, Window, protocols, protocols.Length);
        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask | XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XSelectInput(Display, Window, mask);

        XClearWindow(Display, Window);
        XMapWindow(Display,Window);


        XFlush(Info.Display);
        GC = XCreateGC(Display, Window, 0, 0);
    }

    public void Run()
    {
        Console.WriteLine("Run");
        XRaiseWindow(Display, Window);
        XSetInputFocus(Display, Window, 0, IntPtr.Zero);

        XEvent @event;
        XKeySym key;
        while (true)
        {
            var result = XSync(Display, false);
            Console.WriteLine($"XSync={result}");

            var xNextEvent = XNextEvent(Display, out @event);
            Console.WriteLine($"NextEvent={xNextEvent} {@event}");

            if (@event.type == XEventName.Expose)
            {
                Redraw();
            }
            else if (@event.type == XEventName.ButtonPress)
            {
                XDrawRectangle(Display, Window, GC, @event.ButtonEvent.x, @event.ButtonEvent.y, 2, 2);
            }

            if (xNextEvent != 0)
            {
                break;
            }
        }
    }

    

    private void Redraw()
    {
        //var colormap = XCreateColormap(Display,Window,XDefaultVisual(Display,Screen),1);
        //Console.WriteLine($"Colormap={colormap}");

        //var color = new XColor()
        //{
        //    red = 32000,
        //    flags = (byte) (ColorFlags.DoRed | ColorFlags.DoBlue | ColorFlags.DoGreen)
        //};
        //XAllocColor(Display, colormap, ref color);
        //Console.WriteLine($"Pixel={color.pixel}");
        //var foreground = XSetForeground(Display, GC, color.pixel);
        //Console.WriteLine($"Foreground={foreground}");

        var white = XWhitePixel(Display, Screen);
        var foreground = XSetForeground(Display, GC, white);
        Console.WriteLine($"Foreground={foreground}");

        XDrawRectangle(Display, Window, GC, 10, 10, 100, 100);
    }

    private IntPtr GC { get; }

    public IntPtr DeferredDisplay { get; set; }
    public IntPtr Display { get; set; }

    //public XI2Manager XI2;
    public X11Info Info { get; private set; }
    public IntPtr Window { get; set; }
    public int Screen { get; set; }
}