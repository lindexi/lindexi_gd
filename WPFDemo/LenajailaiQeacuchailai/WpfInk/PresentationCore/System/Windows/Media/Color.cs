// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace System.Windows.Media
{
    /// <summary>
    /// Color
    /// The Color structure, composed of a private, synchronized ScRgb (IEC 61966-2-2) value
    /// a color context, composed of an ICC profile and the native color values.
    /// </summary>
    //[TypeConverter(typeof(ColorConverter))]
    //[Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public struct Color : IFormattable, IEquatable<Color>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        ///<summary>
        /// Color - sRgb legacy interface, assumes Rgb values are sRgb, alpha channel is linear 1.0 gamma
        ///</summary>
        public static Color FromArgb(byte a, byte r, byte g, byte b)// legacy sRGB interface, bytes are required to properly round trip
        {
            Color c1 = new Color();

            c1.scRgbColor.a = (float)a / 255.0f;
            c1.scRgbColor.r = sRgbToScRgb(r);  // note that context is undefined and thus unloaded
            c1.scRgbColor.g = sRgbToScRgb(g);
            c1.scRgbColor.b = sRgbToScRgb(b);
            c1.sRgbColor.a = a;
            c1.sRgbColor.r = ScRgbTosRgb(c1.scRgbColor.r);
            c1.sRgbColor.g = ScRgbTosRgb(c1.scRgbColor.g);
            c1.sRgbColor.b = ScRgbTosRgb(c1.scRgbColor.b);

            c1.isFromScRgb = false;

            return c1;
        }

        ///<summary>
        /// Color - sRgb legacy interface, assumes Rgb values are sRgb
        ///</summary>
        public static Color FromRgb(byte r, byte g, byte b)// legacy sRGB interface, bytes are required to properly round trip
        {
            Color c1 = Color.FromArgb(0xff, r, g, b);
            return c1;
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        ///<summary>
        /// GetHashCode
        ///</summary>
        public override int GetHashCode()
        {
            return this.scRgbColor.GetHashCode(); //^this.context.GetHashCode();
        }

        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.

            string format = isFromScRgb ? c_scRgbFormat : null;

            return ConvertToString(format, null);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal string ConvertToString(string format, IFormatProvider provider)
        {
            return string.Create(provider, stackalloc char[128], $"#{this.sRgbColor.a:X2}{this.sRgbColor.r:X2}{this.sRgbColor.g:X2}{this.sRgbColor.b:X2}");
            //if (context == null)
            //{
            //    if (format == null)
            //    {
            //        return string.Create(provider, stackalloc char[128], $"#{this.sRgbColor.a:X2}{this.sRgbColor.r:X2}{this.sRgbColor.g:X2}{this.sRgbColor.b:X2}");
            //    }
            //    else
            //    {
            //        // Helper to get the numeric list separator for a given culture.
            //        char separator = MS.Internal.TokenizerHelper.GetNumericListSeparator(provider);
            //        return string.Format(provider,
            //            $"sc#{{1:{format}}}{{0}} {{2:{format}}}{{0}} {{3:{format}}}{{0}} {{4:{format}}}",
            //            separator, scRgbColor.a, scRgbColor.r, scRgbColor.g, scRgbColor.b);
            //    }
            //}
            //else
            //{
            //    char separator = MS.Internal.TokenizerHelper.GetNumericListSeparator(provider);

            //    format = c_scRgbFormat;

            //    //First Stepmake sure that nothing that should not be escaped is escaped
            //    Uri safeUnescapedUri = new Uri(context.ProfileUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped),
            //                                        context.ProfileUri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
            //    //Second Step make sure that everything that should escaped is escaped
            //    String uriString = safeUnescapedUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);

            //    var sb = new StringBuilder();
            //    sb.AppendFormat(provider, "{0}{1} ", Parsers.s_ContextColor, uriString);
            //    sb.AppendFormat(provider,"{1:" + format + "}{0}",separator,scRgbColor.a);
            //    for (int i = 0; i < nativeColorValue.Length; ++i )
            //    {
            //        sb.AppendFormat(provider,"{0:" + format + "}",nativeColorValue[i]);
            //        if (i < nativeColorValue.Length - 1)
            //        {
            //            sb.AppendFormat(provider,"{0}",separator);
            //        }
            //    }
            //    return sb.ToString();
            //}
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        #region Public Operators
        ///<summary>
        /// Addition operator - Adds each channel of the second color to each channel of the
        /// first and returns the result
        ///</summary>
        public static Color operator +(Color color1, Color color2)
        {
            throw new NotImplementedException();

            //if (color1.context == null && color2.context == null)
            //{
            //Color c1 = FromScRgb(
            //      color1.scRgbColor.a + color2.scRgbColor.a,
            //      color1.scRgbColor.r + color2.scRgbColor.r,
            //      color1.scRgbColor.g + color2.scRgbColor.g,
            //      color1.scRgbColor.b + color2.scRgbColor.b);
            //    return c1;
            //}
            //else if (color1.context == color2.context)
            //{
            //    Color c1 = new Color { context = color1.context };

            //    c1.nativeColorValue = new float[c1.context.NumChannels];
            //    for (int i = 0; i < c1.nativeColorValue.Length; i++)
            //    {
            //        c1.nativeColorValue[i] = color1.nativeColorValue[i] + color2.nativeColorValue[i];
            //    }

            //    Color c2 = Color.FromRgb(0, 0, 0);

            //    c2.context = new ColorContext(PixelFormats.Bgra32);

            //    ColorTransform colorTransform = new ColorTransform(c1.context, c2.context);
            //    Span<float> sRGBValue = stackalloc float[3];

            //    colorTransform.Translate(c1.nativeColorValue, sRGBValue);

            //    if (sRGBValue[0] < 0.0f)
            //    {
            //        c1.sRgbColor.r = 0;
            //    }
            //    else if (sRGBValue[0] > 1.0f)
            //    {
            //        c1.sRgbColor.r = 255;
            //    }
            //    else
            //    {
            //        c1.sRgbColor.r = (byte)((sRGBValue[0] * 255.0f) + 0.5f);
            //    }

            //    if (sRGBValue[1] < 0.0f)
            //    {
            //        c1.sRgbColor.g = 0;
            //    }
            //    else if (sRGBValue[1] > 1.0f)
            //    {
            //        c1.sRgbColor.g = 255;
            //    }
            //    else
            //    {
            //        c1.sRgbColor.g = (byte)((sRGBValue[1] * 255.0f) + 0.5f);
            //    }

            //    if (sRGBValue[2] < 0.0f)
            //    {
            //        c1.sRgbColor.b = 0;
            //    }
            //    else if (sRGBValue[2] > 1.0f)
            //    {
            //        c1.sRgbColor.b = 255;
            //    }
            //    else
            //    {
            //        c1.sRgbColor.b = (byte)((sRGBValue[2] * 255.0f) + 0.5f);
            //    }

            //    c1.scRgbColor.r = sRgbToScRgb(c1.sRgbColor.r);
            //    c1.scRgbColor.g = sRgbToScRgb(c1.sRgbColor.g);
            //    c1.scRgbColor.b = sRgbToScRgb(c1.sRgbColor.b);
            //    c1.scRgbColor.a = color1.scRgbColor.a + color2.scRgbColor.a;
            //    if (c1.scRgbColor.a < 0.0f)
            //    {
            //        c1.scRgbColor.a = 0.0f;
            //        c1.sRgbColor.a = 0;
            //    }
            //    else if (c1.scRgbColor.a > 1.0f)
            //    {
            //        c1.scRgbColor.a = 1.0f;
            //        c1.sRgbColor.a = 255;
            //    }
            //    else
            //    {
            //        c1.sRgbColor.a = (byte)((c1.scRgbColor.a * 255.0f) + 0.5f);
            //    }

            //    return c1;
            //}
            //else
            //{
            //    throw new ArgumentException(SR.Format(SR.Color_ColorContextTypeMismatch, null));
            //}
        }

        /// <summary>
        /// Subtract operator - substracts each channel of the second color from each channel of the
        /// first and returns the result
        /// </summary>
        /// <param name='color1'>The minuend</param>
        /// <param name='color2'>The subtrahend</param>
        /// <returns>Returns the unclamped differnce</returns>
        public static Color operator -(Color color1, Color color2)
        {
            throw new NotImplementedException();
            //if (color1.context == null && color2.context == null)
            //{
            //    Color c1 = FromScRgb(
            //        color1.scRgbColor.a - color2.scRgbColor.a,
            //        color1.scRgbColor.r - color2.scRgbColor.r,
            //        color1.scRgbColor.g - color2.scRgbColor.g,
            //        color1.scRgbColor.b - color2.scRgbColor.b
            //        );
            //    return c1;
            //}
            //else if (color1.context == null || color2.context == null)
            //{
            //    throw new ArgumentException(SR.Format(SR.Color_ColorContextTypeMismatch, null));
            //}
            //else if (color1.context == color2.context)
            //{
            //    Color c1 = new Color { context = color1.context };

            //    c1.nativeColorValue = new float[c1.context.NumChannels];
            //    for (int i = 0; i < c1.nativeColorValue.Length; i++)
            //    {
            //        c1.nativeColorValue[i] = color1.nativeColorValue[i] - color2.nativeColorValue[i];
            //    }

            //    Color c2 = Color.FromRgb(0, 0, 0);

            //    c2.context = new ColorContext(PixelFormats.Bgra32);

            //    ColorTransform colorTransform = new ColorTransform(c1.context, c2.context);
            //    Span<float> sRGBValue = stackalloc float[3];

            //    colorTransform.Translate(c1.nativeColorValue, sRGBValue);

            //    if (sRGBValue[0] < 0.0f)
            //    {
            //        c1.sRgbColor.r = 0;
            //    }
            //    else if (sRGBValue[0] > 1.0f)
            //    {
            //        c1.sRgbColor.r = 255;
            //    }
            //    else
            //    {
            //        c1.sRgbColor.r = (byte)((sRGBValue[0] * 255.0f) + 0.5f);
            //    }

            //    if (sRGBValue[1] < 0.0f)
            //    {
            //        c1.sRgbColor.g = 0;
            //    }
            //    else if (sRGBValue[1] > 1.0f)
            //    {
            //        c1.sRgbColor.g = 255;
            //    }
            //    else
            //    {
            //        c1.sRgbColor.g = (byte)((sRGBValue[1] * 255.0f) + 0.5f);
            //    }

            //    if (sRGBValue[2] < 0.0f)
            //    {
            //        c1.sRgbColor.b = 0;
            //    }
            //    else if (sRGBValue[2] > 1.0f)
            //    {
            //        c1.sRgbColor.b = 255;
            //    }
            //    else
            //    {
            //        c1.sRgbColor.b = (byte)((sRGBValue[2] * 255.0f) + 0.5f);
            //    }

            //    c1.scRgbColor.r = sRgbToScRgb(c1.sRgbColor.r);
            //    c1.scRgbColor.g = sRgbToScRgb(c1.sRgbColor.g);
            //    c1.scRgbColor.b = sRgbToScRgb(c1.sRgbColor.b);
            //    c1.scRgbColor.a = color1.scRgbColor.a - color2.scRgbColor.a;
            //    if (c1.scRgbColor.a < 0.0f)
            //    {
            //        c1.scRgbColor.a = 0.0f;
            //        c1.sRgbColor.a = 0;
            //    }
            //    else if (c1.scRgbColor.a > 1.0f)
            //    {
            //        c1.scRgbColor.a = 1.0f;
            //        c1.sRgbColor.a = 255;
            //    }
            //    else
            //    {
            //        c1.sRgbColor.a = (byte)((c1.scRgbColor.a * 255.0f) + 0.5f);
            //    }

            //    return c1;
            //}
            //else
            //{
            //    throw new ArgumentException(SR.Format(SR.Color_ColorContextTypeMismatch, null));
            //}
        }

        /// <summary>
        /// Multiplication operator - Multiplies each channel of the color by a coefficient and returns the result
        /// </summary>
        /// <param name='color'>The color</param>
        /// <param name='coefficient'>The coefficient</param>
        /// <returns>Returns the unclamped product</returns>
        public static Color operator *(Color color, float coefficient)
        {
            throw new NotImplementedException();
            //Color c1 = FromScRgb(color.scRgbColor.a * coefficient, color.scRgbColor.r * coefficient, color.scRgbColor.g * coefficient, color.scRgbColor.b * coefficient);

            //if (color.context == null)
            //{
            //    return c1;
            //}
            //else
            //{
            //    c1.context = color.context;

            //    c1.ComputeNativeValues(c1.context.NumChannels);
            //}

            //return c1;
        }

        /// <summary>
        /// Compares two colors for exact equality.  Note that float values can acquire error
        /// when operated upon, such that an exact comparison between two values which are logically
        /// equal may fail. see cref="AreClose" for a "fuzzy" version of this comparison.
        /// </summary>
        /// <param name='color'>The color to compare to "this"</param>
        /// <returns>Whether or not the two colors are equal</returns>
        public bool Equals(Color color)
        {
            return this == color;
        }

        /// <summary>
        /// Compares two colors for exact equality.  Note that float values can acquire error
        /// when operated upon, such that an exact comparison between two vEquals(color);alues which are logically
        /// equal may fail. see cref="AreClose" for a "fuzzy" version of this comparison.
        /// </summary>
        /// <param name='o'>The object to compare to "this"</param>
        /// <returns>Whether or not the two colors are equal</returns>
        public override bool Equals(object o)
        {
            if (o is Color color)
            {
                return (this == color);
            }
            else
            {
                return false;
            }
        }

       ///<summary>
        /// IsEqual operator - Compares two colors for exact equality.  Note that float values can acquire error
        /// when operated upon, such that an exact comparison between two values which are logically
        /// equal may fail. see cref="AreClose".
        ///</summary>
        public static bool operator ==(Color color1, Color color2)
        {
            if (color1.scRgbColor.r != color2.scRgbColor.r)
            {
                return false;
            }

            if (color1.scRgbColor.g != color2.scRgbColor.g)
            {
                return false;
            }

            if (color1.scRgbColor.b != color2.scRgbColor.b)
            {
                return false;
            }

            if (color1.scRgbColor.a != color2.scRgbColor.a)
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// !=
        ///</summary>
        public static bool operator !=(Color color1, Color color2)
        {
            return (!(color1 == color2));
        }
        #endregion Public Operators

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        ///<summary>
        /// A
        ///</summary>
        public byte A
        {
            get
            {
                return sRgbColor.a;
            }
            set
            {
                scRgbColor.a = (float)value / 255.0f;
                sRgbColor.a = value;
            }
        }

        /// <value>The Red channel as a byte whose range is [0..255].
        /// the value is not allowed to be out of range</value>
        /// <summary>
        /// R
        /// </summary>
        public byte R
        {
            get
            {
                return sRgbColor.r;
            }
            set
            {
                scRgbColor.r = sRgbToScRgb(value);
                sRgbColor.r = value;
            }
        }

        ///<value>The Green channel as a byte whose range is [0..255].
        /// the value is not allowed to be out of range</value><summary>
        /// G
        ///</summary>
        public byte G
        {
            get
            {
                return sRgbColor.g;
            }
            set
            {
                scRgbColor.g = sRgbToScRgb(value);
                sRgbColor.g = value;
            }
        }

        ///<value>The Blue channel as a byte whose range is [0..255].
        /// the value is not allowed to be out of range</value><summary>
        /// B
        ///</summary>
        public byte B
        {
            get
            {
                return sRgbColor.b;
            }
            set
            {
                scRgbColor.b = sRgbToScRgb(value);
                sRgbColor.b = value;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        ///<summary>
        /// private helper function to set context values from a color value with a set context and ScRgb values
        ///</summary>
        private static float sRgbToScRgb(byte bval)
        {
            float val = ((float)bval / 255.0f);

            if (!(val > 0.0))       // Handles NaN case too. (Though, NaN isn't actually
                                    // possible in this case.)
            {
                return (0.0f);
            }
            else if (val <= 0.04045)
            {
                return (val / 12.92f);
            }
            else if (val < 1.0f)
            {
                return (float)Math.Pow(((double)val + 0.055) / 1.055, 2.4);
            }
            else
            {
                return (1.0f);
            }
        }

        ///<summary>
        /// private helper function to set context values from a color value with a set context and ScRgb values
        ///</summary>
        ///
        private static byte ScRgbTosRgb(float val)
        {
            if (!(val > 0.0))       // Handles NaN case too
            {
                return (0);
            }
            else if (val <= 0.0031308)
            {
                return ((byte)((255.0f * val * 12.92f) + 0.5f));
            }
            else if (val < 1.0)
            {
                return ((byte)((255.0f * ((1.055f * (float)Math.Pow((double)val, (1.0 / 2.4))) - 0.055f)) + 0.5f));
            }
            else
            {
                return (255);
            }
        }

        //private void ComputeNativeValues(int numChannels)
        //{
        //    this.nativeColorValue = new float[numChannels];
        //    if (this.nativeColorValue.Length > 0)
        //    {
        //        Span<float> sRGBValue = [this.sRgbColor.r / 255.0f, this.sRgbColor.g / 255.0f, this.sRgbColor.b / 255.0f];

        //        ColorTransform colorTransform = new ColorTransform(this.context, new ColorContext(PixelFormats.Bgra32));
        //        colorTransform.Translate(sRGBValue, this.nativeColorValue);
        //    }
        //}

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private struct MILColorF // this structure is the "milrendertypes.h" structure and should be identical for performance
        {
            public float a, r, g, b;

            public override int GetHashCode()
            {
                return a.GetHashCode() ^ r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }
        };

        private MILColorF scRgbColor;

        private struct MILColor
        {
            public byte a, r, g, b;
        }

        private MILColor sRgbColor;

        private float[] nativeColorValue;

        private bool isFromScRgb;

        private const string c_scRgbFormat = "R";

        #endregion Private Fields
    }
}
