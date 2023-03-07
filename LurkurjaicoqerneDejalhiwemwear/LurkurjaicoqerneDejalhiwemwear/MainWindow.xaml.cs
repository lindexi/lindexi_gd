using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace LurkurjaicoqerneDejalhiwemwear;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object sender, RoutedEventArgs e)
    {
        while (true)
        {
            var process = Process.Start("LurkurjaicoqerneDejalhiwemwear.exe", "xx");
            var handle = process.Handle;

            if (_processDictionary.ContainsKey(process.Id))
            {
                Debugger.Launch();
            }
            else
            {
                _processDictionary[process.Id] = handle;
            }
            await Task.Delay(10);
        }
        //var process = Process.Start("LurkurjaicoqerneDejalhiwemwear.exe","xx");
        //process.Dispose(); // Call dispose do not release the process group in process manager
    }

    private readonly Dictionary<int, IntPtr> _processDictionary = new Dictionary<int, IntPtr>();
}

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application application = new Application();
        application.Startup += async (s, e) =>
        {
            if (args.Length == 0)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                application.Shutdown();
            }
        };

        application.Run();
    }
}