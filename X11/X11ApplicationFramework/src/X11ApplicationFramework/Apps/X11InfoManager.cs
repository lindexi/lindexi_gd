using System;
using X11ApplicationFramework.Natives;
using static X11ApplicationFramework.Natives.XLib;

namespace X11ApplicationFramework.Apps;

public record X11InfoManager
{
    public X11InfoManager(IntPtr display, int screen, IntPtr rootWindow)
    {
        Display = display;
        Screen = screen;
        RootWindow = rootWindow;

        X11Atoms = new X11Atoms(display, screen);
    }

    public X11InfoManager(IntPtr display)
    {
        Display = display;
        var screen = XDefaultScreen(display);
        Screen = screen;
        var rootWindow = XDefaultRootWindow(display);
        RootWindow = rootWindow;

        X11Atoms = new X11Atoms(display, screen);
    }

    public IntPtr Display { get; init; }
    public int Screen { get; init; }
    public IntPtr RootWindow { get; init; }

    public int XDisplayWidth => _xDisplayWidth ??= XDisplayWidth(Display, Screen);
    private int? _xDisplayWidth;

    public int XDisplayHeight => _xDisplayHeight ??= XDisplayHeight(Display, Screen);
    private int? _xDisplayHeight;

    internal void ShutdownDispose()
    {
        XCloseDisplay(Display);
    }

    ///// <summary>
    ///// 屏幕的物理尺寸
    ///// 先照86寸屏幕来
    ///// 86寸屏幕的屏幕的长度和宽度分别是多少厘米
    ///// </summary>
    //public double ScreenPhysicalWidthCentimetre { set; get; } = 192;
    //public double ScreenPhysicalHeightCentimetre { set; get; } = 111;

    #region Atoms

    public X11Atoms X11Atoms { get; }

    //public IntPtr HintsPropertyAtom => GetAtom(ref _hintsPropertyAtom, "_MOTIF_WM_HINTS");
    //private IntPtr _hintsPropertyAtom;

    //public IntPtr WMStateAtom => GetAtom(ref _wmStateAtom, "_NET_WM_STATE");
    //private IntPtr _wmStateAtom;

    //private IntPtr GetAtom(ref IntPtr atom, string atomName)
    //{
    //    if (atom == IntPtr.Zero)
    //    {
    //        atom = GetAtom(atomName);
    //    }

    //    return atom;
    //}

    //public IntPtr GetAtom(string atomName)
    //{
    //    return XInternAtom(Display, atomName, true);
    //}

    #endregion
}
