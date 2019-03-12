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
    /// Verify value passes certain boundry conditions.
    /// </summary>
    public static partial class Check
    {
        /// <summary>
        /// Verify that a value is less than the boundry value
        /// </summary>
        /// <typeparam name="T">Type of value to be tested.  Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be verified</param>
        /// <param name="boundry">Value that must be greater than testValue</param>
        /// <returns>True if <paramref name="testValue"/> is less than <paramref name="boundry"/></returns>
        public static bool IsLessThan<T>(T testValue, T boundry) where T : IComparable
        {
            return testValue.CompareTo(boundry) < 0;
        }

        /// <summary>
        /// Verify that a value is greater than the boundry value
        /// </summary>
        /// <typeparam name="T">Type of value to be tested.  Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be verified</param>
        /// <param name="boundry">TestValue must be less than boundry</param>
        /// <returns>True if <paramref name="testValue"/> is greater than <paramref name="boundry"/></returns>
        public static bool IsGreaterThan<T>(T testValue, T boundry) where T : IComparable
        {
            return testValue.CompareTo(boundry) > 0;
        }


        /// <summary>
        /// Verify that a pair of values are equal to each other.  Values do not have to point to same instance of object
        /// </summary>
        /// <typeparam name="T">Type of value to be tested.  Must implement IComparable</typeparam>
        /// <param name="leftValue">First value of comparison</param>
        /// <param name="rightValue">Second value of comparison</param>
        /// <returns>True if <paramref name="leftValue"/> has the same value of <paramref name="rightValue"/></returns>
        public static bool IsSame<T>(T leftValue, T rightValue) where T : IComparable
        {
            return leftValue.CompareTo(rightValue) == 0;
        }

        /// <summary>
        /// Verify that a pair of values are not equal to each other.  Values do not have to point to same instance of object
        /// </summary>
        /// <typeparam name="T">Type of value to be tested.  Must implement IComparable</typeparam>
        /// <param name="leftValue">First value of comparison</param>
        /// <param name="rightValue">Second value of comparison</param>
        /// <returns>True if <paramref name="leftValue"/> is not same value of <paramref name="rightValue"/></returns>
        public static bool IsNotSame<T>(T leftValue, T rightValue) where T : IComparable
        {
            return !IsSame<T>(leftValue, rightValue);
        }

        /// <summary>
        /// Verify that <paramref name="testValue"/> is &gt;= <paramref name="lowerBoundry"/> and &lt;= <paramref name="upperBoundry"/> 
        /// </summary>
        /// <typeparam name="T">Type of object that is being compared</typeparam>
        /// <param name="testValue">Value that is being compared</param>
        /// <param name="lowerBoundry">Lower edge of test range (inclusive)</param>
        /// <param name="upperBoundry">Upper edge of test range (inclusive)</param>
        /// <returns>True if <paramref name="testValue"/> is within boundry conditions (inclusive)</returns>
        public static bool Between<T>(T testValue, T lowerBoundry, T upperBoundry) where T : IComparable
        {
            if (IsLessThan<T>(testValue,lowerBoundry) == false && IsGreaterThan<T>(testValue,upperBoundry) == false)
            {
               return true;
                
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Verify that <paramref name="testValue"/>  &lt; <paramref name="lowerBoundry"/> or <paramref name="testValue"/>  &gt; <paramref name="upperBoundry"/>
        /// </summary>
        /// <typeparam name="T">Type of object that is being compared</typeparam>
        /// <param name="testValue">Value that is being compared</param>
        /// <param name="lowerBoundry">Lower edge of test range (inclusive)</param>
        /// <param name="upperBoundry">Upper edge of test range (inclusive)</param>
        /// <returns>False if <paramref name="testValue"/> outside of boundry conditions (inclusive)</returns>
        public static bool NotBetween<T>(T testValue, T lowerBoundry, T upperBoundry) where T : IComparable
        {
            return !Between(testValue, lowerBoundry, upperBoundry);
        }
    }
}
