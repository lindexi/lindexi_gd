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

namespace KolefijurfeLonaynallcay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var path = new Path()
            {
                Data = Geometry.Parse("M 48.000,0.000 A48.000,48.000,0,0,1,96.000,48.000 L 48.000,48.000 z"),
                Stroke = Brushes.Black,
                StrokeThickness = 5,
                StrokeDashArray = new DoubleCollection(new[] { 0, 0.0d })
            };

            StackPanel.Children.Add(path);
        }

        private void Line_OnMouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
