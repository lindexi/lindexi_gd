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
   public partial class Guard
    {
        /// <summary>
        /// Verify <paramref name="condition"/> is false or not-true
        /// </summary>
        /// <param name="condition">Condition to test</param>
        public  static bool IsFalse(bool condition)
        {
            return _validator.IsFalse(condition);
        }

        /// <summary>
        /// Verify <paramref name="condition"/> is false or not-true
        /// </summary>
        /// <param name="condition">Value that is being verified</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  static bool IsFalse(bool condition, string message, params object[] args)
        {
            return _validator.IsFalse(condition, message, args);
        }



        /// <summary>
        /// Verify <paramref name="condition"/> is false or not-true
        /// </summary>
        /// <param name="condition">Condition to test</param>
        public static  bool IsTrue(bool condition)
        {
            return _validator.IsTrue(condition);
        }

       
        /// <summary>
        /// Verify <paramref name="condition"/> is true or not-false
        /// </summary>
        /// <param name="condition">Condition to test</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  static bool IsTrue(bool condition,  string message, params object[] args)
        {
            return _validator.IsTrue(condition,message, args);
        }



       

    }
}
