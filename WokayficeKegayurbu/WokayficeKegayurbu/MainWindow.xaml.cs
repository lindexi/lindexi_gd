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
                if (Math2DExtensions.CheckIsPointOnLineSegment(point, Line))
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

    public static class Math2DExtensions
    {
        public static bool CheckIsPointOnLineSegment(Point point, Line line, double epsilon = 0.1)
        {
            // 以下是另一个方法，以下方法性能比上面一个好

            // 根据点和任意线段端点连接的线段和当前线段斜率相同，同时点在两个端点中间
            // (x - x1) / (x2 - x1) = (y - y1) / (y2 - y1)
            // x1 < x < x2, assuming x1 < x2
            // y1 < y < y2, assuming y1 < y2
            // 但是需要额外处理 X1 == X2 和 Y1 == Y2 的计算

            var minX = Math.Min(line.APoint.X, line.BPoint.X);
            var maxX = Math.Max(line.APoint.X, line.BPoint.X);

            var minY = Math.Min(line.APoint.Y, line.BPoint.Y);
            var maxY = Math.Max(line.APoint.Y, line.BPoint.Y);

            if (!(minX <= point.X) || !(point.X <= maxX) || !(minY <= point.Y) || !(point.Y <= maxY))
            {
                return false;
            }

            // 以下处理水平和垂直线段
            if (Math.Abs(line.APoint.X - line.BPoint.X) < epsilon)
            {
                // 如果 X 坐标是相同，那么只需要判断点的 X 坐标是否相同
                // 因为在上面代码已经判断了 点的 Y 坐标是在线段两个点之内
                return Math.Abs(line.APoint.X - point.X) < epsilon || Math.Abs(line.BPoint.X - point.X) < epsilon;
            }

            if (Math.Abs(line.APoint.Y - line.BPoint.Y) < epsilon)
            {
                return Math.Abs(line.APoint.Y - point.Y) < epsilon || Math.Abs(line.BPoint.Y - point.Y) < epsilon;
            }

            if (Math.Abs((point.X - line.APoint.X) / (line.BPoint.X - line.APoint.X) - (point.Y - line.APoint.Y) / (line.BPoint.Y - line.APoint.Y)) < epsilon)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public record Line
    {
        public Point APoint { get; init; }

        public Point BPoint { get; init; }
    }
}
