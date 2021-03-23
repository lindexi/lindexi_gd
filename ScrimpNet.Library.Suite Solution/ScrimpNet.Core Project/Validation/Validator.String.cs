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
        /// Verify a string is empty &quot;&quot; or null
        /// </summary>
        /// <param name="testValue">String to Validate</param>
        /// <exception cref="GuardViolationException"></exception>
        public  bool NullOrEmpty(string testValue)
        {
            return NullOrEmpty(testValue, null, null);
        }

        /// <summary>
        /// Verify a string is empty &quot;&quot;or null
        /// </summary>
        /// <param name="testValue">String to Validate</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        /// <exception cref="GuardViolationException"></exception>
        public  bool NullOrEmpty(string testValue, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("{1}NullOrEmpty '{0}'",
                                  testValue, (string.IsNullOrEmpty(testValue) == true) ? "" : testValue + " ");
            }
            if (string.IsNullOrEmpty(testValue) == true)
            {
                AssertExpected<string>(testValue,string.Empty, message, args);
                return true;
            }
            else
            {
                AssertUnexpected<string>(testValue, string.Empty,message, args);
                return false;
            }
        }


        /// <summary>
        /// Verify a string is empty &quot;&quot; or null
        /// </summary>
        /// <param name="testValue">String to Validate</param>
        /// <exception cref="GuardViolationException"></exception>
        public  bool NotNullOrEmpty(string testValue)
        {
            return NullOrEmpty(testValue, null, null);
        }

        /// <summary>
        /// Verify a string is empty &quot;&quot;or null
        /// </summary>
        /// <param name="testValue">String to Validate</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        /// <exception cref="GuardViolationException"></exception>
        public  bool NotNullOrEmpty(string testValue, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) != true)
            {
                message = string.Format("{1}NotNullOrEmpty '{0}'",
                                  testValue, (string.IsNullOrEmpty(testValue) == true) ? "" : testValue + " ");
            }
            if (string.IsNullOrEmpty(testValue) == true)
            {
                AssertExpected<string>(testValue, string.Empty, message, args);
                return true;
            }
            else
            {
                AssertUnexpected<string>(testValue, string.Empty, message, args);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="testValue"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        /// <exception cref="GuardViolationException"></exception>
        public  bool ShorterThan(string testValue, int maxLength)
        {
            if (testValue == null)
            {
                AssertExpected<string>(testValue,string.Empty,"String is (null)");
                return true;
            }
            if (testValue.Length >= maxLength)
            {
                AssertUnexpected<string>(testValue, testValue,"String.Length of {0} is longer than max of {1}", testValue.Length, maxLength);
                return false;
            }
            AssertExpected<string>(testValue, testValue, "String.length of {0} is less than {1}",testValue.Length, maxLength);
            return true;
        }  
    }
}
