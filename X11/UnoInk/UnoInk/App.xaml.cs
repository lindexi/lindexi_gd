using System;
<<<<<<< HEAD
<<<<<<< HEAD
using System.Reflection;
using Windows.Graphics;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Uno.Resizetizer;
using UnoHacker;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using UnoInk.Inking.UnoInkCore;
using Uno.UI.Xaml;
using SkiaSharp;
=======

using Microsoft.Extensions.Logging;

using Uno.Resizetizer;
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======

using Microsoft.Extensions.Logging;

using Uno.Resizetizer;
#if HAS_UNO
using Uno.UI.Xaml;
#endif
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b

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
<<<<<<< HEAD
<<<<<<< HEAD
        //ShowSecondWindow();

        var unoInkWindow = new UnoInkFullScreenWindow();
        unoInkWindow.Activate();
        unoInkWindow.AppWindow.Move(new PointInt32()
        {
            X = 100,
            Y = 100
        });
        return;
        
        Console.WriteLine($"Before new Window");
        MainWindow = new Window();
        Console.WriteLine($"After new Window");
#if DEBUG
        MainWindow.EnableHotReload();
#endif
        // 这里导致没有触摸，因为没有使用控件
        //MainWindow.Content = new Grid()
        //{
        //    // 只靠这里是不够
        //    Background = new SolidColorBrush(Colors.Transparent)
        //};

#if HAS_UNO
        // 这句话似乎也是无效的
        MainWindow.SetBackground(new SolidColorBrush(Colors.Transparent));
        MainWindow.AppWindow.GetApplicationView().TryEnterFullScreenMode();
#endif
     
=======
        MainWindow = new Window();
#if DEBUG
        MainWindow.EnableHotReload();
#endif

>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======
        MainWindow = new Window();
#if DEBUG
        MainWindow.EnableHotReload();
#endif
#if HAS_UNO
        var nativeWindow = MainWindow.GetNativeWindow();
        Console.WriteLine($"NativeWindow={nativeWindow}");
#endif
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();
<<<<<<< HEAD
<<<<<<< HEAD
            rootFrame.Background = new SolidColorBrush(Colors.Transparent);
=======
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

<<<<<<< HEAD
<<<<<<< HEAD
        // 背景透明需要 UNO 还没发布的版本
        // https://github.com/lindexi/uno/tree/7b282851a8ec3ed7eb42a53af8b50ea7fe045d56
        // 这句话似乎才是关键，设置窗口背景透明。通过 MainWindow.SetBackground 配置是无效的
        Hacker.Do();

        MainWindow.SetWindowIcon();
        //Console.WriteLine($"Before Activate");
        // Ensure the current window is active
        MainWindow.Activate();
        //// 此时 x11 窗口已创建
        //var unoX11Window = GetUnoX11Window(MainWindow);
        //Console.WriteLine($"After Activate X11:{unoX11Window}");

=======
        MainWindow.SetWindowIcon();
        // Ensure the current window is active
        MainWindow.Activate();
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======
        MainWindow.SetWindowIcon();
        rootFrame.Loaded += (sender, eventArgs) =>
        {
            Console.WriteLine($"Activate");
            // Ensure the current window is active
            MainWindow.Activate();
        };
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
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
