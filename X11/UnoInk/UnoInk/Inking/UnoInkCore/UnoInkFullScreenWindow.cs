using Microsoft.UI;
using UnoHacker;
using Uno.UI.Xaml;

namespace UnoInk.Inking.UnoInkCore;

public partial class UnoInkFullScreenWindow : Window
{
    public UnoInkFullScreenWindow()
    {
#if DEBUG
        this.EnableHotReload();
#endif

        //if (OperatingSystem.IsWindows())
        {
            Content = new DebugInkUserControl();
            return;
        }

#if HAS_UNO

        // 这句话似乎也是无效的
        this.SetBackground(new SolidColorBrush(Colors.Transparent));
        this.AppWindow.GetApplicationView().TryEnterFullScreenMode();
#endif

        // 背景透明需要 UNO 还没发布的版本
        // https://github.com/lindexi/uno/tree/7b282851a8ec3ed7eb42a53af8b50ea7fe045d56
        // 这句话似乎才是关键，设置窗口背景透明。通过 MainWindow.SetBackground 配置是无效的
        Hacker.Do();

        Content = new UnoInkCanvasUserControl(this);
    }
}
