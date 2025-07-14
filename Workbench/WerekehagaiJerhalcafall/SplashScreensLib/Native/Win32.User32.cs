using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Rectangle = SplashScreensLib.Native.Win32.RECT;
using Point = SplashScreensLib.Native.Win32.POINT;

namespace SplashScreensLib.Native;

public partial class Win32
{
    public static partial class User32
    {
        public const string LibraryName = "user32";

        /// <summary>
        /// 该函数从与hInstance模块相关联的可执行文件中装入lpIconName指定的图标资源,仅当图标资源还没有被装入时该函数才执行装入操作,否则只获取装入的资源句柄
        /// </summary>
        /// <param name="hInstance"></param>
        /// <param name="lpIconName"></param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadIcon(IntPtr hInstance, string lpIconName);

        /// <summary>
        /// 该函数从与hInstance模块相关联的可执行文件中装入lpIconName指定的图标资源,仅当图标资源还没有被装入时该函数才执行装入操作,否则只获取装入的资源句柄
        /// </summary>
        /// <param name="hInstance"></param>
        /// <param name="lpIconResource"></param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconResource);

        /// <summary>
        /// 该函数从与hInstance模块相关联的可执行文件中装入lpIconName指定的图标资源,仅当图标资源还没有被装入时该函数才执行装入操作,否则只获取装入的资源句柄
        /// </summary>
        /// <param name="hInstance"></param>
        /// <param name="lpIconName"></param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode, EntryPoint = "LoadIconW", SetLastError = true)]
        public static extern IntPtr LoadIcon([In] IntPtr hInstance, [In] SystemIcon lpIconName);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadCursor(IntPtr hInstance, string lpCursorName);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorResource);

        /// <summary>
        /// Loads the specified cursor resource from the executable (.EXE) file associated with an application instance.
        /// This function has been superseded by the LoadImage function
        /// </summary>
        /// <param name="hInstance"></param>
        /// <param name="lpCursorName"></param>
        /// <returns></returns>
        [Obsolete("请使用 LoadImage 替代。")]
        [DllImport(LibraryName, CharSet = CharSet.Unicode, EntryPoint = "LoadCursorW", SetLastError = true)]
        public static extern IntPtr LoadCursor([In] IntPtr hInstance, [In] SystemCursor lpCursorName);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadImage(IntPtr hInstance, string lpszName, ResourceImageType uType,
            int cxDesired, int cyDesired, LoadResourceFlags fuLoad);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadImage(IntPtr hInstance, IntPtr resourceId, ResourceImageType uType,
            int cxDesired, int cyDesired, LoadResourceFlags fuLoad);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadBitmap(IntPtr hInstance, string lpBitmapName);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadBitmap(IntPtr hInstance, IntPtr resourceId);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr BeginPaint(IntPtr hWnd, out PaintStruct lpPaint);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern void EndPaint(IntPtr hWnd, [In] ref PaintStruct lpPaint);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr MonitorFromPoint(Point pt, MonitorFlag dwFlags);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr MonitorFromRect(in Rectangle lprc, MonitorFlag dwFlags);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr MonitorFromWindow(IntPtr hWnd, MonitorFlag dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplayMonitors", ExactSpelling = true, SetLastError = true)]
        public static extern bool EnumDisplayMonitors([In] nint hdc, [In] nint lprcClip, [In] Monitorenumproc lpfnEnum, [In] nint dwData);

        public delegate bool Monitorenumproc([In] nint Arg1, [In] nint Arg2, [In] in RECT Arg3, [In] nint Arg4);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int DrawText(IntPtr hdc, string lpString, int nCount, [In] ref Rectangle lpRect,
            uint uFormat);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifierFlags fsModifiers, VirtualKey vk);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint SendInput(uint nInputs, IntPtr pInputs, int cbSize);

        //[DllImport(LibraryName, ExactSpelling = true)]
        //public static extern uint SendInput(uint nInputs, [In] Input[] pInputs, int cbSize);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIdEvent, uint uElapseMillis,
            TimerProc lpTimerFunc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool KillTimer(IntPtr hWnd, IntPtr uIdEvent);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ValidateRect(IntPtr hWnd, [In] ref Rectangle lpRect);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ValidateRect(IntPtr hWnd, IntPtr lpRect);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool InvalidateRect(IntPtr hWnd, [In] ref Rectangle lpRect, bool bErase);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetDC(IntPtr hwnd);

