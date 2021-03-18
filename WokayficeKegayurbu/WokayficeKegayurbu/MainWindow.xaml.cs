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

namespace WokayficeKegayurbu
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

        private void Canvas_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition((IInputElement)sender);
            if (Line != null)
            {
                Line = null;
                _lastPoint = point;
                Move(AEllipse, _lastPoint.Value);
                BEllipse.Visibility = Visibility.Collapsed;
                LineElement.Visibility = Visibility.Collapsed;

                return;
            }

            if (_lastPoint is null)
            {
                _lastPoint = point;
            }
            else
            {
                Line = new Line()
                {
                    APoint = _lastPoint.Value,
                    BPoint = point
                };
            }
        }

        private void Canvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(this);

            if (Line != null)
            {
                if (CheckIsPointOnLine(point, Line))
                {
                    LineElement.Stroke = Brushes.DarkGray;
                }
                else
                {
                    LineElement.Stroke = Brushes.Red;
                }

                return;
            }

            if (_lastPoint is null)
            {
                AEllipse.Visibility = Visibility.Visible;
                Move(AEllipse, point);
            }
            else
            {
                BEllipse.Visibility = Visibility.Visible;
                Move(BEllipse, point);

                LineElement.Visibility = Visibility.Visible;
                LineElement.X1 = _lastPoint.Value.X;
                LineElement.Y1 = _lastPoint.Value.Y;
                LineElement.X2 = point.X;
                LineElement.Y2 = point.Y;
            }
        }

        private static bool CheckIsPointOnLine(Point point, Line line, double epsilon = 0.1)
        {
            // 最简单理解的算法是根据两点之间直线距离最短，只需要求 P 点和线段的 AB 两点的距离是否等于 AB 的距离。如果相等，那么证明 P 点在线段 AB 上
            var ap = point - line.APoint;
            var bp = point - line.BPoint;
            var ab = line.BPoint - line.APoint;

            // 只不过求 Length 内部需要用到一次 Math.Sqrt 性能会比较差
            if (Math.Abs(ap.Length + bp.Length - ab.Length) < epsilon)
            {
                // 相等
            }
            else
            {
                // 不相等
            }

            // 以下是另一个方法，以下方法性能比上面一个好

            // 根据点和任意线段端点连接的线段和当前线段斜率相同，同时点在两个端点中间
            // (x - x1) / (x2 - x1) = (y - y1) / (y2 - y1)
            // 因为乘法性能更高，因此计算方法可以如下
            // (x - x1) * (y2 - y1) = (y - y1) * (x2 - x1)
            // (x - x1) * (y2 - y1) - (y - y1) * (x2 - x1) = 0
            // 但是乘法的误差很大，因此还是继续使用除法
            // x1 < x < x2, assuming x1 < x2
            // y1 < y < y2, assuming y1 < y2

            // 乘法性能更高，误差大。请试试在返回 true 的时候，看看 crossProduct 的值，可以发现这个值依然很大
            var crossProduct = (point.X - line.APoint.X) * (line.BPoint.Y - line.APoint.Y) -
                               (point.Y - line.APoint.Y) * (line.BPoint.X - line.APoint.X);
            // 先判断 crossProduct 是否等于 0 可以解决 A 和 B 两个点的 Y 坐标相同或 X 坐标相同的时候，使用除法的坑
            if (crossProduct == 0 || Math.Abs((point.X - line.APoint.X) / (line.BPoint.X - line.APoint.X) - (point.Y - line.APoint.Y) / (line.BPoint.Y - line.APoint.Y)) < epsilon)
            {
                var minX = Math.Min(line.APoint.X, line.BPoint.X);
                var maxX = Math.Max(line.APoint.X, line.BPoint.X);

                var minY = Math.Min(line.APoint.Y, line.BPoint.Y);
                var maxY = Math.Max(line.APoint.Y, line.BPoint.Y);

                if (minX <= point.X && point.X <= maxX && minY <= point.Y && point.Y <= maxY)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private static bool EqualPoint(Point a, Point b, double epsilon = 0.001)
        {
            return Math.Abs(a.X - b.X) < epsilon && Math.Abs(a.Y - b.Y) < epsilon;
        }

        private void Move(FrameworkElement element, Point point)
        {
            if (element.RenderTransform is not TranslateTransform translateTransform)
            {
                element.RenderTransform = translateTransform = new TranslateTransform();
            }

            translateTransform.X = point.X - element.Width / 2;
            translateTransform.Y = point.Y - element.Height / 2;
        }

        private Point? _lastPoint;

        private Line? Line { set; get; }
    }

    public record Line
    {
        public Point APoint { get; init; }

        public Point BPoint { get; init; }
    }
}
