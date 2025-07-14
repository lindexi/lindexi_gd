using System.Globalization;
using System.Runtime.InteropServices;

namespace SplashScreensLib.Native;

public partial class Win32
{
    /// <summary>
    /// 在 Win32 函数使用的 Size 类，使用 int 表示数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Size : IEquatable<Size>
    {
        public Size(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public bool Equals(Size other)
        {
            return (this.Width == other.Width) && (this.Height == other.Height);
        }

        public override bool Equals(object? obj)
        {
            return obj is Size && this.Equals((Size) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            { return ((int) this.Width * 397) ^ (int) this.Height; }
        }

        public int Width;
        public int Height;

        public static bool operator ==(Size left, Size right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Size left, Size right)
        {
            return !(left == right);
        }

        public void Offset(int width, int height)
        {
            Width += width;
            Height += height;
        }

        public void Set(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            var culture = CultureInfo.CurrentCulture;
            return $"{{ Width = {Width.ToString(culture)}, Height = {Height.ToString(culture)} }}";
        }

        public bool IsEmpty => this.Width == 0 && this.Height == 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public Int32 Left; // x position of upper-left corner
        public Int32 Top; // y position of upper-left corner
        public Int32 Right; // x position of lower-right corner
        public Int32 Bottom; // y position of lower-right corner
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        /// <summary>
        ///     x coordinate of point.
        /// </summary>
        public int X;

        /// <summary>
        ///     y coordinate of point.
        /// </summary>
        public int Y;

        /// <summary>
        ///     Construct a point of coordinates (x,y).
        /// </summary>
        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public enum HResult : uint
    {
        /// <summary>
        ///     Operation successful
        /// </summary>
        S_OK = 0x00000000,

        /// <summary>
        ///     Not implemented
        /// </summary>
        E_NOTIMPL = 0x80004001,

        /// <summary>
        ///     No such interface supported
        /// </summary>
        E_NOINTERFACE = 0x80004002,

        /// <summary>
        ///     Pointer that is not valid
        /// </summary>
        E_POINTER = 0x80004003,

        /// <summary>
        ///     Operation aborted
        /// </summary>
        E_ABORT = 0x80004004,

        /// <summary>
        ///     Unspecified failure
        /// </summary>
        E_FAIL = 0x80004005,

        /// <summary>
        ///     Unexpected failure
        /// </summary>
        E_UNEXPECTED = 0x8000FFFF,

        /// <summary>
        ///     General access denied error
        /// </summary>
        E_ACCESSDENIED = 0x80070005,

        /// <summary>
        ///     Handle that is not valid
        /// </summary>
        E_HANDLE = 0x80070006,

        /// <summary>
        ///     Failed to allocate necessary memory
        /// </summary>
        E_OUTOFMEMORY = 0x8007000E,

        /// <summary>
        ///     One or more arguments are not valid
        /// </summary>
        E_INVALIDARG = 0x80070057
    }
}
