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
    public partial class Validator
    {
        /// <summary>
        /// Verifies two Singles are equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="delta">Comparison succeeds if equality is within ±<paramref name="delta"/> inclusive</param>
        public  Boolean Equal(Single testValue, Single expectedValue, Single delta)
        {
            return Equal(testValue, expectedValue, delta, null, null);
        }

        /// <summary>
        /// Verifies two Singles are equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="delta">Comparison succeeds if equality is within ±<paramref name="delta"/> inclusive</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  Boolean Equal(Single testValue, Single expectedValue, Single delta, string message, params object[] args)
        {
            
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("Expected '{0}' ±{1}. Found '{2}'",
                    expectedValue, delta, testValue);
            }
            Single minValue = expectedValue - delta;
            Single maxValue = expectedValue + delta;
            if (minValue <= expectedValue && expectedValue <= maxValue)
            {
                AssertExpected(testValue,expectedValue, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(testValue,expectedValue, message, args);
                return false;
            }
        }

        /// <summary>
        /// Verifies two Single values are not equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="delta">Comparison succeeds if <paramref name="testValue"/> is less than or greater than within <paramref name="expectedValue"/>±<paramref name="delta"/> exclusive</param>
        public  Boolean AreNotEqual(Single testValue, Single expectedValue, Single delta)
        {
            return AreNotEqual(testValue, expectedValue, delta, null, null);
        }

        /// <summary>
        /// Verifies two Single values are not equal
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="expectedValue">Second value for comparison</param>
        /// <param name="delta">Comparison succeeds if <paramref name="testValue"/> is less than or greater than within <paramref name="expectedValue"/>±<paramref name="delta"/> exclusive</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  Boolean AreNotEqual(Single testValue, Single expectedValue, Single delta, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("Expected '{0}' ±{1}. Found '{2}'",
                    expectedValue, delta, testValue);
            }
            Single minValue = expectedValue - delta;
            Single maxValue = expectedValue + delta;
            if (minValue <= expectedValue && expectedValue <= maxValue)
            {
                AssertExpected(testValue,expectedValue, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(testValue,expectedValue, message, args);
                return false;
            }
        }
        /// <summary>
        /// Compare a Single to see if it falls between two values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="delta">Comparison succeeds if boundry points are within ±<paramref name="delta"/> inclusive</param>
        public  Boolean Between(Single testValue, Single lowerLimit, Single upperLimit, Single delta)
        {
            return Between(testValue, lowerLimit, upperLimit, delta, null, null);
        }

        
        /// <summary>
        /// Compare a Single to see if it falls between two values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="delta">Comparison succeeds if boundry points are within ±<paramref name="delta"/> inclusive</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  Boolean Between(Single testValue, Single lowerLimit, Single upperLimit, Single delta, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("Between '{0}'-'{1} ±{2}. Found {3}",
                    lowerLimit,upperLimit,delta,testValue);
            }
            lowerLimit -= delta;
            upperLimit += delta;
            if (testValue >= lowerLimit && testValue <= upperLimit)
            {
                AssertExpectedRange<Single>(testValue,lowerLimit,upperLimit, message, args);
                return true;
            }
            else
            {
                AssertUnexpectedRange<Single>(testValue,lowerLimit,upperLimit, message, args);
                return false;
            } 
        }

        /// <summary>
        /// Compare a Single to see if it falls outside a range of values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="delta">Comparison succeeds if boundry points are outside ±<paramref name="delta"/> exclusive</param>
        public  Boolean NotBetween(Single testValue, Single lowerLimit, Single upperLimit, Single delta)
        {
            return NotBetween(testValue, lowerLimit, upperLimit, delta, null, null);
        }

        /// <summary>
        /// Compare a Single to see if it falls outside a range of values
        /// </summary>
        /// <param name="testValue">Value to be checked</param>
        /// <param name="lowerLimit">Lower limit of range</param>
        /// <param name="upperLimit">Upper limit of range</param>
        /// <param name="delta">Comparison succeeds if boundry points are outside ±<paramref name="delta"/> exclusive</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  Boolean NotBetween(Single testValue, Single lowerLimit, Single upperLimit, Single delta, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("Not Between '{0}'-'{1} ±{2}. Found {3}",
                    lowerLimit, upperLimit, delta, testValue);
            }
            lowerLimit -= delta;
            upperLimit += delta;
            if (testValue < lowerLimit || testValue > upperLimit)
            {
                AssertExpectedRange(testValue,lowerLimit,upperLimit, message, args);
                return true;
            }
            else
            {
                AssertUnexpectedRange(testValue,lowerLimit,upperLimit, message, args);
                return false;
            } 
        }
    }
}
