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

namespace QarwicukeNehifakajaycair;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

public class Foo : Control
{
    public Foo()
    {
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        Width = 300;
        Height = 300;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = 6.35;
        drawingContext.DrawEllipse(Brushes.Black, null, new Point(100, 100), size, size);
    }
}