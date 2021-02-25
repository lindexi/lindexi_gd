using System.Windows;
using System.Windows.Media;

namespace Eraser
{
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
            _eraserElement.Width = 半径 * 2;
            _eraserElement.Height = 半径 * 2;

            var x = point.X - 半径;
            var y = point.Y - 半径;

            //EraserEllipse.Margin = new Thickness(x, y, 0, 0);
            EraserTranslate.X = x;
            EraserTranslate.Y = y;

            _currentPoint = point;
            this.半径 = 半径;
        }

        public void Move(double 半径)
        {
            Point point = _lastPoint;
            Move(point, 半径);
        }

        public Geometry GetCurrentGeometry()
        {
            圆 圆2 = new 圆(_currentPoint, 半径);
            var 轨迹线 = 圆1.求两圆轨迹线(圆2);

            return new PathGeometry(new PathFigure[]
            {
                new PathFigure(轨迹线.线段1.A,new PathSegment[]
                {
                    new PolyLineSegment(new []{轨迹线.线段1.A,轨迹线.线段1.B,轨迹线.线段2.B,轨迹线.线段2.A},true),
                }, true),
            });
        }

        private double 半径 { set; get; }

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
}