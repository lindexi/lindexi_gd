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

namespace ScrimpNet
{
    public partial class Guard
    {
        /// <summary>
        /// Test a string against standard operating system wild cards
        /// </summary>
        /// <param name="testValue">String being searched</param>
        /// <param name="searchPattern">? or * in a text string (PH*)</param>
        /// <returns>True if seachPattern matches testValue</returns>
        public static  bool MatchesWildCard(string testValue, string searchPattern)
        {
            return _validator.MatchesWildCard(testValue, searchPattern);

        }
        /// <summary>
        /// Verify value matches Regular Expression Pattern
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="regularExpression">Pattern Value to Match</param>
        /// <returns>True if test value matches pattern</returns>
        public static  bool MatchesPattern(string testValue, string regularExpression)
        {
            return MatchesPattern(testValue, regularExpression, null, null);
        }

        
        /// <summary>
        /// Verify value matches Regular Expression Pattern
        /// </summary>
        /// <param name="testValue">First value for comparison</param>
        /// <param name="regularExpression">Pattern Value to Match</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        /// <returns>True if test value matches pattern</returns>
        /// <remarks>If this function is being called within a loop, precompiling the regex will improve the performance.</remarks>
        public  static bool MatchesPattern(string testValue, string regularExpression, string message, params object[] args)
        {
            return MatchesPattern(testValue, regularExpression,message, args);
        }
    }
}
