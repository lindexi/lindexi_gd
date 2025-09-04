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

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        var window1 = new Window()
        {
            Title = "Windows1"
        };
        window1.Show();

        Window window2 = new Window
        {
            Title = "Windows2",
            Owner = window1,
        };

        Task.Run(async () =>
        {
            await Task.Delay(1000);
            await Dispatcher.InvokeAsync(() =>
            {
                Debug.Assert(window1 != null, nameof(window1) + " != null");
                window1.Close();  //Causes other window events to invalid
            });
        });

        window2.ShowDialog();
    }
}