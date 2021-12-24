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

            Foo = new Foo();
            Grid.Children.Add(Foo);

            MouseMove += MainWindow_MouseMove;
        }

        private Foo Foo { get; }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            Foo.InvalidateVisual();
        }
    }

    class Foo : FrameworkElement
    {
        public Foo()
        {
            Width = 500;
            Height = 100;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var pen = new Pen()
            {
                Brush = Brushes.Black,
                DashStyle = new DashStyle(new double[] { 0, 0 }, 0),
                Thickness = 10,
            };

            var geometry = new LineGeometry(new Point(0, 0), new Point(500, 0));

            drawingContext.DrawGeometry(Brushes.Beige, pen, geometry);

            base.OnRender(drawingContext);
        }
    }
}