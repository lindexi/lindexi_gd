using Avalonia;

namespace XiaoXiIme.ImeUi.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return;
        }
        using var entry = new ProductUiEntry(args);
        if (!entry.IsOwner || args.Contains("--exit")) return;
        App.ProductEntry = entry;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
