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

    /// <summary>
    /// Information that is passed to the handler when an validation event (OnExpected,OnUnexpected) fires
    /// </summary>

    public class GuardEventArgs : EventArgs
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public GuardEventArgs() { }

        string _applicationName = CoreConfig.ApplicationKey;

        /// <summary>
        /// Name of application that is triggering the event
        /// </summary>
        public string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = value; }
        }

        /// <summary>
        /// Value that is being tests or evaluated
        /// </summary>
        public object TestValue { get; set; }

        /// <summary>
        /// First value; also called lowerBoundery, or left depending on type of comparison.
        /// Binary comparisons use this value
        /// </summary>
        public object FirstValue { get; set; }

        /// <summary>
        /// Second value of comparison; also called upperBoundery or right depending on type of comparison.
        /// Binary comparisons typically ignore this parameter
        /// </summary>
        public object SecondValue { get; set; }

        /// <summary>
        /// Gets the result of the comparison
        /// </summary>
        public GuardCompareResult IsResult { get; set; }
        private string _message;
        /// <summary>
        /// Standard .Net format string for the message that is passed into Validation routine
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        ///// <summary>
        ///// Convert this event into standard string format
        ///// </summary>
        ///// <returns>Formatted string with populated properties</returns>
        //public override string ToString()
        //{
        //    string msg = IsResult.ToString().ToUpper();
        //    msg += ": ";
        //    if (Message != null)
        //    {
        //        msg += Message;                
        //    }
        //    msg += string.Format(" EvaluatedTarget({0}) '{1}', ExpectedValue({2}) '{3}'",
        //                         EvaluatedValue.GetType().FullName,
        //                         EvaluatedValue,
        //                         ExpectedValue.GetType().FullName,
        //                         ExpectedValue);
        //    return msg;            
        //}
    }
}
