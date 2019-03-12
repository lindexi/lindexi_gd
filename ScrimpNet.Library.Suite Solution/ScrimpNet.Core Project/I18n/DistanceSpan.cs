/**
/// ScrimpNet.Core Library
/// Copyright © 2005-2011
///
/// This module is Copyright © 2005-2011 Steve Powell
/// All rights reserved.
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the Microsoft Public License (Ms-PL)
/// 
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
/// details.
///
/// You should have received a copy of the Microsoft Public License (Ms-PL)
/// License along with this library; if not you may 
/// find it here: http://www.opensource.org/licenses/ms-pl.html
///
/// Steve Powell, spowell@scrimpnet.com
**/
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
namespace ScrimpNet.I18n
{
    /// <summary>
    /// Represents a length in either metric or Imperial units.  Note: By default values are in meters unless specified.  Class is modeled after .Net TimeSpan class
    /// </summary>
    /// <remarks>All parameters are assumed to be valid.  This is for performance reasons. Span is internally stored in meters</remarks>
    public struct DistanceSpan : IFormattable 
    {

        #region embedded Enumerations
        /// <summary>
        /// Used in methods to determine kind of input/output parameters
        /// </summary>
        public enum DistanceType
        {
            /// <summary>
            /// MeasurementType.Metric (1000m)
            /// </summary>
            Kilometer = 0,
            /// <summary>
            /// MeasurementType.Metric (1m)
            /// </summary>
            Meter = 1,
            /// <summary>
            /// MeasurementType.Metric (1/100m)
            /// </summary>
            Centimeter = 2,
            /// <summary>
            /// MeasurementType.Metric (1/1000m)
            /// </summary>
            Millimeter = 3,
            /// <summary>
            /// Statute mile MeasurementType.Imperial (5280ft)
            /// </summary>
            Mile = 4,
            /// <summary>
            /// MeasurementType.Imperial (3ft)
            /// </summary>
            Yard = 5,
            /// <summary>
            /// MeasurementType.Imperial (1ft)
            /// </summary>
            Foot = 6,
            /// <summary>
            /// MeasurementType.Imperial (1/12 ft)
            /// </summary>
            Inch = 7,

            /// <summary>
            /// MeasurementType.Imperial (6076ft., 2025 yards, 1852 meters)
            /// </summary>
            MileNautical = 8
        }

        /// <summary>
        /// Available for applications to track English or metric as necessary
        /// </summary>
        public enum MeasurementType
        {
            /// <summary>
            /// English measurement system.  Primarily in US.
            /// </summary>
            Imperial = 0,
            /// <summary>
            /// Metric measurement system.  Default.  Worldwide
            /// </summary>
            Metric = 1
        }
        #endregion embedded Enumerations

        #region Constructors and Misc. Static Methods
        private static readonly string[] _shortAbbrev;
        private static readonly string[] _nameSingular;
        private static readonly string[] _namePlural;
        /// <summary>
        /// Default static constructor
        /// </summary>
        static DistanceSpan()
        {
            _shortAbbrev = new string[] { "km", "m", "cm", "mm", "mi.", "yd.", "ft.", "in.","nm." };
            _nameSingular = new string[] { "kilometer", "meter", "centimeter", "millimeter", "mile", "yard", "foot", "inch", "nmile" };
            _namePlural = new string[] { "kilometers", "meters", "centimeters", "millimeters", "miles", "yards", "feet", "inches", "nmiles" };
        }

        /// <summary>
        /// Constructor.  By default values in meters unless specified
        /// </summary>
        /// <param name="meters">Meters for this span</param>
        public DistanceSpan(double meters)
        {
            _meters = meters;
            _countryCode = string.Empty;
        }

        /// <summary>
        /// Constructor to create a distance span
        /// </summary>
        /// <param name="value">Value of distance length</param>
        /// <param name="distanceType">Type of distance value represents (miles, meters, yards, etc.)</param>
        public DistanceSpan(double value, DistanceSpan.DistanceType distanceType)
        {
            _meters = ToMetric(value, distanceType);
            _countryCode = string.Empty;
        }

