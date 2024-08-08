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
        var topLevel = TopLevel.GetTopLevel(this)!;

        // 通过窗口获取，方法更加简单：
        var handle = topLevel.TryGetPlatformHandle()!;
        Console.WriteLine($"X11 xid {handle.Handle}");

        // 以下就是反射的方法：
        var platformImpl = topLevel.PlatformImpl;

        var type = platformImpl.GetType();

        var propertyInfo = type.GetProperty("Handle", BindingFlags.Instance | BindingFlags.Public);

        var value = propertyInfo.GetValue(platformImpl);

        Debug.Assert(value is IPlatformHandle);

        if (value is PlatformHandle platformHandle)
        {
            var x11Handler = platformHandle.Handle;
            Console.WriteLine(x11Handler);
        }
        else if(value is IPlatformHandle platformHandle2)
        {
            // 当前在 Windows 的没有明确的类型，是一个放在 WindowImpl 类中的 WindowImplPlatformHandle 内部类
            var hwnd = platformHandle2.Handle;
            Console.WriteLine(hwnd);
        }
    }
}