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
        return window.NativeWindow;
    }
}
