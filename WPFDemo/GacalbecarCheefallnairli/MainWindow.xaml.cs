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

namespace GacalbecarCheefallnairli;

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
        await Task.Delay(1000);
        Foo.Visibility = Visibility.Visible;
    }
}

public class Foo : UserControl
{
    public Foo()
    {
        Loaded += Foo_Loaded;
    }

    private void Foo_Loaded(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Loaded");
    }
}