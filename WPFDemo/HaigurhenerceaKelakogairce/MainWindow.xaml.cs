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

namespace HaigurhenerceaKelakogairce;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var geometry = InkPath.Data;
        geometry = Geometry.Combine(geometry, Geometry.Empty, GeometryCombineMode.Union, Transform.Identity);
        geometry.Transform = new TranslateTransform(0, 1);
        geometry = Geometry.Combine(geometry, Geometry.Empty, GeometryCombineMode.Union, Transform.Identity);

        //var geometry = Geometry.Parse("M8,15C7.85289858,15.5677816,7.85289858,16.4322348,8,17L23,29C23.7348015,29.3762198,25,28.8227297,25,28L25,4C25,3.1772867,23.7348015,2.62379657,23,3L8,15z");
        //geometry = Geometry.Combine(geometry, Geometry.Empty, GeometryCombineMode.Union, Transform.Identity);

        //geometry.Transform = new ScaleTransform(-1, 1,centerX: 0, centerY:0);

        //geometry = Geometry.Combine(geometry, Geometry.Empty, GeometryCombineMode.Union, Transform.Identity);
        //var geometryBounds = geometry.Bounds;

        //geometry.Transform = new TranslateTransform(-geometryBounds.Left,-geometryBounds.Top+3);
        //geometry = Geometry.Combine(geometry, Geometry.Empty, GeometryCombineMode.Union, Transform.Identity);

        //InkPath.Data = geometry;


        var s = geometry.ToString();
    }
}