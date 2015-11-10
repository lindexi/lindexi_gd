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
    /// Exception that is thrown when a Object Validation test fails.  Thrown by default handler
    /// </summary>

    public class GuardViolationException : Exception 
    {
        /// <summary>
        /// Name of object the GuardException was testing when Guard asserted
        /// </summary>
        public string ObjectName { get; set; }
        /// <summary>
        /// Standard constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
        private string _applicationName;
        /// <summary>
        /// <see cref="ArgumentException"> compatible constructor</see>
        /// </summary>
        /// <param name="applicationName">Name of application throwing this exception</param>
        /// <param name="message">Textual message of this exception</param>
        public GuardViolationException(string applicationName, string message)
            : base(message)
        {
            _applicationName = applicationName;
        }

        /// <summary>
        /// <see cref="ArgumentException" /> compatible constructor 
        /// </summary>
        /// <param name="applicationName">Name of application throwing this exception</param>
        /// <param name="message">Textual message of this exception</param>
        /// <param name="objectName">Variable name this is Unexpected</param>
        public GuardViolationException(string applicationName, string message, string objectName)
            : base(message)
        {
            _applicationName = applicationName;
            ObjectName = objectName;
        }

        /// <summary>
        /// Application containing method that is throwing this exception
        /// </summary>
        public string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = value; }
        }
    }
}
