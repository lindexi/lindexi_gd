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

namespace DefilireceHowemdalaqu;

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

    private void OnSourceInitialized(object sender, EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        HwndSource source = HwndSource.FromHwnd(hwnd);
        source.AddHook(Hook);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        const int WM_POINTERDOWN = 0x0246;
        const int WM_POINTERUPDATE = 0x0245;
        const int WM_POINTERUP = 0x0247;

        if (msg is WM_POINTERDOWN or WM_POINTERUPDATE or WM_POINTERUP)
        {
            var pointerId = (uint)(ToInt32(wparam) & 0xFFFF);
        }


        return IntPtr.Zero;
    }

    private static int ToInt32(IntPtr ptr)
    {
        if (IntPtr.Size == 4)
            return ptr.ToInt32();

        return (int) (ptr.ToInt64() & 0xffffffff);
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetPointerTouchInfo(uint pointerId, out POINTER_TOUCH_INFO touchInfo);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct POINTER_TOUCH_INFO
    {
        public POINTER_INFO pointerInfo;
        public TouchFlags touchFlags;
        public TouchMask touchMask;
        public int rcContactLeft;
        public int rcContactTop;
        public int rcContactRight;
        public int rcContactBottom;
        public int rcContactRawLeft;
        public int rcContactRawTop;
        public int rcContactRawRight;
        public int rcContactRawBottom;
        public uint orientation;
        public uint pressure;
    }
}