using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace LoqairjaniferNudalcefinay;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        //var folder = @"C:\lindexi\wpf\artifacts\packaging\Debug\Microsoft.DotNet.Wpf.GitHub.Debug\lib\net9.0\";
        //var presentationCore = Path.Join(folder, "PresentationCore.dll");
        //AssemblyLoadContext.Default.LoadFromAssemblyPath(presentationCore);

        Run();
    }

    private static void Run()
    {
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
