using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LayfilejonarchoDawherehebafonur
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            InitializeComponent();

            //StylusMove += MainWindow_StylusMove;
            //TouchMove += MainWindow_TouchMove;
            StylusPlugIns.Add(new F1(this));
        }

        private void MainWindow_TouchMove(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            var point = touchPoint.Position;

            EraserView.X = point.X - EraserView.Width / 2;
            EraserView.Y = point.Y - EraserView.Height / 2;

            EraserInkView.Count++;

            Debug.WriteLine($"TTTTTTTTTTTT {EraserInkView.Count:D5} " + Environment.TickCount);

            TouchTickCount = Environment.TickCount;
        }

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            var point = e.GetPosition(this);
            EraserView.X = point.X - EraserView.Width / 2;
            EraserView.Y = point.Y - EraserView.Height / 2;
        }

        private void UIElement_OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
        {
            var translation = e.DeltaManipulation.Translation;
            EraserView.X += translation.X;
            EraserView.Y += translation.Y;

            //var point = translation;
            //EraserView.X = point.X - EraserView.Width / 2;
            //EraserView.Y = point.Y - EraserView.Height / 2;
        }

        public static long TouchTickCount { set; get; }
    }

    public class F1 : StylusPlugIn
    {
        public F1(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            EraserInkView.Count++;
            Debug.WriteLine($"TTTTTTTTTTTT {EraserInkView.Count:D5} " + Environment.TickCount);
            MainWindow.TouchTickCount = Environment.TickCount;

            var stylusPointCollection = rawStylusInput.GetStylusPoints();
            if (stylusPointCollection.Count>0)
            {
                foreach (var stylusPoint in stylusPointCollection)
                {
                    _mainWindow.Dispatcher.InvokeAsync(() =>
                    {
                        var point = stylusPoint;

                        _mainWindow.EraserView.X = point.X - _mainWindow.EraserView.Width / 2;
                        _mainWindow.EraserView.Y = point.Y - _mainWindow.EraserView.Height / 2;
                    }, DispatcherPriority.Send);
                }
            }
        }

        private readonly MainWindow _mainWindow;
    }

    public class EraserInkView : EraserView
    {
        public static int Count { set; get; }

        public EraserInkView()
        {
            TranslateTransform = new TranslateTransform();
            RotateTransform = new RotateTransform();
            ScaleTransform = new ScaleTransform();

            TransformGroup = new TransformGroup()
            {
                Children = new TransformCollection()
                {
                    RotateTransform,
                    ScaleTransform,
                    TranslateTransform,
                }
            };
        }

        private TranslateTransform TranslateTransform { get; }
        private RotateTransform RotateTransform { get; }
        private ScaleTransform ScaleTransform { get; }

        private TransformGroup TransformGroup { get; }

        private long TickCount { set; get; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Debug.WriteLine($"RRRRRRRRRRRR {Count:D5} {Environment.TickCount} {Environment.TickCount - MainWindow.TouchTickCount:D5} {Environment.TickCount - TickCount:D5}");

            TickCount = Environment.TickCount;

            var bounds = DrawingVisual.DescendantBounds;
            var width = Width;
            var height = Height;
            var scaleTransform = ScaleTransform;
            if (double.IsNaN(width) || double.IsNaN(height))
            {
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
            }
            else
            {
                scaleTransform.ScaleX = width / bounds.Width;
                scaleTransform.ScaleY = height / bounds.Height;
            }

            TranslateTransform.X = X;
            TranslateTransform.Y = Y;

            RotateTransform.Angle = Rotation;
            var rect = new Rect(X, Y, Width, Height);
            var centerPoint = Center(rect);
            RotateTransform.CenterX = centerPoint.X;
            RotateTransform.CenterY = centerPoint.Y;

            drawingContext.PushTransform(TransformGroup);
            drawingContext.DrawDrawing(DrawingVisual.Drawing);
            drawingContext.Pop();
        }

        /// <summary>
        /// 获取两个点的中点。
        /// </summary>
        /// <param name="point1">点1。</param>
        /// <param name="point2">点2。</param>
        /// <returns>返回点1和点2的中点。</returns>
        [Pure]
        private static Point Midpoint(Point point1, Point point2)
        {
            return new Point((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        /// <summary>
        /// 获取矩形的中点。
        /// </summary>
        /// <param name="rect">矩形。</param>
        /// <returns>返回矩形的中点。</returns>
        [Pure]
        private static Point Center(Rect rect)
        {
            return Midpoint(rect.TopLeft, rect.BottomRight);
        }

        public static readonly DependencyProperty XProperty = DependencyProperty.Register(
            "X", typeof(double), typeof(EraserInkView), new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty YProperty = DependencyProperty.Register(
            "Y", typeof(double), typeof(EraserInkView), new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(
            "Rotation", typeof(double), typeof(EraserInkView), new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.AffectsRender));

        public double Rotation
        {
            get { return (double)GetValue(RotationProperty); }
            set { SetValue(RotationProperty, value); }
        }

        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }
    }


    public class EraserView : FrameworkElement
    {
        static EraserView()
        {
            // 不使用 xaml 的原因是 xaml 速度太慢，不如直接使用后台代码
            // 现在有性能问题，所以使用这个方式提高速度

            var geometryDrawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 50, 70)),
                //#4CFFFFFF
                Brush = new SolidColorBrush(Color.FromArgb(0x4C, 0xFF, 0x00, 0x00))
            };

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawDrawing(new DrawingGroup
                {
                    Children = new DrawingCollection
                    {
                        geometryDrawing,
                    }
                });
            }

            DrawingVisual = drawingVisual;
        }

        /// <inheritdoc />
        protected override int VisualChildrenCount { get; } = 0;

        protected static DrawingVisual DrawingVisual { get; }

        /// <inheritdoc />
        protected override Visual GetVisualChild(int index)
        {
            return DrawingVisual;
        }
    }
}