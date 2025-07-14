using System.Drawing;
using System.Runtime.InteropServices;
using Rectangle = SplashScreensLib.Native.Win32.RECT;

namespace SplashScreensLib.Native;

public static partial class Win32
{
    public delegate IntPtr WindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public delegate IntPtr GetMsgProc(int code, IntPtr wParam, IntPtr lParam);

    public delegate void TimerProc(IntPtr hWnd, uint uMsg, IntPtr nIdEvent, uint dwTickCountMillis);

    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        public IntPtr Hwnd;
        public uint Value;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public Win32.POINT Point;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PaintStruct
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
    }

    /// <summary>
    ///     Note: Marshalled
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WindowClassEx
    {
        public uint Size;
        public WindowClassStyles Styles;
        [MarshalAs(UnmanagedType.FunctionPtr)] public WindowProc WindowProc;
        public int ClassExtraBytes;
        public int WindowExtraBytes;
        public IntPtr InstanceHandle;
        public IntPtr IconHandle;
        public IntPtr CursorHandle;
        public IntPtr BackgroundBrushHandle;
        public string MenuName;
        public string ClassName;
        public IntPtr SmallIconHandle;
    }

    /// <summary>
    ///     Note: Marshalled
    ///     在 C++ 中，MenuName 和 ClassName 既可以传入字符串又可以传入指针，这在 C# 中是不被允许的。因此，我们使用两个不同的类型来分别应对这些不同的 API 调用所需的对象。
    ///     另外，这可能会导致方法也写双份，这很让人头疼，但 WPF 内部代码也这么做了……
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WindowClassExPtr
    {
        public uint Size;
        public WindowClassStyles Styles;
        [MarshalAs(UnmanagedType.FunctionPtr)] public WindowProc WindowProc;
        public int ClassExtraBytes;
        public int WindowExtraBytes;
        public IntPtr InstanceHandle;
        public IntPtr IconHandle;
        public IntPtr CursorHandle;
        public IntPtr BackgroundBrushHandle;
        public IntPtr MenuName;
        public IntPtr ClassName;
        public IntPtr SmallIconHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowClassExBlittable
    {
        public uint Size;
        public WindowClassStyles Styles;
        public IntPtr WindowProc;
        public int ClassExtraBytes;
        public int WindowExtraBytes;
        public IntPtr InstanceHandle;
        public IntPtr IconHandle;
        public IntPtr CursorHandle;
        public IntPtr BackgroundBrushHandle;
        public IntPtr MenuName;
        public IntPtr ClassName;
        public IntPtr SmallIconHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowInfo
    {
        public uint Size;
        public Rectangle WindowRect;
        public Rectangle ClientRect;
        public WindowStyles Styles;
        public ExtendedWindowStyles ExStyles;
        public uint WindowStatus;
        public uint BorderX;
        public uint BorderY;
        public ushort WindowType;
        public ushort CreatorVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CreateStruct
    {
        public IntPtr CreateParams;
        public IntPtr InstanceHandle;
        public IntPtr MenuHandle;
        public IntPtr ParentHwnd;
        public int Height;
        public int Width;
        public int Y;
        public int X;
        public WindowStyles Styles;
        public IntPtr Name;
        public IntPtr ClassName;
        public WindowExStyles ExStyles;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        public uint Size;
        public WindowPlacementFlags Flags;
        public ShowWindowCommands ShowCmd;
        public Point MinPosition;
        public Point MaxPosition;
        public Rectangle NormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public AlphaFormat AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationInfo
    {
        /// <summary>
        ///     Creates an AMINMATIONINFO structure.
        /// </summary>
        /// <param name="iMinAnimate">If non-zero and SPI_SETANIMATION is specified, enables minimize/restore animation.</param>
        public AnimationInfo(int iMinAnimate)
        {

            Size = (uint) Marshal.SizeOf(typeof(AnimationInfo));
            this.MinAnimate = iMinAnimate;
        }

        /// <summary>
        ///     Always must be set to (System.UInt32)Marshal.SizeOf(typeof(ANIMATIONINFO)).
        /// </summary>
        public uint Size;

        /// <summary>
        ///     If non-zero, minimize/restore animation is enabled, otherwise disabled.
        /// </summary>
        public int MinAnimate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MinimizedMetrics
    {
        public uint Size;
        public int Width;
        public int HorizontalGap;
        public int VerticalGap;
        public ArrangeFlags Arrange;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TrackMouseEventOptions
    {
        public uint Size;
        public TrackMouseEventFlags Flags;
        public IntPtr TrackedHwnd;
        public uint HoverTime;

        public const uint DefaultHoverTime = 0xFFFFFFFF;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MinMaxInfo
    {
        Point Reserved;

        /// <summary>
        ///     The maximized width (x member) and the maximized height (y member) of the window. For top-level windows, this value
        ///     is based on the width of the primary monitor.
        /// </summary>
        public Point MaxSize;

        /// <summary>
        ///     The position of the left side of the maximized window (x member) and the position of the top of the maximized
        ///     window (y member). For top-level windows, this value is based on the position of the primary monitor.
        /// </summary>
        public Point MaxPosition;

        /// <summary>
        ///     The minimum tracking width (x member) and the minimum tracking height (y member) of the window. This value can be
        ///     obtained programmatically from the system metrics SM_CXMINTRACK and SM_CYMINTRACK (see the GetSystemMetrics
        ///     function).
        /// </summary>
        public Point MinTrackSize;

        /// <summary>
        ///     The maximum tracking width (x member) and the maximum tracking height (y member) of the window. This value is based
        ///     on the size of the virtual screen and can be obtained programmatically from the system metrics SM_CXMAXTRACK and
        ///     SM_CYMAXTRACK (see the GetSystemMetrics function).
        /// </summary>
        public Point MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPosition
    {
        public IntPtr Hwnd;
        public IntPtr HwndZOrderInsertAfter;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public WindowPositionFlags Flags;
    }

    //[StructLayout(LayoutKind.Sequential)]
    //public unsafe struct NcCalcSizeParams
    //{
    //    public NcCalcSizeRegionUnion Region;
    //    public WindowPosition* Position;
    //}

    [StructLayout(LayoutKind.Explicit)]
    public struct NcCalcSizeRegionUnion
    {
        [FieldOffset(0)] public NcCalcSizeInput Input;
        [FieldOffset(0)] public NcCalcSizeOutput Output;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NcCalcSizeInput
    {
        public Rectangle TargetWindowRect;
        public Rectangle CurrentWindowRect;
        public Rectangle CurrentClientRect;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NcCalcSizeOutput
    {
        public Rectangle TargetClientRect;
        public Rectangle DestRect;
        public Rectangle SrcRect;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
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

    /// <summary>
    /// The MONITORINFOEX structure contains information about a display monitor.
    /// The GetMonitorInfo function stores information into a MONITORINFOEX structure or a MONITORINFO structure.
    /// The MONITORINFOEX structure is a superset of the MONITORINFO structure.The MONITORINFOEX structure adds a string member to contain a name for the display monitor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public partial struct MonitorInfoEx
    {
        /// <summary>
        /// The <see cref="MonitorInfo"/> structure.
        /// </summary>
        public MonitorInfo MonitorInfo;

        /// <summary>
        /// A string that specifies the device name of the monitor being used. Most applications have no use for a display monitor name, and so can save some bytes by using a MONITORINFO structure.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TitleBarInfo
    {
        public uint Size;
        public Rectangle TitleBarRect;
        public ElementSystemStates TitleBarStates;
        private ElementSystemStates Reserved;
        public ElementSystemStates MinimizeButtonStates;
        public ElementSystemStates MaximizeButtonStates;
        public ElementSystemStates HelpButtonStates;
        public ElementSystemStates CloseButtonStates;
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
}
