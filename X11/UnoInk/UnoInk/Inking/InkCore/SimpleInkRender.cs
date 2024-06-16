using System.Numerics;
using Microsoft.Maui.Graphics;

namespace UnoInk.Inking.InkCore
{
    /// <summary>
    /// 特别简单的笔迹渲染器。
    /// </summary>
    internal static class SimpleInkRender
    {
        private static readonly Matrix3x2 RotationPiDiv8 = Matrix3x2.CreateRotation(MathF.PI / 8);
        private static readonly Matrix3x2 RotationPiDiv4 = Matrix3x2.CreateRotation(MathF.PI / 4);
        private static readonly Matrix3x2 Rotation3PiDiv8 = Matrix3x2.CreateRotation(3 * MathF.PI / 8);

        public static Point[] GetOutlinePointList(IReadOnlyList<StylusPoint> pointList, double inkSize)
        {
            if (pointList.Count < 2)
            {
                throw new ArgumentException("小于两个点的无法应用算法");
            }

            var outlinePointList1 = new List<Point>(pointList.Count * 2);
            var outlinePointList2 = new List<Point>(pointList.Count * 2);

            for (var i = 0; i < pointList.Count; i++)
            {
                // 笔迹粗细的一半，一边用一半，合起来就是笔迹粗细了
                var halfThickness = (float)inkSize / 2;

                // 压感这里是直接乘法而已
                halfThickness *= pointList[i].Pressure;
                // 不能让笔迹粗细太小
                halfThickness = MathF.Max(0.01f, halfThickness);

                if (i == 0 || pointList[i].Point == pointList[i - 1].Point)
                {
                    if (i == pointList.Count - 1 || pointList[i].Point == pointList[i + 1].Point)
                    {
                        continue;
                    }

                    var direction = Vector2.Multiply(halfThickness, Vector2.Normalize(new Vector2((float)pointList[i + 1].Point.X - (float)pointList[i].Point.X, (float)pointList[i + 1].Point.Y - (float)pointList[i].Point.Y)));

                    var point1 = new Point(pointList[i].Point.X - direction.Y, pointList[i].Point.Y + direction.X);
                    var point2 = new Point(pointList[i].Point.X + direction.Y, pointList[i].Point.Y - direction.X);

                    if (i == 0)
                    {
                        var direction0 = -direction;
                        var direction1 = Vector2.Transform(direction0, RotationPiDiv8);
                        var direction2 = Vector2.Transform(direction0, RotationPiDiv4);
                        var direction3 = Vector2.Transform(direction0, Rotation3PiDiv8);
                        var directionN1 = new Vector2(direction3.Y, -direction3.X);
                        var directionN2 = new Vector2(direction2.Y, -direction2.X);
                        var directionN3 = new Vector2(direction1.Y, -direction1.X);

                        outlinePointList1.Add(new Point(pointList[i].Point.X + direction0.X, pointList[i].Point.Y + direction0.Y));
                        outlinePointList1.Add(new Point(pointList[i].Point.X + directionN1.X, pointList[i].Point.Y + directionN1.Y));
                        outlinePointList1.Add(new Point(pointList[i].Point.X + directionN2.X, pointList[i].Point.Y + directionN2.Y));
                        outlinePointList1.Add(new Point(pointList[i].Point.X + directionN3.X, pointList[i].Point.Y + directionN3.Y));

                        outlinePointList2.Add(new Point(pointList[i].Point.X + direction0.X, pointList[i].Point.Y + direction0.Y));
                        outlinePointList2.Add(new Point(pointList[i].Point.X + direction1.X, pointList[i].Point.Y + direction1.Y));
                        outlinePointList2.Add(new Point(pointList[i].Point.X + direction2.X, pointList[i].Point.Y + direction2.Y));
                        outlinePointList2.Add(new Point(pointList[i].Point.X + direction3.X, pointList[i].Point.Y + direction3.Y));
                    }

                    outlinePointList1.Add(point1);
                    outlinePointList2.Add(point2);
                }
                else if (i == pointList.Count - 1 || pointList[i].Point == pointList[i + 1].Point)
                {
                    var direction = Vector2.Multiply(halfThickness, Vector2.Normalize(new Vector2((float)pointList[i].Point.X - (float)pointList[i - 1].Point.X, (float)pointList[i].Point.Y - (float)pointList[i - 1].Point.Y)));

                    var point1 = new Point(pointList[i].Point.X - direction.Y, pointList[i].Point.Y + direction.X);
                    var point2 = new Point(pointList[i].Point.X + direction.Y, pointList[i].Point.Y - direction.X);

                    outlinePointList1.Add(point1);
                    outlinePointList2.Add(point2);

                    if (i == pointList.Count - 1)
                    {
                        var rotationPiDiv8 = Matrix3x2.CreateRotation(MathF.PI / 8);
                        var rotationPiDiv4 = Matrix3x2.CreateRotation(MathF.PI / 4);
                        var rotation3PiDiv8 = Matrix3x2.CreateRotation(3 * MathF.PI / 8);

                        var direction0 = direction;
                        var direction1 = Vector2.Transform(direction0, rotationPiDiv8);
                        var direction2 = Vector2.Transform(direction0, rotationPiDiv4);
                        var direction3 = Vector2.Transform(direction0, rotation3PiDiv8);
                        var directionN1 = new Vector2(direction3.Y, -direction3.X);
                        var directionN2 = new Vector2(direction2.Y, -direction2.X);
                        var directionN3 = new Vector2(direction1.Y, -direction1.X);

                        outlinePointList1.Add(new Point(pointList[i].Point.X + direction3.X, pointList[i].Point.Y + direction3.Y));
                        outlinePointList1.Add(new Point(pointList[i].Point.X + direction2.X, pointList[i].Point.Y + direction2.Y));
                        outlinePointList1.Add(new Point(pointList[i].Point.X + direction1.X, pointList[i].Point.Y + direction1.Y));
                        outlinePointList1.Add(new Point(pointList[i].Point.X + direction0.X, pointList[i].Point.Y + direction0.Y));

                        outlinePointList2.Add(new Point(pointList[i].Point.X + directionN3.X, pointList[i].Point.Y + directionN3.Y));
                        outlinePointList2.Add(new Point(pointList[i].Point.X + directionN2.X, pointList[i].Point.Y + directionN2.Y));
                        outlinePointList2.Add(new Point(pointList[i].Point.X + directionN1.X, pointList[i].Point.Y + directionN1.Y));
                        outlinePointList2.Add(new Point(pointList[i].Point.X + direction0.X, pointList[i].Point.Y + direction0.Y));
                    }
                }
                else
                {
                    var direction1 = Vector2.Multiply(halfThickness, Vector2.Normalize(new Vector2((float)pointList[i].Point.X - (float)pointList[i - 1].Point.X, (float)pointList[i].Point.Y - (float)pointList[i - 1].Point.Y)));
                    var direction2 = Vector2.Multiply(halfThickness, Vector2.Normalize(new Vector2((float)pointList[i + 1].Point.X - (float)pointList[i].Point.X, (float)pointList[i + 1].Point.Y - (float)pointList[i].Point.Y)));

                    var vector11 = new Vector2(-direction1.Y, direction1.X);
                    var vector12 = new Vector2(direction1.Y, -direction1.X);
                    var vector21 = new Vector2(-direction2.Y, direction2.X);
                    var vector22 = new Vector2(direction2.Y, -direction2.X);

                    switch (- direction1.X * direction2.Y + direction1.Y * direction2.X)
                    {
                        case < 0:
                        {
                            var vector1 = Vector2.Normalize(vector11 + vector21) * halfThickness;
                            var vector2 = Vector2.Normalize(vector12 + vector22) * halfThickness;

                            outlinePointList1.Add(new Point(pointList[i].Point.X + vector1.X, pointList[i].Point.Y + vector1.Y));
                            outlinePointList2.Add(new Point(pointList[i].Point.X + vector12.X, pointList[i].Point.Y + vector12.Y));
                            outlinePointList2.Add(new Point(pointList[i].Point.X + vector2.X, pointList[i].Point.Y + vector2.Y));
                            outlinePointList2.Add(new Point(pointList[i].Point.X + vector22.X, pointList[i].Point.Y + vector22.Y));
                            break;
                        }
                        case > 0:
                        {
                            var vector1 = Vector2.Normalize(vector11 + vector21) * halfThickness;
                            var vector2 = Vector2.Normalize(vector12 + vector22) * halfThickness;

                            outlinePointList1.Add(new Point(pointList[i].Point.X + vector11.X, pointList[i].Point.Y + vector11.Y));
                            outlinePointList1.Add(new Point(pointList[i].Point.X + vector1.X, pointList[i].Point.Y + vector1.Y));
                            outlinePointList1.Add(new Point(pointList[i].Point.X + vector21.X, pointList[i].Point.Y + vector21.Y));
                            outlinePointList2.Add(new Point(pointList[i].Point.X + vector2.X, pointList[i].Point.Y + vector2.Y));
                            break;
                        }
                        default:
                            outlinePointList1.Add(new Point(pointList[i].Point.X + vector11.X, pointList[i].Point.Y + vector11.Y));
                            outlinePointList1.Add(new Point(pointList[i].Point.X + vector21.X, pointList[i].Point.Y + vector21.Y));
                            outlinePointList2.Add(new Point(pointList[i].Point.X + vector12.X, pointList[i].Point.Y + vector12.Y));
                            outlinePointList2.Add(new Point(pointList[i].Point.X + vector22.X, pointList[i].Point.Y + vector22.Y));
                            break;
                    }
                }
            }

            var outlinePoints = new Point[outlinePointList1.Count + outlinePointList2.Count + 1];
            outlinePointList2.Reverse();
            outlinePointList1.CopyTo(outlinePoints, 0);
            outlinePointList2.CopyTo(outlinePoints, outlinePointList1.Count);
            outlinePoints[^1] = outlinePoints[0];
            return outlinePoints;
        }
    }
}