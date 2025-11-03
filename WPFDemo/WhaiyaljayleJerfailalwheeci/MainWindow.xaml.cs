using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WhaiyaljayleJerfailalwheeci;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InkCanvas inkCanvas = InkCanvas;
        inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
        inkCanvas.EraserShape = new RectangleStylusShape(50, 60);
        inkCanvas.StrokeCollected += InkCanvas_StrokeCollected;

        StrokeCollection s;
        Stroke s1;
    }

    private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        throw new NotImplementedException();
    }
}