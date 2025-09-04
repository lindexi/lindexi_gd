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

namespace DeelagojawweFakejachebelrair;

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
        var mainWindow = new MainWindow()
        {
            Title = "Windows2"
        };
        mainWindow.Owner = this;
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            Dispatcher.Invoke(() => this.Close());
        });
        mainWindow.ShowDialog();
    }
}