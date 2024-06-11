using System;
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
using UnoInk.UnoInkCore;
using Uno.UI.Xaml;
using SkiaSharp;
using UnoInk.Inking.X11Platforms;

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
        UnhandledException += App_UnhandledException;
        //this.DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
#if HAS_UNO
        this.Suspending += (sender, args) =>
        {
            // 这里好像就是退出事件了
        };

        // 设置图标的时间太长，设置为空即可跳过这部分的耗时，解决启动窗口闪烁
        // 设置图标只影响任务栏图标
        Console.WriteLine(global::Windows.ApplicationModel.Package.Current.Logo);
        //global::Windows.ApplicationModel.Package.Current.Logo = null;
#endif
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var appUnhandledExceptionLogger =
            global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger("AppUnhandledException");
        appUnhandledExceptionLogger.LogWarning(e.Message, e.Exception);
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        //ShowSecondWindow();
        StaticDebugLogger.WriteLine($"OnLaunched 时间 {Environment.TickCount64} Thread={Environment.CurrentManagedThreadId}");

        var unoInkWindow = new UnoInkFullScreenWindow();
        unoInkWindow.Activate();
        // 这是无效
        //unoInkWindow.AppWindow.Move(new PointInt32()
        //{
        //    X = 100,
        //    Y = 100
        //});
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


        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();
            rootFrame.Background = new SolidColorBrush(Colors.Transparent);

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
            builder.AddFilter("Uno", LogLevel.Information);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
            
            builder.AddFilter("Uno.WinUI.Runtime.Skia.X11", LogLevel.Debug);
            
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
        var taskExceptionLogger = factory.CreateLogger("TaskException");

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            taskExceptionLogger.LogWarning($"[TaskException] {args.Exception}");
        };
#endif
    }
}
