using System;
using dotnetCampus.OpenXMLUnitConverter;

namespace LalyearnabodaLayheryacucha
{
    /// <summary>
    /// 辅助进行椭圆点计算的类
    /// </summary>
    /// 我觉得这个类应该是框架有带，或现成的方法，但是一时间没找到
    static class EllipseCoordinateHelper
    {
        /// <summary>
        /// 计算椭圆中点坐标
        /// </summary>
        /// <param name="widthRadius"></param>
        /// <param name="heightRadius"></param>
        /// <param name="rotationAngle"></param>
        /// <returns></returns>
        public static (Pixel x, Pixel y) GetEllipseCoordinate(Pixel widthRadius, Pixel heightRadius,
            Degree rotationAngle)
        {
            // 以下为椭圆两个点的计算方法
            // 算法请看 https://astronomy.swin.edu.au/cms/astro/cosmos/E/Ellipse

            var absRotate = Math.Abs(rotationAngle.DoubleValue);
            var rad = Math.Abs(absRotate - 90);
            rad = rad * Math.PI / 180;
            var tan = Math.Tan(rad);

            var a = widthRadius.Value;
            var b = heightRadius.Value;
            var x = Math.Sqrt(1.0 / (1.0 / (a * a) + (tan * tan) / (b * b)));
            var y = x * tan;

            if (rotationAngle.DoubleValue < 0)
            {
                x = -x;
            }

            if (rotationAngle.DoubleValue > -90 && rotationAngle.DoubleValue < 90)
            {
                y = -y;
            }

            x = a + x;
            y = b + y;

            return (new Pixel(x), new Pixel(y));
        }
    }
}