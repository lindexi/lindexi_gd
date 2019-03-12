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
        /// Verify <paramref name="condition"/> is false or not-true
        /// </summary>
        /// <param name="condition">Condition to test</param>
        public  bool IsFalse(bool condition)
        {
            return Equal<bool>(condition, false);
        }

        /// <summary>
        /// Verify <paramref name="condition"/> is false or not-true
        /// </summary>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        /// <param name="condition">Value that is being tested</param>
        public  bool IsFalse(bool condition, string message, params object[] args)
        {
            return Equal<bool>(condition, false, message, args);
        }



        /// <summary>
        /// Verify <paramref name="condition"/> is false or not-true
        /// </summary>
        /// <param name="condition">Condition to test</param>
        public  bool IsTrue(bool condition)
        {
            return IsTrue(condition, null, null);
        }

        /// <summary>
        /// Verify <paramref name="condition"/> is false or not-true
        /// </summary>
        /// <param name="condition">Condition to test</param>
        /// <param name="objectName">Name of object to Validate</param>
        public  bool IsTrue(bool condition, string objectName)
        {
            return IsTrue(condition, objectName, null, null, null);
        }

        /// <summary>
        /// Verify <paramref name="condition"/> is true or not-false
        /// </summary>
        /// <param name="condition">Condition to test</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  bool IsTrue(bool condition,  string message, params object[] args)
        {
            return Equal<bool>(condition, true, message, args);
        }



       

    }
}
