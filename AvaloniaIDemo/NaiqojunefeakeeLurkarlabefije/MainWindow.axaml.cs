using System.Diagnostics;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace NaiqojunefeakeeLurkarlabefije;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //SpinWait.SpinUntil(() => Debugger.IsAttached);

        //Debugger.Break();

        var topLevel = TopLevel.GetTopLevel(this);
        var platformImpl = topLevel.PlatformImpl;

        var type = platformImpl.GetType();

        //foreach (var property in type.GetProperties(BindingFlags.Instance| BindingFlags.Public))
        //{
        //    Console.WriteLine(property);
        //}

        var propertyInfo = type.GetProperty("Handle", BindingFlags.Instance | BindingFlags.Public);

        var value = propertyInfo.GetValue(platformImpl);

        if (value is PlatformHandle platformHandle)
        {
            var x11Handler = platformHandle.Handle;
            Console.WriteLine(x11Handler);
        }

    }
}