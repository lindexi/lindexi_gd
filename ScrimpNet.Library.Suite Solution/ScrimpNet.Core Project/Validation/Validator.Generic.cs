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
    public partial class Validator
    {
        /// <summary>
        /// Verify two fields are equal
        /// </summary>
        /// <typeparam name="T">Type of objects to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        public bool Equal<T>(T testValue, T expectedValue) where T : IComparable
        {
            return Equal<T>(testValue, expectedValue, null, null);
        }


        /// <summary>
        /// Verify two fields are equal
        /// </summary>
        /// <typeparam name="T">Type of object to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public bool Equal<T>(T testValue, T expectedValue, string message, params object[] args) where T : IComparable
        {
            if (testValue.Equals(expectedValue) == true)
            {
                AssertExpected<T>(testValue, expectedValue, message, args);
                return true;
            }
            else
            {
                AssertUnexpected<T>(testValue, expectedValue, message, args);
                return false;
            }
        }


        /// <summary>
        /// Verify <paramref name="testValue"/> equals default(T)
        /// </summary>
        /// <typeparam name="T">Type of objects to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public bool IsDefault<T>(T testValue, string message, params object[] args) where T : IComparable
        {
            if (string.IsNullOrEmpty(message))
            {
                message = string.Format("IsDefault<T>");
            }

            T pivotValue = default(T);
            if (testValue.Equals(pivotValue) == true)
            {

                AssertExpected(testValue, pivotValue, message, args);
                return true;
            }
            else
            {

                AssertUnexpected(testValue, pivotValue, message, args);
                return false;
            }
        }

        /// <summary>
        /// Verify <paramref name="testValue"/> equals default(T)
        /// </summary>
        /// <typeparam name="T">Type of objects to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        public bool IsNotDefault<T>(T testValue) where T : IComparable
        {
            return NotDefault<T>(testValue, null, null);
        }

        /// <summary>
        /// Verify <paramref name="testValue"/> equals default(T)
        /// </summary>
        /// <typeparam name="T">Type of objects to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public bool NotDefault<T>(T testValue, string message, params object[] args) where T : IComparable
        {
            if (string.IsNullOrEmpty(message))
            {
                message = string.Format("IsNotDefault");
            }
            T pivotValue = default(T);
            if (testValue.Equals(pivotValue) == false)
            {

                AssertExpected(testValue, pivotValue, message, args);
                return true;
            }
            else
            {

                AssertUnexpected(testValue, pivotValue, message, args);
                return false;
            }
        }

        /// <summary>
        /// Verify two fields are not equal
        /// </summary>
        /// <typeparam name="T">Type of object to compare.  Must implment IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        public bool NotEqual<T>(T testValue, T expectedValue) where T : IComparable
        {
            return NotEqual<T>(testValue, expectedValue, null, null);
        }

        /// <summary>
        /// Verify two fields are not equal
        /// </summary>
        /// <typeparam name="T">Type of object to compare.  Must implement IComparable</typeparam>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public bool NotEqual<T>(T testValue, T expectedValue, string message, params object[] args) where T : IComparable
        {

            if (string.IsNullOrEmpty(message) == true)
            {
                message = "AreNotEqual<T>";
            }

            if (testValue.Equals(expectedValue) == false)
            {
                AssertExpected(testValue, expectedValue, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(testValue, expectedValue, message, args);
                return false;
            }
        }

        /// <summary>
        /// Verifies that a value is greater than a specified threshhold
        /// </summary>
        /// <typeparam name="T">Type being compared.  Must implemenet IComparable</typeparam>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        public bool GreaterThan<T>(T testValue, T limitValue) where T : IComparable
        {
            return GreaterThan<T>(testValue, limitValue, null, null);
        }


        /// <summary>
        /// Verifies that a <paramref name="limitValue"/> value is greater than a specified testValue
        /// </summary>
        /// <typeparam name="T">Type being compared.  Must implement IComparable</typeparam>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public bool GreaterThan<T>(T testValue, T limitValue, string message, params object[] args) where T : IComparable
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = "GreaterThan<T>";
            }
            if (limitValue.CompareTo(testValue) > 0)
            {
                AssertExpected(testValue, limitValue, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(testValue, limitValue, message, args);
                return false;
            }

        }

        /// <summary>
        /// Verifies that a value is less than a specified threshhold
        /// </summary>
        /// <typeparam name="T">Type being compared.  Must implement IComparable</typeparam>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        public bool LessThan<T>(T testValue, T limitValue) where T : IComparable
        {
            return LessThan<T>(testValue, limitValue, null, null);
        }

        /// <summary>
        /// Verifies that a value is less than a specified threshhold
        /// </summary>
        /// <typeparam name="T">Type of values being compared.  Must implement IComparable</typeparam>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public bool LessThan<T>(T testValue, T limitValue, string message, params object[] args) where T : IComparable
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = "LessThan<T>";

            }
            if (limitValue.CompareTo(testValue) < 0)
            {
                AssertExpected(testValue, limitValue, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(testValue, limitValue, message, args);
                return false;
            }

        }

        /// <summary>
        /// Compare a value to see if it falls within a range of values. Inclusive
        /// </summary>
        /// <typeparam name="T">Type of values for comparison. Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        public bool Between<T>(T testValue, T lowerLimit, T upperLimit) where T : IComparable
        {
            return Between<T>(testValue, lowerLimit, upperLimit, null, null);
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
        public bool Between<T>(T testValue, T lowerLimit, T upperLimit, string message, params object[] args) where T : IComparable
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = "Between<T>";

            }
            if (lowerLimit.CompareTo(testValue) < 0 && upperLimit.CompareTo(testValue) > 0)
            {
                AssertExpected(testValue, lowerLimit, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(testValue, lowerLimit, message, args);
                return false;
            }
        }

        /// <summary>
        /// Compare a value to see if it falls outside a range of values.  Returns TRUE if testValue &lt; lowerLimit AND testValue &gt; upperLimit
        /// </summary>
        /// <typeparam name="T">Type of values for comparison. Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <returns>Returns TRUE if testValue &lt; lowerLimit AND testValue &gt; upperLimit</returns>
        public bool NotBetween<T>(T testValue, T lowerLimit, T upperLimit) where T : IComparable
        {
            return NotBetween<T>(testValue, lowerLimit, upperLimit, null, null);
        }

        /// <summary>
        /// Compare a value to see if it falls outside a range of values.  Returns TRUE if testValue &lt; lowerLimit AND testValue &gt; upperLimit
        /// </summary>
        /// <typeparam name="T">Type of values for comparison. Must implement IComparable</typeparam>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        /// <returns>Returns TRUE if testValue &lt; lowerLimit AND testValue &gt; upperLimit</returns>
        public bool NotBetween<T>(T testValue, T lowerLimit, T upperLimit, string message, params object[] args) where T : IComparable
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = "NotBetween<T>";

            }
            if (lowerLimit.CompareTo(testValue) >= 0 && upperLimit.CompareTo(testValue) <= 0)
            {
                AssertExpected(testValue, lowerLimit, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(testValue, lowerLimit, message, args);
                return false;
            }
        }

    }
}
