using Avalonia;

namespace XiaoXiIme.ImeUi.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ProductUiEntry? entry = null;
        try
        {
            var experience = args.Length == 0 || args[0] is "--dictionary-debug" or "--experience";
            string? identity = null;
            if (experience)
            {
                var index = Array.IndexOf(args, "--debug-settings-path");
                var path = index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
                identity = "experience\0" + DictionaryDebugSettings.ResolvePath(path).ToUpperInvariant();
            }

            if (!experience)
            {
                TraceStartup("Acquiring UI instance");
            }
            try
            {
                entry = new ProductUiEntry(args, identity);
            }
            catch (IOException exception)
            {
                App.StartupError = exception;
            }
            catch (TimeoutException exception)
            {
                App.StartupError = exception;
            }
            catch (UnauthorizedAccessException exception)
            {
                App.StartupError = exception;
            }

            if (entry is not null && (!entry.IsOwner || args.Contains("--exit")))
            {
                return;
            }

            if (!experience)
            {
                TraceStartup(entry is null ? $"UI instance unavailable: {App.StartupError}" : "Initializing Avalonia");
            }
            App.ProductEntry = entry;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            App.ProductEntry = null;
            entry?.Dispose();
        }
    }

    internal static void TraceStartup(string message)
    {
        if (Environment.GetEnvironmentVariable("XIAOXIIME_STARTUP_TRACE") == "1")
        {
            Console.Error.WriteLine($"[ImeUI][Startup] {message}");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}