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

namespace NiwejabainelFehargaye
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var pixelsPerDip = (float)VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
        }
    }

    class F1 : UIElement
    {
        protected override Size MeasureCore(Size availableSize)
        {
            return new Size(availableSize.Width, 10);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(new Pen(Brushes.Red, 2), new Point(10, 10), new Point(100, 10));
            base.OnRender(drawingContext);
        }
    }

    class F2 : UIElement
    {
        protected override Size MeasureCore(Size availableSize)
        {
            return new Size(availableSize.Width, 10);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            //base.ArrangeCore(finalRect);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(new Pen(Brushes.Black, 3), new Point(10, 10), new Point(100, 10));
            base.OnRender(drawingContext);
        }
    }
}
