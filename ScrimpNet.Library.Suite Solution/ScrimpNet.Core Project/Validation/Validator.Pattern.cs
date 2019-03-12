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
using System.Text.RegularExpressions;
using ScrimpNet.Text;

namespace ScrimpNet
{
    public partial class Validator
    {
        /// <summary>
        /// Test a string against standard operating system wild cards
        /// </summary>
        /// <param name="testValue">String being searched</param>
        /// <param name="searchPattern">? or * in a text string (PH*)</param>
        /// <returns>True if seachPattern matches testValue</returns>
        public  bool MatchesWildCard(string testValue, string searchPattern)
        {

            return TextUtils.MatchesWildCard(testValue, searchPattern);

        }
        /// <summary>
        /// Verify value matches Regular Expression Pattern
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="regularExpression">Pattern Value to Match</param>
        /// <returns>True if test value matches pattern</returns>
        public  bool MatchesPattern(string testValue, string regularExpression)
        {
            return MatchesPattern(testValue, regularExpression, null, null, null, null);
        }

        /// <summary>
        /// Verify value matches Regular Expression Pattern
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="regularExpression">Pattern Value to Match</param>
        /// <param name="objectName">Name of object to validate</param>
        /// <returns>True if test value matches pattern</returns>
        public  bool MatchesPattern(string testValue, string regularExpression, string objectName)
        {
            return MatchesPattern(testValue, regularExpression, objectName, null, null, null);
        }
        /// <summary>
        /// Verify value matches Regular Expression Pattern
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="regularExpression">Pattern Value to Match</param>
        /// <param name="objectName">Name of object to validate</param>
        /// <param name="applicationName">Name of application containing object being validated</param>
        /// <returns>True if test value matches pattern</returns>
        public  bool MatchesPattern(string testValue, string regularExpression, string objectName, string applicationName)
        {
            return MatchesPattern(testValue, regularExpression, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Verify value matches Regular Expression Pattern
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="regularExpression">Pattern Value to Match</param>
        /// <param name="objectName">Name of object to validate</param>
        /// <param name="applicationName">Name of application containing object being validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        /// <returns>True if test value matches pattern</returns>
        /// <remarks>If this function is being called within a loop, precompiling the regex will improve the performance.</remarks>
        public  bool MatchesPattern(string testValue, string regularExpression, string objectName, string applicationName, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("{0} Expect '{1}' Found In '{2}'",
                    objectName, testValue,regularExpression);
            }

            Regex regEx = new Regex(regularExpression);

            bool isMatch = regEx.IsMatch(testValue);
            if (isMatch)
            {
                return AssertExpected(objectName, applicationName, message, args);
            }
            
            {
                return AssertUnexpected(objectName, applicationName, message, args);
            }
        }
    }
}
