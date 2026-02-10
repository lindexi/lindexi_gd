using Avalonia;
using System;
using System.IO;
using System.Runtime.Loader;

namespace YewhuhuciDacayfudawi;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AssemblyLoadContext.Default.Resolving += Default_Resolving;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static System.Reflection.Assembly? Default_Resolving(AssemblyLoadContext assemblyLoadContext, System.Reflection.AssemblyName assemblyName)
    {
        if (assemblyName.Name == "Avalonia.HarfBuzz")
        {
        }

        var assemblyFile = Path.Join(AppContext.BaseDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(assemblyFile))
        {
            return assemblyLoadContext.LoadFromAssemblyPath(assemblyFile);
        }

        return null;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
