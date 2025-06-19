using System.Drawing;
using System.Runtime.InteropServices;

namespace DotNetCampus.Installer.Lib.Native;

partial class Win32
{
    public static class Gdi32
    {
        public const string LibraryName = "gdi32";

        /// <summary>
        /// 该函数检索预定义的备用笔、刷子、字体或者调色板的句柄
        /// </summary>
        /// <param name="fnObject">
        /// 指定对象的类型，该参数可取如下值之一
        /// <see cref="StockObject"/> 或使用 <see cref="StockBrush"/> 、 <see cref="StockPen"/> 、 <see cref="StockFont"/> 的一个
        /// </param>
        /// <remarks>
        /// 仅在CS_HREDRAW和CS_UREDRAW风格的窗口中使用DKGRAY_BRUSH、GRAY_BRUSH和LTGRAY_BRUSH对象。
        ///
        ///　　如果在其他风格的窗口中使灰色画笔，可能导致在窗口移动或改变大小之后出现画笔模式错位现象，原始储存画笔不能被调整。
        ///
        ///　　HOLLOW_BRUSH和NULL_BRUSH储存对象相等。
        ///
        ///　　由DEFAULT_GUI_FONT储存对象使用的字体将改变。当想使用菜单、对话框和其他用户界面对象使用的字体时请使用此储存对象。
        ///
        ///　　不必要通过调用DeleteObject函数来删除储存对象。
        ///
        ///　　Windows 98、Windows NT 5.0和以后版本：DC_BRUSH和DC_PEN都能与其他储存对象如BLACK_BRUSH和BLACK_PEN相互交换关于检索当前钢 ///笔和画笔颜色的信息，请参见GetDCBrushColor和GetDCPencolor，带DC BRUSH或DC PEN参数的Getstockobject函数可以与 ///SetDCPenColor和SetDCBrushColor函数相互交换使用。
        ///
        ///　　Windows CE：Windows CE不支持fnObject参数的如下值：
        ///
        ///　　ANSI_FIXED_FONT、ANSI_VAR_FONT、OEM_FIXED_FONT、SYSTEM_FIXED_FONT
        ///
        ///　　Windows CE1.0版本不支持fnObject的DEFAULT_PALETTE值。
        /// </remarks>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr GetStockObject(int fnObject);

