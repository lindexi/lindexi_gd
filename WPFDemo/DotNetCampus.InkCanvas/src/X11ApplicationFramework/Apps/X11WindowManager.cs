using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace X11ApplicationFramework.Apps;

/// <summary>
/// 管理当前 X11 平台的窗口
/// </summary>
[SupportedOSPlatform("Linux")]
public class X11WindowManager
{
    public IReadOnlyCollection<X11Window> Windows => _windows.Values;

    internal void RegisterWindow(X11Window window)
    {
        _windows[window.X11WindowIntPtr] = window;
    }

    public bool TryGetWindow(nint x11WindowIntPtr, [NotNullWhen(true)] out X11Window? window)
    {
        return _windows.TryGetValue(x11WindowIntPtr, out window);
    }

    private readonly Dictionary<nint/*X11WindowIntPtr*/, X11Window> _windows = new();
}