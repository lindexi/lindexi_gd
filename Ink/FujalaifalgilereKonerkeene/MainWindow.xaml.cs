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

namespace FujalaifalgilereKonerkeene;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        _canvas.EditingMode = InkCanvasEditingMode.Select;
    }

    private void OnInkClick(object sender, RoutedEventArgs e)
    {
        _canvas.EditingMode = InkCanvasEditingMode.Ink;
    }
}