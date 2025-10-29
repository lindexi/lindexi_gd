using System.Globalization;

namespace SkiaInkCore.Primitive
{
    /// <summary>
    /// Represents a size in device pixels.
    /// </summary>
    readonly struct PixelSize
    {
        /// <summary>
        /// A size representing zero
        /// </summary>
        public static readonly PixelSize Empty = new PixelSize(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelSize"/> structure.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public PixelSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        ///// <summary>
        ///// Gets the aspect ratio of the size.
        ///// </summary>
        //public double AspectRatio => (double)Width / Height;

        /// <summary>
        /// Gets the width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Checks for equality between two <see cref="PixelSize"/>s.
        /// </summary>
        /// <param name="left">The first size.</param>
        /// <param name="right">The second size.</param>
        /// <returns>True if the sizes are equal; otherwise false.</returns>
        public static bool operator ==(PixelSize left, PixelSize right)
        {
            return left.Width == right.Width && left.Height == right.Height;
        }

        /// <summary>
        /// Checks for inequality between two <see cref="Size"/>s.
        /// </summary>
        /// <param name="left">The first size.</param>
        /// <param name="right">The second size.</param>
        /// <returns>True if the sizes are unequal; otherwise false.</returns>
        public static bool operator !=(PixelSize left, PixelSize right)
        {
            return !(left == right);
        }

        public static PixelSize Parse(string s)
        {
            var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            if (parts.Count == 2)
            {
                return new PixelSize(int.Parse(parts[0]), int.Parse(parts[1]));
            }
            else
            {
                throw new FormatException("Invalid Size.");
            }
        }

        /// <summary>
        /// Checks for equality between a size and an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// True if <paramref name="obj"/> is a size that equals the current size.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is PixelSize other)
            {
                return this == other;
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for a <see cref="PixelSize"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Width.GetHashCode();
                hash = (hash * 23) + Height.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a new <see cref="PixelSize"/> with the same height and the specified width.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <returns>The new <see cref="PixelSize"/>.</returns>
        public PixelSize WithWidth(int width) => new PixelSize(width, Height);

        /// <summary>
        /// Returns a new <see cref="PixelSize"/> with the same width and the specified height.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns>The new <see cref="PixelSize"/>.</returns>
        public PixelSize WithHeight(int height) => new PixelSize(Width, height);

        /// <summary>
        /// Returns the string representation of the size.
        /// </summary>
        /// <returns>The string representation of the size.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Width, Height);
        }
    }
}
