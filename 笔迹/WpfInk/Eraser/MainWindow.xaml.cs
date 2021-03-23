using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Eraser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            _circleEraserManager = new CircleEraserManager(EraserEllipse);

            _circleEraserManager.SetLastPoint(new Point(200, 200));
            _circleEraserManager.Move(new Point(100, 100));

            _circleEraserManager.SetLastPoint(new Point(300 + 半径, 200 + 半径));

            TouchMove += EraserEllipse_TouchMove;
            MouseDown += MainWindow_MouseDown;
            MouseMove += MainWindow_MouseMove;
            MouseUp += MainWindow_MouseUp;
            MouseWheel += MainWindow_MouseWheel;
        }

        private void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        public static readonly DependencyProperty GeometryBrushProperty = DependencyProperty.Register(
            "GeometryBrush", typeof(Brush), typeof(MainWindow), new PropertyMetadata(NormalBrush));

        public Brush GeometryBrush
        {
            get { return (Brush) GetValue(GeometryBrushProperty); }
            set { SetValue(GeometryBrushProperty, value); }
        }

        public static Brush NormalBrush => Brushes.Red;

        public static Brush HitTestBrush => Brushes.Khaki;

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isHitTest = !_isHitTest;

            if (!_isHitTest)
            {
                GeometryBrush = NormalBrush;
            }
        }

        private bool _isHitTest;

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            半径 += e.Delta / 10.0;

            半径 = Math.Max(1, 半径);

            _circleEraserManager.Move(半径);
            DrawElement.Data = _circleEraserManager.GetCurrentGeometry();
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(this);
            Move(point);
        }

        public double 半径 { get; set; } = 75;

        private readonly CircleEraserManager _circleEraserManager;

        private void EraserEllipse_TouchMove(object sender, TouchEventArgs e)
        {
            var point = e.GetTouchPoint(this);

            Move(point.Position);
        }

        private void Move(Point point)
        {
            if (_isHitTest)
            {
                if (_circleEraserManager.HitTest(point))
                {
                    GeometryBrush = HitTestBrush;
                }
                else
                {
                    GeometryBrush = NormalBrush;
                }
            }
            else
            {
                _circleEraserManager.Move(point, 半径);
                DrawElement.Data = _circleEraserManager.GetCurrentGeometry();
            }
        }
    }
}
