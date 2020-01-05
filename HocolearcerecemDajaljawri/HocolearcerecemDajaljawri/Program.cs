using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace HocolearcerecemDajaljawri
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var thread = new Thread(() =>
            {
                var customWindow = new Window("sdf");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            Console.ReadLine();
        }
    }

    internal class Window : IDisposable
    {
        public Window(string className)
        {
            var windClass = new WNDCLASS
            {
                lpszClassName = className
            };
            _wndProc = CustomWndProc;

            windClass.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc);

            RegisterClassW(ref windClass);

            // Create window
            _mHwnd = CreateWindowExW
            (
                0,
                className,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero
            );

            const int SW_MAXIMIZE = 3;
            ShowWindow(_mHwnd, SW_MAXIMIZE);

            int ret;
            while ((ret = GetMessage(out var msg, _mHwnd, 0, 0)) != 0)
            {
                if (ret == -1)
                {
                    //-1 indicates an error
                }
                else
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClassW([In] ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowExW
        (
            uint dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProcW
        (
            IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        private void Dispose(bool disposing)
        {
            if (!_mDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Dispose unmanaged resources
                if (_mHwnd != IntPtr.Zero)
                {
                    DestroyWindow(_mHwnd);
                    _mHwnd = IntPtr.Zero;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetMessage(out int lpMsg, IntPtr hWnd, uint wMsgFilterMin,
            uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref int lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref int lpmsg);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASS
        {
            public readonly uint style;
            public IntPtr lpfnWndProc;
            public readonly int cbClsExtra;
            public readonly int cbWndExtra;
            public readonly IntPtr hInstance;
            public readonly IntPtr hIcon;
            public readonly IntPtr hCursor;
            public readonly IntPtr hbrBackground;

            [MarshalAs(UnmanagedType.LPWStr)] public readonly string lpszMenuName;

            [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        }

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly WndProc _wndProc;

        private bool _mDisposed;
        private IntPtr _mHwnd;
    }
}