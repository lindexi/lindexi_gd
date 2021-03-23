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

namespace ScrimpNet.Diagnostics
{
    /// <summary>
    /// Defines severities of messages logger will persist
    /// </summary>
    [Flags]
    public enum LoggerLevels
    {
        /// <summary>
        /// Logging turned off or do not log
        /// </summary>
        None = 0,

        /// <summary>
        /// Verbose logging that is not generally available at production run-time.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Similar to DEBUG but can be used for routine EnterMethod and ExitMethod tracing of application flow.  Usually turned off at production time.
        /// </summary>
        Trace = 2,

        /// <summary>
        /// Miscellaneous text to be sent to logging store. Similar to Debug or Trace but is often turned ON at production time so care must be taken 
        /// of how much data is being persisted with this level.
        /// </summary>
        Information = 4,

        /// <summary>
        /// Warning could be failure that effects a small number of users, or there is a work around.
        /// </summary>
        Warning = 8,

        /// <summary>
        /// Error could be a medium failure that needs attention, but does not keep the system from running. (Default in most cases)
        /// </summary>
        Error = 16,

        /// <summary>
        /// Critical could be a catastrophic failure that someone needs to be notified immediatly. Critial error or application crash.
        /// </summary>
        Critical = 32,

        /// <summary>
        /// Debug, Trace and Information.  Usually turned off in production but turned on in development.  Warning, Error, Critical are always activated unless explicitly filtered
        /// </summary>
        Verbose = Debug | Trace | Information | Vital,

        /// <summary>
        ///  Warning, Error, or Critial messages
        /// </summary>
        Vital = Warning | Error | Critical,

        /// <summary>
        /// All log levels
        /// </summary>
        All = Verbose | Vital
    }
}
