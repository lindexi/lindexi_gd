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
using System.Reflection;

namespace ScrimpNet
{
    public static partial class Extensions
    {


        /// <summary>
        /// (ScrimpNet.Core extension) Return a list of attributes that belong to a certain group (or type) of attributes or empty list if attributes cannot be found
        /// </summary>
        /// <typeparam name="T">Class (or root class) of attribute to return</typeparam>
        /// <param name="info">Class or method that might have attributes</param>
        /// <param name="inherit">Get attributes of any inherited classes</param>
        /// <returns>List of attributes of type T or empty list if none found</returns>
        public static List<T> GetCustomAttributes<T>(this MemberInfo info, bool inherit) where T : Attribute
        {
            object[] attributes = info.GetCustomAttributes(typeof(T), inherit);
            List<T> list = new List<T>();
            foreach (Attribute attr in attributes)
            {
                if (attr is T)
                {
                    list.Add(attr as T);
                }
            }
            return list;
        }
        /// <summary>
        /// (ScrimpNet.Core extension) Return a list of attributes that belong to a certain group (or type) of attributes or empty list if attributes cannot be found
        /// </summary>
        /// <typeparam name="T">Class (or root class) of attribute to return</typeparam>
        /// <param name="info">Class or method that might have attributes</param>
        /// <returns>List of attributes of type T or empty list if none found</returns>
        public static List<T> GetCustomAttributes<T>(this MemberInfo info) where T : Attribute
        {
            return info.GetCustomAttributes<T>(false);

        }
    }
}
