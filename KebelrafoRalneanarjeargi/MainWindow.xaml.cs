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

namespace KebelrafoRalneanarjeargi
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

    class Foo : UIElement
    {
        public Foo()
        {
            var drawingVisual = new DrawingVisual();
            var translateTransform = new TranslateTransform();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var rectangleGeometry = new RectangleGeometry(new Rect(0, 0, 10, 10));

                for (int i = 0; i < 10; i++)
                {
                    translateTransform.X = i * 15;

                    drawingContext.PushTransform(translateTransform);

                    drawingContext.DrawGeometry(Brushes.Red, null, rectangleGeometry);

                    drawingContext.Pop();
                }
            }

            translateTransform.X = 500;

            Visual = drawingVisual;

            SetTranslateTransform(translateTransform);
        }

        private async void SetTranslateTransform(TranslateTransform translateTransform)
        {
            while (true)
            {
                translateTransform.X++;

                if (translateTransform.X > 700)
                {
                    translateTransform.X = 0;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        protected override Visual GetVisualChild(int index) => Visual;
        protected override int VisualChildrenCount => 1;

        private Visual Visual { get; }
    }
}
