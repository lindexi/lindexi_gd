using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LecacheniJequchaferenal;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TakeSnapshotToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (TakeSnapshotToggleButton.IsChecked is true)
        {
            // 禁止截图模式
            SetWindowDisplayAffinity(new WindowInteropHelper(this).Handle, WDA_MONITOR);

            // 修改内容为再点击就是允许截图
            TakeSnapshotToggleButton.Content = "允许截图";
        }
        else
        {
            // 允许截图模式
            SetWindowDisplayAffinity(new WindowInteropHelper(this).Handle, WDA_NONE);

            // 修改内容为再点击就是禁止截图
            TakeSnapshotToggleButton.Content = "禁止截图";
        }
    }

    private const uint WDA_NONE = 0x00000000;
    private const uint WDA_MONITOR = 0x00000001;

    [DllImport("user32.dll")]
    public static extern uint SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    // 对于如此简单的定义，使用 LibraryImport 不会有任何的优化
    //[LibraryImport("user32.dll")]
    //public static partial uint SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
}