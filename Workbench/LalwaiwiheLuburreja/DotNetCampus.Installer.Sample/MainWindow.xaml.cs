using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DotNetCampus.Installer.Lib.Commandlines;
using SplashScreen = DotNetCampus.Installer.Lib.SplashScreens.SplashScreen;

namespace DotNetCampus.Installer.Sample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 安装窗口显示出来之后，关闭启动画面窗口
        var installOptions = Cli.CommandLine.Parse(Environment.GetCommandLineArgs()).As<InstallOptions>();
        if (installOptions.SplashScreenWindowHandler is {} handler)
        {
            SplashScreen.CloseSplashScreenWindow(new IntPtr(handler));
        }
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
    }
}