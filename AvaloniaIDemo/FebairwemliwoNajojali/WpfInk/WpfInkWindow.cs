using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WpfInk;

public class WpfInkWindow : Window
{
    public WpfInkWindow()
    {
        Title = "WpfInk";
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        Background = new SolidColorBrush(new Color()
        {
            A = 0x5c,
            R = 0x56,
            G = 0x56,
            B = 0x56
        });
        WindowState = WindowState.Maximized;
    }
}
