using System.Configuration;
using System.Data;
using System.IO;
using System.Runtime.Loader;
using System.Windows;

namespace LaifearwheafoWhemhaldaycearai;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        //AssemblyLoadContext.Default.Resolving += (context, name) =>
        //{
        //    if (name.Name?.StartsWith("AdaptiveCards.Rendering.Wpf", StringComparison.OrdinalIgnoreCase) is true)
        //    {
        //        var filePath = Path.Join(AppContext.BaseDirectory, "AdaptiveCards.Rendering.Wpf.Net6.dll");
        //        return context.LoadFromAssemblyPath(filePath);
        //    }

        //    return null;
        //};
    }
}

