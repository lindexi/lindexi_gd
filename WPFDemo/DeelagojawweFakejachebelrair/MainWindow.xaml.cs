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

    private Window? _window1;

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        _window1 = new Window()
        {
            Title = "Windows1"
        };
        _window1.Show();
    }

    private void Button2_OnClick(object sender, RoutedEventArgs e)
    {
        Window window2 = new Window();
        window2.Owner = _window1;

        Task.Run(async () => 
        {
            await Task.Delay(1000);
            await Dispatcher.InvokeAsync(() =>
            {
                Debug.Assert(_window1 != null, nameof(_window1) + " != null");
                _window1.Close();  //Causes other window events to invalid
            });
        });

        window2.ShowDialog();
    }
}