using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Uno.UI.Xaml;
using UnoHacker;

namespace UnoInk.UnoInkCore;

public partial class UnoInkWindow : Window
{
    public UnoInkWindow()
    {
#if DEBUG
        this.EnableHotReload();
#endif

#if HAS_UNO
        // 这句话似乎也是无效的
        this.SetBackground(new SolidColorBrush(Colors.Transparent));
        this.AppWindow.GetApplicationView().TryEnterFullScreenMode();
#endif
        
        // 背景透明需要 UNO 还没发布的版本
        // https://github.com/lindexi/uno/tree/7b282851a8ec3ed7eb42a53af8b50ea7fe045d56
        // 这句话似乎才是关键，设置窗口背景透明。通过 MainWindow.SetBackground 配置是无效的
        Hacker.Do();

        var rootFrame = new Frame();
        Content = rootFrame;
        
        rootFrame.Background = new SolidColorBrush(Colors.Transparent);
        
        // When the navigation stack isn't restored navigate to the first page,
        // configuring the new page by passing required information as a navigation
        // parameter
        rootFrame.Navigate(typeof(MainPage));
    }
}
