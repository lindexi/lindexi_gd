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

        public void Move(Point point)
            => Move(point, 半径);

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

            当前落点的圆 = new 圆(_currentPoint, 半径);
            两圆轨迹 轨迹线 = 前一个落点圆.求两圆轨迹线(当前落点的圆);
            当前轨迹线 = 轨迹线;
        }

        private 圆 当前落点的圆 { set; get; }

        public void Move(double 半径)
        {
            Point point = _currentPoint;
            Move(point, 半径);
        }

        /// <summary>
        /// 判断点是否在圆形橡皮擦里面
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool HitTest(Point point)
        {
            if (当前轨迹线 == null)
            {
                if (前一个落点圆 != null)
                {
                    return 前一个落点圆.Contains(point);
                }

                return false;
            }

            if (当前轨迹线.Contains(point))
            {
                return true;
            }

            return false;
        }

        private 两圆轨迹 当前轨迹线 { set; get; }

        public Geometry GetCurrentGeometry()
        {
            两圆轨迹 轨迹线 = 当前轨迹线;

            return new PathGeometry(new PathFigure[]
            {
                new PathFigure(轨迹线.四边形的点集[0],new PathSegment[]
                {
                    new PolyLineSegment(轨迹线.四边形的点集,true),
                }, true),
            });
        }

        private double 半径 { set; get; } = 75;

        private Point _currentPoint;

        public void SetLastPoint(Point point)
        {
            var 半径 = 75.0;

            _lastPoint = point;
            前一个落点圆 = new 圆(_lastPoint, 半径);
        }

        private 圆 前一个落点圆 { set; get; }

        private Point _lastPoint;

        private readonly FrameworkElement _eraserElement;
        private TranslateTransform EraserTranslate { get; }
    }
}