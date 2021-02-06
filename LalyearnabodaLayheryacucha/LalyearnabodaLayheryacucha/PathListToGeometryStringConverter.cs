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
                foreach (var pathData in path.ChildElements)
                {
                    ConvertToPathString(pathData, stringPath, out var isPathLine);
                    if (!isPathLine)
                    {
                        isLine = false;
                    }
                }
            }

            return (stringPath.ToString(), isLine);
        }

        private const string Comma = ",";

        private static void ConvertToPathString(OpenXmlElement pathData, StringBuilder stringPath, out bool isLine)
        {
            const string comma = Comma;
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
                        var point = PointToPixelPoint(moveToPoint);
                        PointToString(point);
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
                        var point = PointToPixelPoint(lineToPoint);
                        PointToString(point);
                    }

                    break;
                }
                case ArcTo arcTo:
                {
                    var defineKey = "A";

                    Degree rotationAngle = new Degree(0);
                    var swingAngleString = arcTo.SwingAngle;
                    if (swingAngleString != null)
                    {
                        if (int.TryParse(swingAngleString, out var swingAngle))
                        {
                            rotationAngle = new Degree(swingAngle);
                        }
                    }

                    var isLargeArcFlag = rotationAngle.DoubleValue > 180;

                    var widthRadius = EmuStringToPixel(arcTo.WidthRadius);
                    var heightRadius = EmuStringToPixel(arcTo.HeightRadius);
                    var (x, y) = EllipseCoordinateHelper.GetEllipseCoordinate(widthRadius, heightRadius, rotationAngle);

                    // 格式如下
                    // A rx ry x-axis-rotation large-arc-flag sweep-flag x y
                    // 这里 large-arc-flag 是 1 和 0 表示
                    stringPath.Append(defineKey)
                        .Append(EmuToPixelString(arcTo.WidthRadius)) //rx
                        .Append(comma)
                        .Append(EmuToPixelString(arcTo.HeightRadius)) //ry
                        .Append(comma)
                        .Append(rotationAngle.DoubleValue.ToString("0.000")) // x-axis-rotation
                        .Append(comma)
                        .Append(isLargeArcFlag ? "1" : "0") //large-arc-flag
                        .Append(comma)
                        .Append("0") // sweep-flag
                        .Append(comma)
                        .Append(PixelToString(x))
                        .Append(comma)
                        .Append(PixelToString(y));
                    break;
                }
                case QuadraticBezierCurveTo quadraticBezierCurveTo:
                {
                    var defineKey = "Q";

                    ConvertPointList(quadraticBezierCurveTo, defineKey, stringPath);

                    break;
                }
                case CubicBezierCurveTo cubicBezierCurveTo:
                {
                    var defineKey = "C";

                    ConvertPointList(cubicBezierCurveTo, defineKey, stringPath);

                    break;
                }
                case CloseShapePath closeShapePath:
                {
                    var defineKey = "Z";
                    isLine = false;
                    stringPath.Append(defineKey);
                    break;
                }
            }

            void PointToString(PixelPoint point) => PixelPointToString(point, stringPath);
        }

        static void ConvertPointList(OpenXmlCompositeElement element, string defineKey, StringBuilder stringPath)
        {
            bool isFirstPoint = true;
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

                var pixelPoint = PointToPixelPoint(point);
                PixelPointToString(pixelPoint, stringPath);
            }
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

        static PixelPoint PointToPixelPoint(Point? point)
        {
            var x = EmuStringToPixel(point?.X);
            var y = EmuStringToPixel(point?.Y);
            return new PixelPoint(x, y);
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

        static Pixel EmuStringToPixel(StringValue? emuString)
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
                return emu.ToPixel();
            }

            return default;
        }

        static string EmuToPixelString(StringValue? emuString) => EmuStringToPixelString(emuString?.Value);

        static string EmuStringToPixelString(string? emuString)
        {
            if (string.IsNullOrEmpty(emuString))
            {
                return "0";
            }

            if (int.TryParse(emuString, out var emuValue))
            {
                var emu = new Emu(emuValue);
                return PixelToString(emu.ToPixel());
            }

            // 保持数据不出错，但是如果此时的值不对了，应该这个课件是乱写的
            return "0";
        }
    }
}