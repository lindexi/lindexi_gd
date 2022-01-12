using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using HWND = System.IntPtr;

namespace YefajiharwearhaBurharberecall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            MouseHook.Start();

            Loaded += MainWindow_Loaded;
        }

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterTouchWindow(IntPtr hWnd, uint ulFlags);


        [DllImport("comctl32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetWindowSubclass(HWND hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, ref IntPtr pdwRefData);

        [DllImport("comctl32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetWindowSubclass(HWND hwnd, SUBCLASSPROC callback, IntPtr id, IntPtr data);

        private delegate IntPtr SUBCLASSPROC(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data);

        private IntPtr WndProcStub(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
        {
            return WndProc(hwnd, msg, wParam, lParam, id, data);
        }

        private IntPtr WndProc(HWND hwnd, int msg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
        {
            IntPtr retval = new IntPtr(1);

            if (wParam == IntPtr.Zero)
            {

            }
            else
            {
                //Debug.WriteLine($"Message={msg } wParam={wParam}");
            }

            if (msg == WM_TOUCH || wParam == new IntPtr(WM_TOUCH))
            {

            }

            return DefSubclassProc(hwnd,msg,wParam,lParam);
        }

        [DllImport("comctl32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr DefSubclassProc(HWND hwnd, int msg, IntPtr wParam, IntPtr lParam);

        public const int WM_TOUCH = 0x0240;

        private SUBCLASSPROC _wndproc;
        private IntPtr _wndprocPtr;


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var hWnd = windowInteropHelper.Handle;

            DisableWPFTabletSupport();

            RegisterTouchWindow(hWnd, TWF_WANTPALM);

            _wndproc = WndProcStub;
            _wndprocPtr = Marshal.GetFunctionPointerForDelegate(_wndproc);

            SetWindowSubclass(hWnd, _wndproc, IntPtr.Zero, IntPtr.Zero);

        }

        public const uint TWF_WANTPALM = 0x00000002;

        public static void DisableWPFTabletSupport()
        {
            // Get a collection of the tablet devices for this window.  
            TabletDeviceCollection devices = System.Windows.Input.Tablet.TabletDevices;

            if (devices.Count > 0)
            {
                // Get the Type of InputManager.
                Type inputManagerType = typeof(System.Windows.Input.InputManager);

                // Call the StylusLogic method on the InputManager.Current instance.
                object stylusLogic = inputManagerType.InvokeMember("StylusLogic",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, InputManager.Current, null);

                if (stylusLogic != null)
                {
                    //  Get the type of the stylusLogic returned from the call to StylusLogic.
                    Type stylusLogicType = stylusLogic.GetType();

                    // Loop until there are no more devices to remove.
                    while (devices.Count > 0)
                    {
                        // Remove the first tablet device in the devices collection.
                        stylusLogicType.InvokeMember("OnTabletRemoved",
                            BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                            null, stylusLogic, new object[] { (uint) 0 });
                    }
                }

            }
        }
    }

    public static class MouseHook
    {
        private delegate IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static MouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_MOUSEMOVE = 0x0200
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            MouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// 开启全局钩子
        /// </summary>
        public static void Start()
        {
            _hookID = SetHook(_proc);
        }

        public static void Stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(MouseProc proc)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule module = currentProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(module.ModuleName!), 0);
            }
        }
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //Debug.WriteLine(wParam);

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

    }
}
