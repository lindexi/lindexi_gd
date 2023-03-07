using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HakewofuCayorejar;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        var file = System.IO.Path.GetFullPath("HakewofuCayorejar.exe");
        Process.Start(file, "xxxxx");
    }
}

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var application = new Application()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        application.Startup += (sender, eventArgs) =>
        {
            if (args.Length == 0)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                application.Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(5000);
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                });
            }
        };
        application.Run();
    }
}
