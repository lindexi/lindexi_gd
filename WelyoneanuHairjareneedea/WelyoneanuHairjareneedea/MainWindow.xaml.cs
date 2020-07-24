using System;
using System.Collections.Generic;
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

namespace WelyoneanuHairjareneedea
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var geometry1 = PathGeometry.CreateFromGeometry(new RectangleGeometry(new Rect(new Point(0, 10), new Point(100, 20))));

            var geometry2 = PathGeometry.CreateFromGeometry(new RectangleGeometry(new Rect(new Point(50, 2), new Point(60, 30))));

            var newGeometry = Geometry.Combine(geometry1, geometry2, GeometryCombineMode.Exclude, Transform.Identity);
            var figures = newGeometry.Figures;

            var grid = new Canvas();
            Content = grid;

            for (var i = 0; i < figures.Count; i++)
            {
                var figure = figures[i];

                var pathGeometry = new PathGeometry(new[] { figure });

                grid.Children.Add(new Path()
                {
                    Data = pathGeometry,
                    Fill = Brushes.BurlyWood
                });
            }
        }
    }
}
