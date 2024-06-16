using static CPF.Linux.XLib;
namespace UnoInk.Inking.X11Platforms;

public record X11InfoManager(IntPtr Display, int Screen, IntPtr RootWindow)
{
    public int XDisplayWidth => _xDisplayWidth ??= XDisplayWidth(Display, Screen);
    private int? _xDisplayWidth;

    public int XDisplayHeight => _xDisplayHeight ??= XDisplayHeight(Display, Screen);
    private int? _xDisplayHeight;
    
    /// <summary>
    /// 屏幕的物理尺寸
    /// 先照86寸屏幕来
    /// 86寸屏幕的屏幕的长度和宽度分别是多少厘米
    /// </summary>
    public double ScreenPhysicalWidthCentimetre { set; get; } = 192;
    public double ScreenPhysicalHeightCentimetre { set; get; } = 111;

    #region Atoms

    /// <summary>
    ///  `_MOTIF_WM_HINTS` 是一个窗口属性，用于向窗口管理器提供有关窗口的提示信息。让我来详细解释一下：
    /// 
    /// 1. **定义和结构**：
    /// - `_MOTIF_WM_HINTS` 是一个窗口属性，用于存储有关窗口的信息。
    /// - 它使用了 `XWMHints` 结构，该结构在 `X11/Xutil.h` 头文件中定义。
    /// - `XWMHints` 结构包含了一些标志位和值，用于描述窗口的特性。
    /// 
    /// 2. **标志位**：
    /// - `_MOTIF_WM_HINTS` 使用以下标志位：
    /// - `InputHint`：指示应用程序是否依赖于窗口管理器来获取键盘输入。
    /// - `StateHint`：指示窗口的初始状态（例如，正常、最小化等）。
    /// - `IconPixmapHint` 和 `IconWindowHint`：指定用作图标的 pixmap 或窗口。
    /// - `IconPositionHint`：指定图标的初始位置。
    /// - `IconMaskHint`：指定用作图标的遮罩。
    /// - `WindowGroupHint`：指定相关窗口组的 ID。
    /// - `UrgencyHint`：指示窗口是否具有紧急状态。
    /// - `AllHints`：包含所有标志位的组合。
    /// 
    /// 3. **初始状态标志**：
    /// - `initial_state` 标志用于指示窗口的初始状态：
    /// - `WithdrawnState`：窗口已撤回。
    /// - `NormalState`：大多数应用程序初始状态。
    /// - `IconicState`：应用程序希望以图标形式启动。
    /// 
    /// 4. **图标和遮罩**：
    /// - `icon_pixmap` 和 `icon_mask` 用于指定图标的 pixmap 和遮罩。
    /// - 遮罩允许非矩形图标。
    /// 
    /// 总之，`_MOTIF_WM_HINTS` 属性允许应用程序向窗口管理器提供关于窗口的提示信息，以便管理器可以根据这些信息执行相应的操作。¹²
    /// 
    /// 源: 与 Copilot 的对话， 2024/5/29
    /// (1) Xlib Programming Manual: Setting and Reading the WM_HINTS... - Tronche.https://tronche.com/gui/x/xlib/ICC/client-to-window-manager/wm-hints.html.
    /// (2) Xlib这个（删除窗口装饰）如何工作？. https://www.codingdict.com/questions/44359.
    /// (3) Xlib How Does This(Removing Window Decoration) Work?. https://stackoverflow.com/questions/5134297/xlib-how-does-this-removing-window-decoration-work.
    /// (4) VendorShell(3) — Arch manual pages.https://man.archlinux.org/man////VendorShell.3.en.
    /// </summary>
    public IntPtr HintsPropertyAtom => GetAtom(ref _hintsPropertyAtom, "_MOTIF_WM_HINTS");
    private IntPtr _hintsPropertyAtom;
    
    public IntPtr WMStateAtom => GetAtom(ref _wmStateAtom, "_NET_WM_STATE");
    private IntPtr _wmStateAtom;
    
    private IntPtr GetAtom(ref IntPtr atom, string atomName)
    {
        if (atom == IntPtr.Zero)
        {
            atom = GetAtom(atomName);
        }
        
        return atom;
    }
    
    private IntPtr GetAtom(string atomName)
    {
        return XInternAtom(Display, atomName, true);
    }
    
    #endregion
}
