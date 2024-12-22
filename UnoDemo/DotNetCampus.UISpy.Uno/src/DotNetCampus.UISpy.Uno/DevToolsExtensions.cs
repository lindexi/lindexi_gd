using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Windows.System;

namespace DotNetCampus.UISpy.Uno;

public static class DevToolsExtensions
{
    [Conditional("DEBUG")]
    public static void DebugAttachDevTools(this UIElement rootElement)
    {
        AttachDevTools(rootElement);
    }

    public static void AttachDevTools(this UIElement rootElement)
    {
        var root = rootElement.VisualRoot();
        root.KeyDown -= RootElement_KeyDown;
        root.KeyDown += RootElement_KeyDown;
    }

    private static void RootElement_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var root = (UIElement) sender;
        if (e.Key is not VirtualKey.F12)
        {
            return;
        }

        root.ShowUnoSpyWindow();
    }

    public static void ShowUnoSpyWindow(this UIElement element)
    {
        var unoSpyWindow = new UnoSpyWindow(element);
        unoSpyWindow.Activate();
    }
}

file class XamlRootProxy(XamlRoot xamlRoot)
{
    public Window HostWindow => GetHostWindow(xamlRoot);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_HostWindow")]
    private static extern Window GetHostWindow(XamlRoot xamlRoot);
}

file static class XamlRootExtensions
{
    public static XamlRootProxy AsPrivateProxy(this XamlRoot xamlRoot)
    {
        return new XamlRootProxy(xamlRoot);
    }
}

file static class VisualExtensions
{
    public static UIElement VisualRoot(this UIElement visual)
    {
        var current = visual;
        while (true)
        {
            var v = VisualTreeHelper.GetParent(current) as UIElement;
            if (v is null)
            {
                break;
            }
            current = v;
        }
        return current;
    }
}
