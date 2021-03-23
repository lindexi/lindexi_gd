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
        /// Test a string so it contains valid non-empty Guid
        /// </summary>
        /// <param name="testValue">Value that is being tested</param>
        /// <returns>true if testValue is a valid non-empty guid</returns>
        public  bool NotEmptyGuid(string testValue)
        {
            Guid g = Transform.ToGuid(testValue);
            return NotEmptyGuid(g);
        }
        /// <summary>
        /// Test a value to ensure that it is not Guid.Empty
        /// </summary>
        /// <param name="obj">Guid to Validate</param>
        /// <exception cref="GuardViolationException"></exception>
        public  bool NotEmptyGuid(Guid obj)
        {
            return NotEmptyGuid(obj, null, null);
        }



        /// <summary>
        /// Test a value to ensure that it is not Guid.Empty
        /// </summary>
        /// <param name="obj">Guid to Validate</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        /// <exception cref="GuardViolationException"></exception>
        public  bool NotEmptyGuid(Guid obj, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("Expected Guid.Empty <> '{0}'. Found '{1}'.",  Guid.Empty.ToString(), obj);
            }
            if (obj != Guid.Empty)
            {
                AssertExpected(obj, obj,message, args);
                return true;
            }
            else
            {
                AssertUnexpected(obj, obj,message, args);
                return false;
            }
        }

        /// <summary>
        /// Test a value to ensure that it is equal to Guid.Empty
        /// </summary>
        /// <param name="obj">Guid to Validate</param>
        /// <exception cref="GuardViolationException"></exception>
        public  bool EmptyGuid(Guid obj)
        {
            return EmptyGuid(obj, null, null);
        }

        /// <summary>
        /// Test a value to ensure that it is equal to Guid.Empty
        /// </summary>
        /// <param name="obj">Guid to Validate</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        /// <exception cref="GuardViolationException"></exception>
        public  bool EmptyGuid(Guid obj, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("Expected Guid.Empty '{0}'. Found '{1}'.", Guid.Empty.ToString(), obj);
            }
            if (obj == Guid.Empty)
            {
                AssertExpected(obj,Guid.Empty, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(obj, obj, message, args);
                return false;
            }
        }
    }
}
