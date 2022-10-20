using System;
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

namespace Workbench;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        CurrentMainWindow = this;

        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        CurrentMainWindow = null;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
        }

        base.OnKeyDown(e);
    }

    public static void ShowMainWindow()
    {
        if (CurrentMainWindow is null)
        {
            var mainWindow = new MainWindow()
            {
                ShowActivated = true
            };
            mainWindow.Show();
        }
        else
        {
            CurrentMainWindow.Show();
            CurrentMainWindow.Activate();
        }
    }

    public static MainWindow? CurrentMainWindow { set; get; }
}