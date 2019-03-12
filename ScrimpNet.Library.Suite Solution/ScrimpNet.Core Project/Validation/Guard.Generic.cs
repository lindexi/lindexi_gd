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
using System.Threading;

namespace ScrimpNet
{
   public partial class Guard
    {
        /// <summary>
        /// Verify two fields are equal
        /// </summary>
        /// <typeparam name="T">Type of objects to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        public static bool Equal<T>(T testValue, T expectedValue) where T : IComparable
        {
            
            return _validator.Equal<T>(testValue, expectedValue, null, null);
        }


        /// <summary>
        /// Verify two fields are equal
        /// </summary>
        /// <typeparam name="T">Type of object to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static bool Equal<T>(T testValue, T expectedValue, string message, params object[] args) where T : IComparable
        {
            return _validator.Equal<T>(testValue, expectedValue, message, args);        
        }


        /// <summary>
        /// Verify <paramref name="testValue"/> equals default(T)
        /// </summary>
        /// <typeparam name="T">Type of objects to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static  bool IsDefault<T>(T testValue, string message, params object[] args) where T : IComparable
        {
            return _validator.IsDefault<T>(testValue, message, args);
         
        }

        /// <summary>
        /// Verify <paramref name="testValue"/> equals default(T)
        /// </summary>
        /// <typeparam name="T">Type of objects to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        public static  bool IsNotDefault<T>(T testValue) where T : IComparable
        {
            return _validator.NotDefault<T>(testValue, null, null);
        }

        /// <summary>
        /// Verify <paramref name="testValue"/> equals default(T)
        /// </summary>
        /// <typeparam name="T">Type of objects to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static  bool NotDefault<T>(T testValue, string message, params object[] args) where T : IComparable
        {
            return _validator.NotDefault<T>(testValue, message, args);
        }

        /// <summary>
        /// Verify two fields are not equal
        /// </summary>
        /// <typeparam name="T">Type of object to compare.  Must implment IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        public static  bool NotEqual<T>(T testValue, T expectedValue) where T : IComparable
        {
            return _validator.NotEqual(testValue, expectedValue,null, null);
        }

        /// <summary>
        /// Verify two fields are not equal
        /// </summary>
        /// <typeparam name="T">Type of object to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static  bool NotEqual<T>(T testValue, T expectedValue, string message, params object[] args) where T : IComparable
        {

            return _validator.NotEqual<T>(testValue, expectedValue, message, args);
        }

        /// <summary>
        /// Verifies that a value is greater than a specified threshhold
        /// </summary>
        /// <typeparam name="T">Type being compared.  Must implemenet IComparable</typeparam>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        public static  bool GreaterThan<T>(T testValue, T limitValue) where T : IComparable
        {
            return _validator.GreaterThan<T>(testValue, limitValue);
        }


        /// <summary>
        /// Verifies that a <paramref name="limitValue"/> value is greater than a specified testValue
        /// </summary>
        /// <typeparam name="T">Type being compared.  Must implement IComparable</typeparam>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static  bool GreaterThan<T>(T testValue, T limitValue, string message, params object[] args) where T : IComparable
        {

            if (testValue.CompareTo(limitValue) > 0)
            {
                throw new ArgumentException(string.Format(message, args));
            }
            return true;
        }

        /// <summary>
        /// Verifies that a value is less than a specified threshhold
        /// </summary>
        /// <typeparam name="T">Type being compared.  Must implement IComparable</typeparam>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        public static  bool LessThan<T>(T testValue, T limitValue) where T : IComparable
        {
            return _validator.LessThan<T>(testValue, limitValue);
        }

        /// <summary>
        /// Verifies that a value is less than a specified threshhold
        /// </summary>
        /// <typeparam name="T">Type of values being compared.  Must implement IComparable</typeparam>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static  bool LessThan<T>(T testValue, T limitValue, string message, params object[] args) where T : IComparable
        {
            return _validator.LessThan<T>(testValue, limitValue, message, args);
        }

        /// <summary>
        /// Compare a value to see if it falls within a range of values. Inclusive
        /// </summary>
        /// <typeparam name="T">Type of values for comparison. Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        public static  bool Between<T>(T testValue, T lowerLimit, T upperLimit) where T : IComparable
        {
            return _validator.Between<T>(testValue, lowerLimit, upperLimit);
        }

        /// <summary>
        /// Compare a value to see if it falls within a range of values.  Limits are exclusive.
        /// </summary>
        /// <typeparam name="T">Type of values for comparison. Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static  bool Between<T>(T testValue, T lowerLimit, T upperLimit, string message, params object[] args) where T : IComparable
        {
            return _validator.Between<T>(testValue, lowerLimit, upperLimit, message, args);
        }

        /// <summary>
        /// Compare a value to see if it falls outside a range of values.  Returns TRUE if testValue &lt; lowerLimit AND testValue &gt; upperLimit
        /// </summary>
        /// <typeparam name="T">Type of values for comparison. Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <returns>Returns TRUE if testValue &lt; lowerLimit AND testValue &gt; upperLimit</returns>
        public static  bool NotBetween<T>(T testValue, T lowerLimit, T upperLimit) where T : IComparable
        {
            return _validator.NotBetween<T>(testValue, lowerLimit, upperLimit);
        }


        /// <summary>
        /// Compare a value to see if it falls outside a range of values.  Returns TRUE if testValue &lt; lowerLimit AND testValue &gt; upperLimit
        /// </summary>
        /// <typeparam name="T">Type of values for comparison. Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <returns>Returns TRUE if testValue &lt; lowerLimit AND testValue &gt; upperLimit</returns>
        /// <param name="message">Message to be used when Guard asserts an exception (default Guard behavior)</param>
        /// <param name="args">Arguments to be supplied to message (if any)</param>
        public static  bool NotBetween<T>(T testValue, T lowerLimit, T upperLimit, string message, params object[] args) where T : IComparable
        {
            return _validator.NotBetween<T>(testValue, lowerLimit, upperLimit, message, args);
        }

    }
}
