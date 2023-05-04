using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace WhefallralajaHubeanerelair;

[SuppressUnmanagedCodeSecurity]
internal static class UnsafeNativeMethods
{
    public const string INKEDIT_WINDOWCLASS = "INKEDIT";
    public const string INKEDIT_DLL = "INKED.DLL";
    private const int WM_USER = 1024;
    private const int IEC__BASE = 1536;
    private const int IECN__BASE = 2048;
    public const int EM_GETINKMODE = 1537;
    public const int EM_SETINKMODE = 1538;
    public const int EM_GETINKINSERTMODE = 1539;
    public const int EM_SETINKINSERTMODE = 1540;
    public const int EM_GETDRAWATTR = 1541;
    public const int EM_SETDRAWATTR = 1542;
    public const int EM_GETRECOTIMEOUT = 1543;
    public const int EM_SETRECOTIMEOUT = 1544;
    public const int EM_GETGESTURESTATUS = 1545;
    public const int EM_SETGESTURESTATUS = 1546;
    public const int EM_GETRECOGNIZER = 1547;
    public const int EM_SETRECOGNIZER = 1548;
    public const int EM_GETFACTOID = 1549;
    public const int EM_SETFACTOID = 1550;
    public const int EM_GETSELINK = 1551;
    public const int EM_SETSELINK = 1552;
    public const int EM_GETMOUSEICON = 1553;
    public const int EM_SETMOUSEICON = 1554;
    public const int EM_GETMOUSEPOINTER = 1555;
    public const int EM_SETMOUSEPOINTER = 1556;
    public const int EM_GETSTATUS = 1557;
    public const int EM_RECOGNIZE = 1558;
    public const int EM_GETUSEMOUSEFORINPUT = 1559;
    public const int EM_SETUSEMOUSEFORINPUT = 1560;
    public const int EM_SETSELINKDISPLAYMODE = 1561;
    public const int EM_GETSELINKDISPLAYMODE = 1562;
    public const int WM_NOTIFY = 78;
    public const int WM_SYSCOLORCHANGE = 21;
    public const int WM_REFLECT = 8192;
    public const int IECN_STROKE = 2049;
    public const int IECN_GESTURE = 2050;
    public const int IECN_RECOGNITIONRESULT = 2051;
    public const int SM_TABLETPC = 86;
    public const int CLSCTX_INPROC_SERVER = 1;

    [DllImport("Ole32.dll", PreserveSig = false)]
    public static extern UnsafeNativeMethods.IClassFactory2 CoGetClassObject(
        ref Guid clsid,
        uint dwClsContext,
        IntPtr serverInfo,
        ref Guid iid);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr LoadLibrary(string libname);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern bool FreeLibrary(IntPtr hModule);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern IntPtr SendMessage(
    //  IntPtr hWnd,
    //  int msg,
    //  IntPtr wParam,
    //  ref IInkRecognizer lParam);

    //[DllImport("user32.dll", CharSet = CharSet.Auto)]
    //public static extern IntPtr SendMessage(
    //  IntPtr hWnd,
    //  int msg,
    //  IntPtr wParam,
    //  IInkRecognizer lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        InkDrawingAttributes lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        ref InkDrawingAttributes lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        ref object lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.BStr)] string lParam);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern int SaveDC(IntPtr hDC);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    public static extern bool RestoreDC(IntPtr hDC, int savedDC);

    [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
    [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
    public static object PtrToStructure(IntPtr lparam, Type cls) => Marshal.PtrToStructure(lparam, cls);

    internal class RichTextBoxConstants
    {
        internal const int CFM_BOLD = 1;
        internal const int CFM_ITALIC = 2;
        internal const int CFM_UNDERLINE = 4;
        internal const int CFM_STRIKEOUT = 8;
        internal const int CFM_PROTECTED = 16;
        internal const int CFM_LINK = 32;
        internal const int CFM_SIZE = -2147483648;
        internal const int CFM_COLOR = 1073741824;
        internal const int CFM_FACE = 536870912;
        internal const int CFM_OFFSET = 268435456;
        internal const int CFM_CHARSET = 134217728;
        internal const int CFE_BOLD = 1;
        internal const int CFE_ITALIC = 2;
        internal const int CFE_UNDERLINE = 4;
        internal const int CFE_STRIKEOUT = 8;
        internal const int CFE_PROTECTED = 16;
        internal const int CFE_LINK = 32;
        internal const int CFE_AUTOCOLOR = 1073741824;
        internal const int SCF_SELECTION = 1;
        internal const int SCF_WORD = 2;
        internal const int SCF_DEFAULT = 0;
        internal const int SCF_ALL = 4;
        internal const int SCF_USEUIRULES = 8;
        internal const int CFM_EFFECTS = 1073741887;
        internal const int CFM_ALL = -134217665;
        internal const int CFM_SMALLCAPS = 64;
        internal const int CFM_ALLCAPS = 128;
        internal const int CFM_HIDDEN = 256;
        internal const int CFM_OUTLINE = 512;
        internal const int CFM_SHADOW = 1024;
        internal const int CFM_EMBOSS = 2048;
        internal const int CFM_IMPRINT = 4096;
        internal const int CFM_DISABLED = 8192;
        internal const int CFM_REVISED = 16384;
        internal const int CFM_BACKCOLOR = 67108864;
        internal const int CFM_LCID = 33554432;
        internal const int CFM_UNDERLINETYPE = 8388608;
        internal const int CFM_WEIGHT = 4194304;
        internal const int CFM_SPACING = 2097152;
        internal const int CFM_KERNING = 1048576;
        internal const int CFM_STYLE = 524288;
        internal const int CFM_ANIMATION = 262144;
        internal const int CFM_REVAUTHOR = 32768;
        internal const int CFE_SUBSCRIPT = 65536;
        internal const int CFE_SUPERSCRIPT = 131072;
        internal const int CFM_SUBSCRIPT = 196608;
        internal const int CFM_SUPERSCRIPT = 196608;
        internal const int CFM_EFFECTS2 = 1141080063;
        internal const int CFM_ALL2 = -16777217;
        internal const int EM_GETCHARFORMAT = 1082;
    }

    [Guid("B196B28F-BAB4-101A-B69C-00AA00341D07")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [SuppressUnmanagedCodeSecurity]
    [ComImport]
    internal interface IClassFactory2
    {
        void CreateInstance([In] IntPtr pUnkOuter, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObj);

        void LockServer([MarshalAs(UnmanagedType.Bool), In] bool fLock);

        void GetLicInfo(out IntPtr pLicInfo);

        void RequestLic([In] int dwReserved, [MarshalAs(UnmanagedType.BStr)] out string pBstrKey);

        void CreateInstanceLic(
            [In] IntPtr pUnkOuter,
            [In] IntPtr pUnkReserved,
            [In] ref Guid riid,
            [MarshalAs(UnmanagedType.BStr), In] string bstrKey,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppvObj);
    }
}