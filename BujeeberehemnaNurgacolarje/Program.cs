using System.Runtime.Loader;
using Microsoft.UI.Xaml;
using Uno.WinUI.Runtime.Skia.X11;

namespace BujeeberehemnaNurgacolarje;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        AssemblyLoadContext.Default.Resolving += Default_Resolving;

        var x11ApplicationHost = new X11ApplicationHost(() =>
        {
            var application = new App();
           
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
}

class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        var window = Window.Current;
        window.Activate();
    }
}