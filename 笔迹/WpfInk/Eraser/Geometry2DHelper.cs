using System.Windows;

namespace Eraser
{
    internal static partial class Geometry2DHelper
    {
        public static bool 求点是否在任意凸多边形内部算法(Point 点, Point[] 多边形的顶点集)
        {
            // 如果是 true 表示大于零方向，否则是小于零方向
            bool? sign = null;

            for (var i = 0; i < 多边形的顶点集.Length; i++)
            {
                var next = i + 1;
                if (i == 多边形的顶点集.Length - 1)
                {
                    next = 0;
                }

                var currentPoint = 多边形的顶点集[i];
                var nextPoint = 多边形的顶点集[next];

                var v = nextPoint - currentPoint;
                var p = 点;
                var vp = p - currentPoint;

                var n = Vector.CrossProduct(v, vp);
                if (n > 0)
                {
                    if (sign == null)
                    {
                        // 如果这是第一次设置，那么给定值
                        sign = true;
                    }
                    else if (sign == false)
                    {
                        // 如果原先的其他点都是小于零的方向，而这个点是大于零的方向，那么点不在内
                        return false;
                    }
                }
                else if (n < 0)
                {
                    if (sign == null)
                    {
                        sign = false;
                    }
                    else if (sign == true)
                    {
                        // 如果原先的其他点都是大于零的方向，而这个点是小于零的方向，那么就存在不相同的方向
                        return false;
                    }
                }
                else
                {
                    // n==0
                    // 可以忽略，也许点在线上
                }
            }

            return true;
        }
    }
}