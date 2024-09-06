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

namespace CoharnafoherNearlalqanijelayall;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;

        this.TouchDown += MainWindow_TouchDown;
    }

    private void MainWindow_TouchDown(object? sender, TouchEventArgs e)
    {
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TextBox.Focus();
        TextBox.SelectionLength = 3;
    }
}