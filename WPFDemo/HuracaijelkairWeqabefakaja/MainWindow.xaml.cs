using System.Windows;

namespace HuracaijelkairWeqabefakaja;

/// <summary>
/// 提供轨道图标的预览与导出界面。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}