// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using WpfInk.PresentationCore.System.Windows.Ink;

namespace WpfInk.PresentationCore.System.Windows.Input.Stylus
{
    /// <summary>
    /// Represents a single sampling point from a stylus input device
    /// </summary>
    internal struct StylusPoint : IEquatable<StylusPoint>
    {
        internal const float DefaultPressure = 0.5f;


        private double _x;
        private double _y;
        private float _pressureFactor;

        #region Constructors
        /// <summary>
        /// StylusPoint
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        public StylusPoint(double x, double y)
            : this(x, y, DefaultPressure, null, null, false, false)
        {
        }

        /// <summary>
        /// StylusPoint
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="pressureFactor">pressureFactor</param>
        public StylusPoint(double x, double y, float pressureFactor)
            : this(x, y, pressureFactor, null, null, false, true)
        {
        }


        /// <summary>
        /// StylusPoint
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="pressureFactor">pressureFactor</param>
        /// <param name="stylusPointDescription">stylusPointDescription</param>
        /// <param name="additionalValues">additionalValues</param>
        public StylusPoint(double x, double y, float pressureFactor, StylusPointDescription stylusPointDescription, int[] additionalValues)
            : this(x, y, pressureFactor, stylusPointDescription, additionalValues, true, true)
        {
        }

        /// <summary>
        /// internal ctor
        /// </summary>
        internal StylusPoint(
            double x,
            double y,
            float pressureFactor,
            StylusPointDescription stylusPointDescription,
            int[] additionalValues,
            bool validateAdditionalData,
            bool validatePressureFactor)
        {
            if (Double.IsNaN(x))
            {
                throw new ArgumentOutOfRangeException(nameof(x), SR.InvalidStylusPointXYNaN);
            }
            if (Double.IsNaN(y))
            {
                throw new ArgumentOutOfRangeException(nameof(y), SR.InvalidStylusPointXYNaN);
            }


            //we don't validate pressure when called by StylusPointDescription.Reformat
            if (validatePressureFactor &&
                (pressureFactor == Single.NaN || pressureFactor < 0.0f || pressureFactor > 1.0f))
            {
                throw new ArgumentOutOfRangeException(nameof(pressureFactor), SR.InvalidPressureValue);
            }
            //
            // only accept values between MaxXY and MinXY
            // we don't throw when passed a value outside of that range, we just silently trunctate
            //
            _x = GetClampedXYValue(x);
            _y = GetClampedXYValue(y);
            _pressureFactor = pressureFactor;

            if (validateAdditionalData)
            {
                //
                // called from the public verbose ctor
                //
                ArgumentNullException.ThrowIfNull(stylusPointDescription);

                //
                // additionalValues can be null if PropertyCount == 3 (X, Y, P)
                //
                if (stylusPointDescription.PropertyCount > StylusPointDescription.RequiredCountOfProperties)
                {
                    ArgumentNullException.ThrowIfNull(additionalValues);
                }

            }
        }



        #endregion Constructors

        /// <summary>
        /// The Maximum X or Y value supported for backwards compatibility with previous inking platforms
        /// </summary>
        public static readonly double MaxXY = 81164736.28346430d;

        /// <summary>
        /// The Minimum X or Y value supported for backwards compatibility with previous inking platforms
        /// </summary>
        public static readonly double MinXY = -81164736.32125960d;

        /// <summary>
        /// X
        /// </summary>
        public double X
        {
            get { return _x; }
            set
            {
                if (Double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException("X", SR.InvalidStylusPointXYNaN);
                }
                //
                // only accept values between MaxXY and MinXY
                // we don't throw when passed a value outside of that range, we just silently trunctate
                //
                _x = GetClampedXYValue(value);
            }
        }

        /// <summary>
        /// Y
        /// </summary>
        public double Y
        {
            get { return _y; }
            set
            {
                if (Double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException("Y", SR.InvalidStylusPointXYNaN);
                }
                //
                // only accept values between MaxXY and MinXY
                // we don't throw when passed a value outside of that range, we just silently trunctate
                //
                _y = GetClampedXYValue(value);
            }
        }

        /// <summary>
        /// PressureFactor.  A value between 0.0 (no pressure) and 1.0 (max pressure)
        /// </summary>
        public float PressureFactor
        {
            get
            {
                //
                // note that pressure can be stored a > 1 or < 0. 
                // we need to clamp if this is the case
                //
                if (_pressureFactor > 1.0f)
                {
                    return 1.0f;
                }
                if (_pressureFactor < 0.0f)
                {
                    return 0.0f;
                }
                return _pressureFactor;
            }
            set
            {
                if (value < 0.0f || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException("PressureFactor", SR.InvalidPressureValue);
                }
                _pressureFactor = value;
            }
        }


