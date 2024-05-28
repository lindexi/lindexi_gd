using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;

namespace SamplesApp;

public static class UnoHelper
{
    public static object GetNativeWindow(this Window window)
    {
#if WINDOWS10_0_19041_0_OR_GREATER
        return window;
#else
        return window.NativeWindow;
#endif
    }
}
