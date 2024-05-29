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
