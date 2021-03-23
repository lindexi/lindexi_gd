using System;
using System.Runtime.InteropServices;

namespace RuhuyagayBemkaijearfear
{
    static partial class Win32
    {
        /// <summary>
        /// 扩展的窗口风格
        /// 这是 long 类型的，如果想要使用 int 类型请使用 <see cref="WindowExStyles"/> 类
        /// </summary>
        /// 代码：[Extended Window Styles (Windows)](https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543(v=vs.85).aspx )
        /// code from [Extended Window Styles (Windows)](https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543(v=vs.85).aspx )
        [Flags]
        public enum ExtendedWindowStyles : long
        {
            /// <summary>
            /// The window accepts drag-drop files
            /// </summary>
            WS_EX_ACCEPTFILES = 0x00000010L,

            /// <summary>
            /// Forces a top-level window onto the taskbar when the window is visible
            /// </summary>
            WS_EX_APPWINDOW = 0x00040000L,

            /// <summary>
            /// The window has a border with a sunken edge.
            /// </summary>
            WS_EX_CLIENTEDGE = 0x00000200L,

            /// <summary>
            /// Paints all descendants of a window in bottom-to-top painting order using double-buffering. For more information, see Remarks. This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.Windows 2000:  This style is not supported.
            /// </summary>
            WS_EX_COMPOSITED = 0x02000000L,

            /// <summary>
            /// The title bar of the window includes a question mark. When the user clicks the question mark, the cursor changes to a question mark with a pointer. If the user then clicks a child window, the child receives a WM_HELP message. The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command. The Help application displays a pop-up window that typically contains help for the child window.WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.
            /// </summary>
            WS_EX_CONTEXTHELP = 0x00000400L,

            /// <summary>
            /// The window itself contains child windows that should take part in dialog box navigation. If this style is specified, the dialog manager recurses into children of this window when performing navigation operations such as handling the TAB key, an arrow key, or a keyboard mnemonic.
            /// </summary>
            WS_EX_CONTROLPARENT = 0x00010000L,

            /// <summary>
            /// The window has a double border; the window can, optionally, be created with a title bar by specifying the WS_CAPTION style in the dwStyle parameter.
            /// </summary>
            WS_EX_DLGMODALFRAME = 0x00000001L,

            /// <summary>
            /// The window is a layered window. This style cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.Windows 8:  The WS_EX_LAYERED style is supported for top-level windows and child windows. Previous Windows versions support WS_EX_LAYERED only for top-level windows.
            /// </summary>
            WS_EX_LAYERED = 0x00080000,

            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the horizontal origin of the window is on the right edge. Increasing horizontal values advance to the left.
            /// </summary>
            WS_EX_LAYOUTRTL = 0x00400000L,

            /// <summary>
            /// The window has generic left-aligned properties. This is the default.
            /// </summary>
            WS_EX_LEFT = 0x00000000L,

            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the vertical scroll bar (if present) is to the left of the client area. For other languages, the style is ignored.
            /// </summary>
            WS_EX_LEFTSCROLLBAR = 0x00004000L,

            /// <summary>
            /// The window text is displayed using left-to-right reading-order properties. This is the default.
            /// </summary>
            WS_EX_LTRREADING = 0x00000000L,

            /// <summary>
            /// The window is a MDI child window.
            /// </summary>
            WS_EX_MDICHILD = 0x00000040L,

            /// <summary>
            /// A top-level window created with this style does not become the foreground window when the user clicks it. The system does not bring this window to the foreground when the user minimizes or closes the foreground window.To activate the window, use the SetActiveWindow or SetForegroundWindow function.The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
            /// </summary>
            WS_EX_NOACTIVATE = 0x08000000L,

            /// <summary>
            /// The window does not pass its window layout to its child windows.
            /// </summary>
            WS_EX_NOINHERITLAYOUT = 0x00100000L,

            /// <summary>
            /// The child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
            /// </summary>
            WS_EX_NOPARENTNOTIFY = 0x00000004L,

            /// <summary>
            /// The window does not render to a redirection surface. This is for windows that do not have visible content or that use mechanisms other than surfaces to provide their visual.
            /// </summary>
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000L,

            /// <summary>
            /// The window is an overlapped window.
            /// </summary>
            WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE),

            /// <summary>
            /// The window is palette window, which is a modeless dialog box that presents an array of commands.
            /// </summary>
            WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST),

            /// <summary>
            /// The window has generic "right-aligned" properties. This depends on the window class. This style has an effect only if the shell language is Hebrew, Arabic, or another language that supports reading-order alignment; otherwise, the style is ignored.Using the WS_EX_RIGHT style for static or edit controls has the same effect as using the SS_RIGHT or ES_RIGHT style, respectively. Using this style with button controls has the same effect as using BS_RIGHT and BS_RIGHTBUTTON styles.
            /// </summary>
            WS_EX_RIGHT = 0x00001000L,

            /// <summary>
            /// The vertical scroll bar (if present) is to the right of the client area. This is the default.
            /// </summary>
            WS_EX_RIGHTSCROLLBAR = 0x00000000L,

            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading-order alignment, the window text is displayed using right-to-left reading-order properties. For other languages, the style is ignored.
            /// </summary>
            WS_EX_RTLREADING = 0x00002000L,

            /// <summary>
            /// The window has a three-dimensional border style intended to be used for items that do not accept user input.
            /// </summary>
            WS_EX_STATICEDGE = 0x00020000L,

            /// <summary>
            /// The window is intended to be used as a floating toolbar. A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font. A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB. If a tool window has a system menu, its icon is not displayed on the title bar. However, you can display the system menu by right-clicking or by typing ALT+SPACE.
            /// </summary>
            WS_EX_TOOLWINDOW = 0x00000080L,

            /// <summary>
            /// The window should be placed above all non-topmost windows and should stay above them, even when the window is deactivated. To add or remove this style, use the SetWindowPos function.
            /// </summary>
            WS_EX_TOPMOST = 0x00000008L,

            /// <summary>
            /// The window should not be painted until siblings beneath the window (that were created by the same thread) have been painted. The window appears transparent because the bits of underlying sibling windows have already been painted.To achieve transparency without these restrictions, use the SetWindowRgn function.
            /// </summary>
            WS_EX_TRANSPARENT = 0x00000020L,

            /// <summary>
            /// The window has a border with a raised edge
            /// </summary>
            WS_EX_WINDOWEDGE = 0x00000100L
        }

        public static partial class User32
        {
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
            public const string LibraryName = "user32";
        }

        internal static class Properties
        {
#if !ANSI
            public const CharSet BuildCharSet = CharSet.Unicode;
#else
            public const CharSet BuildCharSet = CharSet.Ansi;
#endif
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