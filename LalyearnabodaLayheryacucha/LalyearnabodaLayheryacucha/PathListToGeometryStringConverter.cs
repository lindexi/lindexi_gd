using System;
using System.Diagnostics;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using dotnetCampus.OpenXMLUnitConverter;

namespace LalyearnabodaLayheryacucha
{
    static class PathListToGeometryStringConverter
    {
        /// <summary>
        /// 构建 Path 字符串，这里输出的是 svg 路径格式，可以作为 WPF 或 UWP 的 Geometry 字符串
        /// </summary>
        /// <param name="pathList"></param>
        /// 有关 Geometry 使用字符串表示方式请自行百度 “svg 路径 格式”
        public static (string stringPath, bool isLine) BuildPathString(PathList pathList)
        {
            var stringPath = new StringBuilder(128);
            bool isLine = true;

            foreach (var path in pathList.Elements<DocumentFormat.OpenXml.Drawing.Path>())
            {
                EmuPoint currentPixelPoint = default;
                foreach (var pathData in path.ChildElements)
                {
                    currentPixelPoint =
                        ConvertToPathString(pathData, stringPath, currentPixelPoint, out var isPathLine);
                    if (!isPathLine)
                    {
                        isLine = false;
                    }
                }
            }

            return (stringPath.ToString(), isLine);
        }

        private const string Comma = ",";

        private static EmuPoint ConvertToPathString(OpenXmlElement pathData, StringBuilder stringPath,
            EmuPoint currentPoint, out bool isLine)
        {
            isLine = true;

            switch (pathData)
            {
                case MoveTo moveTo:
                {
                    // 关于定义的 Key 的值请百度参考 svg 规范
                    var defineKey = "M";
                    var moveToPoint = moveTo.Point;
                    if (moveToPoint?.X != null && moveToPoint?.Y != null)
                    {
                        stringPath.Append(defineKey);
                        var emuPoint = moveToPoint.PointToEmuPoint();
                        var point = emuPoint.ToPixelPoint();
                        PointToString(point);
                        return emuPoint;
                    }

                    break;
                }
                case LineTo lineTo:
                {
                    var defineKey = "L";

                    var lineToPoint = lineTo.Point;
                    if (lineToPoint?.X != null && lineToPoint?.Y != null)
                    {
                        stringPath.Append(defineKey);
                        var emuPoint = lineToPoint.PointToEmuPoint();
                        var point = emuPoint.ToPixelPoint();
                        PointToString(point);
                        return emuPoint;
                    }

                    break;
                }
                case ArcTo arcTo:
                {
                    Degree swingAngDegree = new Degree(0);
                    var swingAngleString = arcTo.SwingAngle;
                    if (swingAngleString != null)
                    {
                        if (int.TryParse(swingAngleString, out var swingAngle))
                        {
                            swingAngDegree = new Degree(swingAngle);
                        }
                    }

                    Degree startAngleDegree = new Degree(0);
                    var startAngleString = arcTo.StartAngle;
                    if (startAngleString != null)
                    {
                        if (int.TryParse(startAngleString.Value, out var startAngle))
                        {
                            startAngleDegree = new Degree(startAngle);
                        }
                    }

                    var widthRadius = EmuStringToEmu(arcTo.WidthRadius);
                    var heightRadius = EmuStringToEmu(arcTo.HeightRadius);

                    return ArcToToString(stringPath, currentPoint, widthRadius, heightRadius,
                        startAngleDegree, swingAngDegree);
                }
                case QuadraticBezierCurveTo quadraticBezierCurveTo:
                {
                    var defineKey = "Q";

                    return ConvertPointList(quadraticBezierCurveTo, defineKey, stringPath);
                }
                case CubicBezierCurveTo cubicBezierCurveTo:
                {
                    var defineKey = "C";

                    return ConvertPointList(cubicBezierCurveTo, defineKey, stringPath);
                }
                case CloseShapePath closeShapePath:
                {
                    var defineKey = "Z";
                    isLine = false;
                    stringPath.Append(defineKey);
                    break;
                }
            }

            return default;

            void PointToString(PixelPoint point) => PixelPointToString(point, stringPath);
        }

