using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

namespace FurnaheaneHejichaijair
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 改变一个子窗口、弹出式窗口和顶层窗口的尺寸、位置和 Z 序。
        /// </summary>
        /// <param name="hWnd">窗口句柄。</param>
        /// <param name="hWndInsertAfter">
        /// 在z序中的位于被置位的窗口前的窗口句柄。该参数必须为一个窗口句柄，或下列值之一：
        /// <para>HWND_BOTTOM：将窗口置于 Z 序的底部。如果参数hWnd标识了一个顶层窗口，则窗口失去顶级位置，并且被置在其他窗口的底部。</para>
        /// <para>HWND_NOTOPMOST：将窗口置于所有非顶层窗口之上（即在所有顶层窗口之后）。如果窗口已经是非顶层窗口则该标志不起作用。</para>
        /// <para>HWND_TOP：将窗口置于Z序的顶部。</para>
        /// <para>HWND_TOPMOST：将窗口置于所有非顶层窗口之上。即使窗口未被激活窗口也将保持顶级位置。</para>
        /// 如无须更改，请使用 IntPtr.Zero 的值
        /// </param>
        /// <param name="x">以客户坐标指定窗口新位置的左边界。</param>
        /// <param name="y">以客户坐标指定窗口新位置的顶边界。</param>
        /// <param name="cx">以像素指定窗口的新的宽度。如无须更改，请在 <paramref name="wFlagslong"/> 设置 <see cref="WindowPositionFlags.SWP_NOSIZE"/> 的值 </param>
        /// <param name="cy">以像素指定窗口的新的高度。如无须更改，请在 <paramref name="wFlagslong"/> 设置 <see cref="WindowPositionFlags.SWP_NOSIZE"/> 的值</param>
        /// <param name="wFlagslong">
        /// 可传入 <see cref="WindowPositionFlags"/> 枚举中的值
        /// 窗口尺寸和定位的标志。该参数可以是下列值的组合：
        /// <para>SWP_ASYNCWINDOWPOS：如果调用进程不拥有窗口，系统会向拥有窗口的线程发出需求。这就防止调用线程在其他线程处理需求的时候发生死锁。</para>
        /// <para>SWP_DEFERERASE：防止产生 WM_SYNCPAINT 消息。</para>
        /// <para>SWP_DRAWFRAME：在窗口周围画一个边框（定义在窗口类描述中）。</para>
        /// <para>SWP_FRAMECHANGED：给窗口发送 WM_NCCALCSIZE 消息，即使窗口尺寸没有改变也会发送该消息。如果未指定这个标志，只有在改变了窗口尺寸时才发送 WM_NCCALCSIZE。</para>
        /// <para>SWP_HIDEWINDOW：隐藏窗口。</para>
        /// <para>SWP_NOACTIVATE：不激活窗口。如果未设置标志，则窗口被激活，并被设置到其他最高级窗口或非最高级组的顶部（根据参数hWndlnsertAfter设置）。</para>
        /// <para>SWP_NOCOPYBITS：清除客户区的所有内容。如果未设置该标志，客户区的有效内容被保存并且在窗口尺寸更新和重定位后拷贝回客户区。</para>
        /// <para>SWP_NOMOVE：维持当前位置（忽略X和Y参数）。</para>
        /// <para>SWP_NOOWNERZORDER：不改变 Z 序中的所有者窗口的位置。</para>
        /// <para>SWP_NOREDRAW：不重画改变的内容。如果设置了这个标志，则不发生任何重画动作。适用于客户区和非客户区（包括标题栏和滚动条）和任何由于窗回移动而露出的父窗口的所有部分。如果设置了这个标志，应用程序必须明确地使窗口无效并区重画窗口的任何部分和父窗口需要重画的部分。</para>
        /// <para>SWP_NOREPOSITION：与 SWP_NOOWNERZORDER 标志相同。</para>
        /// <para>SWP_NOSENDCHANGING：防止窗口接收 WM_WINDOWPOSCHANGING 消息。</para>
        /// <para>SWP_NOSIZE：维持当前尺寸（忽略 cx 和 cy 参数）。</para>
        /// <para>SWP_NOZORDER：维持当前 Z 序（忽略 hWndlnsertAfter 参数）。</para>
        /// <para>SWP_SHOWWINDOW：显示窗口。</para>
        /// </param>
        /// <returns>如果函数成功，返回值为非零；如果函数失败，返回值为零。若想获得更多错误消息，请调用 GetLastError 函数。</returns>
        [DllImport(LibraryName, ExactSpelling = true, SetLastError = true)]
        public static extern Int32 SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, Int32 x, Int32 y, Int32 cx,
            Int32 cy, Int32 wFlagslong);

        public const string LibraryName = "user32";

        private async void PositionButton_OnClick(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1000);

            var windowInteropHelper = new WindowInteropHelper(this);
            var SWP_NOSIZE = 0x0001;
            SetWindowPos(windowInteropHelper.Handle, IntPtr.Zero, (int)(Left + 10), (int)(Top + 10), 0, 0, SWP_NOSIZE);
        }

        private void SizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var SWP_NOMOVE = 0x0002;
            SetWindowPos(windowInteropHelper.Handle, IntPtr.Zero, 0, 0, (int)(Width + 10), (int)(Height + 10), SWP_NOMOVE);
        }

        private void SetWindowLongPtrButton_OnClick(object sender, RoutedEventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var hwnd = windowInteropHelper.Handle;

            var style = (WindowStyles)GetWindowLongPtr(hwnd, GetWindowLongFields.GWL_STYLE);

            style &= (~(WindowStyles.WS_THICKFRAME | WindowStyles.WS_MAXIMIZEBOX));
            SetWindowLongPtr(hwnd, GetWindowLongFields.GWL_STYLE, (IntPtr)style);
        }

        internal static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(
            IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        internal enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        /// <summary>
        /// 改变指定窗口的属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">
        /// 指定将设定的大于等于0的偏移值。有效值的范围从0到额外类的存储空间的字节数减4：例如若指定了12或多于12个字节的额外窗口存储空间，则应设索引位8来访问第三个4字节，同样设置0访问第一个4字节，4访问第二个4字节。要设置其他任何值，可以指定下面值之一
        /// 从 GetWindowLongFields 可以找到所有的值
        /// </param>
        /// <param name="dwNewLong">指定的替换值</param>
        /// <returns></returns>
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex, IntPtr dwNewLong) => SetWindowLongPtr(hWnd, (int)nIndex, dwNewLong);

        /// <summary>
        /// 改变指定窗口的属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">指定将设定的大于等于0的偏移值。有效值的范围从0到额外类的存储空间的字节数减4：例如若指定了12或多于12个字节的额外窗口存储空间，则应设索引位8来访问第三个4字节，同样设置0访问第一个4字节，4访问第二个4字节。要设置其他任何值，可以指定下面值之一
        /// 从 GetWindowLongFields 可以找到所有的值
        /// <para>
        /// GetWindowLongFields.GWL_EXSTYLE             -20    设定一个新的扩展风格。 </para>
        /// <para>GWL_HINSTANCE     -6	   设置一个新的应用程序实例句柄。</para>
        /// <para>GWL_ID            -12    设置一个新的窗口标识符。</para>
        /// <para>GWL_STYLE         -16    设定一个新的窗口风格。</para>
        /// <para>GWL_USERDATA      -21    设置与窗口有关的32位值。每个窗口均有一个由创建该窗口的应用程序使用的32位值。</para>
        /// <para>GWL_WNDPROC       -4    为窗口设定一个新的处理函数。</para>
        /// <para>GWL_HWNDPARENT    -8    改变子窗口的父窗口,应使用SetParent函数</para>
        /// </param>
        /// <param name="dwNewLong">指定的替换值</param>
        /// <returns></returns>
        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {

            return IntPtr.Size > 4
#pragma warning disable CS0618 // 类型或成员已过时
                    ? SetWindowLongPtr_x64(hWnd, nIndex, dwNewLong)
                : new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));
