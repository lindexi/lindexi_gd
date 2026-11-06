using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace MediaFoundation.Controls;

internal sealed class VideoHost : HwndHost
{
    internal event Action<nint>? Created;
    internal event Action? Destroying;
    internal event Action? Resized;

    protected override unsafe HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var window = PInvoke.CreateWindowEx
        (
            0, "STATIC", "",
            WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CLIPCHILDREN |
            WINDOW_STYLE.WS_CLIPSIBLINGS,
            0, 0, 1, 1, new HWND(hwndParent.Handle), default, PInvoke.GetModuleHandle((string?)null), null
        );
        if (window.IsNull) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            Created?.Invoke((nint)window.Value);
        }
        catch
        {
            PInvoke.DestroyWindow(window);
            throw;
        }

        return new HandleRef(this, (nint)window.Value);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        Destroying?.Invoke();
        if (!PInvoke.DestroyWindow(new HWND(hwnd.Handle))) throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    protected override nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == 0x007B)
        {
            DependencyObject? element = this;
            while (element != null)
            {
                if (element is FrameworkElement { ContextMenu: { } menu } target)
                {
                    menu.PlacementTarget = target;
                    menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                    menu.IsOpen = true;
                    handled = true;
                    return 0;
                }
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
        }
        return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
    }

    protected override void OnWindowPositionChanged(Rect rcBoundingBox)
    {
        base.OnWindowPositionChanged(rcBoundingBox);
        Resized?.Invoke();
    }
}