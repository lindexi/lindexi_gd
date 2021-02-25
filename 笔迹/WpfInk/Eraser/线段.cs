using System.Windows;

namespace Eraser
{
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