        /// <summary>
        /// 该函数检索指定坐标点的像素的RGB颜色值
        /// </summary>
        /// <param name="hdc">设备环境句柄</param>
        /// <param name="nXPos">指定要检查的像素点的逻辑X轴坐标</param>
        /// <param name="nYPos">指定要检查的像素点的逻辑Y轴坐标</param>
        /// <returns>该象像点的RGB值。如果指定的像素点在当前剪辑区之外；那么返回值是CLR_INVALID 0xFFFFFFFF</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        /// <summary>
        /// 删除 GDI 对象。
        /// </summary>
        /// <param name="hObject">要删除的 GDI 对象指针。</param>
        /// <returns>如果操作成功，则返回 true，否则返回 false。</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// 将指定坐标处的像素设为指定的颜色
        /// </summary>
        /// <param name="hdc">设备环境句柄</param>
        /// <param name="x">指定要设置的点的X轴坐标，按逻辑单位表示坐标</param>
        /// <param name="y">指定要设置的点的Y轴坐标，按逻辑单位表示坐标</param>
        /// <param name="crColor">用来绘制该点的颜色</param>
        /// <remarks>如果像素点坐标位于当前剪辑区之外，那么该函数执行失败</remarks>
        /// <returns>
        /// 如果函数执行成功，那么返回值就是函数设置像素的RGB颜色值。这个值可能与crColor指定的颜色有所不同，之所以有时发生这种情况是因为没有找到对指定颜色进行真正匹配造成的。
        /// 如果函数失败，那么返回值是 ERROR_INVALID_PARAMETER -1。
        /// </returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern uint SetPixel(IntPtr hdc, int x, int y, uint crColor);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetPixelFormat(IntPtr hdc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int SetBkMode(IntPtr hdc, int iBkMode);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, IntPtr pbmi,
            DibBmiColorUsageFlag iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        /// <summary>
        /// 创建一个与指定设备兼容的内存设备上下文环境
        /// </summary>
        /// <param name="hdc">现有设备上下文环境</param>
        /// <remarks>[CreateCompatibleDC](http://blog.csdn.net/shellching/article/details/18405185 )</remarks>
        /// <returns></returns>
        [DllImport(LibraryName, EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern int GetObject(IntPtr hgdiobj, int cbBuffer, IntPtr lpvObject);

        /// <summary>
        /// 创建与指定的设备环境相关的设备兼容的位图
        /// </summary>
        /// <param name="hdc">环境句柄</param>
        /// <param name="nWidth">位图的宽度</param>
        /// <param name="nHeight"></param>
        /// <remarks>由CreateCompatibleBitmap函数创建的位图的颜色格式与由参数hdc标识的设备的颜色格式匹配。该位图可以选入任意一个与原设备兼容的内存设备环境中。由于内存设备环境允许彩色和单色两种位图。因此当指定的设备环境是内存设备环境时，由CreateCompatibleBitmap函数返回的位图格式不一定相同。然而为非内存设备环境创建的兼容位图通常拥有相同的颜色格式，并且使用与指定的设备环境一样的色彩调色板</remarks>
        /// <returns>如果函数执行成功，那么返回值是位图的句柄</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel,
            IntPtr lpvBits);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateBitmapIndirect([In] ref Bitmap lpbm);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateDIBitmap(IntPtr hdc, [In] ref BitmapInfoHeader
        lpbmih, uint fdwInit, byte[] lpbInit, IntPtr lpbmi,
            DibBmiColorUsageFlag fuUsage);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateDIBitmap(IntPtr hdc, [In] ref BitmapInfoHeader
                lpbmih, uint fdwInit, IntPtr lpbInit, IntPtr lpbmi,
            DibBmiColorUsageFlag fuUsage);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int SetDIBitsToDevice(IntPtr hdc, int xDest, int yDest, uint
                dwWidth, uint dwHeight, int xSrc, int ySrc, uint uStartScan, uint cScanLines,
            byte[] lpvBits, IntPtr lpbmi, DibBmiColorUsageFlag fuColorUse);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int SetDIBitsToDevice(IntPtr hdc, int xDest, int yDest, uint
                dwWidth, uint dwHeight, int xSrc, int ySrc, uint uStartScan, uint cScanLines,
            IntPtr lpvBits, IntPtr lpbmi, DibBmiColorUsageFlag fuColorUse);

        /// <summary>
        /// 该函数删除指定的设备上下文环境
        /// </summary>
        /// 如果一个设备上下文环境的句柄是通过调用GetDC函数得到的，那么应用程序不能删除该设备上下文环境，它应该调用ReleaseDC函数来释放该设备上下文环境
        /// <param name="hdc"></param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool DeleteDC([In] IntPtr hdc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int SetBitmapBits(IntPtr hbmp, uint cBytes, [In] byte[] lpBits);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetBitmapBits(IntPtr hbmp, int cbBuffer, [Out] byte[] lpvBits);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int SetBitmapBits(IntPtr hbmp, uint cBytes, IntPtr lpBits);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetBitmapBits(IntPtr hbmp, int cbBuffer, IntPtr lpvBits);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateRectRgnIndirect([In] ref Rectangle lprc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateEllipticRgn(int nLeftRect, int nTopRect,
            int nRightRect, int nBottomRect);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateEllipticRgnIndirect([In] ref Rectangle lprc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2,
            int cx, int cy);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1,
            IntPtr hrgnSrc2, RegionModeFlags fnCombineMode);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool OffsetViewportOrgEx(IntPtr hdc, int nXOffset, int nYOffset, out Point lpPoint);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SetViewportOrgEx(IntPtr hdc, int x, int y, out Point lpPoint);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int SetMapMode(IntPtr hdc, int fnMapMode);


        /// <summary>
        ///     The GetClipBox function retrieves the dimensions of the tightest bounding rectangle that can be drawn around the
        ///     current visible area on the device. The visible area is defined by the current clipping region or clip path, as
        ///     well as any overlapping windows.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="lprc">A pointer to a RECT structure that is to receive the rectangle dimensions, in logical units.</param>
        /// <returns>If the function succeeds, the return value specifies the clipping box's complexity.</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType GetClipBox(IntPtr hdc, out Rectangle lprc);

        /// <summary>
        ///     The GetClipRgn function retrieves a handle identifying the current application-defined clipping region for the
        ///     specified device context.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="hrgn">
        ///     A handle to an existing region before the function is called. After the function returns, this
        ///     parameter is a handle to a copy of the current clipping region.
        /// </param>
        /// <returns>
        ///     f the function succeeds and there is no clipping region for the given device context, the return value is
        ///     zero. If the function succeeds and there is a clipping region for the given device context, the return value is 1.
        ///     If an error occurs, the return value is -1.
        /// </returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetClipRgn(IntPtr hdc, IntPtr hrgn);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType SelectClipRgn(IntPtr hdc, IntPtr hrgn);

