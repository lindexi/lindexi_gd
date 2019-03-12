namespace ScrimpNet.Web
{
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
    using System.Reflection;

    namespace ScrimpNet.Web
    {
        /// <summary>
        /// Reads/writes values to HTTP cache context
        /// </summary>
        public abstract class WebCacheBase
        {
            /// <summary>
            /// Store a value into HTTP Cache context, replacing <paramref name="key"/> if it already exists in cache.  Uses default values for dependencies, callback, and timeouts.
            /// </summary>
            /// <param name="key">Identifier of object in cache</param>
            /// <param name="value">Value to be stored in cache</param>
            protected static void setValue(string key, object value)
            {
                HttpContext.Current.Cache.Insert(key, value);
                HttpContext.Current.Cache[key.Replace("set_", "")] = value;
            }

            /// <summary>
            /// Retrieve a value from HTTP Application context
            /// </summary>
            /// <typeparam name="T">Type to cast retrieved values from</typeparam>
            /// <param name="key">Key to retrieve from application context</param>
            /// <returns>key value cast to a specific type</returns>
            protected static T getValue<T>(string key)
            {
                return (T)HttpContext.Current.Cache[key.Replace("get_", "")];
            }

            /// <summary>
            /// Return a value from HTTP Cache context or default(T) if not found or error converting to T
            /// </summary>
            /// <typeparam name="T">Type to return</typeparam>
            /// <param name="key">Element to retrieve from context</param>
            /// <returns>Value for key or default(T) if key not found or error converting to T</returns>
            protected static T getValueOrDefault<T>(string key)
            {
                return getValue<T>(key, default(T));
            }

            /// <summary>
            /// Return a value from HTTP Cache context or some default value if not found or error converting to T
            /// </summary>
            /// <typeparam name="T">Type to return</typeparam>
            /// <param name="defaultValue">Value to return if key is not found or there is an error converting value to T</param>
            /// <param name="key">Element to retrieve from context</param>
            /// <returns>Value for key or some default value if key not found or error converting to T</returns>
            protected static T getValue<T>(string key, T defaultValue)
            {
                if (HttpContext.Current == null) return defaultValue;
                object value = HttpContext.Current.Cache[key];
                if (value == null) return defaultValue;
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

}
