using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
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

namespace WpfApp1
{
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

    public class ThemeKeyValuePair
    {
        public LinearGradientBrush linearGradientBrush1 = new LinearGradientBrush();

        public ThemeKeyValuePair()
        {
            Color gradientStop1Color1 = Colors.Red;
            Color gradientStop2Color1 = Colors.Green;

            ScaleTransform scaleTransform1 = new ScaleTransform()
            {
                CenterY = Convert.ToDouble(0.5),
                ScaleY = Convert.ToDouble(-1)
            };


            linearGradientBrush1.StartPoint = new Point(0, 0);
            linearGradientBrush1.EndPoint = new Point(0, 2);
            //linearGradientBrush1.RelativeTransform = scaleTransform1;
            //linearGradientBrush1.MappingMode = BrushMappingMode.Absolute;
            linearGradientBrush1.GradientStops.Add(new GradientStop() { Color = gradientStop1Color1, Offset = 0 });
            linearGradientBrush1.GradientStops.Add(new GradientStop() { Color = gradientStop2Color1, Offset = 1 });
        }
        
        public LinearGradientBrush LinearGradientBrush
        {
            get
            {
                return linearGradientBrush1;
            }
            set
            {
                linearGradientBrush1 = value;
            }
        }
    }
}
