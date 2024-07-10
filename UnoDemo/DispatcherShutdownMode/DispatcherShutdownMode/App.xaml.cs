using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Uno.Resizetizer;
#if HAS_UNO
using Uno.UI.Xaml;
#endif

namespace DispatcherShutdownMode;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

#if HAS_UNO
        // 设置图标的时间太长，设置为空即可跳过这部分的耗时，解决启动窗口闪烁
        // 设置图标只影响任务栏图标
        global::Windows.ApplicationModel.Package.Current.Logo = null;
#endif
    }

    //public Window? MainWindow { get; private set; }
    public DispatcherQueue DispatcherQueue { get; private set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        //MainWindow = new Window();
        ////WindowInteropHelper.HideX11Window(MainWindow);
        //await Task.Delay(2000);
        //MainWindow.Close();

        DispatcherQueue = DispatcherQueue.GetForCurrentThread();

        //_ = Task.Run(() =>
        //{
        //    Console.ReadLine();
        //    Console.WriteLine($"打开窗口");

        //    MainWindow.DispatcherQueue.TryEnqueue(() =>
        //    {
        //        var frame = new Frame();
        //        var window = new Window()
        //        {
        //            Content = frame
        //        };
        //        frame.Navigate(typeof(MainPage));
        //        window.Activate();
        //    });
        //});

        //        MainWindow = new Window();
        //#if DEBUG
        //        MainWindow.EnableHotReload();
        //#endif


        //        // Do not repeat app initialization when the Window already has content,
        //        // just ensure that the window is active
        //        if (MainWindow.Content is not Frame rootFrame)
        //        {
        //            // Create a Frame to act as the navigation context and navigate to the first page
        //            rootFrame = new Frame();

        //            // Place the frame in the current Window
        //            MainWindow.Content = rootFrame;

        //            rootFrame.NavigationFailed += OnNavigationFailed;
        //        }

        //        if (rootFrame.Content == null)
        //        {
        //            // When the navigation stack isn't restored navigate to the first page,
        //            // configuring the new page by passing required information as a navigation
        //            // parameter
        //            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        //        }

        //        MainWindow.SetWindowIcon();
        //        // Ensure the current window is active
        //        MainWindow.Activate();
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

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
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

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
#endif
    }
}

public static class WindowInteropHelper
{
    public static void HideX11Window(Window window)
    {
        IntPtr x11WindowIntPtr = GetX11NativeWindow(window);
        IntPtr display = XLib.XOpenDisplay(IntPtr.Zero);
        try
        {
            XLib.XUnmapWindow(display, x11WindowIntPtr);
            XLib.XFlush(display);

        }
        finally
        {
            XLib.XCloseDisplay(display);
        }
    }

    public static IntPtr GetX11NativeWindow(Window unoWindow)
    {
        // [[X11] Add support for get the Uno's X11 window IntPtr · Issue #17194 · unoplatform/uno](https://github.com/unoplatform/uno/issues/17194 )
#if HAS_UNO
        var x11Window = unoWindow.GetNativeWindow()!;
#else
        var type = unoWindow.GetType();
        var nativeWindowPropertyInfo = type.GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        var x11Window = nativeWindowPropertyInfo!.GetMethod!.Invoke(unoWindow, null)!;
#endif
        // Uno.WinUI.Runtime.Skia.X11.X11Window
        var x11WindowType = x11Window.GetType();
        //Console.WriteLine($"x11WindowType={x11WindowType.FullName}");
        Debug.Assert(string.Equals(x11WindowType.FullName, "Uno.WinUI.Runtime.Skia.X11.X11Window"));

        // (internal record struct X11Window(IntPtr Display, IntPtr Window, (int stencilBits, int sampleCount, IntPtr context)? glXInfo))

        var x11WindowIntPtr =
            (IntPtr) x11WindowType.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!.Invoke(
                x11Window, null)!;
        //Console.WriteLine($"Uno 窗口句柄 {x11WindowIntPtr}");

        return x11WindowIntPtr;
    }

    static unsafe class XLib
    {
        const string libX11 = "libX11.so.6";
        const string libX11Randr = "libXrandr.so.2";
        const string libX11Ext = "libXext.so.6";
        const string libXInput = "libXi.so.6";
        const string libXcomposite = "libXcomposite.so.1";

        [DllImport(libX11)]
        public static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern int XCloseDisplay(IntPtr display);

        [DllImport(libX11)]
        public static extern int XUnmapWindow(IntPtr display, IntPtr window);

        [DllImport(libX11)]
        public static extern int XFlush(IntPtr display);
    }
}
