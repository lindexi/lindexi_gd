using System.Diagnostics;
using System.Globalization;
using System;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本库使用的坐标和尺寸
/// </summary>
/// Copy From https://github.com/dotnet/Microsoft.Maui.Graphics 但是修改了一些代码
[DebuggerDisplay("X={X}, Y={Y}, Width={Width}, Height={Height}")]
public struct TextRect
{
    /// <summary>
    /// 创建文本库使用的坐标和尺寸的矩形
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public TextRect(double x, double y, double width, double height) : this()
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 创建文本库使用的坐标和尺寸的矩形
    /// </summary>
    /// <param name="location"></param>
    /// <param name="textSize"></param>
    public TextRect(TextPoint location, TextSize textSize) : this(location.X, location.Y, textSize.Width, textSize.Height)
    {
    }

    /// <summary>
    /// X 坐标
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 宽度
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 创建一个 <see cref="X"/> <see cref="Y"/> <see cref="Width"/> <see cref="Height"/> 都是 0 的矩形
    /// </summary>
    public static TextRect Zero => new TextRect();

    /// <summary>
    /// 从左上和右下点创建矩形
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    /// <param name="bottom"></param>
    /// <returns></returns>
    public static TextRect FromLeftTopRightBottom(double left, double top, double right, double bottom)
    {
        return new TextRect(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// 判断相等
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(TextRect other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        return obj is TextRect rect && Equals(rect);
    }

    /// <inheritdoc />
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

    /// <summary>
    /// 判断相等
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <returns></returns>
    public static bool operator ==(TextRect rect1, TextRect rect2)
    {
        return (rect1.Location == rect2.Location) && (rect1.TextSize == rect2.TextSize);
    }

    /// <summary>
    /// 判断不相等
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <returns></returns>
    public static bool operator !=(TextRect rect1, TextRect rect2)
    {
        return !(rect1 == rect2);
    }

    /// <summary>
    /// 命中测试
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    // Hit Testing / Intersection / Union
    public bool Contains(TextRect rect)
    {
        return X <= rect.X && Right >= rect.Right && Y <= rect.Y && Bottom >= rect.Bottom;
    }

    /// <summary>
    /// 命中测试
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public bool Contains(TextPoint point)
    {
        return Contains(point.X, point.Y);
    }

    /// <summary>
    /// 命中测试
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool Contains(double x, double y)
    {
        return (x >= Left) && (x < Right) && (y >= Top) && (y < Bottom);
    }

    /// <summary>
    /// 判断相交
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public bool IntersectsWith(TextRect rect)
    {
        return !((Left >= rect.Right) || (Right <= rect.Left) || (Top >= rect.Bottom) || (Bottom <= rect.Top));
    }

    /// <summary>
    /// 和其他矩形组合
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public TextRect Union(TextRect rect)
    {
        return Union(this, rect);
    }

    /// <summary>
    /// 组合两个矩形
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <returns></returns>
    public static TextRect Union(TextRect rect1, TextRect rect2)
    {
        return FromLeftTopRightBottom(Math.Min(rect1.Left, rect2.Left), Math.Min(rect1.Top, rect2.Top),
            Math.Max(rect1.Right, rect2.Right), Math.Max(rect1.Bottom, rect2.Bottom));
    }

    /// <summary>
    /// 和其他矩形相交
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public TextRect Intersect(TextRect rect)
    {
        return Intersect(this, rect);
    }

    /// <summary>
    /// 两个矩形相交
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <returns></returns>
    public static TextRect Intersect(TextRect rect1, TextRect rect2)
    {
        double x = Math.Max(rect1.X, rect2.X);
        double y = Math.Max(rect1.Y, rect2.Y);
        double width = Math.Min(rect1.Right, rect2.Right) - x;
        double height = Math.Min(rect1.Bottom, rect2.Bottom) - y;

        if (width < 0 || height < 0)
        {
            return Zero;
        }

        return new TextRect(x, y, width, height);
    }

    /// <summary>
    /// 上方
    /// </summary>
    // Position/LineCharSize
    public double Top
    {
        get => Y;
        set => Y = value;
    }

    /// <summary>
    /// 下方
    /// </summary>
    public double Bottom
    {
        get => Y + Height;
        set => Height = value - Y;
    }

    /// <summary>
    /// 右方
    /// </summary>
    public double Right
    {
        get => X + Width;
        set => Width = value - X;
    }

    /// <summary>
    /// 左方
    /// </summary>
    public double Left
    {
        get => X;
        set => X = value;
    }

    /// <summary>
    /// 左上角的点，和 <see cref="Location"/> 相同
    /// </summary>
    public TextPoint LeftTop => Location; //new Point(Left, Top);

    /// <summary>
    /// 右下角的点
    /// </summary>
    public TextPoint RightBottom => new TextPoint(Right, Bottom);

    /// <summary>
    /// 是否一个空矩形，也就是 <see cref="Width"/> 和 <see cref="Height"/> 都是 0 的值
    /// </summary>
    public bool IsEmpty => (Width <= 0) || (Height <= 0);

    /// <summary>
    /// 尺寸
    /// </summary>
    public TextSize TextSize
    {
        get => new TextSize(Width, Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }
    
    /// <summary>
    /// 坐标
    /// </summary>
    public TextPoint Location
    {
        get => new TextPoint(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    /// <summary>
    /// 中心店
    /// </summary>
    public TextPoint Center => new TextPoint(X + (Width / 2), Y + (Height / 2));

    /// <summary>
    /// 在当前矩形基础上获取放大给定尺寸的新矩形
    /// </summary>
    /// <param name="textSize"></param>
    /// <returns></returns>
    // Inflate and Offset
    public TextRect Inflate(TextSize textSize)
    {
        return Inflate(textSize.Width, textSize.Height);
    }

    /// <summary>
    /// 在当前矩形基础上获取放大给定尺寸的新矩形
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public TextRect Inflate(double width, double height)
    {
        TextRect r = this;
        r.X -= width;
        r.Y -= height;
        r.Width += width * 2;
        r.Height += height * 2;
        return r;
    }

    /// <summary>
    /// 在当前矩形基础上偏移的新矩形
    /// </summary>
    /// <param name="dx"></param>
    /// <param name="dy"></param>
    /// <returns></returns>
    public TextRect Offset(double dx, double dy)
    {
        TextRect r = this;
        r.X += dx;
        r.Y += dy;
        return r;
    }

    /// <summary>
    /// 在当前矩形基础上偏移的新矩形
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public TextRect Offset(TextPoint distance)
    {
        return Offset(distance.X, distance.Y);
    }

    /// <summary>
    /// 对 <see cref="X"/> <see cref="Y"/> <see cref="Width"/> <see cref="Height"/> 都调用 <see cref="Math.Round(double)"/> 的新矩形
    /// </summary>
    /// <returns></returns>
    public TextRect Round()
    {
        return new TextRect(Math.Round(X), Math.Round(Y), Math.Round(Width), Math.Round(Height));
    }

    /// <summary>
    /// 解开构造
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void Deconstruct(out double x, out double y, out double width, out double height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    /// <summary>
    /// 尝试从传入字符串转换为矩形
    /// </summary>
    /// <param name="value"></param>
    /// <param name="rectangle"></param>
    /// <returns></returns>
    //public static implicit operator RectF(Rect rect) => new RectF((float) rect.X, (float) rect.Y, (float) rect.Width, (float) rect.Height);

    public static bool TryParse(string value, out TextRect rectangle)
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
                rectangle = new TextRect(x, y, w, h);
                return true;
            }
        }

        rectangle = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Format("{{X={0} Y={1} Width={2} Height={3}}}", X.ToString(CultureInfo.InvariantCulture),
            Y.ToString(CultureInfo.InvariantCulture), Width.ToString(CultureInfo.InvariantCulture),
            Height.ToString(CultureInfo.InvariantCulture));
    }
}