        /// <summary>
        ///     The ExtSelectClipRgn function combines the specified region with the current clipping region using the specified
        ///     mode.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="hrgn">
        ///     A handle to the region to be selected. This handle must not be NULL unless the RGN_COPY mode is
        ///     specified.
        /// </param>
        /// <param name="fnMode">The operation to be performed</param>
        /// <returns>The return value specifies the new clipping region's complexity.</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType ExtSelectClipRgn(IntPtr hdc, IntPtr hrgn, RegionModeFlags fnMode);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType IntersectClipRect(IntPtr hdc,
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect
        );

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType ExcludeClipRect(IntPtr hdc,
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect
        );

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType OffsetClipRgn(IntPtr hdc,
            int nXOffset,
            int nYOffset
        );

        /// <summary>
        ///     The GetMetaRgn function retrieves the current metaregion for the specified device context.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="hrgn">
        ///     A handle to an existing region before the function is called. After the function returns, this
        ///     parameter is a handle to a copy of the current metaregion.
        /// </param>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool GetMetaRgn(IntPtr hdc, IntPtr hrgn);

        /// <summary>
        ///     The SetMetaRgn function intersects the current clipping region for the specified device context with the current
        ///     metaregion and saves the combined region as the new metaregion for the specified device context. The clipping
        ///     region is reset to a null region.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <returns>The return value specifies the new clipping region's complexity.</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType SetMetaRgn(IntPtr hdc);

        /// <summary>
        ///     The LPtoDP function converts logical coordinates into device coordinates. The conversion depends on the mapping
        ///     mode of the device context, the settings of the origins and extents for the window and viewport, and the world
        ///     transformation.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="lpPoints">
        ///     A pointer to an array of POINT structures. The x-coordinates and y-coordinates contained in each
        ///     of the POINT structures will be transformed.
        /// </param>
        /// <param name="nCount">The number of points in the array.</param>
        /// <remarks>
        ///     The LPtoDP function fails if the logical coordinates exceed 32 bits, or if the converted device coordinates exceed
        ///     27 bits. In the case of such an overflow, the results for all the points are undefined.
        ///     LPtoDP calculates complex floating-point arithmetic, and it has a caching system for efficiency.Therefore, the
        ///     conversion result of an initial call to LPtoDP might not exactly match the conversion result of a later call to
        ///     LPtoDP.We recommend not to write code that relies on the exact match of the conversion results from multiple calls
        ///     to LPtoDP even if the parameters that are passed to each call are identical.
        /// </remarks>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool LPtoDP(IntPtr hdc, [In][Out] ref Point lpPoints, int nCount);

        /// <summary>
        ///     The DPtoLP function converts device coordinates into logical coordinates. The conversion depends on the mapping
        ///     mode of the device context, the settings of the origins and extents for the window and viewport, and the world
        ///     transformation.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="lpPoints">
        ///     A pointer to an array of POINT structures. The x-coordinates and y-coordinates contained in each
        ///     of the POINT structures will be transformed.
        /// </param>
        /// <param name="nCount">The number of points in the array.</param>
        /// <remarks>
        ///     The DPtoLP function fails if the device coordinates exceed 27 bits, or if the converted logical coordinates exceed
        ///     32 bits. In the case of such an overflow, the results for all the points are undefined.
        /// </remarks>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool DPtoLP(IntPtr hdc, [In][Out] ref Point lpPoints, int nCount);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool SelectClipPath(IntPtr hdc, RegionModeFlags iMode);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool FillRgn(IntPtr hdc, IntPtr hrgn, IntPtr hbr);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreateSolidBrush(uint crColor);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool FrameRgn(IntPtr hdc, IntPtr hrgn, IntPtr hbr, int nWidth,
            int nHeight);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool PaintRgn(IntPtr hdc, IntPtr hrgn);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool InvertRgn(IntPtr hdc, IntPtr hrgn);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool LineTo(IntPtr hdc, int nXEnd, int nYEnd);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool MoveToEx(IntPtr hdc, int x, int y, out Point lpPoint);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool RoundRect(IntPtr hdc, int nLeftRect, int nTopRect,
            int nRightRect, int nBottomRect, int nWidth, int nHeight);

        /// <summary>
        /// 选择一对象到指定的设备上下文环境中，该新对象替换先前的相同类型的对象
        /// </summary>
        /// <param name="hdc">设备上下文环境的句柄</param>
        /// <param name="hgdiobj">选择的对象</param>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern bool TextOut(IntPtr hdc, int nXStart, int nYStart,
            string lpString, int cbString);

        /// <summary>
        /// 该函数对指定的源设备环境区域中的像素进行位块转换，以传送到目标设备环境
        /// </summary>
        /// <param name="hdcDest">指向目标设备环境的句柄</param>
        /// <param name="xDest">指定目标矩形区域左上角的X轴和Y轴逻辑坐标</param>
        /// <param name="yDest"></param>
        /// <param name="wDest">指定源和目标矩形区域的逻辑宽度和逻辑高度</param>
        /// <param name="hDest"></param>
        /// <param name="hdcSource">指向源设备环境</param>
        /// <param name="xSrc">指定源矩形区域左上角的X轴和Y轴逻辑坐标</param>
        /// <param name="ySrc"></param>
        /// <param name="rop">指定光栅操作</param>
        /// <remarks>rop 请看 https://msdn.microsoft.com/en-us/library/aa452783.aspx</remarks>
        /// <returns></returns>
        [DllImport(LibraryName)]
        public static extern bool BitBlt(IntPtr hdcDest, Int32 xDest, Int32 yDest, Int32 wDest, Int32 hDest,
            IntPtr hdcSource, Int32 xSrc, Int32 ySrc, CopyPixelOperation rop);

        /// <summary>
        /// 该函数对指定的源设备环境区域中的像素进行位块转换，以传送到目标设备环境
        /// </summary>
        /// <param name="hdc">指向目标设备环境的句柄</param>
        /// <param name="nXDest">指定目标矩形区域左上角的X轴和Y轴逻辑坐标</param>
        /// <param name="nYDest"></param>
        /// <param name="nWidth">指定源和目标矩形区域的逻辑宽度和逻辑高度</param>
        /// <param name="nHeight"></param>
        /// <param name="hdcSrc">指向源设备环境</param>
        /// <param name="nXSrc">指定源矩形区域左上角的X轴和Y轴逻辑坐标</param>
        /// <param name="nYSrc"></param>
        /// <param name="dwRop">指定光栅操作</param>
        /// <remarks>rop 请看 https://msdn.microsoft.com/en-us/library/aa452783.aspx</remarks>
        /// <returns></returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc,
            int nXSrc, int nYSrc, BitBltFlags dwRop);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest,
            int nWidthDest, int nHeightDest,
            IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
            BitBltFlags dwRop);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int SaveDC(IntPtr hdc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern bool RestoreDC(IntPtr hdc, int nSavedDc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr PathToRegion(IntPtr hdc);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreatePolygonRgn(
            Point[] lppt,
            int cPoints,
            int fnPolyFillMode);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern IntPtr CreatePolyPolygonRgn(Point[] lppt,
            int[] lpPolyCounts,
            int nCount, int fnPolyFillMode);

        /// <summary>
        /// 访问使用设备描述表的设备数据，应用程序指定相应设备描述表的句柄和说明该函数访问数据类型的索引来访问这些数据。
        /// </summary>
        /// <param name="hdc"></param>
        /// <param name="nIndex"></param>
        /// <returns>设备相关信息的尺寸大小</returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        /// <summary>
        /// 访问使用设备描述表的设备数据，应用程序指定相应设备描述表的句柄和说明该函数访问数据类型的索引来访问这些数据。
        /// </summary>
        /// <param name="hdc"></param>
        /// <param name="nIndex"></param>
        /// <returns>设备相关信息的尺寸大小</returns>
        [DllImport(LibraryName, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, DeviceCapIndexes nIndex);

        [DllImport(LibraryName, ExactSpelling = true, EntryPoint = "GdiAlphaBlend")]
        public static extern bool AlphaBlend(IntPtr hdcDest, int nXOriginDest, int nYOriginDest,
            int nWidthDest, int nHeightDest,
            IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
            BlendFunction blendFunction);

        /// <summary>
        ///     The GetRandomRgn function copies the system clipping region of a specified device context to a specific region.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="hrgn">
        ///     A handle to a region. Before the function is called, this identifies an existing region. After the
        ///     function returns, this identifies a copy of the current system region. The old region identified by hrgn is
        ///     overwritten.
        /// </param>
        /// <param name="iNum">This parameter must be SYSRGN.</param>
        /// <returns>
        ///     If the function succeeds, the return value is 1. If the function fails, the return value is -1. If the region
        ///     to be retrieved is NULL, the return value is 0. If the function fails or the region to be retrieved is NULL, hrgn
        ///     is not initialized.
        /// </returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern int GetRandomRgn(IntPtr hdc, IntPtr hrgn, DcRegionType iNum);

        /// <summary>
        ///     The OffsetRgn function moves a region by the specified offsets.
        /// </summary>
        /// <param name="hrgn">Handle to the region to be moved.</param>
        /// <param name="nXOffset">Specifies the number of logical units to move left or right.</param>
        /// <param name="nYOffset">Specifies the number of logical units to move up or down.</param>
        /// <returns>The return value specifies the new region's complexity. </returns>
        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType OffsetRgn(IntPtr hrgn, int nXOffset, int nYOffset);

        [DllImport(LibraryName, ExactSpelling = true)]
        public static extern RegionType GetRgnBox(IntPtr hWnd, out Rectangle lprc);

        #region 屏幕

        [DllImport(LibraryName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport(LibraryName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public BitmapCompressionMode CompressionMode;
        public uint SizeImage;
        public int XPxPerMeter;
        public int YPxPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    public struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public RgbQuad[] Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RgbQuad
    {
        public byte Blue;
        public byte Green;
        public byte Red;
        private byte Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Bitmap
    {
        /// <summary>
        ///     The bitmap type. This member must be zero.
        /// </summary>
        public int Type;

        /// <summary>
        ///     The width, in pixels, of the bitmap. The width must be greater than zero.
        /// </summary>
        public int Width;

        /// <summary>
        ///     The height, in pixels, of the bitmap. The height must be greater than zero.
        /// </summary>
        public int Height;

        /// <summary>
        ///     The number of bytes in each scan line. This value must be divisible by 2, because the system assumes that the bit
        ///     values of a bitmap form an array that is word aligned.
        /// </summary>
        public int WidthBytes;

        /// <summary>
        ///     The count of color planes.
        /// </summary>
        public ushort Planes;

        /// <summary>
        ///     The number of bits required to indicate the color of a pixel.
        /// </summary>
        public ushort BitsPerPixel;

        /// <summary>
        ///     A pointer to the location of the bit values for the bitmap. The bmBits member must be a pointer to an array of
        ///     character (1-byte) values.
        /// </summary>
        public IntPtr Bits;
    }

    public enum BitmapInfoColorFormat
    {
        DIB_RGB_COLORS = 0 /* color table in RGBs */,
        DIB_PAL_COLORS = 1 /* color table in palette indices */
    }

    public enum BitmapCompressionMode
    {
        BI_RGB = 0,
        BI_RLE8 = 1,
        BI_RLE4 = 2,
        BI_BITFIELDS = 3,
        BI_JPEG = 4,
        BI_PNG = 5
    }

    public enum StockObject
    {
        /// <summary>
        ///     Black brush.
        /// </summary>
        BLACK_BRUSH = 4,

        /// <summary>
        ///     Dark gray brush.
        /// </summary>
        DKGRAY_BRUSH = 3,

        /// <summary>
        ///     Solid color brush. The default color is white. The color can be changed by using the SetDCBrushColor function. For
        ///     more information, see the Remarks section.
        ///   在Windows98,Windows NT 5.0和以后版本中为纯颜色画笔，缺省色为白色，可以用SetDCBrushColor函数改变颜色
        /// </summary>
        DC_BRUSH = 18,

        /// <summary>
        ///     Gray brush.
        /// </summary>
        GRAY_BRUSH = 2,

        /// <summary>
        ///     Hollow brush (equivalent to NULL_BRUSH).
        /// </summary>
        HOLLOW_BRUSH = NULL_BRUSH,

        /// <summary>
        ///     Light gray brush.
        /// </summary>
        LTGRAY_BRUSH = 1,

        /// <summary>
        ///     Null brush (equivalent to HOLLOW_BRUSH).
        /// </summary>
        NULL_BRUSH = 5,

        /// <summary>
        ///     White brush.
        /// </summary>
        WHITE_BRUSH = 0,

        /// <summary>
        ///     Black pen.
        /// </summary>
        BLACK_PEN = 7,

        /// <summary>
        ///     Solid pen color. The default color is white. The color can be changed by using the SetDCPenColor function. For more
        ///     information, see the Remarks section.
        /// </summary>
        DC_PEN = 19,

        /// <summary>
        ///     Null pen. The null pen draws nothing.
        /// </summary>
        NULL_PEN = 8,

        /// <summary>
        ///     White pen.
        /// </summary>
        WHITE_PEN = 6,

        /// <summary>
        ///     Windows fixed-pitch (monospace) system font.
        /// </summary>
        ANSI_FIXED_FONT = 11,

        /// <summary>
        ///     Windows variable-pitch (proportional space) system font.
        /// </summary>
        ANSI_VAR_FONT = 12,

        /// <summary>
        ///     Device-dependent font.
        /// </summary>
        DEVICE_DEFAULT_FONT = 14,

        /// <summary>
        ///     Default font for user interface objects such as menus and dialog boxes. It is not recommended that you use
        ///     DEFAULT_GUI_FONT or SYSTEM_FONT to obtain the font used by dialogs and windows; for more information, see the
        ///     remarks section.
        ///     The default font is Tahoma.
        /// </summary>
        DEFAULT_GUI_FONT = 17,

        /// <summary>
        ///     Original equipment manufacturer (OEM) dependent fixed-pitch (monospace) font.
        /// </summary>
        OEM_FIXED_FONT = 10,

        /// <summary>
        ///     System font. By default, the system uses the system font to draw menus, dialog box controls, and text. It is not
        ///     recommended that you use DEFAULT_GUI_FONT or SYSTEM_FONT to obtain the font used by dialogs and windows; for more
        ///     information, see the remarks section.
        ///     The default system font is Tahoma.
        /// </summary>
        SYSTEM_FONT = 13,

        /// <summary>
        ///     Fixed-pitch (monospace) system font. This stock object is provided only for compatibility with 16-bit Windows
        ///     versions earlier than 3.0.
        /// </summary>
        SYSTEM_FIXED_FONT = 16,

        /// <summary>
        ///     Default palette. This palette consists of the static colors in the system palette.
        /// </summary>
        DEFAULT_PALETTE = 15
    }

    public enum DibBmiColorUsageFlag
    {
        DIB_RGB_COLORS = 0 /* color table in RGBs */,
        DIB_PAL_COLORS = 1 /* color table in palette indices */
    }

    public enum StockBrush
    {
        /// <summary>
        /// 白色画笔
        /// </summary>
        WHITE_BRUSH = 0,

        /// <summary>
        /// 亮灰色画笔
        /// </summary>
        LTGRAY_BRUSH = 1,

        /// <summary>
        /// 灰色画笔
        /// </summary>
        GRAY_BRUSH = 2,

        /// <summary>
        /// 暗灰色画笔
        /// </summary>
        DKGRAY_BRUSH = 3,

        /// <summary>
        /// 黑色画笔
        /// </summary>
        BLACK_BRUSH = 4,

        /// <summary>
        /// 空画笔（相当于HOLLOW_BRUSH）
        /// </summary>
        NULL_BRUSH = 5,

        /// <summary>
        /// 空画笔（相当于NULL_BRUSH）
        /// </summary>
        HOLLOW_BRUSH = NULL_BRUSH
    }

    public enum StockPen
    {
        WHITE_PEN = 6,
        BLACK_PEN = 7,
        NULL_PEN = 8
    }

    public enum StockFont
    {
        /// <summary>
        /// 原始设备制造商（OEM）相关固定间距（等宽）字体
        /// </summary>
        OEM_FIXED_FONT = 10,

        /// <summary>
        /// 在Windows中为固定间距（等宽）系统字体
        /// </summary>
        ANSI_FIXED_FONT = 11,

        /// <summary>
        /// 在Windows中为变间距（比例间距）系统字体
        /// </summary>
        ANSI_VAR_FONT = 12,

        /// <summary>
        /// 系统字体，在缺省情况下，系统使用系统字体绘制菜单，对话框控制和文本
        /// </summary>
        SYSTEM_FONT = 13,

        /// <summary>
        /// 在WindowsNT中为设备相关字体
        /// </summary>
        DEVICE_DEFAULT_FONT = 14,

        /// <summary>
        /// 固定间距（等宽）系统字体，该对象仅提供给兼容16位Windows版本
        /// </summary>
        SYSTEM_FIXED_FONT = 16,

        /// <summary>
        /// 用户界面对象缺省字体，如菜单和对话框
        /// </summary>
        DEFAULT_GUI_FONT = 17
    }

    public enum DcRegionType
    {
        Clip = 1,
        Meta = 2,
        IntersectedMetaClip = 3,
        System = 4
    }

    [Flags]
    public enum BitBltFlags
    {
        /// <summary>dest = source</summary>
        SRCCOPY = 0x00CC0020,

        /// <summary>dest = source OR dest</summary>
        SRCPAINT = 0x00EE0086,

        /// <summary>dest = source AND dest</summary>
        SRCAND = 0x008800C6,

        /// <summary>dest = source XOR dest</summary>
        SRCINVERT = 0x00660046,

        /// <summary>dest = source AND (NOT dest)</summary>
        SRCERASE = 0x00440328,

        /// <summary>dest = (NOT source)</summary>
        NOTSRCCOPY = 0x00330008,

        /// <summary>dest = (NOT src) AND (NOT dest)</summary>
        NOTSRCERASE = 0x001100A6,

        /// <summary>dest = (source AND pattern)</summary>
        MERGECOPY = 0x00C000CA,

        /// <summary>dest = (NOT source) OR dest</summary>
        MERGEPAINT = 0x00BB0226,

        /// <summary>dest = pattern</summary>
        PATCOPY = 0x00F00021,

        /// <summary>dest = DPSnoo</summary>
        PATPAINT = 0x00FB0A09,

        /// <summary>dest = pattern XOR dest</summary>
        PATINVERT = 0x005A0049,

        /// <summary>dest = (NOT dest)</summary>
        DSTINVERT = 0x00550009,

        /// <summary>dest = BLACK</summary>
        BLACKNESS = 0x00000042,

        /// <summary>dest = WHITE</summary>
        WHITENESS = 0x00FF0062,

        /// <summary>
        ///     Capture window as seen on screen.  This includes layered windows
        ///     such as WPF windows with AllowsTransparency="true"
        /// </summary>
        CAPTUREBLT = 0x40000000,

        /// <summary>
        ///     Prevents the bitmap from being mirrored.
        /// </summary>
        NOMIRRORBITMAP = unchecked((int) 0x80000000)
    }

    /// <summary>
    /// <para>
    /// <see cref="GetDeviceCaps"/> Indexes
    /// </para>
    /// <para>
    /// From: https://docs.microsoft.com/zh-cn/windows/win32/api/wingdi/nf-wingdi-getdevicecaps
    /// </para>
    /// </summary>
    public enum DeviceCapIndexes
    {
        /// <summary>
        /// The device driver version.
        /// </summary>
        DRIVERVERSION = 0,

        /// <summary>
        /// Device technology.
        /// </summary>
        TECHNOLOGY = 2,

        /// <summary>
        /// Width, in millimeters, of the physical screen.
        /// </summary>
        HORZSIZE = 4,

        /// <summary>
        /// Height, in millimeters, of the physical screen.
        /// </summary>
        VERTSIZE = 6,

        /// <summary>
        /// Width, in pixels, of the screen; or for printers, the width, in pixels, of the printable area of the page.
        /// </summary>
        HORZRES = 8,

        /// <summary>
        /// Height, in raster lines, of the screen; or for printers, the height, in pixels, of the printable area of the page.
        /// </summary>
        VERTRES = 10,

        /// <summary>
        /// Number of pixels per logical inch along the screen width. In a system with multiple display monitors, this value is the same for all monitors.
        /// </summary>
        LOGPIXELSX = 88,

        /// <summary>
        /// Number of pixels per logical inch along the screen height. In a system with multiple display monitors, this value is the same for all monitors.
        /// </summary>
        LOGPIXELSY = 90,

        /// <summary>
        /// Number of adjacent color bits for each pixel.
        /// </summary>
        BITSPIXEL = 12,

        /// <summary>
        /// Number of color planes.
        /// </summary>
        PLANES = 14,

        /// <summary>
        /// Number of device-specific brushes.
        /// </summary>
        NUMBRUSHES = 16,

        /// <summary>
        /// Number of device-specific pens.
        /// </summary>
        NUMPENS = 18,

        /// <summary>
        /// Number of device-specific fonts.
        /// </summary>
        NUMFONTS = 22,

        /// <summary>
        /// Number of entries in the device's color table, if the device has a color depth of no more than 8 bits per pixel.
        /// For devices with greater color depths, 1 is returned.
        /// </summary>
        NUMCOLORS = 24,

        /// <summary>
        /// Relative width of a device pixel used for line drawing.
        /// </summary>
        ASPECTX = 40,

        /// <summary>
        /// Relative height of a device pixel used for line drawing.
        /// </summary>
        ASPECTY = 42,

        /// <summary>
        /// Diagonal width of the device pixel used for line drawing.
        /// </summary>
        ASPECTXY = 44,

        /// <summary>
        /// Reserved.
        /// </summary>
        PDEVICESIZE = 26,

        /// <summary>
        /// Flag that indicates the clipping capabilities of the device. If the device can clip to a rectangle, it is 1. Otherwise, it is 0.
        /// </summary>
        CLIPCAPS = 36,

        /// <summary>
        /// Number of entries in the system palette.
        /// This index is valid only if the device driver sets the RC_PALETTE bit in the RASTERCAPS index and is available only if the driver is compatible with 16-bit Windows.
        /// </summary>
        SIZEPALETTE = 104,

        /// <summary>
        /// Number of reserved entries in the system palette.
        /// This index is valid only if the device driver sets the RC_PALETTE bit in the RASTERCAPS index and is available only if the driver is compatible with 16-bit Windows.
        /// </summary>
        NUMRESERVED = 106,

        /// <summary>
        /// Actual color resolution of the device, in bits per pixel.
        /// This index is valid only if the device driver sets the RC_PALETTE bit in the RASTERCAPS index and is available only if the driver is compatible with 16-bit Windows.
        /// </summary>
        COLORRES = 108,

        /// <summary>
        /// For printing devices: the width of the physical page, in device units. 
        /// For example, a printer set to print at 600 dpi on 8.5-x11-inch paper has a physical width value of 5100 device units. 
        /// Note that the physical page is almost always greater than the printable area of the page, and never smaller.
        /// </summary>
        PHYSICALWIDTH = 110,

        /// <summary>
        /// For printing devices: the height of the physical page, in device units.
        /// For example, a printer set to print at 600 dpi on 8.5-by-11-inch paper has a physical height value of 6600 device units.
        /// Note that the physical page is almost always greater than the printable area of the page, and never smaller.
        /// </summary>
        PHYSICALHEIGHT = 111,

        /// <summary>
        /// For printing devices: the distance from the left edge of the physical page to the left edge of the printable area, in device units.
        /// For example, a printer set to print at 600 dpi on 8.5-by-11-inch paper, that cannot print on the leftmost 0.25-inch of paper,
        /// has a horizontal physical offset of 150 device units.
        /// </summary>
        PHYSICALOFFSETX = 112,

        /// <summary>
        /// For printing devices: the distance from the top edge of the physical page to the top edge of the printable area, in device units.
        /// For example, a printer set to print at 600 dpi on 8.5-by-11-inch paper, that cannot print on the topmost 0.5-inch of paper, 
        /// has a vertical physical offset of 300 device units.
        /// </summary>
        PHYSICALOFFSETY = 113,

        /// <summary>
        /// For display devices: the current vertical refresh rate of the device, in cycles per second (Hz).
        /// A vertical refresh rate value of 0 or 1 represents the display hardware's default refresh rate.
        /// This default rate is typically set by switches on a display card or computer motherboard, or by a configuration program 
        /// that does not use display functions such as ChangeDisplaySettings.
        /// </summary>
        VREFRESH = 116,

        /// <summary>
        /// Scaling factor for the x-axis of the printer.
        /// </summary>
        SCALINGFACTORX = 114,

        /// <summary>
        /// Scaling factor for the y-axis of the printer.
        /// </summary>
        SCALINGFACTORY = 115,

        /// <summary>
        /// Preferred horizontal drawing alignment, expressed as a multiple of pixels.
        /// For best drawing performance, windows should be horizontally aligned to a multiple of this value.
        /// A value of zero indicates that the device is accelerated, and any alignment may be used.
        /// </summary>
        BLTALIGNMENT = 119,

        /// <summary>
        /// Value that indicates the shading and blending capabilities of the device.
        /// </summary>
        SHADEBLENDCAPS = 45,

        /// <summary>
        /// Value that indicates the raster capabilities of the device.
        /// </summary>
        RASTERCAPS = 38,

        /// <summary>
        /// Value that indicates the curve capabilities of the device.
        /// </summary>
        CURVECAPS = 28,

        /// <summary>
        /// Value that indicates the line capabilities of the device.
        /// </summary>
        LINECAPS = 30,

        /// <summary>
        /// Value that indicates the polygon capabilities of the device.
        /// </summary>
        POLYGONALCAPS = 32,

        /// <summary>
        /// Value that indicates the text capabilities of the device.
        /// </summary>
        TEXTCAPS = 34,

        /// <summary>
        /// Value that indicates the color management capabilities of the device.
        /// </summary>
        COLORMGMTCAPS = 121,

        /// <summary>
        /// Vertical height of entire desktop in pixels.
        /// Not in document but in header file.
        /// </summary>
        DESKTOPVERTRES = 117,

        /// <summary>
        /// Horizontal width of entire desktop in pixels.
        /// Not in document but in header file.
        /// </summary>
        DESKTOPHORZRES = 118,

    }

    #region 屏幕

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RAMP
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Red;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Green;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Blue;
    }

    #endregion

    [ComVisible(true)]
    public enum CopyPixelOperation
    {
        NoMirrorBitmap = -2147483648, // 0x80000000
        Blackness = 66, // 0x00000042
        NotSourceErase = 1114278, // 0x001100A6
        NotSourceCopy = 3342344, // 0x00330008
        SourceErase = 4457256, // 0x00440328
        DestinationInvert = 5570569, // 0x00550009
        PatInvert = 5898313, // 0x005A0049
        SourceInvert = 6684742, // 0x00660046
        SourceAnd = 8913094, // 0x008800C6
        MergePaint = 12255782, // 0x00BB0226
        MergeCopy = 12583114, // 0x00C000CA
        SourceCopy = 13369376, // 0x00CC0020
        SourcePaint = 15597702, // 0x00EE0086
        PatCopy = 15728673, // 0x00F00021
        PatPaint = 16452105, // 0x00FB0A09
        Whiteness = 16711778, // 0x00FF0062
        CaptureBlt = 1073741824, // 0x40000000
    }

}
