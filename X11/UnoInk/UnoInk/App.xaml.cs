<<<<<<< HEAD
<<<<<<< HEAD
using System;
<<<<<<< HEAD
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

=======

using Microsoft.Extensions.Logging;
using Microsoft.UI;
>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3
using Uno.Resizetizer;
#if HAS_UNO
using Uno.UI.Xaml;
#endif
<<<<<<< HEAD
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
=======
>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3
=======
using Uno.Foundation.Extensibility;
=======
>>>>>>> 6e8b447b0a87c7b2e270193e467236399f292aed
using Uno.Resizetizer;
>>>>>>> 86984cb5eab3fd16df49ab173ec129b7bdf7ec0e

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
<<<<<<< HEAD
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e.Exception);

        var appUnhandledExceptionLogger =
            global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger("AppUnhandledException");
        appUnhandledExceptionLogger.LogWarning(e.Message, e.Exception);
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        //ShowSecondWindow();

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
<<<<<<< HEAD
     
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
=======
        MainWindow = new Window();
#if DEBUG
        MainWindow.EnableHotReload();
#endif

#if HAS_UNO
        MainWindow.SetBackground(new SolidColorBrush(Colors.Red));
#endif
>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3
=======

>>>>>>> 51b5111169159630cdc7b25028403087323e7144

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
<<<<<<< HEAD
            rootFrame = new Frame();
<<<<<<< HEAD
<<<<<<< HEAD
            rootFrame.Background = new SolidColorBrush(Colors.Transparent);
=======
>>>>>>> 5adb265d4a50b5cb5c3d83f6d1bef1cd2ada93c0
=======
>>>>>>> dbc7fdb4f998d39ec1f48b0046d8f88914f6b50b
=======
            rootFrame = new Frame()
            {
                Background = new SolidColorBrush(Colors.Transparent)
            };
>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3

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
=======
        MainWindow.SetWindowIcon();
        // Ensure the current window is active
        MainWindow.Activate();
>>>>>>> bb3ca7ae7043b68590131278b336efd0445ad5c3
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
        var taskExceptionLogger = factory.CreateLogger("TaskException");

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Console.WriteLine(args.Exception);

            taskExceptionLogger.LogWarning($"[TaskException] {args.Exception}");
        };
#endif
=======
        
#if HAS_UNO
   
#endif
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    // TODO: Register your services
                    //services.AddSingleton<IMyService, MyService>();
                })
                .UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.EnableHotReload();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellModel)),
            new ViewMap<MainPage, MainModel>(),
            new DataViewMap<SecondPage, SecondModel, Entity>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellModel>(),
                Nested:
                [
                    new ("Main", View: views.FindByViewModel<MainModel>()),
                    new ("Second", View: views.FindByViewModel<SecondModel>()),
                ]
            )
        );
>>>>>>> 86984cb5eab3fd16df49ab173ec129b7bdf7ec0e
    }
}
