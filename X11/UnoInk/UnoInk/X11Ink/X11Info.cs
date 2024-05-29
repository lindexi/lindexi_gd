using Microsoft.UI;

using static CPF.Linux.XLib;
namespace UnoInk.X11Ink;

public record X11Info(IntPtr Display, int Screen, IntPtr RootWindow)
{
    public int XDisplayWidth => _xDisplayWidth ??= XDisplayWidth(Display, Screen);
    private int? _xDisplayWidth;

    public int XDisplayHeight => _xDisplayHeight ??= XDisplayHeight(Display, Screen);
    private int? _xDisplayHeight;
}
