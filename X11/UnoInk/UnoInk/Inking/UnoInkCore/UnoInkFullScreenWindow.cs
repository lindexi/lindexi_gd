using Windows.UI;
using Microsoft.UI;
using UnoHacker;
using UnoInk.UnoInkCore;
using Uno.UI.Xaml;

namespace UnoInk.Inking.UnoInkCore;

public partial class UnoInkFullScreenWindow : Window
{
    public UnoInkFullScreenWindow()
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

        Content = new UnoInkCanvasUserControl();
        
        ChangeBackground();
    }
    
    private async void ChangeBackground()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            
#if HAS_UNO
            // 这句话似乎也是无效的
            this.SetBackground(new SolidColorBrush(new Color()
            {
                A = (byte) Random.Shared.Next(255),
                R = (byte) Random.Shared.Next(255),
                G = (byte) Random.Shared.Next(255),
                B = (byte) Random.Shared.Next(255),
            }));

            this.Content?.InvalidateArrange();
#endif
        }
    }
}
