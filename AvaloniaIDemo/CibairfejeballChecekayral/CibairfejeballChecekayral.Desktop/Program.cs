using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.ReactiveUI;

namespace CibairfejeballChecekayral.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        LoadNativeLib();
        
        RunAvalonia(args);
    }

    private static void LoadNativeLib()
    {
        var assembly = typeof(Program).Assembly;
        var manifestResourceNames = assembly.GetManifestResourceNames();

        var platform = "win_x86";
        var platformResource = $"CibairfejeballChecekayral.Desktop.Assets.{platform}.";
        var folder = Directory.CreateDirectory(Path.Join(AppContext.BaseDirectory, platform));

        foreach (var manifestResourceName in manifestResourceNames)
        {
            if (manifestResourceName.StartsWith(platformResource))
            {
                using var manifestResourceStream = assembly.GetManifestResourceStream(manifestResourceName)!;
                var fileName = manifestResourceName[platformResource.Length..];
                var file = Path.Join(folder.FullName, fileName);
                if (!File.Exists(file))
                {
                    using var fileStream = File.OpenWrite(file);
                    manifestResourceStream.CopyTo(fileStream);
                }

                NativeLibrary.Load(file);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunAvalonia(string[] args)
    {
        return BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
