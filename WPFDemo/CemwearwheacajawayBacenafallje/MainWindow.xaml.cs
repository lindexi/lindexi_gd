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

namespace CemwearwheacajawayBacenafallje;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Exception? lastException = null;
        var app = (App) Application.Current;
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            if (lastException != null && ReferenceEquals(lastException, app.FirstException))
            {
                continue;
            }

            lastException = app.FirstException;

            if (lastException is null)
            {
                continue;
            }

            LogTextBox.Text = lastException.ToString();
        }
    }
}