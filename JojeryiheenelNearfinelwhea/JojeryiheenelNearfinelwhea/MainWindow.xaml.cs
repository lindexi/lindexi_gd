using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JojeryiheenelNearfinelwhea;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    [STAThread]
    static void Main(string[] args)
    {
        var app = new App();

        var stopwatch = Stopwatch.StartNew();
        app.InitializeComponent();
        stopwatch.Stop();

        app.Run();
    }

    public MainWindow()
    {
        InitializeComponent();

        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            FindResource($"Geometry{i}");
        }
        stopwatch.Stop();

        var txt = "1.txt";

        for (int i = 0; i < 1000; i++)
        {
            var text = $@"    <DrawingImage x:Key=""DrawingImage{i}"">
        <DrawingImage.Drawing>
            <GeometryDrawing Brush=""#999999"" Geometry=""{{StaticResource Geometry{i}}}"" />
        </DrawingImage.Drawing>
    </DrawingImage>
";

            File.AppendAllText(txt, text);
        }
    }
}
