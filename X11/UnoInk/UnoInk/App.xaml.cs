using System;
using System.Reflection;
using Windows.Graphics;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Uno.Resizetizer;
using UnoHacker;
using Windows.UI.ViewManagement;


#if HAS_UNO
using Uno.UI.Xaml;
#endif

namespace UnoInk;
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ShowSecondWindow();

        //        Console.WriteLine($"Before new Window");
        //        MainWindow = new Window();
        //        Console.WriteLine($"After new Window");
        //#if DEBUG
        //        MainWindow.EnableHotReload();
        //#endif
        //        MainWindow.Content = new Grid()
        //        {
        //            Background = new SolidColorBrush(Colors.White)
        //        };

        //        //// Do not repeat app initialization when the Window already has content,
        //        //// just ensure that the window is active
        //        //if (MainWindow.Content is not Frame rootFrame)
        //        //{
        //        //    // Create a Frame to act as the navigation context and navigate to the first page
        //        //    rootFrame = new Frame();

        //        //    // Place the frame in the current Window
        //        //    MainWindow.Content = rootFrame;

        //        //    rootFrame.NavigationFailed += OnNavigationFailed;
        //        //}

        //        //if (rootFrame.Content == null)
        //        //{
        //        //    // When the navigation stack isn't restored navigate to the first page,
        //        //    // configuring the new page by passing required information as a navigation
        //        //    // parameter
        //        //    rootFrame.Navigate(typeof(MainPage), args.Arguments);
        //        //}

        //        MainWindow.SetWindowIcon();
        //        Console.WriteLine($"Before Activate");
        //        // Ensure the current window is active
        //        MainWindow.Activate();
        //        //// 此时 x11 窗口已创建
        //        //var unoX11Window = GetUnoX11Window(MainWindow);
        //        //Console.WriteLine($"After Activate X11:{unoX11Window}");

    }

    private void ShowSecondWindow()
    {
        // 第二个窗口也是会闪烁，也就是只要是窗口就会闪烁
        var window = new InnerBoardWindow()
        {
            Content = new Grid()
            {
                Background = new SolidColorBrush(Colors.Transparent)
            }
        };


        //ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
#if HAS_UNO
        window.AppWindow.GetApplicationView().TryEnterFullScreenMode();

        // Do nothing in Skia.Gtk
        window.SetBackground(new SolidColorBrush(Colors.Transparent));
#endif
        //window.AppWindow.Move(new PointInt32()
        //{
        //    X = 500,
        //    Y = 0,
        //});
        window.Activate();

#if HAS_UNO
        // GetNativeWindow=X11Window { Display = 139734279120656, Window = 111149057, glXInfo =  }
        var nativeWindow = window.GetNativeWindow();
        Console.WriteLine($"GetNativeWindow={nativeWindow}");
#endif

        Hacker.Do();
    }

    IntPtr GetUnoX11Window(Window unoWindow)
    {
        var type = unoWindow.GetType();
        var nativeWindowPropertyInfo = type.GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        var x11Window = nativeWindowPropertyInfo!.GetMethod!.Invoke(unoWindow, null)!;
        // Uno.WinUI.Runtime.Skia.X11.X11Window
        var x11WindowType = x11Window.GetType();

        var x11WindowIntPtr = (IntPtr) x11WindowType.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!.Invoke(x11Window, null)!;
        return x11WindowIntPtr;
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


partial class InnerBoardWindow : Window
{
    public InnerBoardWindow()
    {
        Content = new Grid()
        {
            Background = new SolidColorBrush(Colors.White)
        };
    }
}
