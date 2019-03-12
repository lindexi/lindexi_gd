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
using System.Linq;
using System.Text;
using ScrimpNet.Text;

namespace ScrimpNet
{
    public partial class Validator
    {
       

        /// <summary>
        /// Pipeline of event handlers that are fired when a value fails validation
        /// </summary>        
        public  event GuardValidEventHandler OnUnexpectedEvents= null;      
        
        /// <summary>
        /// Pipline of event handlers that are fired when a value passes validation.  Normally null but can be activated for auditing purposes
        /// </summary>
        public  event GuardValidEventHandler OnExpectedEvents = null;
        

        /// <summary>
        /// A function that evaluates an object and verifies it's integrity.  Used for creating custom verification
        /// behaviors or for use with complex and/or composite types
        /// </summary>
        /// <param name="obj">Object to verify</param>
        /// <returns>True if object is verified.  False if not.</returns>
        public delegate bool IsExpectedVerifier(object obj);

        /// <summary>
        /// Default constructor and set up default handler pipeline
        /// </summary>
         public Validator()
        {
            ResetHandlers();
        }

        /// <summary>
        /// Reset all handlers to defaults
        /// </summary>
        public  void ResetHandlers()
        {
            ResetOnUnexpected();
            ResetOnExpected();
        }

        /// <summary>
        /// Reset OnExpected event pipeline to default
        /// </summary>
        public  void ResetOnExpected()
        {
            OnExpectedEvents = null;
        }

        /// <summary>
        /// Reset OnUnexpected event pipeline to default
        /// </summary>
        public  void ResetOnUnexpected()
        {

            ClearOnUnexpected();
            OnUnexpectedEvents += _defaultUnexpectedHandler;
        }

        /// <summary>
        /// Remove any OnUnExpected handlers in the event pipeline
        /// </summary>
        public  void ClearOnUnexpected()
        {
            OnExpectedEvents = null;
        }

        /// <summary>
        /// Remove any OnExpected handlers in the event pipeline
        /// </summary>
        public  void ClearOnExpected()
        {
            OnExpectedEvents = null;
        }

        /// <summary>
        /// Remove all handlers from the event pipeline
        /// </summary>
        public  void ClearHandlers()
        {
            ClearOnUnexpected();
            ClearOnExpected();
        }

        /// <summary>
        /// Fire any OnUnexpected event handlers
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public  bool AssertUnexpected(GuardEventArgs eventArgs)
        {
            if (OnUnexpectedEvents != null)
            {
                OnUnexpectedEvents(eventArgs);
                //IsValidEventHandler[] delegateList = (IsValidEventHandler[])OnUnexpectedEvents.GetInvocationList();
                //foreach (IsValidEventHandler dlgate in delegateList)
                //{
                //    dlgate.Invoke(eventArgs);
                //}
            }
            return false;
        }

        /// <summary>
        /// Method that is called when an unexpected (e.g. out of range)condition occurs
        /// </summary>
        /// <param name="obj">Object that is being validated</param>
        /// <param name="message">Message the should be passed to any handler</param>
        /// <returns>Always returns false (object is not valid)</returns>
        public bool AssertUnexpected(object obj, string message)
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.ApplicationName = CoreConfig.ApplicationKey;
            eventArgs.TestValue = obj;
            eventArgs.IsResult = GuardCompareResult.Failure;
            AssertUnexpected(eventArgs);
            return false;
        }

