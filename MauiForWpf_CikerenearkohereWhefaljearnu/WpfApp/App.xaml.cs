using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Windows;
using MauiWpfAdapt.Hosts;

namespace WpfApp;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        AssemblyLoadContext.Default.Resolving += AssemblyLoadContext_Resolving;

        var mauiApplicationProxy = MauiForWpfHostHelper.InitMauiApplication(this);
        MauiApplicationProxy = mauiApplicationProxy;
    }

    private System.Reflection.Assembly? AssemblyLoadContext_Resolving(AssemblyLoadContext context, System.Reflection.AssemblyName assemblyName)
    {
        var folder = Path.GetDirectoryName(typeof(App).Assembly.Location);
        var file = Path.Join(folder, assemblyName.Name + ".dll");
        if (File.Exists(file))
        {
            return context.LoadFromAssemblyPath(file);
        }

        return null;
    }

    public MauiApplicationProxy MauiApplicationProxy { get; }
}
