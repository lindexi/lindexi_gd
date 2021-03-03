using System;
using System.Windows;

namespace Eraser
{
    class 两圆轨迹
    {
        public 两圆轨迹(圆 圆1, 圆 圆2, 线段 线段1, 线段 线段2, Point[] 四边形的点集)
        {
            this.圆1 = 圆1;
            this.圆2 = 圆2;
            this.线段1 = 线段1;
            this.线段2 = 线段2;

            if (四边形的点集.Length != 4)
            {
                throw new ArgumentException("此点集不能表示四边形");
            }

            this.四边形的点集 = 四边形的点集;
        }

        public 圆 圆1 { get; }
        public 圆 圆2 { get; }
        public 线段 线段1 { get; }
        public 线段 线段2 { get; }

        public Point[] 四边形的点集 { get; }

        public bool Contains(Point point)
        {
            // 不将条件放在一起的原因是为了方便调试，看是进入了哪个
            if (圆1.Contains(point))
            {
                return true;
            }

            if (圆2.Contains(point))
            {
                return true;
            }

            if (Geometry2DHelper.求点是否在任意凸多边形内部算法(point, 四边形的点集))
            {
                return true;
            }

            return false;
        }
    }
}