        /// <summary>
        /// Provides read access to all stylus properties
        /// </summary>
        /// <param name="stylusPointProperty">The StylusPointPropertyIds of the property to retrieve</param>
        public int GetPropertyValue(StylusPointProperty stylusPointProperty)
        {
            ArgumentNullException.ThrowIfNull(stylusPointProperty);
            if (stylusPointProperty.Id == StylusPointPropertyIds.X)
            {
                return (int) _x;
            }
            else if (stylusPointProperty.Id == StylusPointPropertyIds.Y)
            {
                return (int) _y;
            }
            else if (stylusPointProperty.Id == StylusPointPropertyIds.NormalPressure)
            {
                //StylusPointPropertyInfo info =
                //    this.Description.GetPropertyInfo(StylusPointProperties.NormalPressure);

                //int max = info.Maximum;
                return (int) _pressureFactor * 1024;
            }
            else
            {
                throw new ArgumentException(SR.InvalidStylusPointProperty, nameof(stylusPointProperty));
            }
        }

        /// <summary>
        /// Explicit cast converter between StylusPoint and Point
        /// </summary>
        /// <param name="stylusPoint">stylusPoint</param>
        public static explicit operator Point(StylusPoint stylusPoint)
        {
            return new Point(stylusPoint.X, stylusPoint.Y);
        }

        /// <summary>
        /// Allows languages that don't support operator overloading
        /// to convert to a point
        /// </summary>
        public Point ToPoint()
        {
            return new Point(this.X, this.Y);
        }


        /// <summary>
        /// Compares two StylusPoint instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// Descriptions must match for equality to succeed and additional values must match
        /// </summary>
        /// <returns>
        /// bool - true if the two Stylus instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='stylusPoint1'>The first StylusPoint to compare</param>
        /// <param name='stylusPoint2'>The second StylusPoint to compare</param>
        public static bool operator ==(StylusPoint stylusPoint1, StylusPoint stylusPoint2)
        {
            return StylusPoint.Equals(stylusPoint1, stylusPoint2);
        }

        /// <summary>
        /// Compares two StylusPoint instances for exact inequality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Stylus instances are exactly inequal, false otherwise
        /// </returns>
        /// <param name='stylusPoint1'>The first StylusPoint to compare</param>
        /// <param name='stylusPoint2'>The second StylusPoint to compare</param>
        public static bool operator !=(StylusPoint stylusPoint1, StylusPoint stylusPoint2)
        {
            return !StylusPoint.Equals(stylusPoint1, stylusPoint2);
        }

        /// <summary>
        /// Compares two StylusPoint instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// Descriptions must match for equality to succeed and additional values must match
        /// </summary>
        /// <returns>
        /// bool - true if the two Stylus instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='stylusPoint1'>The first StylusPoint to compare</param>
        /// <param name='stylusPoint2'>The second StylusPoint to compare</param>
        public static bool Equals(StylusPoint stylusPoint1, StylusPoint stylusPoint2)
        {
            //
            // do the cheap comparison first
            //
            bool membersEqual =
                stylusPoint1._x == stylusPoint2._x &&
                stylusPoint1._y == stylusPoint2._y &&
                stylusPoint1._pressureFactor == stylusPoint2._pressureFactor;

            if (!membersEqual)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Compares two StylusPoint instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// Descriptions must match for equality to succeed and additional values must match
        /// </summary>
        /// <returns>
        /// bool - true if the object is an instance of StylusPoint and if it's equal to "this".
        /// </returns>
        /// <param name='o'>The object to compare to "this"</param>
        public override bool Equals(object o)
        {
            if ((null == o) || !(o is StylusPoint))
            {
                return false;
            }

            StylusPoint value = (StylusPoint) o;
            return StylusPoint.Equals(this, value);
        }

        /// <summary>
        /// Equals - compares this StylusPoint with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if "value" is equal to "this".
        /// </returns>
        /// <param name='value'>The StylusPoint to compare to "this"</param>
        public bool Equals(StylusPoint value)
        {
            return StylusPoint.Equals(this, value);
        }
        /// <summary>
        /// Returns the HashCode for this StylusPoint
        /// </summary>
        /// <returns>
        /// int - the HashCode for this StylusPoint
        /// </returns>
        public override int GetHashCode()
        {
            int hash =
                _x.GetHashCode() ^
                _y.GetHashCode() ^
                _pressureFactor.GetHashCode();

            return hash;
        }


        /// <summary>
        /// Internal helper used by SPC.Reformat to preserve the pressureFactor
        /// </summary>
        internal float GetUntruncatedPressureFactor()
        {
            return _pressureFactor;
        }

        /// <summary>
        /// Internal helper to determine if a stroke has default pressure
        /// This is used by ISF serialization to not serialize pressure
        /// </summary>
        internal bool HasDefaultPressure
        {
            get
            {
                return (_pressureFactor == DefaultPressure);
            }
        }

        /// <summary>
        /// Private helper that returns a double clamped to MaxXY or MinXY
        /// We only accept values in this range to support ISF serialization
        /// </summary>
        private static double GetClampedXYValue(double xyValue)
        {
            if (xyValue > MaxXY)
            {
                return MaxXY;
            }
            if (xyValue < MinXY)
            {
                return MinXY;
            }

            return xyValue;
        }
    }
}