        private static EmuPoint ArcToToString(StringBuilder stringPath, EmuPoint currentPoint,
            Emu widthRadius,
            Emu heightRadius,
            Degree startAngleString, Degree swingAngleString)
        {
            const string comma = Comma;

            var stAng = DegreeToRadiansAngle(startAngleString);
            var swAng = DegreeToRadiansAngle(swingAngleString);

            var wR = widthRadius.Value;
            var hR = heightRadius.Value;

            var p1 = GetEllipsePoint(wR, hR, stAng);
            var p2 = GetEllipsePoint(wR, hR, stAng + swAng);
            var pt = new EmuPoint(currentPoint.X.Value - p1.X.Value + p2.X.Value,
                currentPoint.Y.Value - p1.Y.Value + p2.Y.Value);

            var isLargeArcFlag = swAng >= Math.PI;
            currentPoint = pt;

            // 格式如下
            // A rx ry x-axis-rotation large-arc-flag sweep-flag x y
            // 这里 large-arc-flag 是 1 和 0 表示
            stringPath.Append("A")
                .Append(EmuToPixelString(wR)) //rx
                .Append(comma)
                .Append(EmuToPixelString(hR)) //ry
                .Append(comma)
                .Append("0") // x-axis-rotation
                .Append(comma)
                .Append(isLargeArcFlag ? "1" : "0") //large-arc-flag
                .Append(comma)
                .Append("1") // sweep-flag
                .Append(comma)
                .Append(EmuToPixelString(pt.X))
                .Append(comma)
                .Append(EmuToPixelString(pt.Y))
                .Append(' ');
            return currentPoint;
        }

        private static EmuPoint GetEllipsePoint(double a, double b, double theta)
        {
            var aSinTheta = a * Math.Sin(theta);
            var bCosTheta = b * Math.Cos(theta);
            var circleRadius = Math.Sqrt(aSinTheta * aSinTheta + bCosTheta * bCosTheta);
            return new EmuPoint(a * bCosTheta / circleRadius, b * aSinTheta / circleRadius);
        }

        static EmuPoint ConvertPointList(OpenXmlCompositeElement element, string defineKey, StringBuilder stringPath)
        {
            bool isFirstPoint = true;
            EmuPoint lastPoint = default;
            foreach (var point in element.Elements<Point>())
            {
                if (isFirstPoint)
                {
                    isFirstPoint = false;
                    stringPath.Append(defineKey);
                }
                else
                {
                    // 同类型的点之间用空格分开
                    stringPath.Append(" ");
                }

                var emuPoint = point.PointToEmuPoint();
                PixelPoint pixelPoint = emuPoint.ToPixelPoint();
                PixelPointToString(pixelPoint, stringPath);
                lastPoint = emuPoint;
            }

            return lastPoint;
        }

        private static double DegreeToRadiansAngle(Degree x)
        {
            return x.DoubleValue * Math.PI / 180;
        }

        readonly struct EmuPoint
        {
            public EmuPoint(Emu x, Emu y)
            {
                X = x;
                Y = y;
            }

            public EmuPoint(double x, double y)
            {
                X = new Emu(x);
                Y = new Emu(y);
            }

            public Emu X { get; }
            public Emu Y { get; }
        }

        readonly struct PixelPoint
        {
            public PixelPoint(Pixel x, Pixel y)
            {
                X = x;
                Y = y;
            }

            public Pixel X { get; }
            public Pixel Y { get; }
        }

        static PixelPoint ToPixelPoint(this EmuPoint emuPoint)
        {
            return new PixelPoint(emuPoint.X.ToPixel(), emuPoint.Y.ToPixel());
        }

        static EmuPoint PointToEmuPoint(this Point? point)
        {
            var x = EmuStringToEmu(point?.X);
            var y = EmuStringToEmu(point?.Y);
            return new EmuPoint(x, y);
        }

        static Emu EmuStringToEmu(StringValue? emuString)
        {
            if (emuString == null)
            {
                return default;
            }

            if (string.IsNullOrEmpty(emuString))
            {
                return default;
            }


            if (int.TryParse(emuString, out var emuValue))
            {
                var emu = new Emu(emuValue);
                return emu;
            }

            return default;
        }

        static void PixelPointToString(PixelPoint point, StringBuilder stringPath)
        {
            stringPath.Append(PixelToString(point.X))
                .Append(Comma)
                .Append(PixelToString(point.Y));
        }

        static string PixelToString(Pixel x) =>
            // 太小了很看不到形状，丢失精度，这里的值都是采用形状的大小进行填充，所以参数都是相对大小就可以
            (x.Value * 1.000).ToString("0.000");

        private static string EmuToPixelString(double emuValue)
        {
            var emu = new Emu(emuValue);
            return EmuToPixelString(emu);
        }

        private static string EmuToPixelString(Emu emu)
        {
            return PixelToString(emu.ToPixel());
        }
    }
}