#pragma warning restore CS0618 // 类型或成员已过时
        }


        /// <summary>
        /// 改变指定窗口的属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">指定将设定的大于等于0的偏移值。有效值的范围从0到额外类的存储空间的字节数减4：例如若指定了12或多于12个字节的额外窗口存储空间，则应设索引位8来访问第三个4字节，同样设置0访问第一个4字节，4访问第二个4字节。要设置其他任何值，可以指定下面值之一
        /// 从 GetWindowLongFields 可以找到所有的值
        /// <para>
        /// GetWindowLongFields.GWL_EXSTYLE             -20    设定一个新的扩展风格。 </para>
        /// <para>GWL_HINSTANCE     -6	   设置一个新的应用程序实例句柄。</para>
        /// <para>GWL_ID            -12    设置一个新的窗口标识符。</para>
        /// <para>GWL_STYLE         -16    设定一个新的窗口风格。</para>
        /// <para>GWL_USERDATA      -21    设置与窗口有关的32位值。每个窗口均有一个由创建该窗口的应用程序使用的32位值。</para>
        /// <para>GWL_WNDPROC       -4    为窗口设定一个新的处理函数。</para>
        /// <para>GWL_HWNDPARENT    -8    改变子窗口的父窗口,应使用SetParent函数</para>
        /// </param>
        /// <param name="dwNewLong">指定的替换值</param>
        /// <returns></returns>
        [Obsolete("请使用 SetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
        [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// 改变指定窗口的属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">指定将设定的大于等于0的偏移值。有效值的范围从0到额外类的存储空间的字节数减4：例如若指定了12或多于12个字节的额外窗口存储空间，则应设索引位8来访问第三个4字节，同样设置0访问第一个4字节，4访问第二个4字节。要设置其他任何值，可以指定下面值之一
        /// 从 GetWindowLongFields 可以找到所有的值
        /// <para>
        /// GetWindowLongFields.GWL_EXSTYLE             -20    设定一个新的扩展风格。 </para>
        /// <para>GWL_HINSTANCE     -6	   设置一个新的应用程序实例句柄。</para>
        /// <para>GWL_ID            -12    设置一个新的窗口标识符。</para>
        /// <para>GWL_STYLE         -16    设定一个新的窗口风格。</para>
        /// <para>GWL_USERDATA      -21    设置与窗口有关的32位值。每个窗口均有一个由创建该窗口的应用程序使用的32位值。</para>
        /// <para>GWL_WNDPROC       -4    为窗口设定一个新的处理函数。</para>
        /// <para>GWL_HWNDPARENT    -8    改变子窗口的父窗口,应使用SetParent函数</para>
        /// </param>
        /// <param name="dwNewLong">指定的替换值</param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = Properties.BuildCharSet, EntryPoint = "SetWindowLongPtr")]
        [Obsolete("请使用 SetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
        public static extern IntPtr SetWindowLongPtr_x64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        /// <summary>
        /// 获得指定窗口的信息
        /// </summary>
        /// <param name="hWnd">指定窗口的句柄</param>
        /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
        /// <returns></returns>
        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex) => GetWindowLongPtr(hWnd, (int)nIndex);

        /// <summary>
        /// 获得指定窗口的信息
        /// </summary>
        /// <param name="hWnd">指定窗口的句柄</param>
        /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
        /// <returns></returns>
        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size > 4
#pragma warning disable CS0618 // 类型或成员已过时
                    ? GetWindowLongPtr_x64(hWnd, nIndex)
                : new IntPtr(GetWindowLong(hWnd, nIndex));
#pragma warning restore CS0618 // 类型或成员已过时
        }

        /// <summary>
        /// 获得指定窗口的信息
        /// </summary>
        /// <param name="hWnd">指定窗口的句柄</param>
        /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
        /// <returns></returns>
        [Obsolete("请使用 GetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
        [DllImport(LibraryName, CharSet = Properties.BuildCharSet)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// 获得指定窗口的信息
        /// </summary>
        /// <param name="hWnd">指定窗口的句柄</param>
        /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
        /// <returns></returns>
        [Obsolete("请使用 GetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
        [DllImport(LibraryName, CharSet = Properties.BuildCharSet, EntryPoint = "GetWindowLongPtr")]
        public static extern IntPtr GetWindowLongPtr_x64(IntPtr hWnd, int nIndex);

        internal static class Properties
        {
#if !ANSI
            public const CharSet BuildCharSet = CharSet.Unicode;
#else
            public const CharSet BuildCharSet = CharSet.Ansi;
#endif
        }

        [Flags]
        public enum WindowStyles
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
            WS_POPUP = unchecked((int)0x80000000),

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

        /// <summary>
        /// 用于在 <see cref="Win32.GetWindowLong"/> 的 int index 传入
        /// </summary>
        /// 代码：[GetWindowLong function (Windows)](https://msdn.microsoft.com/en-us/library/windows/desktop/ms633584(v=vs.85).aspx )
        public enum GetWindowLongFields
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
    }
}