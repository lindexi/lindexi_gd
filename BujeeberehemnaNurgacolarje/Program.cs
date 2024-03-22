using System.Runtime.Loader;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Sia;

using Uno.Foundation.Logging;
using Uno.WinUI.Runtime.Skia.X11;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
            var application = new App();

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
    }
}

