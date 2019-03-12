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
using System.Linq;
using System.Text;
using System.Data;

namespace ScrimpNet
{
    public static partial class Extensions
    {
        /// <summary>
        /// Extracts a value from a data record
        /// </summary>
        /// <typeparam name="T">Simple type to return</typeparam>
        /// <param name="dr">DataRow that contains the field</param>
        /// <param name="fieldName">Name of field to return</param>
        /// <returns>Converted type or default(T) if not able to convert</returns>
        public static T GetValue<T>(this IDataRecord dr, string fieldName)
        {
            return Transform.ConvertValue<T>(dr[fieldName]);
        }

        /// <summary>
        /// Extracts a value from a data record
        /// </summary>
        /// <typeparam name="T">Simple type to return</typeparam>
        /// <param name="dr">DataRow that contains the field</param>
        /// <param name="fieldName">Name of field to return</param>
        /// <param name="defaultValue">Value to return if value is null or an error occured</param>
        /// <returns>Converted type or default(T) if not able to convert</returns>
        public static T GetValue<T>(this IDataRecord dr, string fieldName, T defaultValue)
        {
            return Transform.ConvertValue<T>(dr[fieldName],defaultValue);
        }
    }
}