        /// <summary>
        /// Constructor to create a distance span(miles or kilometers)for a given country code or locale code
        /// </summary>
        /// <param name="value">Distance length to assign (miles for US, kilometers for non-US</param>
        /// <param name="localeCode">Locale (en-US) or country code (CA) for this distance</param>
        public DistanceSpan(double value, string localeCode)
        {
            _countryCode = "";
            _meters = 0;
            CountryCode = localeCode;
            if (SystemType == MeasurementType.Imperial)
            {
                _meters = ToMetric(value, DistanceType.Mile);
                CountryCode = "US";
            }
            else
                _meters = ToMetric(value, DistanceType.Kilometer);
            CountryCode = localeCode;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="span">Span to copy</param>
        public DistanceSpan(DistanceSpan span)
        {
            _meters = span._meters;
            _countryCode = span.CountryCode;
        }

        /// <summary>
        /// Returns the unit abbreviation for a particular distance type (ft, km, mi, etc.)
        /// </summary>
        /// <param name="distanceType">Distance type to get abbreviation for</param>
        /// <returns>Two character lower case abbreviation for a distance type</returns>
        public static string Unit(DistanceType distanceType)
        {
            return _shortAbbrev[(int)distanceType];
        }

        /// <summary>
        /// Returns the full unit name (in English) for a particular distance span (kilometer, feet, mile)
        /// </summary>
        /// <param name="distance">Distance value to get name for</param>
        /// <param name="distanceType">Distance type to get name for</param>
        /// <returns>Lower case full name for a distance type</returns>
        public static string Name(DistanceSpan distance, DistanceType distanceType)        
        {            
            return (Math.Abs(distance._meters)<1)?_nameSingular[(int)distanceType]:_namePlural[(int)distanceType];
        }
        /// <summary>
        /// Returns the full unit name (in English) for a particular distance time (km, ft., m)
        /// </summary>
        /// <param name="distanceType">Distance type to get abbreviation for</param>
        /// <returns>Lower case abbreviation for a distance type</returns>
        public static string Abbreviation(DistanceType distanceType)
        {
            return _shortAbbrev[(int)distanceType];
        }

        /// <summary>
        /// Determines what (if any) measurement type is available base on country code
        /// </summary>
        /// <returns>Defined measurement type.  Defaults to Metric if non defined</returns>
        public MeasurementType SystemType
        {
            get
            {
                return GetSystemType(CountryCode);
            }
        }

        /// <summary>
        /// Evaluates country code and determines if it is metric or english
        /// </summary>
        /// <param name="countryCode">Full locale code(en-US) or only country code (CA) for this distance</param>
        /// <returns>Metric or English</returns>
        public static MeasurementType GetSystemType(string countryCode)
        {
            if (String.Compare(parseCountryCode(countryCode), "US", true, CultureInfo.InvariantCulture) == 0)
            {
                return MeasurementType.Imperial;
            }
            return MeasurementType.Metric;
        }

        /// <summary>
        /// find country code in a locale string
        /// </summary>
        /// <param name="localeCode"></param>
        private static string parseCountryCode(string localeCode)
        {
            string countryCode = localeCode.Trim().ToUpperInvariant();
            countryCode = countryCode.Substring(countryCode.Length - 2, 2);
            return countryCode;
        }
        #endregion Constructors

        #region Localized Country Code
        private string _countryCode;
        /// <summary>
        /// Gets/Sets country code for this distance.  Setter may be locale (en-US) or country code (CA). NOTE: If not set then default to 'US' 
        /// if unable to determine value;
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public string CountryCode
        {
            get
            {
                if (string.IsNullOrEmpty(_countryCode) == true)
                {
                    CountryCode = "US";
                }
                return _countryCode;
            }
            set
            {
                _countryCode = parseCountryCode(value);
            }
        }



        /// <summary>
        /// Returns miles or kilometer version of this distance span based on locale
        /// </summary>
        /// <returns>Miles or Kilometers depending on value of country code</returns>
        public double LocaleValue()
        {
            return LocaleValue(CountryCode);
        }

        /// <summary>
        /// Sets country code for this span then returns kilometers or miles depending on country code setting
        /// </summary>
        /// <param name="localeCode">Full locale code(en-US) or only country code (CA) for this distance</param>
        /// <returns>Miles or kilometers depending on country code</returns>
        public double LocaleValue(string localeCode)
        {
            CountryCode = localeCode;
            if (SystemType == MeasurementType.Imperial)
                return this.TotalMiles;
            else
                return this.TotalKilometers;
        }

        /// <summary>
        /// Returns the unit of measurement (mi or km) based on current country code
        /// </summary>
        /// <returns>Two character lower cased abbreviation for localized distance unit</returns>
        public string LocaleUnits()
        {
            return LocaleUnits(CountryCode);
        }

        /// <summary>
        /// Sets country code for this distance then looks up appropriate unit of measurement (mi or km)
        /// </summary>
        /// <param name="localeCode">Full locale code(en-US) or only country code (CA) for this distance</param>
        /// <returns>Two character lower cased abbreviation for localized distance unit</returns>
        public string LocaleUnits(string localeCode)
        {
            CountryCode = localeCode;
            if (SystemType == MeasurementType.Imperial)
            {
                return DistanceSpan.Unit(DistanceType.Mile);
            }
            else
            {
                return DistanceSpan.Unit(DistanceType.Kilometer);
            }
        }

        /// <summary>
        /// Returns full name for unit of measurement (kilometer or mile) for this country code
        /// </summary>
        /// <returns>Lower cased full name of distance unit</returns>
        public string LocaleName()
        {
            return LocaleName(CountryCode);
        }

        /// <summary>
        /// Sets country code for this distance this looks up appropriate full name (kilometer or mile)
        /// </summary>
        /// <param name="localeCode">Full locale code(en-US) or only country code (CA) for this distance</param>
        /// <returns>Lower cased full name for distance unit (kilometer or mile)</returns>
        public string LocaleName(string localeCode)
        {
            CountryCode = localeCode;
            switch (SystemType)
            {
                case MeasurementType.Imperial: return DistanceSpan.Name(this,DistanceType.Mile);
                default:
                    return DistanceSpan.Name(this,DistanceType.Kilometer);
            }
        }
        #endregion Localized Country Code

        /// <summary>
        /// Returns a span that is the maximum the structure can support
        /// </summary>
        public static DistanceSpan MaxValue
        {
            get { return new DistanceSpan(GeoConstant.MAXVALUE); }
        }

        /// <summary>
        /// Returns a span that is the minimum the structure can support
        /// </summary>
        public static DistanceSpan MinValue
        {
            get { return new DistanceSpan(GeoConstant.MINVALUE); }
        }

        /// <summary>
        /// Returns a span that is zero for the structure
        /// </summary>
        public static DistanceSpan Zero
        {
            get { return new DistanceSpan(0D); }
        }


        #region Structure Segment Properties
        /// <summary>
        /// Gets/Sets total number of miles in this span; including any fractional miles
        /// </summary>
        public double TotalMiles
        {
            get
            {
                return _meters / GeoConstant.METERSINMILE;
            }
            set
            {
                _meters = DistanceSpan.ToMetric(value, DistanceType.Mile);
            }
        }
        /// <summary>
        /// Gets/Sets total number of nautical miles in this span; including any fractional miles
        /// </summary>
        public double TotalNauticalMiles
        {
            get
            {
                return _meters / GeoConstant.METERSINNMILE;
            }
            set
            {
                _meters = DistanceSpan.ToMetric(value, DistanceType.MileNautical);
            }
        }
        /// <summary>
        /// Gets/Sets total number of years in this span; including any fraction yards
        /// </summary>
        public double TotalYards
        {
            get
            {
                return _meters / GeoConstant.METERSINYARD;
            }
            set
            {
                _meters = DistanceSpan.ToMetric(value, DistanceType.Yard);
            }
        }

        /// <summary>
        /// Gets/Sets total number of feet in this span; including any fractional feet
        /// </summary>
        public double TotalFeet
        {
            get
            {
                return _meters / GeoConstant.METERSINFOOT;
            }
            set
            {
                _meters = DistanceSpan.ToMetric(value, DistanceType.Foot);
            }
        }

        /// <summary>
        /// Gets/Sets total number of inches in this span including any fractional parts
        /// </summary>
        public double TotalInches
        {
            get
            {
                return _meters / GeoConstant.METERSININCH;
            }
            set
            {
                _meters = DistanceSpan.ToMetric(value, DistanceType.Inch);
            }
        }

        /// <summary>
        /// Gets/Sets total number of kilometers in this span including any fractional parts
        /// </summary>
        public double TotalKilometers
        {
            get
            {
                return _meters / 1000D;
            }
            set
            {
                _meters = DistanceSpan.ToMetric(value, DistanceType.Kilometer);
            }
        }

        double _meters;
        /// <summary>
        /// Gets/Sets total number of meters in this span including any frational part
        /// </summary>
        public double TotalMeters
        {
            get
            {
                return _meters;
            }
            set
            {
                _meters = value;
            }
        }

        /// <summary>
        /// Gets/Sets total number of centimeters in this span including any fractional part
        /// </summary>
        public double TotalCentimeters
        {
            get
            {
                return _meters * 100D;
            }
            set
            {
                _meters = DistanceSpan.ToMetric(value, DistanceType.Centimeter);
            }
        }

        /// <summary>
        /// Gets/Sets total number of millimeters in this span including any frational part
        /// </summary>
        public double TotalMillimeters
        {
            get
            {
                return _meters * 1000D;
            }
            set
            {
                _meters = DistanceSpan.ToMetric(value, DistanceType.Millimeter);
            }
        }

        #endregion Structure Segment Properties

        #region Core Conversion Methods

        /// <summary>
        /// Convert a distance from one type to another
        /// </summary>
        /// <param name="value">Distance to convert</param>
        /// <param name="inputType">Type that value represents</param>
        /// <param name="outputType">Target type the return value represents</param>
        /// <returns>Converted distance that is of outputType</returns>
        public static double Convert(double value, DistanceSpan.DistanceType inputType, DistanceType outputType)
        {
            return FromMetric(ToMetric(value, inputType), outputType);
        }

        /// <summary>
        /// Convert a distance to meters
        /// </summary>
        /// <param name="value">Distance to convert</param>
        /// <param name="inputDistanceType">Type of distance value represents</param>
        /// <returns>Meter representation of input value</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        public static double ToMetric(double value, DistanceType inputDistanceType)
        {
            double _meters = 0D;
            switch (inputDistanceType)
            {
                case DistanceType.MileNautical: _meters = value * GeoConstant.METERSINNMILE;
                    break;
                case DistanceType.Mile: _meters = value * GeoConstant.METERSINMILE;
                    break;
                case DistanceType.Yard: _meters = value * GeoConstant.METERSINYARD;
                    break;
                case DistanceType.Inch: _meters = value * GeoConstant.METERSININCH;
                    break;
                case DistanceType.Foot: _meters = value * GeoConstant.METERSINFOOT;
                    break;
                case DistanceType.Kilometer: _meters = value * 1000D;
                    break;
                case DistanceType.Meter: _meters = value;
                    break;
                case DistanceType.Centimeter: _meters = value / 100D;
                    break;
                case DistanceType.Millimeter: _meters = value / 1000D;
                    break;
                default:
                    throw new ArgumentException("DistanceType not supported");
            }
            return _meters;
        }

        /// <summary>
        /// Convert a metric meter value into another value
        /// </summary>
        /// <param name="meters">Distance to convert</param>
        /// <param name="outputType">Type of distance return value will represent</param>
        /// <returns>Converted meters in outputType format</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        public static double FromMetric(double meters, DistanceType outputType)
        {
            double _retval = 0D;
            switch (outputType)
            {
                case DistanceType.MileNautical: _retval = meters / GeoConstant.METERSINNMILE;
                    break;
                case DistanceType.Mile: _retval = meters / GeoConstant.METERSINMILE;
                    break;
                case DistanceType.Yard: _retval = meters / GeoConstant.METERSINYARD;
                    break;
                case DistanceType.Inch: _retval = meters / GeoConstant.METERSININCH;
                    break;
                case DistanceType.Foot: _retval = meters / GeoConstant.METERSINFOOT;
                    break;
                case DistanceType.Kilometer: _retval = meters / 1000D;
                    break;
                case DistanceType.Meter: _retval = meters;
                    break;
                case DistanceType.Centimeter: _retval = meters * 100D;
                    break;
                case DistanceType.Millimeter: _retval = meters * 1000D;
                    break;
                default:
                    throw new ArgumentException("DistanceType not supported");
            }
            return _retval;
        }

        #endregion

        #region Overloaded Operators
        /// <summary>
        /// Add a meters to this span
        /// </summary>
        /// <param name="meters">Value to add.  Use negative to subtract</param>
        /// <returns>New span that is sum of current and parameter</returns>
        public DistanceSpan Add(double meters)
        {
            return new DistanceSpan(_meters + meters);
        }
        /// <summary>
        /// Add a value to this span
        /// </summary>
        /// <param name="value">Distance to add</param>
        /// <param name="distanceType">Type of distance to add (kilometers, miles, inches)</param>
        /// <returns>New span that is sum of current and parameter</returns>
        public DistanceSpan Add(double value, DistanceSpan.DistanceType distanceType)
        {
            return new DistanceSpan(_meters + ToMetric(value, distanceType));
        }

        /// <summary>
        /// Add a value to this span
        /// </summary>
        /// <param name="distance">Distance to add</param>
        /// <returns>New span that is sum of current and parameter</returns>
        public DistanceSpan Add(DistanceSpan distance)
        {
            return new DistanceSpan(_meters + distance._meters);
        }

        /// <summary>
        /// Compare two distance objects
        /// </summary>
        /// <param name="d1">Distance to compare</param>
        /// <param name="d2">Distance to compare</param>
        /// <returns>Returns 1,0,-1 depending on comparision</returns>
        public static int Compare(DistanceSpan d1, DistanceSpan d2)
        {
            if (d1._meters > d2._meters) return 1;
            if (d1._meters < d2._meters) return -1;
            return 0;
        }

        /// <summary>
        /// Compare a distance object to this object. 1 if this is greater than distance; else 0 or -1
        /// </summary>
        /// <param name="distance">Distance to compare</param>
        /// <returns>1 if this is greater than distance; else 0 or -1</returns>
        public int CompareTo(DistanceSpan distance)
        {
            return Compare(this, distance);
        }

        /// <summary>
        /// Return a new DistanceSpan that is the absolute length of this distance
        /// </summary>
        public DistanceSpan Length
        {
            get
            {
                return new DistanceSpan(Math.Abs(_meters));
            }
        }

        /// <summary>
        /// Compare two distance objects
        /// </summary>
        /// <param name="obj">Object to compare.  Must be DistanceSpan type</param>
        /// <returns>True if object equals this distance span</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        public override bool Equals(object obj)
        {
            if (obj is DistanceSpan)
            {
                return this.Equals((DistanceSpan)obj);
            }
            throw new ArgumentException("obj must be a DistanceSpan type");
        }

        /// <summary>
        /// Compare a distance span to this span
        /// </summary>
        /// <param name="distance">Distance span to compare</param>
        /// <returns>True if they represent the same internal value</returns>
        public bool Equals(DistanceSpan distance)
        {
            return Equals(this, distance);
        }

        /// <summary>
        /// Compare two distance spans
        /// </summary>
        /// <param name="d1">First distance span to compare</param>
        /// <param name="d2">Second distanc span to compare</param>
        /// <returns>True if both spans represent the same value; otherwise false</returns>
        public static bool Equals(DistanceSpan d1, DistanceSpan d2) { return (Compare(d1, d2) == 0); }

        /// <summary>
        /// Compare two distance spans
        /// </summary>
        /// <param name="objA">First distance span to compare</param>
        /// <param name="objB">Second distance span to compare</param>
        /// <returns>Ture if both spans represent the same value; otherwise false</returns>
        public new static bool Equals(object objA, object objB) { return objA.Equals(objB); }

        /// <summary>
        /// Create a new distance span from a kilometer value
        /// </summary>
        /// <param name="kilometers">Kilometers for this distance</param>
        /// <returns>New DistanceSpan</returns>
        public static DistanceSpan FromKilometers(double kilometers) { return new DistanceSpan(kilometers, DistanceType.Kilometer); }

        /// <summary>
        /// Create a new distance span from a meter value
        /// </summary>
        /// <param name="meters">Meters for this distance</param>
        /// <returns>New DistanceSpan</returns>
        public static DistanceSpan FromMeters(double meters) { return new DistanceSpan(meters); }

        /// <summary>
        /// Create a new distance span from a miles value
        /// </summary>
        /// <param name="miles">Miles for this distance</param>
        /// <returns>New DistanceSpan</returns>
        public static DistanceSpan FromMiles(double miles) { return new DistanceSpan(miles, DistanceType.Mile); }

        /// <summary>
        /// Create a new distance span from a yard value
        /// </summary>
        /// <param name="yards">Yards for this distance</param>
        /// <returns>New DistanceSpan</returns>
        public static DistanceSpan FromYards(double yards) { return new DistanceSpan(yards, DistanceType.Yard); }

        /// <summary>
        /// Create a new distance span from a feet value
        /// </summary>
        /// <param name="feet">Feet for this distance</param>
        /// <returns>New DistanceSpan</returns>
        public static DistanceSpan FromFeet(double feet) { return new DistanceSpan(feet, DistanceType.Foot); }

        /// <summary>
        /// Create a new distance span from feet and inches
        /// </summary>
        /// <param name="feet">Feet portion of distance</param>
        /// <param name="inches">Inch portion of distance</param>
        /// <returns>New DistanceSpan</returns>
        public static DistanceSpan FromFeet(double feet, double inches)
        {
            return FromFeet(feet) + FromInches(inches);
        }

        /// <summary>
        /// Create a new distance span from an inch value
        /// </summary>
        /// <param name="inches">Inches for this distance</param>
        /// <returns>New DistanceSpan</returns>
        public static DistanceSpan FromInches(double inches) { return new DistanceSpan(inches, DistanceType.Inch); }

        /// <summary>
        /// Change the sign on a Distance Span
        /// </summary>
        /// <returns>New Distance span that has an opposite sign</returns>
        public DistanceSpan Negate() { return new DistanceSpan(_meters * -1D); }

        /// <summary>
        /// Add two spans togehter
        /// </summary>
        /// <param name="d1">First distance</param>
        /// <param name="d2">Second distance</param>
        /// <returns>New DistanceSpan giving combined length</returns>
        public static DistanceSpan operator +(DistanceSpan d1, DistanceSpan d2) { return new DistanceSpan(d1._meters + d2._meters); }

        /// <summary>
        /// Compare values of two distances
        /// </summary>
        /// <param name="d1">First distance</param>
        /// <param name="d2">Second distance</param>
        /// <returns>True if d1 has same value as d2</returns>
        public static Boolean operator ==(DistanceSpan d1, DistanceSpan d2) { return d1._meters == d2._meters; }

        /// <summary>
        /// Compare values of two distances
        /// </summary>
        /// <param name="d1">First distance</param>
        /// <param name="d2">Second distance</param>
        /// <returns>True if d1 is greater than d2</returns>
        public static Boolean operator >(DistanceSpan d1, DistanceSpan d2) { return d1._meters > d2._meters; }

        /// <summary>
        /// Compare values of two distances
        /// </summary>
        /// <param name="d1">First distance</param>
        /// <param name="d2">Second distance</param>
        /// <returns>True if d1 is greater than or equal to d2</returns>
        public static Boolean operator >=(DistanceSpan d1, DistanceSpan d2) { return d1._meters >= d2._meters; }

        /// <summary>
        /// Compare values of two distances
        /// </summary>
        /// <param name="d1">First distance</param>
        /// <param name="d2">Second distance</param>
        /// <returns>True if d1 is not equal to d2</returns>
        public static Boolean operator !=(DistanceSpan d1, DistanceSpan d2) { return d1._meters != d2._meters; }

        /// <summary>
        /// Compare values of two distances
        /// </summary>
        /// <param name="d1">First distance</param>
        /// <param name="d2">Second distance</param>
        /// <returns>True if d1 is less than d2</returns>
        public static Boolean operator <(DistanceSpan d1, DistanceSpan d2) { return d1._meters < d2._meters; }

        /// <summary>
        /// Compare values of two distances
        /// </summary>
        /// <param name="d1">First distance</param>
        /// <param name="d2">Second distance</param>
        /// <returns>True if d1 is less than or equal to d2</returns>
        public static Boolean operator <=(DistanceSpan d1, DistanceSpan d2) { return d1._meters <= d2._meters; }

        /// <summary>
        /// Subtract a second distance from the first distance
        /// </summary>
        /// <param name="d1">First distance</param>
        /// <param name="d2">Second distance</param>
        /// <returns>New Distance</returns>
        public static DistanceSpan operator -(DistanceSpan d1, DistanceSpan d2) { return new DistanceSpan(d1.Subtract(d2)); }

        /// <summary>
        /// Unary minus.  Change the sign on a distance
        /// </summary>
        /// <param name="d1">Distance to change sign on</param>
        /// <returns>New distance with sign changed</returns>
        public static DistanceSpan operator -(DistanceSpan d1) { return new DistanceSpan(d1._meters * -1D); }

        /// <summary>
        /// Unaray plus.  No change.
        /// </summary>
        /// <param name="d1">DistanceSpan</param>
        /// <returns>New DistanceSpan with same value as D1</returns>
        public static DistanceSpan operator +(DistanceSpan d1) { return new DistanceSpan(d1); }


        /// <summary>
        /// Subtract a distance from this span
        /// </summary>
        /// <param name="value">Distance (meters) to subtract</param>
        /// <returns>New DistanceSpan</returns>
        public DistanceSpan Subtract(double value)
        {
            return this.Add(value * -1D);
        }

        /// <summary>
        /// Subtract a distance from this DistanceSpan
        /// </summary>
        /// <param name="value">Value to shorten DistanceSpan by</param>
        /// <param name="distanceType">Type of distance value represents (miles, feet, etc)</param>
        /// <returns>New DistanceSpan</returns>
        public DistanceSpan Subtract(double value, DistanceSpan.DistanceType distanceType)
        {
            return this.Add(value * -1D, distanceType);
        }

        /// <summary>
        /// Subtract a distance from this DistanceSpan
        /// </summary>
        /// <param name="distance">Distance to shorten this DistanceSpan by</param>
        /// <returns>New DistanceSpan</returns>
        public DistanceSpan Subtract(DistanceSpan distance)
        {
            return this.Add(distance.TotalMeters * -1D);
        }

        /// <summary>
        /// Converts the value of this class to a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(null,null);
        }

