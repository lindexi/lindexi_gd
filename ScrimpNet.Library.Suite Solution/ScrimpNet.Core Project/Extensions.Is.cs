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

namespace ScrimpNet
{
    /// <summary>
    /// Extensions that can be used for parameter validation
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Verifies a value not null
        /// </summary>
        /// <param name="target">Nullable value to check</param>
        /// <returns>True if <paramref name="target"/> is not null</returns>
        public static bool IsNotNull(this object  target)
        {
            return target == null;
        }

        /// <summary>
        /// Verifies a string is not null or empty
        /// </summary>
        /// <param name="target">String value to check</param>
        /// <returns>True if <paramref name="target"/> is not null or empty</returns>
        public static bool IsNotNullEmpty(this string target)
        {
            return !string.IsNullOrEmpty(target);
        }

        /// <summary>
        /// Verifies a string length is between two values (inclusive)
        /// </summary>
        /// <param name="target">String to check</param>
        /// <param name="minLength">Shortest string can be and still pass the test</param>
        /// <param name="maxLength">Longest strong can be and still pass the test</param>
        /// <returns></returns>
        public static bool IsLengthBetween(this string target, int minLength, int maxLength)
        {
            if (target.IsNotNull() == false) return false;
            return Check.Between<int>(target.Length, minLength, maxLength);
        }

        /// <summary>
        /// Compares two object for value equality, not referential equality
        /// </summary>
        /// <param name="left">Base object to compare</param>
        /// <param name="right">Value to compare against</param>
        /// <returns>True if both values are the same value, not necessarily the same reference</returns>
        public static bool AreSame(this IComparable left, IComparable right)
        {
            return Check.IsSame<IComparable>(left,right);
        }
    }
}
