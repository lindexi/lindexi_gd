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

namespace FawalgunawKigewifu;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var geometry = Geometry.Parse("M14.9559 17.0686C14.6497 16.9666 14.6497 16.5334 14.9559 16.4314L15.9395 16.1035C16.0169 16.0777 16.0777 16.0169 16.1035 15.9395L16.4314 14.9559C16.5334 14.6497 16.9666 14.6497 17.0686 14.9559L17.3965 15.9395C17.4223 16.0169 17.4831 16.0777 17.5605 16.1035L18.5441 16.4314C18.8503 16.5334 18.8503 16.9666 18.5441 17.0686L17.5605 17.3965C17.4831 17.4223 17.4223 17.4831 17.3965 17.5605L17.0686 18.5441C16.9666 18.8503 16.5334 18.8503 16.4314 18.5441L16.1035 17.5605C16.0777 17.4831 16.0169 17.4223 15.9395 17.3965L14.9559 17.0686ZM11.9912 14.7837C11.9644 14.8104 11.9316 14.8309 11.8959 14.8431L9.28979 15.7311C9.09498 15.7974 8.90854 15.611 8.9749 15.4162L9.86293 12.8102C9.87512 12.7744 9.89564 12.7416 9.92232 12.7149L14.3189 8.31823L16.3878 10.3871L11.9912 14.7837ZM17.3559 8.71874C17.5493 8.91241 17.5488 9.22606 17.3552 9.41963L16.8877 9.88712L14.8189 7.81828L15.2864 7.35079C15.48 7.15714 15.7943 7.15714 15.988 7.35079L17.3559 8.71874ZM5.42302 9.25156C4.96718 9.09961 4.96718 8.45483 5.42302 8.30288L6.72307 7.86953C6.79222 7.84648 6.84648 7.79222 6.86953 7.72307L7.30288 6.42302C7.45483 5.96717 8.09962 5.96717 8.25157 6.42302L8.68492 7.72307C8.70797 7.79222 8.76223 7.84648 8.83138 7.86953L10.1314 8.30288C10.5873 8.45483 10.5873 9.09961 10.1314 9.25156L8.83138 9.68491C8.76223 9.70796 8.70797 9.76222 8.68492 9.83138L8.25157 11.1314C8.09962 11.5873 7.45483 11.5873 7.30288 11.1314L6.86953 9.83138C6.84648 9.76222 6.79222 9.70796 6.72307 9.68491L5.42302 9.25156Z");
        geometry = PathGeometry.CreateFromGeometry(geometry);
        Path.Data = geometry;

        var bounds = geometry.Bounds;

        var size = 24;
        var scaleX = size / bounds.Width;
        var scaleY = size / bounds.Height;
        var scale = Math.Min(scaleX, scaleY);

        geometry.Transform = new ScaleTransform(scale, scale);

        var pathGeometry = PathGeometry.CreateFromGeometry(geometry);
        var path = pathGeometry.ToString();
    }
}