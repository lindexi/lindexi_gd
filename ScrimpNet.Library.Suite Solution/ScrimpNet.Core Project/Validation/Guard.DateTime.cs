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

namespace ScrimpNet
{
    public static partial class Guard
    {
        /// <summary>
        /// Verifies two DateTimes are equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        public static Boolean Equal(DateTime testValue, DateTime expectedValue)
        {
            return Equal<DateTime>(testValue, expectedValue,  null, null, null, null);
        }
        /// <summary>
        /// Verifies two DateTimes are equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="objectName">Name of object to Validate</param>
        public static Boolean Equal(DateTime testValue, DateTime expectedValue, string objectName)
        {
            return Equal<DateTime>(testValue, expectedValue, objectName, null, null, null);
        }
        /// <summary>
        /// Verifies two DateTimes are equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public static Boolean Equal(DateTime testValue, DateTime expectedValue, string objectName, string applicationName)
        {
            return Equal<DateTime>(testValue, expectedValue, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Verifies two DateTimes are equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static Boolean Equal(DateTime testValue, DateTime expectedValue, string objectName, string applicationName, string message, params object[] args)
        {
            return Equal<DateTime>(testValue, expectedValue, objectName, applicationName, message, args);
        }

        /// <summary>
        /// Verifies two DateTime values are not equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        public static Boolean AreNotEqual(DateTime testValue, DateTime expectedValue)
        {
            return NotEqual(testValue, expectedValue, null, null, null, null);
        }

        /// <summary>
        /// Verifies two DateTime values are not equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public static Boolean AreNotEqual(DateTime testValue, DateTime expectedValue, string objectName, string applicationName)
        {
            return NotEqual(testValue, expectedValue, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Verifies two DateTime values are not equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static Boolean AreNotEqual(DateTime testValue, DateTime expectedValue, string objectName, string applicationName, string message, params object[] args)
        {
            return NotEqual(testValue, expectedValue, objectName, applicationName, message, args);
        }

        /// <summary>
        /// Verifies that a DateTime is greater than a specified threshhold
        /// </summary>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        public static Boolean GreaterThan(DateTime testValue, DateTime limitValue)
        {
            return GreaterThan(testValue, limitValue, null, null, null, null);
        }

        /// <summary>
        /// Verifies that a DateTime is greater than a specified threshhold
        /// </summary>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="objectName">Name of object to Validate</param>
        public static Boolean GreaterThan(DateTime testValue, DateTime limitValue, string objectName)
        {
            return GreaterThan(testValue, limitValue, objectName, null, null, null);
        }

        /// <summary>
        /// Verifies that a DateTime is greater than a specified threshhold
        /// </summary>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public static Boolean GreaterThan(DateTime testValue, DateTime limitValue, string objectName, string applicationName)
        {
            return GreaterThan(testValue, limitValue, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Verifies that a DateTime <paramref name="testValue"/> is greater than a specified threshhold
        /// </summary>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static Boolean GreaterThan(DateTime testValue, DateTime limitValue, string objectName, string applicationName, string message, params object[] args)
        {
            return GreaterThan<DateTime>(testValue, limitValue, objectName, applicationName, message, args);
        }

        /// <summary>
        /// Verifies that a DateTime is less than a specified threshhold
        /// </summary>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        public static Boolean LessThan(DateTime testValue, DateTime limitValue)
        {
            return LessThan(testValue, limitValue, null, null, null, null);
        }

        /// <summary>
        /// Verifies that a DateTime is less than a specified threshhold
        /// </summary>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="objectName">Name of object to Validate</param>
        public static Boolean LessThan(DateTime testValue, DateTime limitValue, string objectName)
        {
            return LessThan(testValue, limitValue, objectName, null, null, null);
        }

        /// <summary>
        /// Verifies that a DateTime is less than a specified threshhold
        /// </summary>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public static Boolean LessThan(DateTime testValue, DateTime limitValue, string objectName, string applicationName)
        {
            return LessThan(testValue, limitValue, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Verifies that a DateTime is less than a specified threshhold
        /// </summary>
        /// <param name="testValue">Value being tested</param>
        /// <param name="limitValue">Threshhold that comparison is made against</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static Boolean LessThan(DateTime testValue, DateTime limitValue, string objectName, string applicationName, string message, params object[] args)
        {
            return LessThan<DateTime>(testValue, limitValue, objectName, applicationName, message, args);
        }

        /// <summary>
        /// Compare a DateTime to see if it falls between two values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        public static Boolean Between(DateTime testValue, DateTime lowerLimit, DateTime upperLimit)
        {
            return Between(testValue, lowerLimit, upperLimit, null, null, null, null);
        }

        /// <summary>
        /// Compare a DateTime to see if it falls between two values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="objectName">Name of object to Validate</param>
        public static Boolean Between(DateTime testValue, DateTime lowerLimit, DateTime upperLimit, string objectName)
        {
            return Between(testValue, lowerLimit, upperLimit, objectName, null, null, null);
        }

        /// <summary>
        /// Compare a DateTime to see if it falls between two values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public static Boolean Between(DateTime testValue, DateTime lowerLimit, DateTime upperLimit,  string objectName, string applicationName)
        {
            return Between(testValue, lowerLimit, upperLimit, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Compare a DateTime to see if it falls between two values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static Boolean Between(DateTime testValue, DateTime lowerLimit, DateTime upperLimit,  string objectName, string applicationName, string message, params object[] args)
        {
            return Between<DateTime>(testValue, lowerLimit, upperLimit, objectName, applicationName, message, args);
        }

        /// <summary>
        /// Compare a DateTime to see if it falls outside a range of values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        public static Boolean NotBetween(DateTime testValue, DateTime lowerLimit, DateTime upperLimit)
        {
            return NotBetween(testValue, lowerLimit, upperLimit, null, null, null, null);
        }

        /// <summary>
        /// Compare a DateTime to see if it falls outside a range of values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="objectName">Name of object to Validate</param>
        public static Boolean NotBetween(DateTime testValue, DateTime lowerLimit, DateTime upperLimit, string objectName)
        {
            return NotBetween(testValue, lowerLimit, upperLimit, objectName, null, null, null);
        }

        /// <summary>
        /// Compare a DateTime to see if it falls outside a range of values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public static Boolean NotBetween(DateTime testValue, DateTime lowerLimit, DateTime upperLimit, string objectName, string applicationName)
        {
            return NotBetween(testValue, lowerLimit, upperLimit, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Compare a DateTime to see if it falls outside a range of values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public static Boolean NotBetween(DateTime testValue, DateTime lowerLimit, DateTime upperLimit, string objectName, string applicationName, string message, params object[] args)
        {
            return NotBetween<DateTime>(testValue, lowerLimit, upperLimit, objectName, applicationName, message, args);
        }
    } // Is_DateTime
}