        /// <summary>
        /// Retrieves the specified system metric or system configuration setting.
        /// Note that all dimensions retrieved by GetSystemMetrics are in pixels.
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns>
        ///If the function succeeds, the return value is the requested system metric or configuration setting.
        /// If the function fails, the return value is 0. GetLastError does not provide extended error information.
        /// </returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetSystemMetrics(SystemMetrics nIndex);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam,
            SystemParamtersInfoFlags fWinIni);

        /// <summary>
        /// 指定的窗口的设备环境
        /// </summary>
        /// <param name="hWnd"></param>
        /// <remarks>获得的设备环境覆盖了整个窗口（包括非客户区），例如标题栏、菜单、滚动条，以及边框。这使得程序能够在非客户区域实现自定义图形，例如自定义标题或者边框。当不再需要该设备环境时，需要调用ReleaseDC函数释放设备环境。注意，该函数只获得通用设备环境，该设备环境的任何属性改变都不会反映到窗口的私有或者类设备环境中</remarks>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr WindowFromDC(IntPtr hdc);

        /// <summary>
        /// 释放指定的设备上下文环境
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hdc"></param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hdc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool InvertRect(IntPtr hdc, [In] ref Rectangle lprc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetRectEmpty(out Rectangle lprc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool AdjustWindowRect([In][Out] ref Rectangle lpRect, WindowStyles dwStyle,
            bool hasMenu);

        //[DllImport(LibraryName, ExactSpelling = true)]
        //public static extern bool AdjustWindowRectEx([In][Out] ref Rectangle lpRect, WindowStyles dwStyle,
        //    bool hasMenu,
        //    ExtendedWindowStyles dwExStyle);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool CopyRect(out Rectangle lprcDst, [In] ref Rectangle lprcSrc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool IntersectRect(out Rectangle lprcDst, [In] ref Rectangle lprcSrc1,
            [In] ref Rectangle lprcSrc2);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool UnionRect(out Rectangle lprcDst, [In] ref Rectangle lprcSrc1,
            [In] ref Rectangle lprcSrc2);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool IsRectEmpty([In] ref Rectangle lprc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool PtInRect([In] ref Rectangle lprc, Point pt);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool OffsetRect([In][Out] ref Rectangle lprc, int dx, int dy);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool InflateRect([In][Out] ref Rectangle lprc, int dx, int dy);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool FrameRect(IntPtr hdc, [In] ref Rectangle lprc, IntPtr hbr);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool FillRect(IntPtr hdc, [In] ref Rectangle lprc, IntPtr hbr);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType GetWindowRgnBox(IntPtr hWnd, out Rectangle lprc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha,
            LayeredWindowAttributeFlag dwFlags);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool WinHelp(IntPtr hWndMain, string lpszHelp, uint uCommand, uint dwData);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool InvalidateRgn(IntPtr hWnd, IntPtr hRgn, bool bErase);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetUpdateRect(IntPtr hWnd, out Rectangle rect, bool bErase);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ValidateRgn(IntPtr hWnd, IntPtr hRgn);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hrgnClip, DeviceContextFlags flags);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern void DisableProcessWindowsGhosting();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst,
            [In] ref Point pptDst, [In] ref Win32.Size psize, IntPtr hdcSrc, [In] ref Point pptSrc, uint crKey,
            [In] ref BlendFunction pblend, uint dwFlags);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, IntPtr lpPoints, int cPoints);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ScreenToClient(IntPtr hWnd, [In][Out] ref Point lpPoint);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr WindowFromPhysicalPoint(Point point);

        #region Hook

        [DllImport(LibraryName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport(LibraryName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        #endregion

        /// <summary>
        /// 获取指定点所在窗口的句柄
        /// </summary>
        /// <remarks>[WinAPI: WindowFromPoint- 获取指定点所在窗口的句柄](http://www.cnblogs.com/del/archive/2008/03/09/1097942.html )</remarks>
        /// <param name="point"></param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr WindowFromPoint(Point point);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool OpenIcon(IntPtr hwnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool IsHungAppWindow(IntPtr hwnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool IsZoomed(IntPtr hwnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool IsIconic(IntPtr hwnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool LogicalToPhysicalPoint(IntPtr hWnd, [In][Out] ref Point point);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr ChildWindowFromPoint(IntPtr hwndParent, Point point);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr ChildWindowFromPointEx(IntPtr hwndParent, Point point,
            ChildWindowFromPointFlags flags);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetMenu(IntPtr hwnd);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern MessageBoxResult MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint type);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern MessageBoxResult MessageBoxEx(IntPtr hWnd, string lpText, string lpCaption, uint type,
            ushort wLanguageId);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetThreadDesktop(IntPtr hDesk);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetThreadDesktop(uint threadId);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        [DllImport(LibraryName, CharSet = CharSet.Unicode, EntryPoint = "GetDpiForWindow", SetLastError = true)]
        public static extern uint GetDpiForWindow([In] IntPtr hwnd);

        #region Keyboard, Mouse & Input Method Functions

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        /// <summary>
        /// 该函数在属于当前线程的指定窗口里设置鼠标捕获。一旦窗口捕获了鼠标，所有鼠标输入都针对该窗口，无论光标是否在窗口的边界内。同一时刻只能有一个窗口捕获鼠标。如果鼠标光标在另一个线程创建的窗口上，只有当鼠标键按下时系统才将鼠标输入指向指定的窗口
        /// </summary>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ReleaseCapture();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetFocus();

        /// <summary>
        /// 该函数对指定的窗口设置键盘焦点。该窗口必须与调用线程的消息队列相关
        /// </summary>
        /// <param name="hWnd">接收键盘输入的窗口指针</param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetActiveWindow();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool BlockInput(bool fBlockIt);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint WaitForInputIdle(IntPtr hProcess, uint dwMilliseconds);

        /// <summary>
        /// 把一个线程的输入消息连接到另一个线程
        /// </summary>
        /// <param name="idAttach"></param>
        /// <param name="idAttachTo"></param>
        /// <param name="fAttach"></param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool DragDetect(IntPtr hWnd, Point point);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ClientToScreen(IntPtr hWnd, [In][Out] ref Point point);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ClipCursor([In] ref Rectangle rect);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ClipCursor(IntPtr ptr);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool TrackMouseEvent([In][Out] ref TrackMouseEventOptions lpEventTrack);

        //[DllImport(LibraryName, ExactSpelling = true)]
        //public static extern bool GetLastInputInfo(out LastInputInfo plii);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern uint MapVirtualKey(uint uCode, VirtualKeyMapType uMapType);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern uint MapVirtualKey(VirtualKey uCode, VirtualKeyMapType uMapType);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern uint MapVirtualKeyEx(uint uCode, VirtualKeyMapType uMapType, IntPtr dwhkl);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern uint MapVirtualKeyEx(VirtualKey uCode, VirtualKeyMapType uMapType, IntPtr dwhkl);

        //[DllImport(LibraryName, ExactSpelling = true)]
        //public static extern KeyState GetAsyncKeyState(int vKey);

        //[DllImport(LibraryName, ExactSpelling = true)]
        //public static extern KeyState GetAsyncKeyState(VirtualKey vKey);

        /// <summary>
        /// 模拟键盘
        /// </summary>
        /// <param name="bVk"></param>
        /// <param name="bScan"></param>
        /// <param name="dwFlags"></param>
        /// <param name="dwExtraInfo"></param>
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "keybd_event")]
        public static extern void Keyboard_Event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        ///// <summary>
        ///// 检取指定虚拟键的状态
        ///// <remarks>得到某一个键实时的状态请使用GetAsyncKeyState</remarks>
        ///// </summary>
        ///// <param name="nVirtKey"></param>
        ///// <returns></returns>
        //[DllImport(LibraryName, ExactSpelling = true)]
        //public static extern KeyState GetKeyState(int nVirtKey);

        ///// <summary>
        ///// 检取指定虚拟键的状态
        ///// <remarks>得到某一个键实时的状态请使用GetAsyncKeyState</remarks>
        ///// </summary>
        ///// <param name="nVirtKey"></param>
        ///// <returns></returns>
        //[DllImport(LibraryName, ExactSpelling = true)]
        //public static extern KeyState GetKeyState(VirtualKey nVirtKey);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetKeyboardState(
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 256)]
                out byte[] lpKeyState);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetKeyboardState(IntPtr lpKeyState);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetKeyboardState(
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 256)] [In]
                byte[] lpKeyState);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetKeyboardState(IntPtr lpKeyState);

        /// <summary>
        ///     Retrieves information about the specified title bar.
        /// </summary>
        /// <param name="hWnd">A handle to the title bar whose information is to be retrieved.</param>
        /// <param name="pti">
        ///     A pointer to a TITLEBARINFO structure to receive the information. Note that you must set the cbSize
        ///     member to sizeof(TITLEBARINFO) before calling this function.
        /// </param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetTitleBarInfo(IntPtr hWnd, IntPtr pti);

        #endregion

        #region Cursor Functions

        /// <summary>
        /// 获得光标坐标
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetCursorPos(out Point point);

        /// <summary>
        /// 设置光标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetPhysicalCursorPos(int x, int y);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetSystemCursor(IntPtr cursor, SystemCursor id);

        /// <summary>
        /// 显示或隐藏光标，通过 <paramref name="bShow"/> 参数决定是否显示或隐藏光标。内部有维护计数器，只有返回值是小于零才能表示隐藏光标，只有返回值大于等于零才表示显示光标。推荐使用封装好的 <see cref="WindowsCursorsManager.ShowCursor()"/> 或 <see cref="WindowsCursorsManager.HideCursor"/> 方法进行显示或隐藏光标
        /// </summary>
        /// <param name="bShow">如果为 true 则显示光标，如果为 false 则隐藏光标</param>
        /// <returns>如果返回的值小于0 ，那么隐藏鼠标，如果返回的值大于等于0，显示鼠标</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int ShowCursor(bool bShow);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CopyCursor(IntPtr hCursor);

        //[DllImport(LibraryName, ExactSpelling = true)]
        //public static extern bool GetCursorInfo(ref CursorInfo info);

        #endregion

        #region CaretPos

        /// <summary>
        /// 函数将插入输入法光标的位置（按客户区坐标）信息拷贝到指定的POINT结构中
        /// </summary>
        /// <param name="p">指向POINT结构的指针。该结构接收插入标记的客户坐标信息</param>
        /// <returns>如果函数执行成功，那么返回值非零；如果函数执行失败，那么返回值为零。若想获取更多错误信息，请调用GetLastError函数</returns>
        [DllImport("user32")]
        public static extern int GetCaretPos(out Point p);

        /// <summary>
        /// 设置当前输出法光标所在坐标
        /// 如果要和 TextOut 一起使用，请先设置 SetCaretPos 然后调用 TextOut 方法
        /// </summary>
        /// [SetCaretPos和TextOut调用的顺序 - Marcelxx的专栏 - CSDN博客](https://blog.csdn.net/Marcelxx/article/details/11583815 )
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [DllImport("user32")]
        public static extern int SetCaretPos(int x, int y);

        /// <summary>
        /// 显示输入法光标
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        [DllImport("user32")]
        public static extern bool ShowCaret(IntPtr hwnd);

        /// <summary>
        /// 为系统插入标记创建一个新的形状，并且将插入标记的属主关系指定给特定的窗口。插入标记的形状。可以是线、块或位图
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="hBitmap"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        [DllImport("user32")]
        public static extern int CreateCaret(IntPtr hwnd, IntPtr hBitmap, int width, int height);
        #endregion

        #region Window Functions

        /// <summary>
        ///     Enables you to produce special effects when showing or hiding windows. There are four types of animation: roll,
        ///     slide, collapse or expand, and alpha-blended fade.
        /// </summary>
        /// <param name="hWnd">
        ///     [Type: HWND]
        ///     A handle to the window to animate.The calling thread must own this window.
        /// </param>
        /// <param name="dwTime">
        ///     [Type: DWORD]
        ///     The time it takes to play the animation, in milliseconds.Typically, an animation takes 200 milliseconds to play.
        /// </param>
        /// <param name="dwFlags">
        ///     [Type: DWORD]
        ///     The type of animation.This parameter can be one or more of the following values. Note that, by default, these flags
        ///     take effect when showing a window. To take effect when hiding a window, use AW_HIDE and a logical OR operator with
        ///     the appropriate flags.
        /// </param>
        /// <returns>
        ///     [Type: BOOL]
        ///     If the function succeeds, the return value is nonzero.
        ///     If the function fails, the return value is zero. The function will fail in the following situations:
        ///     If the window is already visible and you are trying to show the window.
        ///     If the window is already hidden and you are trying to hide the window.
        ///     If there is no direction specified for the slide or roll animation.
        ///     When trying to animate a child window with AW_BLEND.
        ///     If the thread does not own the window. Note that, in this case, AnimateWindow fails but GetLastError returns
        ///     ERROR_SUCCESS.
        ///     To get extended error information, call the GetLastError function.
        /// </returns>
        /// <remarks>
        ///     To show or hide a window without special effects, use ShowWindow.
        ///     When using slide or roll animation, you must specify the direction. It can be either AW_HOR_POSITIVE,
        ///     AW_HOR_NEGATIVE, AW_VER_POSITIVE, or AW_VER_NEGATIVE.
        ///     You can combine AW_HOR_POSITIVE or AW_HOR_NEGATIVE with AW_VER_POSITIVE or AW_VER_NEGATIVE to animate a window
        ///     diagonally.
        ///     The window procedures for the window and its child windows should handle any WM_PRINT or WM_PRINTCLIENT messages.
        ///     Dialog boxes, controls, and common controls already handle WM_PRINTCLIENT. The default window procedure already
        ///     handles WM_PRINT.
        ///     If a child window is displayed partially clipped, when it is animated it will have holes where it is clipped.
        ///     AnimateWindow supports RTL windows.
        ///     Avoid animating a window that has a drop shadow because it produces visually distracting, jerky animations.
        /// </remarks>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool AnimateWindow(IntPtr hWnd, int dwTime, AnimateWindowFlags dwFlags);

        /// <summary>
        /// 根据窗口类名或窗口标题名来获得窗口的句柄
        ///     Retrieves a handle to the top-level window whose class name and window name match the specified strings. This
        ///     function does not search child windows. This function does not perform a case-sensitive search.
        ///     To search child windows, beginning with a specified child window, use the FindWindowEx function.
        /// </summary>
        /// <param name="lpClassName">
        /// 窗口类名
        ///     [Type: LPCTSTR]
        ///     The class name or a class atom created by a previous call to the RegisterClass or RegisterClassEx function. The
        ///     atom must be in the low-order word of lpClassName; the high-order word must be zero.
        ///     If lpClassName points to a string, it specifies the window class name. The class name can be any name registered
        ///     with RegisterClass or RegisterClassEx, or any of the predefined control-class names.
        ///     If lpClassName is NULL, it finds any window whose title matches the lpWindowName parameter.
        /// </param>
        /// <param name="lpWindowName">
        /// 标题
        ///     [Type: LPCTSTR]
        ///     The window name (the window's title). If this parameter is NULL, all window names match.
        /// </param>
        /// <returns>
        ///     [Type: HWND]
        ///     If the function succeeds, the return value is a handle to the window that has the specified class name and window
        ///     name.
        ///     If the function fails, the return value is NULL. To get extended error information, call GetLastError.
        /// </returns>
        /// <remarks>
        ///     If the lpWindowName parameter is not NULL, FindWindow calls the GetWindowText function to retrieve the window name
        ///     for comparison. For a description of a potential problem that can arise, see the Remarks for GetWindowText.
        /// </remarks>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 显示窗口
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool UpdateWindow(IntPtr hwnd);

        [DllImport(LibraryName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            WindowExStyles dwExStyle,
            string lpClassName,
            string lpWindowName,
            WindowStyles dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hwndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        /// <summary>
        /// 获取的是以屏幕为坐标轴窗口坐标
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpRect"></param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

        /// <summary>
        /// 获取的是以其自身的最左上角的点为坐标原点
        /// Retrieves the coordinates of a window's client area. The client coordinates specify the upper-left and lower-right corners of the client area. Because client coordinates are relative to the upper-left corner of a window's client area, the coordinates of the upper-left corner are (0,0)
        /// </summary>
        /// <remarks>In conformance with conventions for the RECT structure, the bottom-right coordinates of the returned rectangle are exclusive. In other words, the pixel at (right, bottom) lies immediately outside the rectangle.</remarks>
        /// <param name="hWnd">A handle to the window whose client coordinates are to be retrieved</param>
        /// <param name="rect">A pointer to a RECT structure that receives the client coordinates. The left and top members are zero. The right and bottom members contain the width and height of the window</param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out Rectangle rect);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        /// <summary>
        /// 获取当前系统中被激活的窗口
        /// </summary>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetTopWindow();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetNextWindow(IntPtr hWnd, uint wCmd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint wCmd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool AllowSetForegroundWindow(uint dwProcessId);

        /// <summary>
        /// 将创建指定窗口的线程设置到前台
        /// </summary>
        /// <remarks>[SetForegroundWindow](http://www.cnblogs.com/dengpeng1004/p/5049280.html )</remarks>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// 窗口放在最前
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

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
        public static extern Int32 SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, Int32 x, Int32 y, Int32 cx, Int32 cy, Int32 wFlagslong);

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
        /// </param>
        /// <param name="x">以客户坐标指定窗口新位置的左边界。</param>
        /// <param name="y">以客户坐标指定窗口新位置的顶边界。</param>
        /// <param name="cx">以像素指定窗口的新的宽度。</param>
        /// <param name="cy">以像素指定窗口的新的高度。</param>
        /// <param name="wFlagslong">
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
        [Obsolete("请使用SetWindowPos重载方法，hWndInsertAfter的类型错误，在64位下长度错误")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern Int32 SetWindowPos(IntPtr hWnd, Int32 hWndInsertAfter, Int32 x, Int32 y, Int32 cx,
            Int32 cy,
            Int32 wFlagslong);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        /// <summary>
        /// 获得窗口所属线程 Id。
        /// </summary>
        /// <param name="hWnd">要查找进程和线程的窗口句柄。</param>
        /// <param name="processId">
        /// <list type="bullet">
        /// <item>如果希望同时获取进程 Id，请使用 <see cref="GetWindowThreadProcessId(IntPtr, out uint)"/> 重载。</item>
        /// <item>如果不希望获得进程 Id，请传入 <see cref="IntPtr.Zero"/>。</item>
        /// </list>
        /// </param>
        /// <returns>线程 Id。</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        /// <summary>
        /// 获得窗口所属线程 Id 和进程 Id。
        /// </summary>
        /// <param name="hWnd">要查找进程和线程的窗口句柄。</param>
        /// <param name="processId">
        /// 进程 Id。
        /// <list type="bullet">
        /// <item>如果不希望获得进程 Id，请使用弃元或使用 <see cref="GetWindowThreadProcessId(IntPtr, IntPtr)"/> 重载。</item>
        /// </list>
        /// </param>
        /// <returns>线程 Id。</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetWindowInfo(IntPtr hWnd, [In][Out] ref WindowInfo pwi);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetWindowPlacement(IntPtr hWnd,
            [In] ref WindowPlacement lpwndpl);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool RedrawWindow(IntPtr hWnd, [In] ref Rectangle lprcUpdate, IntPtr hrgnUpdate,
            RedrawWindowFlags flags);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool CloseWindow(IntPtr hwnd);

        #endregion

        #region Window Class Functions

        /// <summary>
        /// 为随后在调用Createwindow函数和CreatewindowEx函数中使用的窗口注册一个窗口类
        /// </summary>
        /// <param name="lpwcx"></param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern ushort RegisterClassEx([In] ref WindowClassEx lpwcx);

        /// <summary>
        /// 为随后在调用Createwindow函数和CreatewindowEx函数中使用的窗口注册一个窗口类
        /// </summary>
        /// <param name="lpwcx"></param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ushort RegisterClassEx([In] ref WindowClassExPtr lpwcx);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern ushort RegisterClassEx([In] ref WindowClassExBlittable lpwcx);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        /// <summary>
        /// 获得指定窗口的信息
        /// </summary>
        /// <param name="hWnd">指定窗口的句柄</param>
        /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
        /// <returns></returns>
        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex) => GetWindowLongPtr(hWnd, (int) nIndex);

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
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// 获得指定窗口的信息
        /// </summary>
        /// <param name="hWnd">指定窗口的句柄</param>
        /// <param name="nIndex">需要获得的信息的类型 请使用<see cref="GetWindowLongFields"/></param>
        /// <returns></returns>
        [Obsolete("请使用 GetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
        [DllImport(LibraryName, CharSet = CharSet.Unicode, EntryPoint = "GetWindowLongPtr")]
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
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, GetWindowLongFields nIndex, IntPtr dwNewLong) => SetWindowLongPtr(hWnd, (int) nIndex, dwNewLong);

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
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
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
        [DllImport(LibraryName, CharSet = CharSet.Unicode, EntryPoint = "SetWindowLongPtr")]
        [Obsolete("请使用 SetWindowLongPtr 解决 x86 和 x64 需要使用不同方法")]
        public static extern IntPtr SetWindowLongPtr_x64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool GetClassInfoEx(IntPtr hInstance, string lpClassName,
            out WindowClassExBlittable lpWndClass);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size > 4
                ? GetClassLongPtr_x64(hWnd, nIndex)
                : new IntPtr(unchecked((int) GetClassLong(hWnd, nIndex)));
        }

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        private static extern uint GetClassLong(IntPtr hWnd, int nIndex);

        [DllImport(LibraryName, CharSet = CharSet.Unicode, EntryPoint = "GetClassLongPtr")]
        private static extern IntPtr GetClassLongPtr_x64(IntPtr hWnd, int nIndex);


        public static IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size > 4
                ? SetClassLongPtr_x64(hWnd, nIndex, dwNewLong)
                : new IntPtr(unchecked((int) SetClassLong(hWnd, nIndex, dwNewLong.ToInt32())));
        }

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        private static extern uint SetClassLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport(LibraryName, CharSet = CharSet.Unicode, EntryPoint = "SetClassLongPtr")]
        private static extern IntPtr SetClassLongPtr_x64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        #endregion

        #region Window Procedure Functions

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr CallWindowProc(WindowProc lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam,
            IntPtr lParam);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam,
            IntPtr lParam);

        #endregion

        #region Message Functions

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool PeekMessage(out Message lpMsg, IntPtr hWnd, uint wMsgFilterMin,
            uint wMsgFilterMax, uint wRemoveMsg);

        /// <summary>
        ///     Dispatches a message to a window procedure. It is typically used to dispatch a message retrieved by the GetMessage
        ///     function.
        /// </summary>
        /// <param name="lpMsg">
        ///     [Type: const MSG*]
        ///     A pointer to a structure that contains the message.
        /// </param>
        /// <returns>
        ///     [Type: LRESULT]
        ///     The return value specifies the value returned by the window procedure.Although its meaning depends on the message
        ///     being dispatched, the return value generally is ignored.
        /// </returns>
        /// <remarks>
        ///     The MSG structure must contain valid message values. If the lpmsg parameter points to a WM_TIMER message and the
        ///     lParam parameter of the WM_TIMER message is not NULL, lParam points to a function that is called instead of the
        ///     window procedure.
        ///     Note that the application is responsible for retrieving and dispatching input messages to the dialog box.Most
        ///     applications use the main message loop for this. However, to permit the user to move to and to select controls by
        ///     using the keyboard, the application must call IsDialogMessage.For more information, see Dialog Box Keyboard
        ///     Interface.
        /// </remarks>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr DispatchMessage([In] ref Message lpMsg);

        /// <summary>
        ///     Translates virtual-key messages into character messages. The character messages are posted to the calling thread's
        ///     message queue, to be read the next time the thread calls the GetMessage or PeekMessage function.
        /// </summary>
        /// <param name="lpMsg">
        ///     [Type: const MSG*]
        ///     A pointer to an MSG structure that contains message information retrieved from the calling thread's message queue
        ///     by using the GetMessage or PeekMessage function.
        /// </param>
        /// <returns>
        ///     [Type: BOOL]
        ///     If the message is translated(that is, a character message is posted to the thread's message queue), the return
        ///     value is nonzero. If the message is WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP, the return value is
        ///     nonzero, regardless of the translation. If the message is not translated (that is, a character message is not
        ///     posted to the thread's message queue), the return value is zero.
        /// </returns>
        /// <remarks>
        ///     The TranslateMessage function does not modify the message pointed to by the lpMsg parameter.
        ///     WM_KEYDOWN and WM_KEYUP combinations produce a WM_CHAR or WM_DEADCHAR message.WM_SYSKEYDOWN and WM_SYSKEYUP
        ///     combinations produce a WM_SYSCHAR or WM_SYSDEADCHAR message.
        ///     TranslateMessage produces WM_CHAR messages only for keys that are mapped to ASCII characters by the keyboard
        ///     driver.
        ///     If applications process virtual-key messages for some other purpose, they should not call TranslateMessage.For
        ///     instance, an application should not call TranslateMessage if the TranslateAccelerator function returns a nonzero
        ///     value.Note that the application is responsible for retrieving and dispatching input messages to the dialog box.Most
        ///     applications use the main message loop for this. However, to permit the user to move to and to select controls by
        ///     using the keyboard, the application must call IsDialogMessage.For more information, see Dialog Box Keyboard
        ///     Interface.
        /// </remarks>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool TranslateMessage([In] ref Message lpMsg);

        /// <summary>
        ///     Retrieves a message from the calling thread's message queue. The function dispatches incoming sent messages until a
        ///     posted message is available for retrieval. Unlike GetMessage, the PeekMessage function does not wait for a message
        ///     to be posted before returning.
        /// </summary>
        /// <param name="lpMsg">
        ///     [Type: LPMSG]
        ///     A pointer to an MSG structure that receives message information from the thread's message queue.
        /// </param>
        /// <param name="hWnd">
        ///     [Type: HWND]
        ///     A handle to the window whose messages are to be retrieved.The window must belong to the current thread.
        ///     If hWnd is NULL, GetMessage retrieves messages for any window that belongs to the current thread, and any messages
        ///     on the current thread's message queue whose hwnd value is NULL (see the MSG structure). Therefore if hWnd is NULL,
        ///     both window messages and thread messages are processed.
        ///     If hWnd is -1, GetMessage retrieves only messages on the current thread's message queue whose hwnd value is NULL,
        ///     that is, thread messages as posted by PostMessage (when the hWnd parameter is NULL) or PostThreadMessage.
        /// </param>
        /// <param name="wMsgFilterMin">
        ///     [Type: UINT] The integer value of the lowest message value to be retrieved.Use WM_KEYFIRST (0x0100) to specify the
        ///     first keyboard message or WM_MOUSEFIRST(0x0200) to specify the first mouse message.
        ///     Use WM_INPUT here and in wMsgFilterMax to specify only the WM_INPUT messages.
        ///     If wMsgFilterMin and wMsgFilterMax are both zero, GetMessage returns all available messages (that is, no range
        ///     filtering is performed).
        /// </param>
        /// <param name="wMsgFilterMax">
        ///     [Type: UINT]
        ///     The integer value of the highest message value to be retrieved.Use WM_KEYLAST to specify the last keyboard message
        ///     or WM_MOUSELAST to specify the last mouse message.
        ///     Use WM_INPUT here and in wMsgFilterMin to specify only the WM_INPUT messages.
        ///     If wMsgFilterMin and wMsgFilterMax are both zero, GetMessage returns all available messages (that is, no range
        ///     filtering is performed).
        /// </param>
        /// <returns>
        ///     [Type: BOOL]
        ///     If the function retrieves a message other than WM_QUIT, the return value is nonzero.
        ///     If the function retrieves the WM_QUIT message, the return value is zero.
        ///     If there is an error, the return value is -1. For example, the function fails if hWnd is an invalid window handle
        ///     or lpMsg is an invalid pointer.To get extended error information, call GetLastError.
        /// </returns>
        /// <returns>
        ///     An application typically uses the return value to determine whether to end the main message loop and exit the
        ///     program.
        ///     The GetMessage function retrieves messages associated with the window identified by the hWnd parameter or any of
        ///     its children, as specified by the IsChild function, and within the range of message values given by the
        ///     wMsgFilterMin and wMsgFilterMax parameters. Note that an application can only use the low word in the wMsgFilterMin
        ///     and wMsgFilterMax parameters; the high word is reserved for the system.
        ///     Note that GetMessage always retrieves WM_QUIT messages, no matter which values you specify for wMsgFilterMin and
        ///     wMsgFilterMax.
        ///     During this call, the system delivers pending, nonqueued messages, that is, messages sent to windows owned by the
        ///     calling thread using the SendMessage, SendMessageCallback, SendMessageTimeout, or SendNotifyMessage function. Then
        ///     the first queued message that matches the specified filter is retrieved. The system may also process internal
        ///     events. If no filter is specified, messages are processed in the following order:
        ///     Sent messages
        ///     Posted messages
        ///     Input (hardware) messages and system internal events
        ///     Sent messages (again)
        ///     WM_PAINT messages
        ///     WM_TIMER messages
        ///     To retrieve input messages before posted messages, use the wMsgFilterMin and wMsgFilterMax parameters.
        ///     GetMessage does not remove WM_PAINT messages from the queue. The messages remain in the queue until processed.
        ///     If a top-level window stops responding to messages for more than several seconds, the system considers the window
        ///     to be not responding and replaces it with a ghost window that has the same z-order, location, size, and visual
        ///     attributes. This allows the user to move it, resize it, or even close the application. However, these are the only
        ///     actions available because the application is actually not responding. When in the debugger mode, the system does
        ///     not generate a ghost window.
        /// </returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int GetMessage(out Message lpMsg, IntPtr hWnd, uint wMsgFilterMin,
            uint wMsgFilterMax);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetMessageExtraInfo();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetMessageExtraInfo(IntPtr lParam);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool WaitMessage();

        /// <summary>
        /// 消息发送
        /// </summary>
        /// <param name="hWnd">信息发往的窗口的句柄</param>
        /// <param name="msg">消息</param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 消息发送
        /// </summary>
        /// <param name="hWnd">信息发往的窗口的句柄</param>
        /// <param name="msg">消息</param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);


        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool SendNotifyMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 该函数将一个消息放入（寄送）到与指定窗口创建的线程相联系消息队列里，不等待线程处理消息就返回，是异步消息模式
        /// 与<see cref="SendMessage(System.IntPtr,int,int,int)"/>相对，这个函数是不堵塞的
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ReplyMessage(IntPtr lResult);

        /// <summary>
        /// Defines a new window message that is guaranteed to be unique throughout the system.
        /// The message value can be used when sending or posting messages.
        /// </summary>
        /// <param name="msg">The message to be registered.</param>
        /// <returns>
        /// If the message is successfully registered, the return value is a message identifier in the range 0xC000 through 0xFFFF.
        /// If the function fails, the return value is zero.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport(LibraryName, CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string msg);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetInputState();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint GetMessagePos();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint GetMessageTime();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool InSendMessage();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint GetQueueStatus(QueueStatusFlags flags);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Clipboard Functions

        /// <summary>
        /// Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
        /// </summary>
        /// <param name="hWndNewOwner">A handle to the window to be associated with the open clipboard. If this parameter is NULL, the open clipboard is associated with the current task.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        /// <summary>
        /// Closes the clipboard.
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool CloseClipboard();

        /// <summary>
        /// Retrieves data from the clipboard in a specified format. The clipboard must have been opened previously.
        /// </summary>
        /// <param name="uFormat">A clipboard format.</param>
        /// <returns>If the function succeeds, the return value is the handle to a clipboard object in the specified format.</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetClipboardData(uint uFormat);

        /// <summary>
        /// Places data on the clipboard in a specified clipboard format. The window must be the current clipboard owner, and the application must have called the OpenClipboard function
        /// </summary>
        /// <param name="uFormat">The clipboard format</param>
        /// <param name="handle">A handle to the data in the specified format</param>
        /// <returns>If the function succeeds, the return value is the handle to the data.</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr handle);

        /// <summary>
        /// Empties the clipboard and frees handles to data in the clipboard. The function then assigns ownership of the clipboard to the window that currently has the clipboard open.
        /// </summary>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool EmptyClipboard();

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetPriorityClipboardFormat(IntPtr paFormatPriorityList, int cFormats);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint EnumClipboardFormats(uint format);

        #endregion
    }
}
