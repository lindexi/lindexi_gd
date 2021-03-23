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
using System.Web;

namespace ScrimpNet.Web
{
    /// <summary>
    /// Reads/writes objects using HTTP Session context
    /// </summary>
    public abstract class WebSessionCacheBase
    {
        /// <summary>
        /// Store a value into HTTP session context
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetValue(string key, object value)
        {
            HttpContext.Current.Session[key] = value;
        }

        /// <summary>
        /// Retrieve a value from HTTP session context
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetValue<T>(string key)
        {
            return (T)HttpContext.Current.Session[key];
        }

        /// <summary>
        /// Return a value from HTTP Session context or default(T) if not found or error converting to T
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="key">Element to retrieve from context</param>
        /// <returns>Value for key or default(T) if key not found or error converting to T</returns>
        public  static T GetValueOrDefault<T>(string key)
        {
            return GetValue<T>(key, default(T));
        }

        /// <summary>
        /// Return a value from HTTP Session context or some default value if not found or error converting to T.  Initializes missing session[key] to defaultValue
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="defaultValue">Value to return if key is not found or there is an error converting value to T</param>
        /// <param name="key">Element to retrieve from context</param>
        /// <returns>Value for key or some default value if key not found or error converting to T</returns>
        public  static T GetValue<T>(string key, T defaultValue)
        {
            if (HttpContext.Current == null) return defaultValue;
            object value = HttpContext.Current.Session[key];
            if (value == null)
            {
                HttpContext.Current.Session[key] = defaultValue;
                return defaultValue;
            }
            try
            {
                return (T)value;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
