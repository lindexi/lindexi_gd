using DotNetCampus.Numerics;
using DotNetCampus.Numerics.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Inking.Primitive;

/// <summary>
/// 带有旋转的矩形。
/// </summary>
public readonly record struct RotatedRect : ISimilarityTransformable2D<RotatedRect>
{
    /// <summary>
    /// 创建一个带有旋转的矩形。
    /// </summary>
    /// <param name="location">矩形所在的位置。UI 上表示为未旋转前矩形的左上角。</param>
    /// <param name="size">矩形的大小。</param>
    /// <param name="rotate">矩形的旋转角度。旋转的中心点为 <paramref name="location" />。</param>
    public RotatedRect(Point2D location, Size2D size, AngularMeasure rotate)
    {
        Location = location;
        Size = size;
        Rotate = rotate;
    }

    /// <summary>
    /// 矩形所在的位置。UI 上表示为未旋转前矩形的左上角。
    /// </summary>
    public Point2D Location { get; }

    /// <summary>
    /// 矩形的大小。
    /// </summary>
    public Size2D Size { get; }

    /// <summary>
    /// 矩形的旋转角度。旋转的中心点为 <see cref="Location" />。
    /// </summary>
    public AngularMeasure Rotate { get; }

    /// <inheritdoc />
    public RotatedRect Transform(SimilarityTransformation2D transformation)
    {
        var newLocation = transformation.Transform(Location);
        var newRotate = Rotate + transformation.Rotation;
        if (transformation.IsYScaleNegative)
        {
            newLocation -= transformation.Scaling * Size.Height * newRotate.UnitVector.NormalVector;
        }

        var newSize = new Size2D(Size.Width * transformation.Scaling, Size.Height * transformation.Scaling);
        return new RotatedRect(newLocation, newSize, newRotate.Normalized);
    }

    /// <summary>
    /// 判断指定的点是否在矩形内。这里考虑了矩形的旋转。
    /// </summary>
    /// <param name="point">要判断的点。</param>
    /// <returns></returns>
    public bool Contains(Point2D point)
    {
        var transformation = AffineTransformation2D.Identity.RotateAt(-Rotate, Location);
        var transformedPoint = transformation.Transform(point);
        return transformedPoint.X >= Location.X && transformedPoint.X <= Location.X + Size.Width &&
               transformedPoint.Y >= Location.Y && transformedPoint.Y <= Location.Y + Size.Height;
    }

    /// <summary>
    /// 过滤掉不在矩形内的点。这里考虑了矩形的旋转。
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public IEnumerable<Point2D> FilterContained(IEnumerable<Point2D> points)
    {
        var transformation = AffineTransformation2D.Identity.RotateAt(-Rotate, Location).Translate(-Location.ToVector());
        foreach (var point in points)
        {
            var transformedPoint = transformation.Transform(point);
            if (transformedPoint.X >= 0 && transformedPoint.X <= Size.Width && transformedPoint.Y >= 0 && transformedPoint.Y <= Size.Height)
            {
                yield return point;
            }
        }
    }

    /// <summary>
    /// 获取矩形的包围盒。这里考虑了矩形的旋转。
    /// </summary>
    /// <returns></returns>
    public BoundingBox2D GetBoundingBox()
    {
        var transformation = AffineTransformation2D.Identity.RotateAt(Rotate, Location);
        var points = new[]
        {
            Location,
            Location + new Vector2D(Size.Width, 0),
            Location + new Vector2D(Size.Width, Size.Height),
            Location + new Vector2D(0, Size.Height),
        };

        for (var index = 0; index < points.Length; index++)
        {
            points[index] = transformation.Transform(points[index]);
        }

        return BoundingBox2D.Create(points);
    }

    public RotatedRect FlipAtBottom()
    {
        var widthUnitVector = Rotate.UnitVector;
        var heightUnitVector = Rotate.UnitVector.NormalVector;
        var newLocation = Location + Size.Width * widthUnitVector + 2 * Size.Height * heightUnitVector;
        return new RotatedRect(newLocation, Size, (Rotate + AngularMeasure.Pi).Normalized);
    }

    /// <summary>
    /// 是否与另一个带旋转的矩形相交。
    /// </summary>
    /// <param name="other">另一个带旋转的矩形。</param>
    /// <returns>是否相交。</returns>
    public bool Intersects(RotatedRect other)
    {
        var widthUnitVector1 = Rotate.UnitVector;
        var heightUnitVector1 = Rotate.UnitVector.NormalVector;
        var widthUnitVector2 = other.Rotate.UnitVector;
        var heightUnitVector2 = other.Rotate.UnitVector.NormalVector;
        var axisUnitVectors = new[]
        {
            widthUnitVector1,
            heightUnitVector1,
            widthUnitVector2,
            heightUnitVector2,
        };

        var vectors1 = new[]
        {
            Location.ToVector(),
            Location.ToVector() + Size.Width * widthUnitVector1,
            Location.ToVector() + Size.Width * widthUnitVector1 + Size.Height * heightUnitVector1,
            Location.ToVector() + Size.Height * heightUnitVector1,
        };
        var vectors2 = new[]
        {
            other.Location.ToVector(),
            other.Location.ToVector() + other.Size.Width * widthUnitVector2,
            other.Location.ToVector() + other.Size.Width * widthUnitVector2 + other.Size.Height * heightUnitVector2,
            other.Location.ToVector() + other.Size.Height * heightUnitVector2,
        };

        foreach (var axisUnitVector in axisUnitVectors)
        {
            var min1 = double.MaxValue;
            var max1 = double.MinValue;
            var min2 = double.MaxValue;
            var max2 = double.MinValue;
            foreach (var vector in vectors1)
            {
                var projection = axisUnitVector.Dot(vector);
                min1 = Math.Min(min1, projection);
                max1 = Math.Max(max1, projection);
            }

            foreach (var vector in vectors2)
            {
                var projection = axisUnitVector.Dot(vector);
                min2 = Math.Min(min2, projection);
                max2 = Math.Max(max2, projection);
            }

            if (max1 < min2 || max2 < min1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 是否与指定的点集相交。
    /// </summary>
    /// <param name="points">指定的点集。</param>
    /// <param name="isPolyline">点集是否视为折线。</param>
    /// <returns>是否相交。</returns>
    public bool Intersects(IEnumerable<Point2D> points, bool isPolyline)
    {
        var transformation = AffineTransformation2D.Identity.RotateAt(-Rotate, Location).Translate(-Location.ToVector());

        if (isPolyline)
        {
            Point2D? lastPoint = null;
            foreach (var point in points)
            {
                var currentPoint = transformation.Transform(point);

                if (lastPoint is not { } lastPointValue)
                {
                    if (currentPoint.X >= 0 && currentPoint.X <= Size.Width && currentPoint.Y >= 0 && currentPoint.Y <= Size.Height)
                    {
                        return true;
                    }

                    lastPoint = point;
                    continue;
                }

                var vector = currentPoint - lastPointValue;
                if (vector.LengthSquared.IsAlmostZero())
                {
                    continue;
                }

                if (!vector.X.IsAlmostEqual(0))
                {
                    var k = vector.Y / vector.X;
                    var y = currentPoint.Y - currentPoint.X * k;
                    if (y >= 0 && y <= Size.Height)
                    {
                        return true;
                    }

                    y = currentPoint.Y - (currentPoint.X - Size.Width) * k;
                    if (y >= 0 && y <= Size.Height)
                    {
                        return true;
                    }
                }

                if (!vector.Y.IsAlmostEqual(0))
                {
                    // 斜率的倒数
                    var rk = vector.X / vector.Y;
                    var x = currentPoint.X - currentPoint.Y * rk;
                    if (x >= 0 && x <= Size.Width)
                    {
                        return true;
                    }

                    x = currentPoint.X - (currentPoint.Y - Size.Height) * rk;
                    if (x >= 0 && x <= Size.Width)
                    {
                        return true;
                    }
                }

                lastPoint = point;
            }
        }
        else
        {
            foreach (var point in points)
            {
                var transformedPoint = transformation.Transform(point);
                if (transformedPoint.X >= 0 && transformedPoint.X <= Size.Width && transformedPoint.Y >= 0 && transformedPoint.Y <= Size.Height)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 在矩形的四个边上分别向外扩张形成新的矩形。
    /// </summary>
    /// <param name="widthAmount">宽度上的扩张量。</param>
    /// <param name="heightAmount">高度上的扩张量。</param>
    /// <returns>扩张后的矩形。</returns>
    public RotatedRect Inflate(double widthAmount, double heightAmount)
    {
        var widthUnitVector = Rotate.UnitVector;
        var heightUnitVector = Rotate.UnitVector.NormalVector;
        var newLocation = Location - widthAmount * widthUnitVector - heightAmount * heightUnitVector;
        var newSize = new Size2D(Size.Width + 2 * widthAmount, Size.Height + 2 * heightAmount);
        return new RotatedRect(newLocation, newSize, Rotate);
    }

    /// <summary>
    /// 在矩形的四个边上分别向外扩张形成新的矩形。
    /// </summary>
    /// <param name="amount">扩张量。</param>
    /// <returns>扩张后的矩形。</returns>
    public RotatedRect Inflate(double amount)
    {
        return Inflate(amount, amount);
    }
}