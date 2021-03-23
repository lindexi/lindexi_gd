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

namespace ScrimpNet.I18n
{
    /// <summary>
    /// Standard constants used for GeoCoding and distance calculations
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
    public static class GeoConstant
    {
        /// <summary>
        /// Core distance metric.  All other constants are precalculated to reduce operatons at run time (1.609344)
        /// </summary>
        public const double KILOMETERSINMILE = 1.609344D;  // root constant

        /// <summary>
        /// Total meters in nautical mile (1852)
        /// </summary>
        public const double METERSINNMILE =  1852D;  // root constant for nautical miles

        /// <summary>
        /// Total meters in a mile (1609.344)
        /// </summary>
        public const double METERSINMILE = 1609.344D;      // following constants are precalcualted

        /// <summary>
        /// Total meters a yard (0.9144)
        /// </summary>
        public const double METERSINYARD = 0.9144D;        //   to reduce operations at run time

        /// <summary>
        /// Total meters in an inch (0.0254)
        /// </summary>
        public const double METERSININCH = 0.0254D;

        /// <summary>
        /// Total meters in a foot (0.3048)
        /// </summary>
        public const double METERSINFOOT = 0.3048D;

        /// <summary>
        /// Maximum double value.  Used to adjust for database limits
        /// </summary>
        public const double MAXVALUE = 1.79769313486232e305D; //adjust min/max to account for millimeters

        /// <summary>
        /// Minimum double value.  Used to adjust for database limits
        /// </summary>
        public const double MINVALUE = -1.79769313486232e305D;

        /// <summary>
        /// Length of one latitudal mile at the equator (69.172)
        /// </summary>
        public const double EQUATOR_LAT_MILE = 69.172D;

        /// <summary>
        /// Average radius of the earth at the equator (3963.189)
        /// </summary>
        public const double EARTH_RADIUS_MILES = 3963.189D;

        /// <summary>
        /// Radius of the earth in kilometers; assuming spherical value
        /// </summary>
        public const double EARTH_RADIUS_KILOMETERS = 6378138.12D;

        /// <summary>
        /// Distance in kilometers, for a degree of latitude at the equator
        /// </summary>
        public const double EQUATOR_LAT_KILOMETERS = 111.321543168D;
    }
}
