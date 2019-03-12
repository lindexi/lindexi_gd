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
using System.Threading;

namespace ScrimpNet.Web
{
    /// <summary>
    /// Reads/writes values to HTTP application context
    /// </summary>
    public abstract class WebAppCacheBase
    {
        private static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Store a value into HTTP Application context
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetValue(string key, object value)
        {
            try
            {
                _lock.EnterWriteLock();
                HttpContext.Current.Application[key.Replace("set_", "")] = value;
            }
            finally
            {
                if (_lock.IsWriteLockHeld == true)
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Retrieve a value from HTTP Application context
        /// </summary>
        /// <typeparam name="T">Type to cast retrieved values from</typeparam>
        /// <param name="key">Key to retrieve from application context</param>
        /// <returns>key value cast to a specific type</returns>
        public static T GetValue<T>(string key) 
        {
            try
            {
                _lock.EnterReadLock();
                return (T)HttpContext.Current.Application[key.Replace("get_", "")];
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Return a value from HTTP Application context or default(T) if not found or error converting to T
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="key">Element to retrieve from context</param>
        /// <returns>Value for key or default(T) if key not found or error converting to T</returns>
        public static T GetValueOrDefault<T>(string key)
        {            
            return GetValue<T>(key, default(T));
        }

        /// <summary>
        /// Return a value from HTTP Application context or some default value if not found or error converting to T
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="defaultValue">Value to return if key is not found or there is an error converting value to T</param>
        /// <param name="key">Element to retrieve from context</param>
        /// <returns>Value for key or some default value if key not found or error converting to T</returns>
        public static T GetValue<T>(string key, T defaultValue)
        {
            try
            {
                _lock.EnterReadLock();
                if (HttpContext.Current == null) return defaultValue;
                object value = HttpContext.Current.Application[key];
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
            finally
            {
                if (_lock.IsReadLockHeld)
                {
                    _lock.ExitReadLock();
                }
            }
        }
    }
}
