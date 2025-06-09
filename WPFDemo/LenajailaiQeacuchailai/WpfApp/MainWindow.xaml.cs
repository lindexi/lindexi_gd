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

using WpfApp.InkDataModels;

namespace WpfApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ArrayOfArrayOfInkDataModel.ReadFromFile(@"Assets\Ink.xml");
    }
}

public class SimpleInkCanvas : FrameworkElement
{
    public SimpleInkCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        drawingContext.DrawLine(new Pen(Brushes.Red, 20), new Point(), new Point(200, 200));
    }
}