        /// <summary>
        /// Method that is called when an value with within expected parameters
        /// </summary>
        /// <param name="obj">Object that is being validated</param>
        /// <param name="message">Message that should be passed to any handler</param>
        /// <returns>Always returns true (object is valid)</returns>
        public bool AssertExpected(object obj, string message)
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.ApplicationName = CoreConfig.ApplicationKey;
            eventArgs.TestValue = obj;
            eventArgs.IsResult = GuardCompareResult.Success;
            AssertUnexpected(eventArgs);
            return true;
        }
        /// <summary>
        /// Fire the OnExpected event handlers.  This is considered a low-level method and should be used with 
        /// caution only if there is not another more suitable alternative.
        /// </summary>
        /// <param name="eventArgs"></param>
        public  bool AssertExpected(GuardEventArgs eventArgs)
        {
            if (OnExpectedEvents != null)
            {
                GuardValidEventHandler[] delegateList = (GuardValidEventHandler[])OnExpectedEvents.GetInvocationList();
                foreach (GuardValidEventHandler dlgate in delegateList)
                {
                    dlgate.Invoke(eventArgs);
                }
            }
            return true;
        }

        /// <summary>
        /// Fire the OnExpected event handlers. This is considered a low-level method and should be used with 
        /// caution only if there is not another more suitable alternative.
        /// </summary>        
        /// <typeparam name="T">IComparable datatype that these values represent</typeparam>
        /// <param name="expectedValue">Value that is expected</param>
        /// <param name="targetValue">Value being tested</param>
        /// <param name="message">Custom message with .Net {0} string format place holders</param>
        /// <param name="args">List of arguments that can be supplied to <paramref name="message"/>.  This will be
        /// added to final message</param>
        public  bool AssertExpected<T>(T targetValue, T expectedValue, string message, params object[] args) where T : IComparable
        {          
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.ApplicationName = CoreConfig.ApplicationKey;
            eventArgs.TestValue = targetValue;
            eventArgs.FirstValue = expectedValue;
            eventArgs.IsResult = GuardCompareResult.Success;
            eventArgs.Message = "";
            if (string.IsNullOrEmpty(message)==false)
            {
                eventArgs.Message  = TextUtils.StringFormat(message, args);
            }
            if (string.IsNullOrEmpty(eventArgs.Message)==false)
            {
                eventArgs.Message += " ";
            }
            eventArgs.Message += eventArgs.IsResult.ToString().ToUpper() + " ";
            eventArgs.Message += string.Format("{0} Test Value '{1}', ", typeof(T).FullName, targetValue);
            eventArgs.Message += string.Format("First Value '{0}'", expectedValue);
            eventArgs.Message = message;
            return AssertExpected(eventArgs);
        }

        /// <summary>
        /// Fire the OnExpected event handlers. This is considered a low-level method and should be used with 
        /// caution only if there is not another more suitable alternative.
        /// </summary>        
        /// <typeparam name="T">nullable struct datatype that these values represent</typeparam>
        /// <param name="expectedValue">Value that is expected</param>
        /// <param name="targetValue">Value being tested</param>
        /// <param name="message">Custom message with .Net {0} string format place holders</param>
        /// <param name="args">List of arguments that can be supplied to <paramref name="message"/>.  This will be
        /// added to final message</param>
        public  bool AssertExpected<T>(T? targetValue, T? expectedValue, string message, params object[] args) where T : struct
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.TestValue  = targetValue;
            eventArgs.FirstValue = expectedValue;
            eventArgs.IsResult = GuardCompareResult.Success;
            if (string.IsNullOrEmpty(message))
            {
                message = TextUtils.StringFormat(message, args);
            }
            if (string.IsNullOrEmpty(eventArgs.Message) == false)
            {
                eventArgs.Message += " ";
            }
            eventArgs.Message += eventArgs.IsResult.ToString().ToUpper() + " ";
            eventArgs.Message += string.Format("{0}? Test Value: '{1}', ", typeof(T).FullName, targetValue);
            eventArgs.Message += string.Format("First Value: '{0}'", expectedValue);
            eventArgs.Message = message;
            eventArgs.Message = message;
            return AssertExpected(eventArgs);
        }


        /// <summary>
        /// Fire the OnExpected event handlers. This is considered a low-level method and should be used with 
        /// caution only if there is not another more suitable alternative.
        /// </summary>        
        /// <typeparam name="T">IComparable datatype that these values represent</typeparam>
        /// <param name="lowerValue">Lower boundry for a range</param>
        /// <param name="upperValue">Upper boundry for a range</param>
        /// <param name="testValue">Value being tested</param>
        /// <param name="message">Custom message with .Net {0} string format place holders</param>
        /// <param name="args">List of arguments that can be supplied to <paramref name="message"/>.  This will be
        /// added to final message</param>
        public  bool AssertExpectedRange<T>(T testValue, T lowerValue, T upperValue, string message, params object[] args) where T:IComparable
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.ApplicationName = CoreConfig.ApplicationKey;
            eventArgs.TestValue = testValue;
            eventArgs.FirstValue = lowerValue;
            eventArgs.SecondValue = upperValue;
            eventArgs.IsResult = GuardCompareResult.Success;
            eventArgs.Message = "";
            if (string.IsNullOrEmpty(message) == false)
            {
                eventArgs.Message = TextUtils.StringFormat(message, args);
            }
            if (string.IsNullOrEmpty(eventArgs.Message) == false)
            {
                eventArgs.Message += " ";
            }
            eventArgs.Message += eventArgs.IsResult.ToString().ToUpper() + " ";
            eventArgs.Message += string.Format("{0} Test Value: '{1}', ", typeof(T).FullName, lowerValue);
            eventArgs.Message += string.Format("First Value: '{0}'", testValue);
            eventArgs.Message += string.Format("Second Value: '{0}", upperValue);
            eventArgs.Message = message;
            return AssertExpected(eventArgs);
        }


        /// <summary>
        /// Fire the OnExpected event handlers. This is considered a low-level method and should be used with 
        /// caution only if there is not another more suitable alternative.
        /// </summary>        
        /// <typeparam name="T">nullable struct datatype that these values represent</typeparam>
        /// <param name="lowerValue">Lower boundry for a range</param>
        /// <param name="upperValue">Upper boundry for a range</param>
        /// <param name="testValue">Value being tested</param>
        /// <param name="message">Custom message with .Net {0} string format place holders</param>
        /// <param name="args">List of arguments that can be supplied to <paramref name="message"/>.  This will be
        /// added to final message</param>
        public  bool AssertExpectedRange<T>(T? testValue, T? lowerValue, T? upperValue, string message, params object[] args) where T : struct
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.ApplicationName = CoreConfig.ApplicationKey;
            eventArgs.IsResult = GuardCompareResult.Success;
            eventArgs.TestValue = testValue;
            eventArgs.FirstValue = lowerValue;
            eventArgs.SecondValue = upperValue;
            eventArgs.Message = "";
            if (string.IsNullOrEmpty(message) == false)
            {
                eventArgs.Message = TextUtils.StringFormat(message, args);
            }
            if (string.IsNullOrEmpty(eventArgs.Message) == false)
            {
                eventArgs.Message += " ";
            }
            eventArgs.Message += eventArgs.IsResult.ToString().ToUpper() + " ";
            eventArgs.Message += string.Format("Nullable<{0}> Test Value: '{1}', ", typeof(T).FullName, lowerValue);
            eventArgs.Message += string.Format("First Value: '{0}'", testValue);
            eventArgs.Message += string.Format("Second Value: '{0}", upperValue);
            eventArgs.Message = message;
            return AssertExpected(eventArgs);
        }

        
        /// <summary>
        /// Fire the OnUnExpected event handlers. This is considered a low-level method and should be used with 
        /// caution only if there is not another more suitable alternative.
        /// </summary>        
        /// <param name="targetValue">Value that is being verified</param>
        /// <param name="expectedValue">Value that is expected</param>
        /// <param name="message">Custom message with .Net {0} string format place holders</param>
        /// <param name="args">List of arguments that can be supplied to <paramref name="message"/>.  This will be
        /// added to final message</param>
        public  bool AssertUnexpected<T>(T targetValue, T expectedValue, string message, params object[] args) where T : IComparable
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.ApplicationName = CoreConfig.ApplicationKey;
            eventArgs.TestValue = targetValue;
            eventArgs.FirstValue = expectedValue;
            eventArgs.IsResult = GuardCompareResult.Failure;
            eventArgs.Message = "";
            if (string.IsNullOrEmpty(message) == false)
            {
                eventArgs.Message = TextUtils.StringFormat(message, args);
            }
            if (string.IsNullOrEmpty(eventArgs.Message) == false)
            {
                eventArgs.Message += " ";
            }
            eventArgs.Message += eventArgs.IsResult.ToString().ToUpper() + " ";
            eventArgs.Message += string.Format("{0} Test Value '{1}', ", typeof(T).FullName, targetValue);
            eventArgs.Message += string.Format("First Value '{0}'", expectedValue);
            eventArgs.Message = message;
            return AssertUnexpected(eventArgs);
        }


        /// <summary>
        /// Fire the OnUnExpected event handlers. This is considered a low-level method and should be used with 
        /// caution only if there is not another more suitable alternative.
        /// </summary>        
        /// <param name="targetValue">Value that is being verified</param>
        /// <param name="expectedValue">Value that is expected</param>
        /// <param name="message">Custom message with .Net {0} string format place holders</param>
        /// <param name="args">List of arguments that can be supplied to <paramref name="message"/>.  This will be
        /// added to final message</param>
        public  bool AssertUnexpected<T>(T? targetValue, T? expectedValue, string message, params object[] args) where T : struct
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.TestValue = targetValue;
            eventArgs.FirstValue = expectedValue;
            eventArgs.IsResult = GuardCompareResult.Failure;
            if (string.IsNullOrEmpty(message))
            {
                message = TextUtils.StringFormat(message, args);
            }
            if (string.IsNullOrEmpty(eventArgs.Message) == false)
            {
                eventArgs.Message += " ";
            }
            eventArgs.Message += eventArgs.IsResult.ToString().ToUpper() + " ";
            eventArgs.Message += string.Format("Nullable<{0}> Test Value: '{1}', ", typeof(T).FullName, targetValue);
            eventArgs.Message += string.Format("First Value: '{0}'", expectedValue);
            eventArgs.Message = message;
            eventArgs.Message = message;
            return AssertUnexpected(eventArgs);
        }

        /// <summary>
        /// Fire OnUnexpectedHandler
        /// </summary>
        /// <typeparam name="T">IComparable reference type that will be compared</typeparam>
        /// <param name="testValue">Value that is being verified</param>
        /// <param name="lowerValue">Lower boundry of comparison</param>
        /// <param name="upperValue">Upper boundry of comparsion</param>
        /// <param name="message">Message to be passed on validation handler with optional format specifiers</param>
        /// <param name="args">Arugments to supply to message</param>
        /// <returns>Always returns false (validation did not end in expected result)</returns>
        public  bool AssertUnexpectedRange<T>(T testValue, T lowerValue, T upperValue, string message, params object[] args) where T : IComparable
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.ApplicationName = CoreConfig.ApplicationKey;
            eventArgs.IsResult = GuardCompareResult.Failure;
            eventArgs.TestValue = testValue;
            eventArgs.FirstValue = lowerValue;
            eventArgs.SecondValue = upperValue;
            eventArgs.Message = "";
            if (string.IsNullOrEmpty(message) == false)
            {
                eventArgs.Message = TextUtils.StringFormat(message, args);
            }
            if (string.IsNullOrEmpty(eventArgs.Message) == false)
            {
                eventArgs.Message += " ";
            }
            eventArgs.Message += eventArgs.IsResult.ToString().ToUpper() + " ";
            eventArgs.Message += string.Format("{0} Test Value: '{1}', ", typeof(T).FullName, lowerValue);
            eventArgs.Message += string.Format("First Value: '{0}'", testValue);
            eventArgs.Message += string.Format("Second Value: '{0}", upperValue);
            eventArgs.Message = message;
            return AssertUnexpected(eventArgs);
        }

        /// <summary>
        /// Fire OnUnexpectedHandler
        /// </summary>
        /// <typeparam name="T">Nullable value type that will be compared</typeparam>
        /// <param name="testValue">Value that is being verified</param>
        /// <param name="lowerValue">Lower boundry of comparison</param>
        /// <param name="upperValue">Upper boundry of comparsion</param>
        /// <param name="message">Message to be passed on validation handler with optional format specifiers</param>
        /// <param name="args">Arugments to supply to message</param>
        /// <returns>Always returns false (validation did not end in expected result)</returns>
        public  bool AssertUnexpectedRange<T>(T? testValue, T? lowerValue, T? upperValue, string message, params object[] args) where T : struct
        {
            GuardEventArgs eventArgs = new GuardEventArgs();
            eventArgs.ApplicationName = CoreConfig.ApplicationKey;
            eventArgs.IsResult = GuardCompareResult.Failure;
            eventArgs.TestValue = testValue;
            eventArgs.FirstValue = lowerValue;
            eventArgs.SecondValue = upperValue;
            eventArgs.Message = "";
            if (string.IsNullOrEmpty(message) == false)
            {
                eventArgs.Message = TextUtils.StringFormat(message, args);
            }
            if (string.IsNullOrEmpty(eventArgs.Message) == false)
            {
                eventArgs.Message += " ";
            }
            eventArgs.Message += eventArgs.IsResult.ToString().ToUpper() + " ";
            eventArgs.Message += string.Format("Nullable<{0}> Test Value: '{1}', ", typeof(T).FullName, lowerValue);
            eventArgs.Message += string.Format("First Value: '{0}'", testValue);
            eventArgs.Message += string.Format("Second Value: '{0}", upperValue);
            eventArgs.Message = message;
            return AssertUnexpected(eventArgs);
        }

        /// <summary>
        /// Method that is called by default when validation fails.  Overwrite this to customize response
        /// </summary>
        /// <param name="eventArgs">Information about the value that is being Validated</param>
        private static void _defaultUnexpectedHandler(GuardEventArgs eventArgs)
        {
            //GuardInvalidException pe = new GuardInvalidException(eventArgs.ApplicationName, eventArgs.Message);
            //pe.Data.Add("ValidationDetails", eventArgs);
            ////Log.Error(pe);
            //throw pe;
            //do nothing

        }
    }
}
