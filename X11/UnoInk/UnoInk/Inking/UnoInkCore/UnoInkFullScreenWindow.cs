using Microsoft.UI;
using Microsoft.UI.Windowing;
using UnoHacker;
using Uno.UI.Xaml;
using UnoInk.Inking.InkCore;
using UnoInk.Inking.X11Platforms;

namespace UnoInk.Inking.UnoInkCore;

public partial class UnoInkFullScreenWindow : Window
{
    public UnoInkFullScreenWindow()
    {
        StaticDebugLogger.WriteLine($"窗口构造函数 {Environment.TickCount64} Thread={Environment.CurrentManagedThreadId}");

#if DEBUG
        this.EnableHotReload();
#endif

        if (OperatingSystem.IsWindows())
        {
            Content = new DebugInkUserControl();
            return;
        }

#if HAS_UNO
        // 设置背景透明
        // 这句话似乎也是无效的
        this.SetBackground(new SolidColorBrush(Colors.Transparent));
#endif
        //this.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

        StaticDebugLogger.WriteLine($"全屏时间 {Environment.TickCount64} Thread={Environment.CurrentManagedThreadId}");

        // 背景透明需要 UNO 还没发布的版本
        // https://github.com/lindexi/uno/tree/7b282851a8ec3ed7eb42a53af8b50ea7fe045d56
        // 这句话似乎才是关键，设置窗口背景透明。通过 MainWindow.SetBackground 配置是无效的
        // https://github.com/unoplatform/uno/pull/16956 代码合入，等待新版本发布
        Hacker.Do();

        Content = new UnoInkCanvasUserControl(this);
    }
}
