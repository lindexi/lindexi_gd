using System;
using System.Windows;

namespace LeajemhurhoCaiwhemqurhahawwhaw
{
    class 旋转矩形
    {
        public 旋转矩形(Point a, Point b, Point c, Point d)
        {
            /*
             *A    B
             *------
             *|    |
             *|    |
             *------
             *C    D
             */
            // 顺序需要是一个逆时针，因此就是 A C D B 的传入

            Polygon = new Point[4]
            {
                a, c, d, b
            };

            A = a;
            B = b;
            C = c;
            D = d;
        }

        public static 旋转矩形 Create旋转矩形(Point position, double width, double height, double rotation)
        {
            var w = width;
            var h = height;

            var ax = -w / 2;
            var ay = -h / 2;
            var bx = w / 2;
            var by = ay;
            var cx = ax;
            var cy = h / 2;
            var dx = bx;
            var dy = cy;

            var a = 已知未旋转的相对矩形中心点的坐标求旋转后的相对于零点的坐标(ax, ay, position, rotation);
            var b = 已知未旋转的相对矩形中心点的坐标求旋转后的相对于零点的坐标(bx, by, position, rotation);
            var c = 已知未旋转的相对矩形中心点的坐标求旋转后的相对于零点的坐标(cx, cy, position, rotation);
            var d = 已知未旋转的相对矩形中心点的坐标求旋转后的相对于零点的坐标(dx, dy, position, rotation);
            return new 旋转矩形(a, b, c, d);
        }

        /// <summary>
        /// 根据未旋转的相对圆角矩形 中心点 的坐标计算旋转后的相对于零点的坐标。
        /// </summary>
        /// <returns>旋转后的相对于零点的坐标</returns>
        private static Point 已知未旋转的相对矩形中心点的坐标求旋转后的相对于零点的坐标(double x, double y, Point position, double 旋转角度弧度)
        {
            var x0 = position.X;
            var y0 = position.Y;
            var θ = 旋转角度弧度;
            return new Point(x * Math.Cos(θ) - y * Math.Sin(θ) + x0, x * Math.Sin(θ) + y * Math.Cos(θ) + y0);
        }

        public Point A { get; }
        public Point B { get; }
        public Point C { get; }
        public Point D { get; }

        public Point[] Polygon { get; }

        public bool Contains(Point point)
        {
            // https://math.stackexchange.com/a/190373/440577
            // (0<AM⋅AB<AB⋅AB)∧(0<AM⋅AC<AC⋅AC)
            var am = A - point;
            var ab = A - B;
            var am_ab = am * ab;
            var ab_ab = ab * ab;

            var ac = A - C;
            var am_ac = am * ac;
            var ac_ac = ac * ac;

            if (am_ab > 0 && am_ab < ab_ab /*(0<AM⋅AB<AB⋅AB)*/
                          && am_ac > 0 && am_ac < ac_ac)
            {
                return true;
            }

            return false;
        }
    }
}