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

            App.ProductEntry = entry;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            App.ProductEntry = null;
            entry?.Dispose();
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