using System.Diagnostics;
using System.Globalization;
using System;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本库使用的坐标和尺寸
/// </summary>
/// Copy From https://github.com/dotnet/Microsoft.Maui.Graphics 但是修改了一些代码
[DebuggerDisplay("X={X}, Y={Y}, Width={Width}, Height={Height}")]
public struct Rect
{
    public Rect(double x, double y, double width, double height) : this()
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Rect(Point location, Size size) : this(location.X, location.Y, size.Width, size.Height)
    {
    }

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public static Rect Zero => new Rect();

    public static Rect FromLeftTopRightBottom(double left, double top, double right, double bottom)
    {
        return new Rect(left, top, right - left, bottom - top);
    }

    public bool Equals(Rect other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        return obj is Rect rect && Equals(rect);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
        //unchecked
        //{
        //    int hashCode = X.GetHashCode();
        //    hashCode = (hashCode * 397) ^ Y.GetHashCode();
        //    hashCode = (hashCode * 397) ^ Width.GetHashCode();
        //    hashCode = (hashCode * 397) ^ Height.GetHashCode();
        //    return hashCode;
        //}
    }

    public static bool operator ==(Rect rect1, Rect rect2)
    {
        return (rect1.Location == rect2.Location) && (rect1.Size == rect2.Size);
    }

    public static bool operator !=(Rect rect1, Rect rect2)
    {
        return !(rect1 == rect2);
    }

    // Hit Testing / Intersection / Union
    public bool Contains(Rect rect)
    {
        return X <= rect.X && Right >= rect.Right && Y <= rect.Y && Bottom >= rect.Bottom;
    }

    public bool Contains(Point point)
    {
        return Contains(point.X, point.Y);
    }

    public bool Contains(double x, double y)
    {
        return (x >= Left) && (x < Right) && (y >= Top) && (y < Bottom);
    }

    public bool IntersectsWith(Rect rect)
    {
        return !((Left >= rect.Right) || (Right <= rect.Left) || (Top >= rect.Bottom) || (Bottom <= rect.Top));
    }

    public Rect Union(Rect rect)
    {
        return Union(this, rect);
    }

    public static Rect Union(Rect rect1, Rect rect2)
    {
        return FromLeftTopRightBottom(Math.Min(rect1.Left, rect2.Left), Math.Min(rect1.Top, rect2.Top),
            Math.Max(rect1.Right, rect2.Right), Math.Max(rect1.Bottom, rect2.Bottom));
    }

    public Rect Intersect(Rect rect)
    {
        return Intersect(this, rect);
    }

    public static Rect Intersect(Rect rect1, Rect rect2)
    {
        double x = Math.Max(rect1.X, rect2.X);
        double y = Math.Max(rect1.Y, rect2.Y);
        double width = Math.Min(rect1.Right, rect2.Right) - x;
        double height = Math.Min(rect1.Bottom, rect2.Bottom) - y;

        if (width < 0 || height < 0)
        {
            return Zero;
        }

        return new Rect(x, y, width, height);
    }

    // Position/Size
    public double Top
    {
        get => Y;
        set => Y = value;
    }

    public double Bottom
    {
        get => Y + Height;
        set => Height = value - Y;
    }

    public double Right
    {
        get => X + Width;
        set => Width = value - X;
    }

    public double Left
    {
        get => X;
        set => X = value;
    }

    /// <summary>
    /// 左上角的点，和 <see cref="Location"/> 相同
    /// </summary>
    public Point LeftTop => Location; //new Point(Left, Top);
    public Point RightBottom => new Point(Right, Bottom);

    public bool IsEmpty => (Width <= 0) || (Height <= 0);

    public Size Size
    {
        get => new Size(Width, Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }

    public Point Location
    {
        get => new Point(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    public Point Center => new Point(X + (Width / 2), Y + (Height / 2));

    // Inflate and Offset
    public Rect Inflate(Size size)
    {
        return Inflate(size.Width, size.Height);
    }

    public Rect Inflate(double width, double height)
    {
        Rect r = this;
        r.X -= width;
        r.Y -= height;
        r.Width += width * 2;
        r.Height += height * 2;
        return r;
    }

    public Rect Offset(double dx, double dy)
    {
        Rect r = this;
        r.X += dx;
        r.Y += dy;
        return r;
    }

    public Rect Offset(Point distance)
    {
        return Offset(distance.X, distance.Y);
    }

    public Rect Round()
    {
        return new Rect(Math.Round(X), Math.Round(Y), Math.Round(Width), Math.Round(Height));
    }

    public void Deconstruct(out double x, out double y, out double width, out double height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    //public static implicit operator RectF(Rect rect) => new RectF((float) rect.X, (float) rect.Y, (float) rect.Width, (float) rect.Height);

    public static bool TryParse(string value, out Rect rectangle)
    {
        if (!string.IsNullOrEmpty(value))
        {
            string[] xywh = value.Split(',');
            if (xywh.Length == 4
                && double.TryParse(xywh[0], NumberStyles.Number, CultureInfo.InvariantCulture, out double x)
                && double.TryParse(xywh[1], NumberStyles.Number, CultureInfo.InvariantCulture, out double y)
                && double.TryParse(xywh[2], NumberStyles.Number, CultureInfo.InvariantCulture, out double w)
                && double.TryParse(xywh[3], NumberStyles.Number, CultureInfo.InvariantCulture, out double h))
            {
                rectangle = new Rect(x, y, w, h);
                return true;
            }
        }

        rectangle = default;
        return false;
    }

    public override string ToString()
    {
        return string.Format("{{X={0} Y={1} Width={2} Height={3}}}", X.ToString(CultureInfo.InvariantCulture),
            Y.ToString(CultureInfo.InvariantCulture), Width.ToString(CultureInfo.InvariantCulture),
            Height.ToString(CultureInfo.InvariantCulture));
    }
}