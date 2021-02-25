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

            _circleEraserManager.SetLastPoint(new Point(300, 200));

            TouchMove += EraserEllipse_TouchMove;
        }

        private readonly CircleEraserManager _circleEraserManager;

        private void EraserEllipse_TouchMove(object sender, TouchEventArgs e)
        {
            var point = e.GetTouchPoint(this);
            //var x = point.Position.X - EraserEllipse.ActualWidth / 2;
            //var y = point.Position.Y - EraserEllipse.ActualHeight / 2;

            ////EraserEllipse.Margin = new Thickness(x, y, 0, 0);
            //EraserTranslate.X = x;
            //EraserTranslate.Y = y;

            _circleEraserManager.Move(point.Position);
        }
    }

    class CircleEraserManager
    {
        public CircleEraserManager(FrameworkElement eraserElement)
        {
            _eraserElement = eraserElement;
            var transform = new TranslateTransform();
            EraserTranslate = transform;
            _eraserElement.RenderTransform = EraserTranslate;
        }

        public void Move(Point point, double 半径 = 75)
        {
            var x = point.X - _eraserElement.ActualWidth / 2;
            var y = point.Y - _eraserElement.ActualHeight / 2;

            //EraserEllipse.Margin = new Thickness(x, y, 0, 0);
            EraserTranslate.X = x;
            EraserTranslate.Y = y;

            _currentPoint = point;
            this.半径 = 半径;


            //var vector = _lastPoint - point;
            //var 斜边 = vector.Length;
            //var cosθ = vector.Y / 斜边;
            //var 弧度 = Math.Acos(cosθ);
            //var 角度 = 弧度 / Math.PI * 180;

            //var 切线角度 = 角度 + 90;
            //var 半径 = 75.0;

        }

        public void GetCurrentGeometry()
        {
            圆 圆2 = new 圆(_currentPoint, 半径);
            var 轨迹线 = 圆1.求两圆轨迹线(圆2);

        }

        public double 半径 { set; get; }

        private Point _currentPoint;

        public void SetLastPoint(Point point)
        {
            var 半径 = 75.0;

            _lastPoint = point;
            圆1 = new 圆(_lastPoint, 半径);
        }

        private 圆 圆1 { set; get; }



        private Point _lastPoint;

        private readonly FrameworkElement _eraserElement;
        private TranslateTransform EraserTranslate { get; }
    }

    class 圆
    {
        public 圆(Point 圆心, double 半径)
        {
            this.圆心 = 圆心;
            this.半径 = 半径;
        }

        public Point 圆心 { get; }
        public double 半径 { get; }

        public 两圆轨迹 求两圆轨迹线(圆 另一个圆)
        {
            var p1 = this.圆心;
            var p2 = 另一个圆.圆心;
            var r1 = this.半径;
            var r2 = 另一个圆.半径;

            var vector = p1 - p2;
            var cosθ = vector.GetCos();
            var sinθ = vector.GetSin();

            var ax = p1.X + r1 * cosθ;
            var ay = p1.Y - r1 * sinθ;

            var bx = p1.X - r1 * cosθ;
            var by = p1.Y + r1 * sinθ;

            var cx = p2.X - r2 * cosθ;
            var cy = p2.Y + r2 * sinθ;

            var dx = p2.X + r2 * cosθ;
            var dy = p2.Y - r2 * sinθ;

            var aPoint = new Point(ax, ay);
            var bPoint = new Point(bx, by);
            var cPoint = new Point(cx, cy);
            var dPoint = new Point(dx, dy);

            线段 线段1 = new 线段(aPoint, dPoint);
            线段 线段2 = new 线段(bPoint, cPoint);

            return new 两圆轨迹(this, 另一个圆, 线段1, 线段2);
        }


    }

    static class VectorExtensions
    {
        /// <summary>
        /// 获取向量的 cos（θ）值
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double GetCos(this Vector vector)
          => vector.Y / vector.Length;

        /// <summary>
        /// 获取向量的 sin（θ）值
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double GetSin(this Vector vector)
            => vector.X / vector.Length;
    }

    class 两圆轨迹
    {
        public 两圆轨迹(圆 圆1, 圆 圆2, 线段 线段1, 线段 线段2)
        {
            this.圆1 = 圆1;
            this.圆2 = 圆2;
            this.线段1 = 线段1;
            this.线段2 = 线段2;
        }

        public 圆 圆1 { get; }
        public 圆 圆2 { get; }
        public 线段 线段1 { get; }
        public 线段 线段2 { get; }
    }

    class 线段
    {
        public 线段(Point a, Point b)
        {
            A = a;
            B = b;
        }

        public Point A { get; }
        public Point B { get; }


    }
}
