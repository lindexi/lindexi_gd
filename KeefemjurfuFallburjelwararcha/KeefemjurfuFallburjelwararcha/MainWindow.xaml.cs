using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PInvoke;
using MessageBox = System.Windows.MessageBox;

namespace KeefemjurfuFallburjelwararcha
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var handle = windowInteropHelper.Handle;

            var touchWindow = new TouchWindow();
            touchWindow.Show();
        }
    }

    class TouchWindow : Window
    {
        public TouchWindow()
        {
            Loaded += TouchWindow_Loaded;
            Title = "TouchWindow";

            TextBlock = new TextBlock()
            {
                Margin = new Thickness(10, 10, 10, 10),
                VerticalAlignment = VerticalAlignment.Bottom,
                TextWrapping = TextWrapping.Wrap
            };

            Content = new Grid
            {
                Children =
                {
                    TextBlock
                }
            };
        }

        public static TextBlock TextBlock { private set; get; } = null!;

        private void TouchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            RegisterTouchWindow(windowInteropHelper.Handle, 0);

            HwndSource source = HwndSource.FromHwnd(windowInteropHelper.Handle)!;
            source.AddHook(Hook);
        }

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == NativeMethods.WM_TOUCH)
            {
                TextBlock.Text += $"Tick={_stopwatch.ElapsedTicks} ms={_stopwatch.ElapsedMilliseconds}\r\n";


                _stopwatch.Restart();
            }

            return IntPtr.Zero;
        }

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterTouchWindow(System.IntPtr hWnd, uint ulFlags);
    }

    internal static class NativeMethods
    {
        public const int WM_TOUCH = 0x0240;
        public const uint TWF_WANTPALM = 0x00000002;

        public static readonly int TouchInputSize = Marshal.SizeOf(new TOUCHINPUT());

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterTouchWindow(IntPtr hWnd, uint ulFlags);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTouchInputInfo(IntPtr hTouchInput, int cInputs,
            [In, Out] TOUCHINPUT[] pInputs, int cbSize);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern void CloseTouchInputHandle(IntPtr lParam);

        [Flags]
        internal enum TOUCHEVENTF
        {
            TOUCHEVENTF_MOVE = 0x0001,
            TOUCHEVENTF_DOWN = 0x0002,
            TOUCHEVENTF_UP = 0x0004,
            TOUCHEVENTF_INRANGE = 0x0008,
            TOUCHEVENTF_PRIMARY = 0x0010,
            TOUCHEVENTF_NOCOALESCE = 0x0020,
            TOUCHEVENTF_PALM = 0x0080,
        }

        [Flags]
        internal enum TOUCHINPUTMASK
        {
            /// <summary>
            /// cxContact and cyContact are valid
            /// </summary>
            TOUCHINPUTMASKF_CONTACTAREA = 0x0004,

            /// <summary>
            /// dwExtraInfo is valid
            /// </summary>
            TOUCHINPUTMASKF_EXTRAINFO = 0x0002,

            /// <summary>
            /// The system time was set in the TOUCHINPUT structure
            /// </summary>
            TOUCHINPUTMASKF_TIMEFROMSYSTEM = 0x0001,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TOUCHINPUT
        {
            /// <summary>
            /// 触控输入的 X 坐标（水平点）。此成员用物理屏幕坐标的像素的百分之一表示
            /// </summary>
            public int X;

            /// <summary>
            /// 触控输入的 y 坐标（垂直点）。此成员用物理屏幕坐标的像素的百分之一表示
            /// </summary>
            public int Y;

            /// <summary>
            /// 源输入设备的设备句柄。触控输入提供程序在运行时为每个设备指定一个唯一的提供程序
            /// </summary>
            public IntPtr Source;

            /// <summary>
            /// 一个用于区别某个特定触控输入的触控点标识符。此值在触控点序列中从触控点下降到重新上升的整个过程中保持一致。稍后可对后续触控点重用一个 ID
            /// </summary>
            public int DwID;

            /// <summary>
            /// 用于指定触控点按住、释放和移动的各个方面
            /// </summary>
            public TOUCHEVENTF DwFlags;

            /// <summary>
            /// 指定结构中包含有效值的可选字段。可选字段中的有效信息的可用性是特定于设备的
            /// </summary>
            public TOUCHINPUTMASK DwMask;

            /// <summary>
            /// 事件的时间戳（以毫秒为单位）。使用方应用程序应通知系统不对此字段进行验证
            /// </summary>
            public int DwTime;

            public IntPtr DwExtraInfo;

            /// <summary>
            /// 触控区域的宽度用物理屏幕坐标的像素的百分之一表示。只有在 <see cref="DwMask"/> 成员设置了 TOUCHEVENTFMASK_CONTACTAREA 标记的情况下，此值才会有效
            /// </summary>
            public int CxContact;

            /// <summary>
            /// 触控区域的高度用物理屏幕坐标的像素的百分之一表示。只有在 <see cref="DwMask"/> 成员设置了 TOUCHEVENTFMASK_CONTACTAREA 标记的情况下，此值才会有效
            /// </summary>
            public int CyContact;
        }
    }
}
