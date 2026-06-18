using Avalonia.Controls;

namespace ChatRoom.AvaloniaShell;

/// <summary>
/// 主窗口。承载 MainView，自身不包含业务逻辑。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}