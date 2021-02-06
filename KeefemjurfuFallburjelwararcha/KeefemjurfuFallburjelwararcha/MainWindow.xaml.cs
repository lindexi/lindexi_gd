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

            var wmTouchForm = new WMTouchForm();
            wmTouchForm.Show();
        }
    }

    public class WMTouchForm : Form
    {
        ///////////////////////////////////////////////////////////////////////
        // Public interface

        // Constructor
        [SecurityPermission(SecurityAction.Demand)]
        public WMTouchForm()
        {
            // Setup handlers
            Load += new System.EventHandler(this.OnLoadHandler);

            // GetTouchInputInfo needs to be
            // passed the size of the structure it will be filling.
            // We get the size upfront so it can be used later.
            touchInputSize = Marshal.SizeOf(new TOUCHINPUT());
        }

        ///////////////////////////////////////////////////////////////////////
        // Protected members, for derived classes.

        // Touch event handlers
        protected event EventHandler<WMTouchEventArgs> Touchdown;   // touch down event handler
        protected event EventHandler<WMTouchEventArgs> Touchup;     // touch up event handler
        protected event EventHandler<WMTouchEventArgs> TouchMove;   // touch move event handler

        // EventArgs passed to Touch handlers
        protected class WMTouchEventArgs : System.EventArgs
        {
            // Private data members
            private int x;                  // touch x client coordinate in pixels
            private int y;                  // touch y client coordinate in pixels
            private int id;                 // contact ID
            private int mask;               // mask which fields in the structure are valid
            private int flags;              // flags
            private int time;               // touch event time
            private int contactX;           // x size of the contact area in pixels
            private int contactY;           // y size of the contact area in pixels

            // Access to data members
            public int LocationX
            {
                get { return x; }
                set { x = value; }
            }
            public int LocationY
            {
                get { return y; }
                set { y = value; }
            }
            public int Id
            {
                get { return id; }
                set { id = value; }
            }
            public int Flags
            {
                get { return flags; }
                set { flags = value; }
            }
            public int Mask
            {
                get { return mask; }
                set { mask = value; }
            }
            public int Time
            {
                get { return time; }
                set { time = value; }
            }
            public int ContactX
            {
                get { return contactX; }
                set { contactX = value; }
            }
            public int ContactY
            {
                get { return contactY; }
                set { contactY = value; }
            }
            public bool IsPrimaryContact
            {
                get { return (flags & TOUCHEVENTF_PRIMARY) != 0; }
            }

            // Constructor
            public WMTouchEventArgs()
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // Private class definitions, structures, attributes and native
        // functions

        // Multitouch/Touch glue (from winuser.h file)
        // Since the managed layer between C# and WinAPI functions does not 
        // exist at the moment for multi-touch related functions this part of 
        // the code is required to replicate definitions from winuser.h file.

        // Touch event window message constants [winuser.h]
        private const int WM_TOUCH = 0x0240;

        // Touch event flags ((TOUCHINPUT.dwFlags) [winuser.h]
        private const int TOUCHEVENTF_MOVE = 0x0001;
        private const int TOUCHEVENTF_DOWN = 0x0002;
        private const int TOUCHEVENTF_UP = 0x0004;
        private const int TOUCHEVENTF_INRANGE = 0x0008;
        private const int TOUCHEVENTF_PRIMARY = 0x0010;
        private const int TOUCHEVENTF_NOCOALESCE = 0x0020;
        private const int TOUCHEVENTF_PEN = 0x0040;

        // Touch input mask values (TOUCHINPUT.dwMask) [winuser.h]
        private const int TOUCHINPUTMASKF_TIMEFROMSYSTEM = 0x0001; // the dwTime field contains a system generated value
        private const int TOUCHINPUTMASKF_EXTRAINFO = 0x0002; // the dwExtraInfo field is valid
        private const int TOUCHINPUTMASKF_CONTACTAREA = 0x0004; // the cxContact and cyContact fields are valid

        // Touch API defined structures [winuser.h]
        [StructLayout(LayoutKind.Sequential)]
        private struct TOUCHINPUT
        {
            public int x;
            public int y;
            public System.IntPtr hSource;
            public int dwID;
            public int dwFlags;
            public int dwMask;
            public int dwTime;
            public System.IntPtr dwExtraInfo;
            public int cxContact;
            public int cyContact;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTS
        {
            public short x;
            public short y;
        }

        // Currently touch/multitouch access is done through unmanaged code
        // We must p/invoke into user32 [winuser.h]
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterTouchWindow(System.IntPtr hWnd, uint ulFlags);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTouchInputInfo(System.IntPtr hTouchInput, int cInputs, [In, Out] TOUCHINPUT[] pInputs, int cbSize);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern void CloseTouchInputHandle(System.IntPtr lParam);

        // Attributes
        private int touchInputSize;        // size of TOUCHINPUT structure

        ///////////////////////////////////////////////////////////////////////
        // Private methods

        // OnLoad window event handler: Registers the form for multi-touch input.
        // in:
        //      sender      object that has sent the event
        //      e           event arguments
        private void OnLoadHandler(Object sender, EventArgs e)
        {
            try
            {
                // Registering the window for multi-touch, using the default settings.
                // p/invoking into user32.dll
                if (!RegisterTouchWindow(this.Handle, 0))
                {
                    Debug.Print("ERROR: Could not register window for multi-touch");
                }
            }
            catch (Exception exception)
            {
               
            }
        }

        // Window procedure. Receives WM_ messages.
        // Translates WM_TOUCH window messages to touch events.
        // Normally, touch events are sufficient for a derived class,
        // but the window procedure can be overriden, if needed.
        // in:
        //      m       message
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            // Decode and handle WM_TOUCH message.
            bool handled;
            switch (m.Msg)
            {
                case WM_TOUCH:
                    //handled = DecodeTouch(ref m);
                    handled = true;
                    break;
                default:
                    handled = false;
                    break;
            }

            // Call parent WndProc for default message processing.
            base.WndProc(ref m);

            if (handled)
            {
                // Acknowledge event if handled.
                m.Result = new System.IntPtr(1);
            }
        }

        // Extracts lower 16-bit word from an 32-bit int.
        // in:
        //      number      int
        // returns:
        //      lower word
        private static int LoWord(int number)
        {
            return (number & 0xffff);
        }

     
    }

    class TouchWindow : Window
    {
        public TouchWindow()
        {
            Loaded += TouchWindow_Loaded;
        }

        private void TouchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            RegisterTouchWindow(windowInteropHelper.Handle, 0);

            HwndSource source = HwndSource.FromHwnd(windowInteropHelper.Handle);
            source.AddHook(Hook);
        }

        private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == NativeMethods.WM_TOUCH)
            {

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
