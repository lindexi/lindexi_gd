using System.Runtime.InteropServices;

namespace RalllawfairlekolairHemyiqearkice.Skia.Wpf.Utils
{
    public static partial class FullScreenHelper
    {
        static class Win32
        {
            [Flags]
            public enum ShowWindowCommands
            {
                /// <summary>
                ///     Maximizes the specified window.
                /// </summary>
                SW_MAXIMIZE = 3,

                /// <summary>
                ///     Activates and displays the window. If the window is minimized or maximized, the system restores it to its original
                ///     size and position. An application should specify this flag when restoring a minimized window.
                /// </summary>
                SW_RESTORE = 9,
            }


            internal static class Properties
            {
#if !ANSI
                public const CharSet BuildCharSet = CharSet.Unicode;
#else
                public const CharSet BuildCharSet = CharSet.Ansi;
#endif
            }

            public static class Dwmapi
            {
                public const string LibraryName = "Dwmapi.dll";

                [DllImport(LibraryName, ExactSpelling = true, PreserveSig = false)]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool DwmIsCompositionEnabled();

                [DllImport("Dwmapi.dll", ExactSpelling = true, SetLastError = true)]
                public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute,
                    in int pvAttribute, uint cbAttribute);
            }

            public static class User32
            {
                public const string LibraryName = "user32";

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
                public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern IntPtr MonitorFromRect(in Rectangle lprc, MonitorFlag dwFlags);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool IsIconic(IntPtr hwnd);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool SetWindowPlacement(IntPtr hWnd,
                    [In] ref WINDOWPLACEMENT lpwndpl);

                [return: MarshalAs(UnmanagedType.Bool)]
                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

                [DllImport(LibraryName, ExactSpelling = true, SetLastError = true)]
                public static extern Int32 SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, Int32 x, Int32 y, Int32 cx,
                    Int32 cy, Int32 wFlagslong);

                [DllImport(LibraryName, ExactSpelling = true)]
                public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

                public static IntPtr GetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex) =>
                    GetWindowLongPtr(hWnd, (int) nIndex);

                public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
                {
                    return IntPtr.Size > 4
#pragma warning disable CS0618 // 类型或成员已过时
                        ? GetWindowLongPtr_x64(hWnd, nIndex)
                        : new IntPtr(GetWindowLong(hWnd, nIndex));
#pragma warning restore CS0618 // 类型或成员已过时
                }

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
                public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet, EntryPoint = "GetWindowLongPtr")]
                public static extern IntPtr GetWindowLongPtr_x64(IntPtr hWnd, int nIndex);

                public static IntPtr SetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex, IntPtr dwNewLong) =>
                    SetWindowLongPtr(hWnd, (int) nIndex, dwNewLong);

                public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
                {
                    return IntPtr.Size > 4
#pragma warning disable CS0618 // 类型或成员已过时
                        ? SetWindowLongPtr_x64(hWnd, nIndex, dwNewLong)
                        : new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));
