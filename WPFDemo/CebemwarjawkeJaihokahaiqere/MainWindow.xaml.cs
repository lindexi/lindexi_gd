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
using System.Windows.Threading;

namespace CebemwarjawkeJaihokahaiqere;

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

class Foo : FrameworkElement
{
    private readonly Pen _pen = new Pen(Brushes.Black, 1);

    protected override void OnRender(DrawingContext drawingContext)
    {
        var w = 900;
        var h = 600;

        for (int i = 0; i < 1_000; i++)
        {
            drawingContext.DrawLine(_pen, GetRandomPoint(), GetRandomPoint());
        }

        Point GetRandomPoint() => new Point(Random.Shared.Next(w), Random.Shared.Next(h));

        Dispatcher.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
    }
}