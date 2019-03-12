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
using ScrimpNet.Text;

namespace ScrimpNet
{
    /// <summary>
    /// Class that provides a set of common valdiation routines for parameter validation, etc.
    /// </summary>
    public partial class Validator
    {
        /// <summary>
        /// Verify an object is null
        /// </summary>
        /// <param name="obj">Object to check</param>
        public  bool Null(object obj)
        {
            return Null(obj, null, null);
        }

        /// <summary>
        /// Verify an object is null
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  bool Null(object obj, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("Expected Null. Found '{0}'.", (obj == null ? "Null" : obj.GetType().ToString()));
            }
            if (obj == null)
            {

                return AssertExpected(obj, TextUtils.StringFormat(message, args));
            
            }
            else
            {
                return AssertUnexpected(obj, TextUtils.StringFormat(message,args));
            }
        }


        /// <summary>
        /// Verify an object is NOT null
        /// </summary>
        /// <param name="obj">Object to check</param>
        public  bool NotNull(object obj)
        {
            return NotNull(obj, null, null, null, null);
        }

        /// <summary>
        /// Verify an object is NOT null
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="objectName">Name of object to Validate</param>
        public  bool NotNull(object obj, string objectName)
        {
            return NotNull(obj, objectName, null, null, null);
        }

        /// <summary>
        /// Verify an object is null
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public  bool NotNull(object obj, string objectName, string applicationName)
        {
            return NotNull(obj, objectName, applicationName, null, null);
        } 

        /// <summary>
        /// Verify an object is not null
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  bool NotNull(object obj, string objectName, string applicationName, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("{0} Expected Not Null. Found {1}.", objectName, (obj == null ? "Null" : obj.GetType().ToString()));
            }
            if (obj != null)
            {
                AssertExpected(objectName, applicationName, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(objectName, applicationName, message, args);
                return false;
            }
        }

        /// <summary>
        /// Verify an object implements a certain type 
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="expectedType">Type the object is expected to implement</param>
        public  bool InstanceOfType(object obj, Type expectedType)
        {
            return InstanceOfType(obj, expectedType, null, null, null, null);
        }

        /// <summary>
        /// Verify an object implements a certain type 
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="expectedType">Type the object is expected to implement</param>
        /// <param name="objectName">Name of object to Validate</param>
        public  bool InstanceOfType(object obj, Type expectedType, string objectName)
        {
            return InstanceOfType(obj, expectedType, objectName, null,null,null);
        }


        /// <summary>
        /// Verify an object implements a certain type 
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="expectedType">Type the object is expected to implement</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public  bool InstanceOfType(object obj, Type expectedType, string objectName, string applicationName)
        {
            return InstanceOfType(obj, expectedType, objectName, applicationName,null,null);
        }

        /// <summary>
        /// Verify an object implements a certain type 
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="expectedType">Type the object is expected to implement</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  bool InstanceOfType(object obj, Type expectedType, string objectName, string applicationName, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("{0} Expected Type {1}. Found {2}.", objectName, expectedType.FullName, (obj == null ? "Null" : obj.GetType().FullName));
            }
            if (obj != null && obj.GetType() == expectedType)
            {
                AssertExpected(objectName, applicationName, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(objectName, applicationName, message, args);
                return false;
            }
        }

        /// <summary>
        /// Verify an object implements a certain type 
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="expectedType">Type the object is expected to implement</param>
        public  bool NotInstanceOfType(object obj, Type expectedType)
        {
            return NotInstanceOfType(obj, expectedType, null, null, null, null);
        }

        /// <summary>
        /// Verify an object implements a certain type 
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="expectedType">Type the object is expected to implement</param>
        /// <param name="objectName">Name of object to Validate</param>
        public  bool NotInstanceOfType(object obj, Type expectedType, string objectName)
        {
            return NotInstanceOfType(obj, expectedType, objectName, null, null, null);
        }

        /// <summary>
        /// Verify an object implements a certain type 
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="expectedType">Type the object is expected to implement</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public  bool NotInstanceOfType(object obj, Type expectedType, string objectName, string applicationName)
        {
            return NotInstanceOfType(obj, expectedType, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Verify an object does not implement a certain type 
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="expectedType">Type the object is expected to implement</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  bool NotInstanceOfType(object obj, Type expectedType, string objectName, string applicationName, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("{0} Expected Differing Types. Found {1} = {2}.", objectName, expectedType.FullName, (obj == null ? "Null" : obj.GetType().FullName));
            }
            if (obj != null && obj.GetType() != expectedType)
            {
                AssertExpected(objectName, applicationName, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(objectName, applicationName, message, args);
                return false;
            }
        }

        /// <summary>
        /// Verify two objects point at the same reference point in memory
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="src">Object to check</param>        
        public  bool ReferenceSame(object obj, object src)
        {
            return ReferenceSame(obj, src, null, null, null, null);
        }

        /// <summary>
        /// Verify two objects point at the same reference point in memory
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="src">Object to check</param>
        /// <param name="objectName">Name of object to Validate</param>
        public  bool ReferenceSame(object obj, object src, string objectName)
        {
            return ReferenceSame(obj, src, objectName, null, null, null);
        }

        /// <summary>
        /// Verify two objects point at the same reference point in memory
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="src">Object to check</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        public  bool ReferenceSame(object obj, object src, string objectName, string applicationName)
        {
            return ReferenceSame(obj, src, objectName, applicationName, null, null);
        }

        /// <summary>
        /// Verify two objects point at the same reference point in memory
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="src">Object to check</param>
        /// <param name="objectName">Name of object to Validate</param>
        /// <param name="applicationName">Name of application containing object being Validated</param>
        /// <param name="message">Message to include in any exceptions.  May contain .Net standard {} token templates</param>
        /// <param name="args">Arguments to pass to message</param>
        public  bool ReferenceSame(object obj, object src, string objectName, string applicationName, string message, params object[] args)
        {

            if (string.IsNullOrEmpty(message) == true)
            {
                message = string.Format("{0} Expected Object.ReferenceEquals({1},{2}). Found Object.ReferenceEquals({1},{2}) == {3}",
                    objectName, src.GetType().FullName, (obj == null ? "Null" : obj.GetType().FullName),
                    object.ReferenceEquals(obj, src));
            }
            if (obj != null && src != null && Object.ReferenceEquals(obj, src) == true)
            {
                AssertExpected(objectName, applicationName, message, args);
                return true;
            }
            else
            {
                AssertUnexpected(objectName, applicationName, message, args);
                return false;
            }
        }


    }
}
