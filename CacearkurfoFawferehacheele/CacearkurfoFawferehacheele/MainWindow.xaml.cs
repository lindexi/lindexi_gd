using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CacearkurfoFawferehacheele
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            StylusMove += MainWindow_StylusMove;
            StylusUp += MainWindow_StylusUp;

            RegisterStoryboardHandler();
        }

        private void MainWindow_StylusUp(object sender, StylusEventArgs e)
        {
            _currentStroke = StrokeVisualList[e.StylusDevice.Id].Stroke;
            StrokeVisualList.Remove(e.StylusDevice.Id);
            StartAnimation();
        }

        private Stroke _currentStroke;

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
            var stylusPointCollection = e.GetStylusPoints(this);
            foreach (var stylusPoint in stylusPointCollection)
            {
                strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y));
            }

            strokeVisual.Redraw();
        }

        private StrokeVisual GetStrokeVisual(int id)
        {
            if (StrokeVisualList.TryGetValue(id, out var visual))
            {
                return visual;
            }

            var strokeVisual = new StrokeVisual();
            StrokeVisualList[id] = strokeVisual;
            var visualCanvas = new VisualCanvas(strokeVisual);
            StrokeGrid.Children.Add(visualCanvas);

            return strokeVisual;
        }

        private Dictionary<int, StrokeVisual> StrokeVisualList { get; } = new Dictionary<int, StrokeVisual>();

        private Storyboard GetAnimationStoryboard()
        {
            _storyboard = Resources["StrokePointAnimation"] as Storyboard;
            return _storyboard;
        }

        private void RegisterStoryboardHandler()
        {
            var descriptor =
                DependencyPropertyDescriptor.FromProperty(System.Windows.Media.EllipseGeometry.CenterProperty,
                    typeof(EllipseGeometry));
            descriptor.RemoveValueChanged(StrokePointGeometry, StrokeAnimating);
            descriptor.AddValueChanged(StrokePointGeometry, StrokeAnimating);
        }

        private bool _isFirst = true;

        private Geometry GetCurrentGeometry()
        {
            return _currentStroke.GetGeometry();
        }

        private void StrokeAnimating(object sender, EventArgs e)
        {
            var animatedStrokeGrid = StrokeGrid;
            if (_isFirst)
            {
                _isFirst = false;
                // 设置背景这句话是重要的哦，否则Grid不会撑开宽度和高度
                animatedStrokeGrid.Background = Brushes.Transparent;

                var mask = new DrawingBrush
                {
                    TileMode = TileMode.None, Stretch = Stretch.None, AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };
                var geoMask = new GeometryGroup {FillRule = FillRule.Nonzero};
                mask.Drawing = new GeometryDrawing {Geometry = geoMask, Brush = Brushes.Black};
                geoMask.Children.Add(new RectangleGeometry(new Rect(0, 0, 1, 1)));

                animatedStrokeGrid.OpacityMask = mask;
            }

            var group = (GeometryGroup) ((GeometryDrawing) ((DrawingBrush) animatedStrokeGrid.OpacityMask).Drawing)
                .Geometry;
            var center = StrokePointGeometry.Center;
            var x = StrokePointGeometry.RadiusX;
            var y = StrokePointGeometry.RadiusY;
            group.Children.Add(new System.Windows.Media.EllipseGeometry(center, x, y));
        }

        private Storyboard _storyboard;

        public static readonly DependencyProperty StrokePointThicknessProperty = DependencyProperty.Register(
            "StrokePointThickness", typeof(double), typeof(MainWindow), new PropertyMetadata(20.0));

        public double StrokePointThickness
        {
            get { return (double) GetValue(StrokePointThicknessProperty); }
            set { SetValue(StrokePointThicknessProperty, value); }
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            StartAnimation();
        }

        private void StartAnimation()
        {
            var geometry = GetCurrentGeometry();

            var storyboard = GetAnimationStoryboard();
            if (storyboard.Children[0] is PointAnimationUsingPath pointAnimationUsingPath)
            {
                pointAnimationUsingPath.PathGeometry = geometry.GetFlattenedPathGeometry();
                pointAnimationUsingPath.Duration = TimeSpan.FromMilliseconds(3000);
            }

            storyboard.Completed -= StoryboardOnCompleted;
            storyboard.Completed += StoryboardOnCompleted;

            void StoryboardOnCompleted(object sender, EventArgs e)
            {
                storyboard.Completed -= StoryboardOnCompleted;

                Dispatcher.InvokeAsync(() =>
                {
                    StrokeGrid.OpacityMask = null;
                    _isFirst = true;
                }, DispatcherPriority.Background);
            }

            storyboard.Begin();
        }
    }

    public class VisualCanvas : FrameworkElement
    {
        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }

        protected override int VisualChildrenCount => 1;

        public VisualCanvas(DrawingVisual visual)
        {
            Visual = visual;
            AddVisualChild(visual);
        }

        public DrawingVisual Visual { get; }
    }

    /// <summary>
    ///     用于显示笔迹的类
    /// </summary>
    public class StrokeVisual : DrawingVisual
    {
        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        public StrokeVisual() : this(new DrawingAttributes()
        {
            Color = Colors.Red,
            FitToCurve = true,
            Width = 5
        })
        {
        }

        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        /// <param name="drawingAttributes"></param>
        public StrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes;
        }

        /// <summary>
        ///     设置或获取显示的笔迹
        /// </summary>
        public Stroke Stroke { set; get; }

        /// <summary>
        ///     在笔迹中添加点
        /// </summary>
        /// <param name="point"></param>
        public void Add(StylusPoint point)
        {
            if (Stroke == null)
            {
                var collection = new StylusPointCollection {point};
                Stroke = new Stroke(collection) {DrawingAttributes = _drawingAttributes};
            }
            else
            {
                Stroke.StylusPoints.Add(point);
            }
        }

        /// <summary>
        ///     重新画出笔迹
        /// </summary>
        public void Redraw()
        {
            using var dc = RenderOpen();
            Stroke.Draw(dc);
        }

        private readonly DrawingAttributes _drawingAttributes;
    }
}