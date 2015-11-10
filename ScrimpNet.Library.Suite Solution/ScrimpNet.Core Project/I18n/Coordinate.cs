/**
/// ScrimpNet.Core Library
/// Copyright � 2005-2011
///
/// This module is Copyright � 2005-2011 Steve Powell
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
using System.Xml;
using System.Xml.Serialization;

namespace ScrimpNet.I18n
{
    /// <summary>
    /// Geographical ISO 6709 coordinate representing a single lat/long pair.
    /// </summary>
    /// <remarks>
    /// Validation of input paramters is not done for performance reasons.
    /// </remarks>
    public sealed class Coordinate : IFormattable ,IXmlSerializable 
    {
        /// <summary>
        /// Represents a single degree in a coordinate pair
        /// </summary>
        public sealed class Degree : IFormattable,IXmlSerializable 
        {
            /// <summary>
            /// Determines on which axis this degree represents
            /// </summary>
            public enum AxisType
            {
                /// <summary>
                /// Degree location on the sphere is not known
                /// </summary>
                Unknown = 0,
                /// <summary>
                /// Degree is on the longitudinal axis or North-South
                /// </summary>
                Longitude = 3,

                /// <summary>
                /// Degree is on the parallel or latitudinal axis
                /// </summary>
                Latitude = 2,
            }

            private AxisType _axis = AxisType.Unknown;
            /// <summary>
            /// Determines on what as
            /// </summary>
            [XmlIgnore]
            public AxisType Axis
            {
                get { return _axis; }
                set { _axis = value; }
            }

            private double _totalSeconds = 0.00D;
            /// <summary>
            /// Total seconds this degree represents including any fractional part.  Seconds is used as 
            /// base value since it is the lowest common demnominator for
            /// display output
            /// </summary>
            [XmlIgnore]
            public double TotalSeconds
            {
              get { return _totalSeconds; }
              set { _totalSeconds = value; }
            }

            /// <summary>
            /// Total minutes this degree represents including any fractional part.
            /// </summary>
            [XmlIgnore]
            public double TotalMinutes
            {
                get { return (_totalSeconds / 60D); }
            }

            /// <summary>
            /// Total degrees including any fractional part
            /// </summary>
            [XmlIgnore]
            public double TotalDegrees
            {
                get { return _totalSeconds / 3600D; }
                set { _totalSeconds = value * 3600D; }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="degrees">Create a degree from a double value</param>
            /// <param name="axisType">Type of axis (longitude/latitude) this degree is on</param>
            public Degree(double degrees, AxisType axisType)
            {
                _totalSeconds = degrees*60D*60D;
                _axis = axisType;
            }
            /// <summary>
            /// Return total number of whole degrees
            /// </summary>
            [XmlIgnore]
            public int Degrees
            {
                get
                {
                    return (int)Math.Truncate(TotalDegrees);
                }
            }
            /// <summary>
            /// Return unsigned total number of minutes within fraction of degree
            /// </summary>
            [XmlIgnore]
            public int Minutes
            {
                get
                {
                    double a = (double)Math.Abs(TotalSeconds);
                    double totalDegrees = (double)Math.Abs(Degrees);
                    double remainSeconds = (a - (totalDegrees * 3600D));
                    double remainMinutes = (remainSeconds / 60D);
                    return (int)Math.Truncate(remainMinutes);
                }
            }
            /// <summary>
            /// Return seconds within fraction of minute
            /// </summary>
            [XmlIgnore]
            public double Seconds
            {
                get
                {
                    double wholeSeconds = ((Math.Abs(Degrees) * 3600D) + (Minutes * 60D));
                    double remainSeconds = Math.Abs(TotalSeconds) - wholeSeconds;
                    return Math.Abs(remainSeconds);
                }
            }

            #region IFormattable Members
            /// <summary>
            /// Default coordinate string 켆DD.DDDD
            /// </summary>
            /// <returns>Default formatted string 켆DD.DDDD</returns>
            public override string ToString()
            {
                return ToString("D", null);
            }

            /// <summary>
            /// Convert degree into various representations including ISO 6709 formats.  See Remarks for formatting codes.
            /// </summary>
            /// <param name="format">Format code</param>
            /// <param name="formatProvider">Always null or ignored</param>
            /// <returns>Formatted degree</returns>
            /// <exception cref="FormatException">Unable to determine the format code to use.</exception>
            /// <remarks>
            /// <para>empty, G, D  켆DD.DDDD</para>
            /// <para>DM  켆DDMM.MMMM</para>
            /// <para>DMS,ISO 켆DDMMSS.SSSS</para>
            /// <para>d DDD.DDDD� NESW</para>
            /// <para>dm DDD캫M.MMMM NESW</para>
            /// <para>dms DDD캫M'SS.SSSS' NESW</para>
            /// </remarks>
            /// <exception cref="FormatException">If unable to understand format provided</exception>
            public string ToString(string format, IFormatProvider formatProvider)
            {
                string result = "";
                if (string.IsNullOrEmpty(format) == true)  //always default to some format
                    format = "D";
                double totalMinutes;
                switch (format)
                {
                    case "G":  //best practice implementation
                    case "D":
                        switch (Axis)
                        {
                            case AxisType.Latitude: result = string.Format("{0:+00.0000;+00.0000;-00.0000}", TotalDegrees);
                                break;
                            case AxisType.Longitude: result = string.Format("{0:+000.0000;+000.0000;-000.0000}", TotalDegrees);
                                break;
                            default:
                                throw new FormatException(string.Format("Unable to determine format for Axis: '{0}'",Axis.ToString()));
                        }
                        break;
                    case "DM":
                        totalMinutes = Math.Abs((TotalSeconds - (Degrees * 3600D)) / 60D);
                        switch (Axis)
                        {
                            case AxisType.Latitude: result = string.Format("{0:+00;+00;-00}{1:00.0000}", Degrees, totalMinutes );
                                break;
                            case AxisType.Longitude: result = string.Format("{0:+000;+000;-000}{1:00.0000}", Degrees, totalMinutes);
                                break;
                            default:
                                throw new FormatException(string.Format("Unable to determine format for Axis: '{0}'", Axis.ToString()));
                        }
                        break;
                    case "ISO":
                    case "DMS":
                        switch (Axis)
                        {
                            case AxisType.Latitude: result = string.Format("{0:+00;+00;-00}{1:00}{2:00.0000}", Degrees, Minutes,Seconds);
                                break;
                            case AxisType.Longitude: result = string.Format("{0:+000;+000;-000}{1:00}{2:00.0000}", Degrees, Minutes,Seconds );
                                break;
                            default:
                                throw new FormatException(string.Format("Unable to determine format for Axis: '{0}'", Axis.ToString()));
                        }
                        break;
                    case "d":
                        result = string.Format("{0:0.0000}� {1}", Math.Abs(TotalDegrees), Direction);
                        break;
                    case "dm":
                        totalMinutes = Math.Abs((TotalSeconds - (Degrees * 3600D)) / 60D);
                        result = string.Format("{0:0}�{1:00.0000}' {2}", Math.Abs(Degrees), totalMinutes, Direction);
                        break;
                    case "dms": 
                        result = string.Format("{0:0}�{1:00}'{2:0.0###}\" {3}", Math.Abs(Degrees), Minutes, Seconds,Direction);
                        break;
                    default:
                        throw new FormatException(string.Format("Unable to determine format for: '{0}'", format));
                }
                return result;
            }

            #endregion
            /// <summary>
            /// Get N E S W value for displaying on coordinate based on value of class
            /// </summary>
            [XmlIgnore]
            public string Direction
            {
                get
                {
                    switch (Axis)
                    {
                        case AxisType.Latitude: return (_totalSeconds > 0) ? "N" : "S";
                        case AxisType.Longitude: return (_totalSeconds > 0) ? "E" : "W";
                        default: return "";
                    }
                }
            }

            #region IXmlSerializable Members
            /// <summary>
            /// Get schema for this XML element.  Always returns NULL
            /// </summary>
            /// <returns>NULL</returns>
            public System.Xml.Schema.XmlSchema GetSchema()
            {
                return null;
            }

            /// <summary>
            /// Reads an &lt;coord&gt; element and parses contained value
            /// </summary>
            /// <param name="reader"></param>
            public void ReadXml(XmlReader reader)
            {                
                string str = reader.ReadElementContentAsString().Trim();
                this.TotalSeconds = Degree.Parse(str).TotalSeconds;                
            }

            /// <summary>
            /// Creates an &lt;coord&gt; element and stores contained value in ISO standard format 켆DDMMSS.SSSS
            /// </summary>
            /// <param name="writer">XML writer that element will be written to</param>
            public void WriteXml(XmlWriter writer)
            {
                writer.WriteElementString("degree", ToString("ISO",null));
                
            }

            #endregion
            /// <summary>
            /// Try to parse a string into a degree.
            /// </summary>
            /// <param name="isoDegree">Degree to parse</param>
            /// <returns>Converted degree</returns>
            /// 
            public static Degree Parse(string isoDegree)
            {
                return null;
            }
            
        } //internal class DegreeType

        
        private double _altitudeMeters;
        /// <summary>
        /// Optional altitude in meters of this coordinate.
        /// </summary>
        public double AltitudeMeters
        {
            get { return _altitudeMeters; }
            set { _altitudeMeters = value; }
        }
        private Degree _longitude;

        /// <summary>
        /// Longitude component of component
        /// </summary>
        public Degree Longitude
        {
            get { return _longitude; }
        }
        private Degree _latitude;

        /// <summary>
        /// Latitude component of coordinate
        /// </summary>
        public Degree Latitude
        {
            get { return _latitude; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="latitude">latitude of pair</param>
        /// <param name="longitude">longitude of pair</param>
        public Coordinate(Degree latitude, Degree longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
            _altitudeMeters = 0;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="latitude">latitude of pair</param>
        /// <param name="longitude">longitude of pair</param>
        public Coordinate(double latitude, double longitude)
        {
            _latitude = new Degree(latitude, Degree.AxisType.Latitude);
            _longitude = new Degree(longitude, Degree.AxisType.Longitude);
            _altitudeMeters = 0;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="coordinate">Coordinate to duplicate</param>
        public Coordinate(Coordinate coordinate)
        {
            _latitude = coordinate.Latitude;
            _longitude = coordinate.Longitude;
            _altitudeMeters = 0;
        }

        /// <summary>
        /// Calculate the distance in kilometers between to coordinates
        /// </summary>
        /// <param name="a">Start coordinate</param>
        /// <param name="b">Stop coordinate</param>
        /// <returns>Distance between two values</returns>
        public static DistanceSpan operator -(Coordinate a, Coordinate b)
        {
            return Distance(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
        }

        /// <summary>
        /// Subtract c1 from this coordinate
        /// </summary>
        /// <param name="c1">Coordinate to subtract from this one</param>
        /// <returns>Distance between two coordinates</returns>
        public DistanceSpan Subtract(Coordinate c1)
        {
            return (this - c1);
        }

        /// <summary>
        /// Calculate straight line distance between this coordinate and another
        /// </summary>
        /// <param name="c1">End point of line</param>
        /// <returns>Distance between this coordinate and c1</returns>
        public DistanceSpan Distance(Coordinate c1)
        {
            return Distance(this, c1);
        }

        /// <summary>
        /// Calculate straight line distance between two coordinates
        /// </summary>
        /// <param name="c1">First coordinate on line</param>
        /// <param name="c2">Second coordiante on line</param>
        /// <returns></returns>
        public static DistanceSpan Distance(Coordinate c1, Coordinate c2)
        {
            return Distance(c1.Latitude, c1.Longitude, c2.Latitude, c2.Longitude);
        }

        /// <summary>
        /// Calculate straight line distance between two coordinates
        /// </summary>
        /// <param name="latitude1">Latitude of first point</param>
        /// <param name="longitude1">Logitude of first point</param>
        /// <param name="latitude2">Latitude of second point</param>
        /// <param name="longitude2">Logitude of second point</param>
        /// <returns>Distance between the two points</returns>
        public static DistanceSpan Distance(Degree latitude1, Degree longitude1, Degree latitude2, Degree longitude2)
        {
            return Distance(latitude1.TotalSeconds, longitude1.TotalSeconds, latitude2.TotalSeconds, longitude1.TotalSeconds);
        }
        /// <summary>
        /// Calculates the distance between two lat/long pairs
        /// </summary>
        /// <param name="dblLat1">Start Latitude</param>
        /// <param name="dblLong1">Start Longitude</param>
        /// <param name="dblLat2">Stop Latitude</param>
        /// <param name="dblLong2">Stop Longitude</param>
        /// <returns>Distance span object</returns>
        public static DistanceSpan Distance(double dblLat1, double dblLong1, double dblLat2, double dblLong2)
        {
            //convert degrees to radians
            dblLat1 = dblLat1 * Math.PI / 180;
            dblLong1 = dblLong1 * Math.PI / 180;
            dblLat2 = dblLat2 * Math.PI / 180;
            dblLong2 = dblLong2 * Math.PI / 180;

            double dist = 0;

            if (dblLat1 != dblLat2 || dblLong1 != dblLong2)
            {
                //the two points are not the same
                dist =
                    Math.Sin(dblLat1) * Math.Sin(dblLat2)
                    + Math.Cos(dblLat1) * Math.Cos(dblLat2)
                    * Math.Cos(dblLong2 - dblLong1);

                dist =
                    GeoConstant.EARTH_RADIUS_KILOMETERS
                    * (-1 * Math.Atan(dist / Math.Sqrt(1 - dist * dist)) + Math.PI / 2);
            }
            return DistanceSpan.FromKilometers(dist);
        }

        /// <summary>
        /// Calculate a bounding square that is a certain width and height.
        /// </summary>
        /// <param name="distance">Width/Height of bounding box</param>
        /// <param name="minimum">Returns one corner of bounding box</param>
        /// <param name="maximum">Returns opposite corner of bounding box</param>
        public void Radius(DistanceSpan distance, out Coordinate minimum, out Coordinate maximum)
        {
            Radius(distance, this, out minimum, out maximum);
        }

        /// <summary>
        /// Calculate a bounding box that is a certain size
        /// </summary>
        /// <param name="distance">Size of box</param>
        /// <param name="center">Center of box</param>
        /// <param name="minimum">One corner of bounding box</param>
        /// <param name="maximum">Opposite corner of bounding box</param>
        public static void Radius(DistanceSpan distance, Coordinate center, out Coordinate minimum, out Coordinate maximum)
        {
            double radius = double.NaN;
            radius = distance.TotalKilometers;

            double latitude = center.Latitude.TotalSeconds ;
            double longitude = center.Longitude.TotalSeconds ;
            double maxLat = latitude + radius / GeoConstant.EQUATOR_LAT_KILOMETERS;
            double minLat = latitude - (maxLat - latitude);
            double maxLong = longitude + radius / (Math.Cos(minLat * Math.PI / 180) * GeoConstant.EQUATOR_LAT_KILOMETERS);
            double minLong = longitude - (maxLong - longitude);

            minimum = new Coordinate(minLat, minLong);
            maximum = new Coordinate(maxLat, maxLong);
        }

        /// <summary>
        /// Compares an object to this coordinate
        /// </summary>
        /// <param name="obj">Ojbect to compare</param>
        /// <returns>True if both objects have the same longitude and latitude</returns>
        public override bool Equals(object obj)
        {
            return (this == (Coordinate)obj);
        }

        /// <summary>
        /// Compare two objects for value equality
        /// </summary>
        /// <param name="objA">Object to compare</param>
        /// <param name="objB">Object to compare</param>
        /// <returns>True if both objects refer to same point</returns>
        public static bool operator ==(Coordinate objA, Coordinate objB)
        {
            return (objA.GetHashCode() == objB.GetHashCode());
        }

        /// <summary>
        /// Compare two objects to see if they refer to different coordinate
        /// </summary>
        /// <param name="objA">Object to compare</param>
        /// <param name="objB">Object to compare</param>
        /// <returns>True if objA and objB are NOT equal</returns>
        public static bool operator !=(Coordinate objA, Coordinate objB)
        {
            return !objA.Equals(objB);
        }

        /// <summary>
    /// Create a uniqe hash code for this coordinate
    /// </summary>
    /// <returns>Integer hash</returns>
    public override int GetHashCode()
    {
        return ((int)((_longitude.TotalSeconds  * 1000000D) + (Latitude.TotalSeconds  * 10000D))).GetHashCode();
    }

    #region IFormattable Members

        /// <summary>
        /// Override default behavior and always return 켆DDMMSS.SSSS
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString("DMS", null);
        }
        /// <summary>
        /// Convert coordinate into various representations including ISO 6709 formats.  
        /// Always Latitude-Longitude pairs with optional altitude.  
        /// Altitude is always in meters.  NOTE:  always include trailing / delimiter
        /// per ISO standards.
        /// See Remarks for formatting codes.
        /// </summary>
        /// <param name="format">Format code</param>
        /// <param name="formatProvider">Always null or ignored</param>
        /// <returns>Formatted degree</returns>
        /// <exception cref="FormatException">Unable to determine the format code to use.</exception>
        /// <remarks>
        /// <para>empty, G, D  켆D.DDDD켆DD.DDDD</para>
        /// <para>A, DA  켆D.DDDD켆DD.DDDD켥aaa</para>
        /// <para>DM   켆DMM.MMMM켆DDMM.MMMM</para>
        /// <para>DMA  켆DMM.MMMM켆DDMM.MMMM켥aaa</para>
        /// <para>DMS  켆DMMSS.SSSS켆DDMMSS.SSSS</para>
        /// <para>DMSA 켆DMMSS.SSSS켆DDMMSS.SSSS켥aaa</para>
        /// <para>d  DD.DDDD� W DDD.DDDD� S</para>
        /// <para>dm DD캫M.MMMM E DDD캫M.MMMM W</para>
        /// <para>dms DD캫M'SS.SSSS' W DDD캫M'SS.SSSS' N</para>
        /// </remarks>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format) == true)
            {
                format = "D"; //always default to something
            }
            switch (format)
            {
                case "":
                case "G":  // best practice to include            
                case "D":
                case "DM":
                case "DMS": return string.Format("{0}{1}/", Latitude.ToString(format, null), Longitude.ToString(format, null));
                case "A": return string.Format("{0:+0000;-0000;+0000}/", AltitudeMeters);
                case "DA":
                case "DMSA":
                case "DMA":
                    return string.Format("{0}{1}{2:+0000;-0000;+0000}/", Latitude.ToString(format, null), Longitude.ToString(format, null), AltitudeMeters);
                case "d":
                case "dm":
                case "dms":
                    return string.Format("{0} {1}", Latitude.ToString(format, null), Longitude.ToString(format, null));
                default:
                    throw new FormatException(string.Format("Unable to determine format for: '{0}'", format));
            }
        }

    #endregion

    #region IXmlSerializable Members

    /// <summary>
    /// Always return NULL schema
    /// </summary>
    /// <returns>Null</returns>
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Read this element from Xml 
        /// </summary>
        /// <param name="reader">Reader to get xml from</param>
        public void ReadXml(XmlReader reader)
        {
            string str = reader.ReadElementContentAsString().Trim();

            Coordinate c = Coordinate.Parse(str);
            _latitude = c.Latitude;
            _longitude = c.Longitude;
            _altitudeMeters = c.AltitudeMeters;
        }
        /// <summary>
        /// Write this &lt;coord&gt; Xml using format "DA"
        /// </summary>
        /// <param name="writer">Writer that will get this element</param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("coord", this.ToString("DA", null));            
        }

        /// <summary>
        /// Attempt to parse a string and create a coordinate object
        /// </summary>
        /// <param name="coordString">String to format.  Must be in one of the formats of ToString()</param>
        /// <returns>Created coordiate</returns>
        /// <exception cref="FormatException">Thrown if coordString is not in recognizable format</exception>
        /// <remarks>
        /// <para>empty, G, D  켆D.DDDD켆DD.DDDD</para>
        /// <para>A, DA  켆D.DDDD켆DD.DDDD켥aaa</para>
        /// <para>DM   켆DMM.MMMM켆DDMM.MMMM</para>
        /// <para>DMA  켆DMM.MMMM켆DDMM.MMMM켥aaa</para>
        /// <para>DMS  켆DMMSS.SSSS켆DDMMSS.SSSS</para>
        /// <para>DMSA 켆DMMSS.SSSS켆DDMMSS.SSSS켥aaa</para>
        /// <para>d  DD.DDDD� W DDD.DDDD� S</para>
        /// <para>dm DD캫M.MMMM E DDD캫M.MMMM W</para>
        /// <para>dms DD캫M'SS.SSSS' W DDD캫M'SS.SSSS' N</para>
        /// </remarks>
        public static Coordinate Parse(string coordString)
        {
            string latStr;
            string lonStr;
            coordString = coordString.Replace("/", ""); //remove trailing delimiter on ISO compatible coordinates
            Coordinate c;
            switch (coordString.Length)
            {
                case 17: //01234567890123456
                         //켆D.DDDD켆DD.DDDD
                    latStr = coordString.Substring(0, 8);
                    lonStr = coordString.Substring(8);
                    return new Coordinate(Degree.Parse(latStr), Degree.Parse(lonStr));
                    
                case 21: //012345678901234567890
                         //켆DMM.MMMM켆DDMM.MMMM
                    latStr = coordString.Substring(0,10);
                    lonStr = coordString.Substring(10);
                    return new Coordinate(Degree.Parse(latStr), Degree.Parse(lonStr));

                    
                case 22: //0123456789012345678901
                         //켆D.DDDD켆DD.DDDD켥aaa
                    latStr = coordString.Substring(0, 8);
                    lonStr = coordString.Substring(8, 9);
                    c= new Coordinate(Degree.Parse(latStr), Degree.Parse(lonStr));
                    c._altitudeMeters = Convert.ToDouble(coordString.Substring(17,5));
                    return c;
                    
                case 26: //01234567890123456789012345
                         //켆DMM.MMMM켆DDMM.MMMM켥aaa
                    latStr = coordString.Substring(0, 10);
                    lonStr = coordString.Substring(10, 11);
                    c = new Coordinate(Degree.Parse(latStr), Degree.Parse(lonStr));
                    c._altitudeMeters = Convert.ToDouble(coordString.Substring(21, 5));
                    return new Coordinate(Degree.Parse(latStr), Degree.Parse(lonStr));
                    
                case 25: //0123456789012345678901234
                         //켆DMMSS.SSSS켆DDMMSS.SSSS
                    latStr = coordString.Substring(0, 12);
                    lonStr = coordString.Substring(12);
                    return new Coordinate(Degree.Parse(latStr), Degree.Parse(lonStr));
                    
                case 30: //012345678901234567890123456789
                         //켆DMMSS.SSSS켆DDMMSS.SSSS켥aaa
                    latStr = coordString.Substring(0, 12);
                    lonStr = coordString.Substring(12, 25);
                    c = new Coordinate(Degree.Parse(latStr), Degree.Parse(lonStr));
                    c._altitudeMeters = Convert.ToDouble(coordString.Substring(25, 5));
                    return c;

//DD.DDDD� W DDD.DDDD� S (22)
//DD캫M.MMMM E DDD캫M.MMMM W (26)
//DD캫M'SS.SSSS' W DDD캫M'SS.SSSS' N (36)
                
                default: throw new FormatException(string.Format("Unable to parse coordString: '{0}'", coordString));
            }
        }
    #endregion
}
}

