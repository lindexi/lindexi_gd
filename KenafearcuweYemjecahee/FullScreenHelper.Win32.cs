using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KenafearcuweYemjecahee
{
    public static partial class FullScreenHelper
    {
        static class Win32
        {
            [Flags]
            public enum ShowWindowCommands
            {
                /// <summary>
                ///     Minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when
                ///     minimizing windows from a different thread.
                /// </summary>
                SW_FORCEMINIMIZE = 11,

                /// <summary>
                ///     Hides the window and activates another window.
                /// </summary>
                SW_HIDE = 0,

                /// <summary>
                ///     Maximizes the specified window.
                /// </summary>
                SW_MAXIMIZE = 3,

                /// <summary>
                ///     Minimizes the specified window and activates the next top-level window in the Z order.
                /// </summary>
                SW_MINIMIZE = 6,

                /// <summary>
                ///     Activates and displays the window. If the window is minimized or maximized, the system restores it to its original
                ///     size and position. An application should specify this flag when restoring a minimized window.
                /// </summary>
                SW_RESTORE = 9,

                /// <summary>
                ///     Activates the window and displays it in its current size and position.
                /// </summary>
                SW_SHOW = 5,

                /// <summary>
                ///     Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess
                ///     function by the program that started the application.
                /// </summary>
                SW_SHOWDEFAULT = 10,

                /// <summary>
                ///     Activates the window and displays it as a maximized window.
                /// </summary>
                SW_SHOWMAXIMIZED = 3,

                /// <summary>
                ///     Activates the window and displays it as a minimized window.
                /// </summary>
                SW_SHOWMINIMIZED = 2,

                /// <summary>
                ///     Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED, except the window is not
                ///     activated.
                /// </summary>
                SW_SHOWMINNOACTIVE = 7,

                /// <summary>
                ///     Displays the window in its current size and position. This value is similar to SW_SHOW, except that the window is
                ///     not activated.
                /// </summary>
                SW_SHOWNA = 8,

                /// <summary>
                ///     Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the
                ///     window is not activated.
                /// </summary>
                SW_SHOWNOACTIVATE = 4,

                /// <summary>
                ///     Activates and displays a window. If the window is minimized or maximized, the system restores it to its original
                ///     size and position. An application should specify this flag when displaying the window for the first time.
                /// </summary>
                SW_SHOWNORMAL = 1
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
            MONITORINFOF_NONE = 0,

            /// <summary>
            ///     This is the primary display monitor.
            /// </summary>
            MONITORINFOF_PRIMARY = 0x00000001
        }

        enum MonitorFlag
        {
            /// <summary>
            ///     Returns NULL.
            /// </summary>
            MONITOR_DEFAULTTONULL = 0,

            /// <summary>
            ///     Returns NULL.
            /// <para></para>
            /// 和 <see cref="MONITOR_DEFAULTTONULL"/> 等价，只是修改命名提高可读性
            /// </summary>
            MonitorDefaultToNull = MONITOR_DEFAULTTONULL,

            /// <summary>
            ///     Returns a handle to the primary display monitor.
            /// </summary>
            MONITOR_DEFAULTTOPRIMARY = 1,

            /// <summary>
            ///     Returns a handle to the primary display monitor.
            /// <para></para>
            /// 和 <see cref="MONITOR_DEFAULTTOPRIMARY"/> 等价，只是修改命名提高可读性
            /// </summary>
            MonitorDefaultToPrimary = MONITOR_DEFAULTTOPRIMARY,

            /// <summary>
            ///     Returns a handle to the display monitor that is nearest to the window.
            /// </summary>
            MONITOR_DEFAULTTONEAREST = 2,

            /// <summary>
            ///     Returns a handle to the display monitor that is nearest to the window.
            /// <para></para>
            /// 和 <see cref="MONITOR_DEFAULTTONEAREST"/> 等价，只是修改命名提高可读性
            /// </summary>
            MonitorDefaultToNearest = MONITOR_DEFAULTTONEAREST,
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
            ///     Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses
            ///     its topmost status and is placed at the bottom of all other windows.
            /// </summary>
            HWND_BOTTOM = 1,

            /// <summary>
            ///     Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if
            ///     the window is already a non-topmost window.
            /// </summary>
            HWND_NOTOPMOST = -2,

            /// <summary>
            ///     Places the window at the top of the Z order.
            /// </summary>
            HWND_TOP = 0,

            /// <summary>
            ///     Places the window above all non-topmost windows. The window maintains its topmost position even when it is
            ///     deactivated.
            /// </summary>
            HWND_TOPMOST = -1
        }

        enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_NCRENDERING_ENABLED = 1,

            DWMWA_NCRENDERING_POLICY,

            DWMWA_TRANSITIONS_FORCEDISABLED,

            DWMWA_ALLOW_NCPAINT,

            DWMWA_CAPTION_BUTTON_BOUNDS,

            DWMWA_NONCLIENT_RTL_LAYOUT,

            DWMWA_FORCE_ICONIC_REPRESENTATION,

            DWMWA_FLIP3D_POLICY,

            DWMWA_EXTENDED_FRAME_BOUNDS,

            DWMWA_HAS_ICONIC_BITMAP,

            DWMWA_DISALLOW_PEEK,

            DWMWA_EXCLUDED_FROM_PEEK,

            DWMWA_CLOAK,

            DWMWA_CLOAKED,

            DWMWA_FREEZE_REPRESENTATION,

            DWMWA_PASSIVE_UPDATE_MODE,

            DWMWA_LAST
        }

        enum GetWindowLongFields
        {
            /// <summary>
            /// 设定一个新的扩展风格
            /// Retrieves the extended window styles
            /// </summary>
            GWL_EXSTYLE = -20,

            /// <summary>
            /// 设置一个新的应用程序实例句柄
            /// Retrieves a handle to the application instance
            /// </summary>
            GWL_HINSTANCE = -6,

            /// <summary>
            /// 改变子窗口的父窗口
            /// Retrieves a handle to the parent window, if any
            /// </summary>
            GWL_HWNDPARENT = -8,

            /// <summary>
            ///  设置一个新的窗口标识符
            /// Retrieves the identifier of the window
            /// </summary>
            GWL_ID = -12,

            /// <summary>
            /// 设定一个新的窗口风格
            /// Retrieves the window styles
            /// </summary>
            GWL_STYLE = -16,

            /// <summary>
            /// 设置与窗口有关的32位值。每个窗口均有一个由创建该窗口的应用程序使用的32位值
            /// Retrieves the user data associated with the window. This data is intended for use by the application that created the window. Its value is initially zero
            /// </summary>
            GWL_USERDATA = -21,

            /// <summary>
            /// 为窗口设定一个新的处理函数
            /// Retrieves the address of the window procedure, or a handle representing the address of the window procedure. You must use the CallWindowProc function to call the window procedure
            /// </summary>
            GWL_WNDPROC = -4,
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
            /// 如果调用进程不拥有窗口，系统会向拥有窗口的线程发出需求。这就防止调用线程在其他线程处理需求的时候发生死锁。
            /// </para>
            ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts
            ///     the request to the thread that owns the window. This prevents the calling thread from blocking its execution while
            ///     other threads process the request.
            /// </summary>
            SWP_ASYNCWINDOWPOS = 0x4000,

            /// <summary>
            /// <para>
            /// 防止产生 WM_SYNCPAINT <see cref="WM.SYNCPAINT"/> 消息
            /// </para>
            ///     Prevents generation of the WM_SYNCPAINT message.
            /// </summary>
            SWP_DEFERERASE = 0x2000,

            /// <summary>
            /// <para>
            /// 在窗口周围画一个边框（定义在窗口类描述中）
            /// </para>
            ///     Draws a frame (defined in the window's class description) around the window.
            /// </summary>
            SWP_DRAWFRAME = 0x0020,

            /// <summary>
            /// <para>
            /// 给窗口发送 WM_NCCALCSIZE 消息，即使窗口尺寸没有改变也会发送该消息。如果未指定这个标志，只有在改变了窗口尺寸时才发送 WM_NCCALCSIZE 消息
            /// </para>
            ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if
            ///     the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's
            ///     size is being changed.
            /// </summary>
            SWP_FRAMECHANGED = 0x0020,

            /// <summary>
            /// <para>
            /// 隐藏窗口
            /// </para>
            ///     Hides the window.
            /// </summary>
            SWP_HIDEWINDOW = 0x0080,

            /// <summary>
            /// <para>
            /// 不激活窗口。如果未设置标志，则窗口被激活，并被设置到其他最高级窗口或非最高级组的顶部（根据参数hWndlnsertAfter设置）
            /// </para>
            ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the
            ///     topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOACTIVATE = 0x0010,

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
            /// 不改变 Z 序中的所有者窗口的位置
            /// </para>
            ///     Does not change the owner window's position in the Z order.
            /// </summary>
            SWP_NOOWNERZORDER = 0x0200,

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
            /// <para>不改变 Z 序中的所有者窗口的位置</para>
            /// <para>
            /// 与 <see cref="SWP_NOOWNERZORDER"/> 标志相同
            /// </para>
            ///     Same as the SWP_NOOWNERZORDER flag.
            /// </summary>
            SWP_NOREPOSITION = 0x0200,

            /// <summary>
            /// <para>
            /// 防止窗口接收 WM_WINDOWPOSCHANGING <see cref="WM.WINDOWPOSCHANGING"/> 消息
            /// </para>
            ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
            /// </summary>
            SWP_NOSENDCHANGING = 0x0400,

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

            /// <summary>
            /// <para>
            /// 显示窗口
            /// </para>
            ///     Displays the window.
            /// </summary>
            SWP_SHOWWINDOW = 0x0040
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Rectangle
        {
            public Rectangle(int left = 0, int top = 0, int right = 0, int bottom = 0)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

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
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X;
            public int Y;
        }

        [Flags]
        enum WindowPlacementFlags
        {
            /// <summary>
            ///     The coordinates of the minimized window may be specified.
            ///     This flag must be specified if the coordinates are set in the ptMinPosition member.
            /// </summary>
            SETMINPOSITION = 0x0001,

            /// <summary>
            ///     The restored window will be maximized, regardless of whether it was maximized before it was minimized. This setting
            ///     is only valid the next time the window is restored. It does not change the default restoration behavior.
            ///     This flag is only valid when the SW_SHOWMINIMIZED value is specified for the showCmd member.
            /// </summary>
            RESTORETOMAXIMIZED = 0x0002,

            /// <summary>
            ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts
            ///     the request to the thread that owns the window. This prevents the calling thread from blocking its execution while
            ///     other threads process the request.
            /// </summary>
            ASYNCWINDOWPLACEMENT = 0x0004
        }

        [Flags]
        enum WindowStyles
        {
            /// <summary>
            ///     The window has a thin-line border.
            /// </summary>
            WS_BORDER = 0x00800000,

            /// <summary>
            ///     The window has a title bar (includes the WS_BORDER style).
            /// </summary>
            WS_CAPTION = 0x00C00000,

            /// <summary>
            ///     The window is a child window. A window with this style cannot have a menu bar. This style cannot be used with the
            ///     WS_POPUP style.
            /// </summary>
            WS_CHILD = 0x40000000,

            /// <summary>
            ///     Same as the WS_CHILD style.
            /// </summary>
            WS_CHILDWINDOW = 0x40000000,

            /// <summary>
            ///     Excludes the area occupied by child windows when drawing occurs within the parent window. This style is used when
            ///     creating the parent window.
            /// </summary>
            WS_CLIPCHILDREN = 0x02000000,

            /// <summary>
            ///     Clips child windows relative to each other; that is, when a particular child window receives a WM_PAINT message,
            ///     the WS_CLIPSIBLINGS style clips all other overlapping child windows out of the region of the child window to be
            ///     updated. If WS_CLIPSIBLINGS is not specified and child windows overlap, it is possible, when drawing within the
            ///     client area of a child window, to draw within the client area of a neighboring child window.
            /// </summary>
            WS_CLIPSIBLINGS = 0x04000000,

            /// <summary>
            ///     The window is initially disabled. A disabled window cannot receive input from the user. To change this after a
            ///     window has been created, use the EnableWindow function.
            /// </summary>
            WS_DISABLED = 0x08000000,

            /// <summary>
            ///     The window has a border of a style typically used with dialog boxes. A window with this style cannot have a title
            ///     bar.
            /// </summary>
            WS_DLGFRAME = 0x00400000,

            /// <summary>
            ///     The window is the first control of a group of controls. The group consists of this first control and all controls
            ///     defined after it, up to the next control with the WS_GROUP style. The first control in each group usually has the
            ///     WS_TABSTOP style so that the user can move from group to group. The user can subsequently change the keyboard focus
            ///     from one control in the group to the next control in the group by using the direction keys.
            ///     You can turn this style on and off to change dialog box navigation. To change this style after a window has been
            ///     created, use the SetWindowLong function.
            /// </summary>
            WS_GROUP = 0x00020000,

            /// <summary>
            ///     The window has a horizontal scroll bar.
            /// </summary>
            WS_HSCROLL = 0x00100000,

            /// <summary>
            ///     The window is initially minimized. Same as the WS_MINIMIZE style.
            /// </summary>
            WS_ICONIC = 0x20000000,

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
            ///     The window has a minimize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must
            ///     also be specified.
            /// </summary>
            WS_MINIMIZEBOX = 0x00020000,

            /// <summary>
            ///     The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_TILED style.
            /// </summary>
            WS_OVERLAPPED = 0x00000000,

            /// <summary>
            ///     The window is an overlapped window. Same as the WS_TILEDWINDOW style.
            /// </summary>
            WS_OVERLAPPEDWINDOW =
                WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,

            /// <summary>
            ///     The windows is a pop-up window. This style cannot be used with the WS_CHILD style.
            /// </summary>
            WS_POPUP = unchecked((int) 0x80000000),

            /// <summary>
            ///     The window is a pop-up window. The WS_CAPTION and WS_POPUPWINDOW styles must be combined to make the window menu
            ///     visible.
            /// </summary>
            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,

            /// <summary>
            ///     The window has a sizing border. Same as the WS_THICKFRAME style.
            /// </summary>
            WS_SIZEBOX = 0x00040000,

            /// <summary>
            ///     The window has a window menu on its title bar. The WS_CAPTION style must also be specified.
            /// </summary>
            WS_SYSMENU = 0x00080000,

            /// <summary>
            ///     The window is a control that can receive the keyboard focus when the user presses the TAB key. Pressing the TAB key
            ///     changes the keyboard focus to the next control with the WS_TABSTOP style.
            ///     You can turn this style on and off to change dialog box navigation. To change this style after a window has been
            ///     created, use the SetWindowLong function. For user-created windows and modeless dialogs to work with tab stops,
            ///     alter the message loop to call the IsDialogMessage function.
            /// </summary>
            WS_TABSTOP = 0x00010000,

            /// <summary>
            ///     The window has a sizing border. Same as the WS_SIZEBOX style.
            /// </summary>
            WS_THICKFRAME = 0x00040000,

            /// <summary>
            ///     The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_OVERLAPPED
            ///     style.
            /// </summary>
            WS_TILED = 0x00000000,

            /// <summary>
            ///     The window is  an overlapped window. Same as the WS_OVERLAPPEDWINDOW style.
            /// </summary>
            WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,

            /// <summary>
            ///     The window is initially visible.
            ///     This style can be turned on and off by using the ShowWindow or SetWindowPos function.
            /// </summary>
            WS_VISIBLE = 0x10000000,

            /// <summary>
            ///     The window has a vertical scroll bar.
            /// </summary>
            WS_VSCROLL = 0x00200000
        }
    }
}