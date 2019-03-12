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
using System.Collections.Specialized;

namespace ScrimpNet
{
	public static partial class Extensions
	{
        /// <summary>
        /// (ScrimpNet.Core extension) Get a strongly typed value from a name/value (string/string) collection
        /// </summary>
        /// <typeparam name="T">Type to cast target string into</typeparam>
        /// <param name="collection">Hydrated list of name/value pairs</param>
        /// <param name="key">Key in list to look up</param>
        /// <returns>Value set at [key] if found or default(T) if key doesn't exist</returns>
        public static T Get<T>(this NameValueCollection collection, string key)
        {
            string retVal = collection[key];
            if (retVal == null) 
                throw new InvalidOperationException(string.Format("Required key: '{0}' missing from collection",
                    key));
            return Transform.ConvertValue<T>(retVal);
        }

        /// <summary>
        /// (ScrimpNet.Core extension) Get a strongly typed value from a name/value (string/string) collection
        /// </summary>
        /// <typeparam name="T">Type to cast target string into</typeparam>
        /// <param name="collection">Hydrated list of name/value pairs</param>
        /// <param name="key">Key in list to look up</param>
        /// <param name="defaultValue">Value to use if key cannot be found OR an excetpion occurs when converting found value into T</param>
        /// <returns>Value set at [key] if found or defaultValue if key doesn't exist</returns>
        public static T Get<T>(this NameValueCollection collection, string key, T defaultValue)
        {
            string retVal = collection[key];
            try
            {
                if (retVal == null) return defaultValue;
                return Transform.ConvertValue<T>(retVal);
            }
            catch (Exception)  //all exceptions return default value
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// (ScrimpNet.Core extension) Copy values from one collection to another
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static NameValueCollection Clone(this NameValueCollection collection)
        {
            var retList = new NameValueCollection(collection);
            return retList;
        }

	}
}
