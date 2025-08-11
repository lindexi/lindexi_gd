using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace X11ApplicationFramework.Apps.Threading;

/// <summary>
/// 命名是从 Avalonia 抄的
/// </summary>
[SupportedOSPlatform("Linux")]
class X11PlatformThreading //: IDisposable
{
    public X11PlatformThreading(X11Application x11Application)
    {
        _x11Application = x11Application;
    }

    private readonly X11Application _x11Application;
}