        /// <summary>
        /// Parse a string.  Input string is assumed to be meters
        /// </summary>
        /// <param name="distance">numeric formatted value</param>
        /// <returns>Converted string value</returns>
        public static DistanceSpan Parse(string distance)
        {
            return new DistanceSpan(double.Parse(distance, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Parse a string.
        /// </summary>
        /// <param name="distance">String of valid numeric characters</param>
        /// <param name="distanceType">Distance type string represents (inches, miles, kilometers)</param>
        /// <returns>New DistanceSpan</returns>
        public static DistanceSpan Parse(string distance, DistanceType distanceType)
        {
            return new DistanceSpan(double.Parse(distance, CultureInfo.InvariantCulture), distanceType);
        }

        /// <summary>
        /// Parse a string into a DistanceSpan
        /// </summary>
        /// <param name="distance">String of valid numeric characters for meters</param>
        /// <param name="result">Newly created DistanceSpan if successful</param>
        /// <returns>True if parsing was successful.</returns>
        public static bool TryParse(string distance, out DistanceSpan result)
        {
            try
            {
                result = new DistanceSpan(double.Parse(distance, CultureInfo.InvariantCulture), DistanceType.Meter);
                return true;
            }
            catch
            {
                result = DistanceSpan.Zero;
                return false;
            }
        }

        /// <summary>
        /// Parse a string into a DistanceSpan
        /// </summary>
        /// <param name="distance">String of valid numeric characters</param>
        /// <param name="distanceType">Type of distance represents (miles, feet, etc)</param>
        /// <param name="result">Newly created DistanceSpan if successful</param>
        /// <returns>True if parsing was successful.</returns>
        public static bool TryParse(string distance, DistanceSpan.DistanceType distanceType, out DistanceSpan result)
        {
            try
            {
                result = new DistanceSpan(double.Parse(distance, CultureInfo.InvariantCulture), distanceType);
            }
            catch
            {
                result = DistanceSpan.Zero;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Default hash code provider
        /// </summary>
        /// <returns>32 bit integer hash code</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion Overloaded Operators

        #region IFormattable Members

        /// <summary>
        /// Displays the value of this span.  Use one of the format prefixes to determine output value and format.  See 
        /// notes for important formatting details
        /// </summary>
        /// <param name="format">Format argument as provided in the string.Format call</param>
        /// <param name="formatProvider">Format provider as passed in the string.Format call</param>
        /// <returns>Formatted value of this span</returns>
        /// <example>
        /// <para></para>
        /// <para>Uppercase values display entire word.  Lowercase values display abbreviation</para>
        /// <para>{0:^KM} ==> 1.345 kilometers</para>
        /// <para>{0:^km} ==> 1.345 km</para>
        /// <para>{0:^MI} ==> 1.345 miles</para>
        /// <para>{0:^MI} ==> 1.345 mi</para>
        /// <para></para>
        /// <para><b>String Format Codes</b></para>
        /// <para>^(KM,km) kilometers, km</para>
        /// <para>^(M,m)   meters, m</para>
        /// <para>^(CM,cm) centimeters, m</para>
        /// <para>^(MM,mm) millimeters, mm</para>
        /// <para>^(MI,mi) miles, mi.</para>
        /// <para>^(YD,yd) yards, yd.</para>
        /// <para>^(FT,ft) feet, ft.</para>
        /// <para>^(IN,in) inches, in.</para>
        /// <para>^(NM,nm) nautical, nm.</para>
        /// <para></para>
        /// <para>^(Km) return kilometers without unit identifier (e.g. km)</para>
        /// <para>^(Cm) return centimeters without unit identifier</para>
        /// <para>^() no distance span specified but delimiter '^' optionally is.  Return meters</para>
        /// <para>^(Mm) return millimeters without unit identifier</para>
        /// <para>^(Mi) return miles without unit identifier</para>
        /// <para>^(Yd) return yards without unit identifier</para>
        /// <para>^(Ft) return feet without unit identifier</para>
        /// <para>^(In) return Inches without unit identifier</para>
        /// <para>^(Nm) return nautical miles without unit identifer</para>
        /// <para><b>Numerical Formatting</b></para>
        /// <para>You can change the default numerical formatting by supplying .Net standard numerical formatting codes in front of the abbreviation suffix</para>
        /// <para></para>
        /// <para>{0:00#.00K} to display leading zeros => 001.20 kilometers</para>
        /// </example>
        /// <remarks>Formatting codes had to be chosen not to conflict with existing numerical formatting codes</remarks>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format) == true)
            {
                return _meters.ToString(formatProvider);
            }
            
            string[] aParts = format.Split(new char[]{'^'});
            if (aParts.Length < 2)  // distance span delimiter '^' not specified so just use default numeric formatting and return meters
            {
                return string.Format(formatProvider, format, TotalMeters); //return value in meters (default)
            }
            string sFormat = "{0";
            if (string.IsNullOrEmpty(aParts[0]) == false)
                sFormat += ":" + aParts[0];
            sFormat += "}";
            switch (aParts[1])
            {
                case "KM": return string.Format(formatProvider, sFormat + " "+DistanceSpan.Name(this, DistanceType.Kilometer), TotalKilometers);
                case "km": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.Kilometer), TotalKilometers);
                case "M": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Name(this, DistanceType.Meter), TotalMeters);
                case "m": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.Meter), TotalMeters);
                case "CM": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Name(this, DistanceType.Centimeter), TotalCentimeters);
                case "cm": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.Centimeter), TotalCentimeters);
                case "MM": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Name(this, DistanceType.Millimeter), TotalMillimeters);
                case "mm": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.Millimeter), TotalMillimeters );
                case "MI": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Name(this, DistanceType.Mile), TotalMiles);
                case "mi": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.Mile), TotalMiles);
                case "YD": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Name(this, DistanceType.Yard), TotalYards);
                case "yd": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.Yard), TotalYards);
                case "FT": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Name(this, DistanceType.Foot), TotalFeet);
                case "ft": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.Foot), TotalFeet);
                case "IN": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Name(this, DistanceType.Inch), TotalInches);
                case "in": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.Inch), TotalInches);

                case "NM": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Name(this, DistanceType.MileNautical), TotalNauticalMiles);
                case "nm": return string.Format(formatProvider, sFormat + " " + DistanceSpan.Abbreviation(DistanceType.MileNautical), TotalNauticalMiles);

                case "Km": return string.Format(formatProvider, sFormat, TotalKilometers);
                case "": return string.Format(formatProvider, sFormat, TotalMeters);
                case "Cm": return string.Format(formatProvider, sFormat, TotalCentimeters);
                case "Mm": return string.Format(formatProvider, sFormat, TotalMillimeters);
                case "Mi": return string.Format(formatProvider, sFormat, TotalMiles);
                case "Yd": return string.Format(formatProvider, sFormat, TotalYards);
                case "Ft": return string.Format(formatProvider, sFormat, TotalFeet);
                case "In": return string.Format(formatProvider, sFormat, TotalInches);
                case "Nm": return string.Format(formatProvider, sFormat, TotalNauticalMiles);
            }
            throw new FormatException(string.Format("Unable to display requested format: '{0}'",format));
        }

        /// <summary>
        /// Gets/Sets value of this span in meters.  
        /// </summary>
        public double Value
        {
            get
            {
                return TotalMeters;
            }
            set
            {
                _meters = value;
            }
        }
        #endregion
    }
}
