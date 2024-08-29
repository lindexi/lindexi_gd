using System.Diagnostics;
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

namespace GibibealaFoheyufairha;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        PInvoke.RegisterTouchWindow(new HWND(hwnd), 0);

        HwndSource source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(Hook);
    }

    private unsafe IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if ((uint)msg is PInvoke.WM_TOUCH)
        {
            var touchInputCount = wparam.ToInt32();

            var pTouchInputs = stackalloc TOUCHINPUT[touchInputCount];
            if (PInvoke.GetTouchInputInfo(new HTOUCHINPUT(lparam), (uint)touchInputCount, pTouchInputs,
                    sizeof(TOUCHINPUT)))
            {
                for (var i = 0; i < touchInputCount; i++)
                {
                    var touchInput = pTouchInputs[i];
                    var point = new System.Drawing.Point(touchInput.x / 100, touchInput.y / 100);
                    PInvoke.ScreenToClient(new HWND(hwnd), ref point);

                    Debug.WriteLine($"Touch {touchInput.dwID} XY={point.X}, {point.Y}");
                }

                PInvoke.CloseTouchInputHandle(new HTOUCHINPUT(lparam));
            }
        }

        return IntPtr.Zero;
    }
}