using System.Windows;

namespace Eraser
{
    internal class 圆
    {
        public 圆(Point 圆心, double 半径)
        {
            this.圆心 = 圆心;
            this.半径 = 半径;
        }

        public Point 圆心 { get; }
        public double 半径 { get; }

        /// <summary>
        /// 判断点是否在点在圆里面
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Point point)
        {
            var 圆角半径平方 = 半径 * 半径;
            var distance = (point - 圆心).LengthSquared;
            return distance <= 圆角半径平方;
        }

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

            return new 两圆轨迹(this, 另一个圆, 线段1, 线段2, 四边形的点集: new[]
            {
                aPoint, bPoint, cPoint, dPoint
            });
        }
    }
}