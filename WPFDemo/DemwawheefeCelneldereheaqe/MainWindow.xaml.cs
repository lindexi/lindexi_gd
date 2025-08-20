using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DemwawheefeCelneldereheaqe;

/// <summary>
/// Interaction logic for MyWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int WM_ERASEBKGND = 0x0014;
    private const int WM_SHOWWINDOW = 0x0018;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var hwndSource = (HwndSource) PresentationSource.FromVisual(this);
        if (hwndSource != null)
        {
            hwndSource.AddHook(WndProc);
        }
        base.OnSourceInitialized(e);
    }

    private void LogWindowsMessage(int msg)
    {
        switch (msg)
        {
            case 0x0018: Debug.WriteLine("WM_SHOWWINDOW"); break;
            //case 0x0046: Debug.WriteLine("WM_WINDOWPOSCHANGING"); break;
            //case 0x0047: Debug.WriteLine("WM_WINDOWPOSCHANGED"); break;
            //case 0x0086: Debug.WriteLine("WM_NCACTIVATE"); break;
            //case 0x0006: Debug.WriteLine("WM_ACTIVATE"); break;
            //case 0x0281: Debug.WriteLine("WM_IME_SETCONTEXT"); break;
            //case 0x0282: Debug.WriteLine("WM_IME_NOTIFY"); break;
            //case 0x003D: Debug.WriteLine("WM_GETOBJECT"); break;
            //case 0x0007: Debug.WriteLine("WM_SETFOCUS"); break;
            //case 0x0085: Debug.WriteLine("WM_NCPAINT"); break;
            case 0x0014: Debug.WriteLine("WM_ERASEBKGND"); break;
            //case 0x0005: Debug.WriteLine("WM_SIZE"); break;
            //case 0x000D: Debug.WriteLine("WM_GETTEXT"); break;
            //case 0x0003: Debug.WriteLine("WM_MOVE"); break;
            //case 0x007C: Debug.WriteLine("WM_STYLECHANGING"); break;
            //case 0x007D: Debug.WriteLine("WM_STYLECHANGED"); break;
            //case 0x001C: Debug.WriteLine("WM_ACTIVATEAPP"); break;
            //case 0x0008: Debug.WriteLine("WM_KILLFOCUS"); break;
            // Unknown or custom messages
            case 0x031F:
            case 0xC24E:
            case 0xC17A:
            case 0xC250:
                //Debug.WriteLine($"Message: 0x{msg:X4}"); break;
            default:
                //Debug.WriteLine($"Message: 0x{msg:X4}");
                break;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        LogWindowsMessage(msg);
        switch (msg)
        {
            case WM_ERASEBKGND:
                FillRect(hwnd);
                int cloak1 = 0; // 1 to enable cloaking, 0 to disable
                DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.Cloak, ref cloak1, sizeof(int));
                break;
            case WM_SHOWWINDOW:
                FillRect(hwnd);
                int cloak = 1; // 1 to enable cloaking, 0 to disable
                var result = DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.Cloak, ref cloak, sizeof(int));
                break;
        }
        return IntPtr.Zero;
    }

    private void FillRect(IntPtr hwnd)
    {
        var wpfColor = Colors.Black;
        // Convert WPF Color to a COLORREF value (0x00BBGGRR format).
        uint colorRef = (uint) ((wpfColor.B << 16) | (wpfColor.G << 8) | wpfColor.R);
        IntPtr brush = CreateSolidBrush(colorRef);

        try
        {
            // 2. Get the window's rectangle in screen coordinates.
            if (GetWindowRect(hwnd, out RECT rect))
            {
                // 3. Adjust the rectangle's coordinates to be relative to the window's top-left corner (0,0).
                // This is equivalent to the MapWindowPoints and OffsetRect calls in the C++ sample.
                OffsetRect(ref rect, -rect.Left, -rect.Top);

                // 4. Get the Device Context (DC) for the entire window, including non-client areas.
                IntPtr hdc = GetWindowDC(hwnd);
                if (hdc != IntPtr.Zero)
                {
                    try
                    {
                        // 5. Fill the rectangle with the solid brush.
                        FillRect(hdc, ref rect, brush);
                    }
                    finally
                    {
                        // 6. Release the Device Context.
                        ReleaseDC(hwnd, hdc);
                    }
                }
            }
        }
        finally
        {
            // 7. Clean up and delete the GDI brush to free resources.
            if (brush != IntPtr.Zero)
            {
                DeleteObject(brush);
            }
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attr, ref int attrValue, int attrSize);

    public enum DWMWINDOWATTRIBUTE : uint
    {
        NCRenderingEnabled = 1,
        NCRenderingPolicy,
        TransitionsForceDisabled,
        AllowNCPaint,
        CaptionButtonBounds,
        NonClientRtlLayout,
        ForceIconicRepresentation,
        Flip3DPolicy,
        ExtendedFrameBounds,
        HasIconicBitmap,
        DisallowPeek,
        ExcludedFromPeek,
        Cloak,
        Cloaked,
        FreezeRepresentation
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool OffsetRect(ref RECT lprc, int dx, int dy);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateSolidBrush(uint crColor);

    [DllImport("user32.dll")]
    public static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject([In] IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}