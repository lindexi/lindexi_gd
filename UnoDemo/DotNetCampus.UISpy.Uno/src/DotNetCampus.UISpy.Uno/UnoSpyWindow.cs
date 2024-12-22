using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;

using Windows.System;

namespace DotNetCampus.UISpy.Uno;

internal class UnoSpyWindow : Window
{
    public UnoSpyWindow(DependencyObject rootElement)
    {
        Content = new UnoSpyPage
        {
            TargetRootElement = rootElement,
        };
//#if DEBUG
//        this.EnableHotReload();
//#endif
    }
}