#pragma warning restore CS0618 // 类型或成员已过时
                }

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet, EntryPoint = "SetWindowLongPtr")]
                public static extern IntPtr SetWindowLongPtr_x64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

                [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
                public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MonitorInfo
        {
            /// <summary>
            ///     The size of the structure, in bytes.
            /// </summary>
            public uint Size;

            /// <summary>
            ///     A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates. Note that
            ///     if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public Rectangle MonitorRect;

            /// <summary>
            ///     A RECT structure that specifies the work area rectangle of the display monitor, expressed in virtual-screen
            ///     coordinates. Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may
            ///     be negative values.
            /// </summary>
            public Rectangle WorkRect;

            /// <summary>
            ///     A set of flags that represent attributes of the display monitor.
            /// </summary>
            public MonitorInfoFlag Flags;
        }

        enum MonitorInfoFlag
        {
        }

        enum MonitorFlag
        {
            /// <summary>
            ///     Returns a handle to the primary display monitor.
            /// </summary>
            MONITOR_DEFAULTTOPRIMARY = 1,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WindowPosition
        {
            public IntPtr Hwnd;
            public IntPtr HwndZOrderInsertAfter;
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public WindowPositionFlags Flags;
        }

        enum HwndZOrder
        {
            /// <summary>
            ///     Places the window at the top of the Z order.
            /// </summary>
            HWND_TOP = 0,
        }

        enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_TRANSITIONS_FORCEDISABLED = 3,
        }

        enum GetWindowLongFields
        {
            /// <summary>
            /// 设定一个新的窗口风格
            /// Retrieves the window styles
            /// </summary>
            GWL_STYLE = -16,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPLACEMENT // WindowPlacement
        {
            public uint Size;
            public WindowPlacementFlags Flags;
            public Win32.ShowWindowCommands ShowCmd;
            public Point MinPosition;
            public Point MaxPosition;
            public Rectangle NormalPosition;
        }

        [Flags]
        public enum WindowPositionFlags
        {
            /// <summary>
            /// <para>
            /// 清除客户区的所有内容。如果未设置该标志，客户区的有效内容被保存并且在窗口尺寸更新和重定位后拷贝回客户区
            /// </para>
            ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client
            ///     area are saved and copied back into the client area after the window is sized or repositioned.
            /// </summary>
            SWP_NOCOPYBITS = 0x0100,

            /// <summary>
            /// <para>
            /// 维持当前位置（忽略X和Y参数）
            /// </para>
            ///     Retains the current position (ignores X and Y parameters).
            /// </summary>
            SWP_NOMOVE = 0x0002,

            /// <summary>
            /// <para>
            /// 不重画改变的内容。如果设置了这个标志，则不发生任何重画动作。适用于客户区和非客户区（包括标题栏和滚动条）和任何由于窗回移动而露出的父窗口的所有部分。如果设置了这个标志，应用程序必须明确地使窗口无效并区重画窗口的任何部分和父窗口需要重画的部分
            /// </para>
            ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area,
            ///     the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a
            ///     result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any
            ///     parts of the window and parent window that need redrawing.
            /// </summary>
            SWP_NOREDRAW = 0x0008,

            /// <summary>
            /// <para>
            /// 维持当前尺寸（忽略 cx 和 cy 参数）
            /// </para>
            ///     Retains the current size (ignores the cx and cy parameters).
            /// </summary>
            SWP_NOSIZE = 0x0001,

            /// <summary>
            /// <para>
            /// 维持当前 Z 序（忽略 hWndlnsertAfter 参数）
            /// </para>
            ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOZORDER = 0x0004,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Rectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            /// <summary>
            /// 矩形的宽度
            /// </summary>
            public int Width
            {
                get { return unchecked((int) (Right - Left)); }
                set { Right = unchecked((int) (Left + value)); }
            }

            /// <summary>
            /// 矩形的高度
            /// </summary>
            public int Height
            {
                get { return unchecked((int) (Bottom - Top)); }
                set { Bottom = unchecked((int) (Top + value)); }
            }

            public bool Equals(Rectangle other)
            {
                return (Left == other.Left) && (Right == other.Right) && (Top == other.Top) && (Bottom == other.Bottom);
            }

            public override bool Equals(object obj)
            {
                return obj is Rectangle rectangle && Equals(rectangle);
            }

            public static bool operator ==(Rectangle left, Rectangle right)
            {
                return left.Equals(right);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int) Left;
                    hashCode = (hashCode * 397) ^ (int) Top;
                    hashCode = (hashCode * 397) ^ (int) Right;
                    hashCode = (hashCode * 397) ^ (int) Bottom;
                    return hashCode;
                }
            }

            public static bool operator !=(Rectangle left, Rectangle right)
            {
                return !(left == right);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Point
        {
            public int X;
            public int Y;
        }

        [Flags]
        enum WindowPlacementFlags
        {
        }

        [Flags]
        enum WindowStyles
        {
            /// <summary>
            ///     The window is initially maximized.
            /// </summary>
            WS_MAXIMIZE = 0x01000000,

            /// <summary>
            ///     The window has a maximize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must
            ///     also be specified.
            /// </summary>
            WS_MAXIMIZEBOX = 0x00010000,

            /// <summary>
            ///     The window is initially minimized. Same as the WS_ICONIC style.
            /// </summary>
            WS_MINIMIZE = 0x20000000,

            /// <summary>
            ///     The window has a sizing border. Same as the WS_SIZEBOX style.
            /// </summary>
            WS_THICKFRAME = 0x00040000,

            WS_CAPTION = 0x00C00000,
        }
    }
}
