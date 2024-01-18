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

namespace GawlihihunarneFemhaygohaw;

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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        for (int i = 0; i < 100; i++)
        {
            Foo(i);
        }
    }

    private async void Foo(int n)
    {
        Debug.WriteLine($"Start {n}");
        await Task.Delay(100);
        Debug.WriteLine(n);
    }
}