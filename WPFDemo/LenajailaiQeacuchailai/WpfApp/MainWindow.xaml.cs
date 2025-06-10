using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using WpfApp.Inking;

namespace WpfApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var grid = (Grid)Content;
        var skiaCanvas = grid.Children.OfType<SkiaCanvas>().First();
        var simpleInkCanvas = grid.Children.OfType<SimpleInkCanvas>().First();

        simpleInkCanvas.SkiaCanvas = skiaCanvas;
    }
}