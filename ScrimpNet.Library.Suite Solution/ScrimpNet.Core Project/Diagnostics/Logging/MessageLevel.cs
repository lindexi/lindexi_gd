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
using System.Runtime.Serialization;

namespace ScrimpNet.Diagnostics
{
    /// <summary>
    /// Defines relative severity or importance of message being persisted.  Severity is the recommended level.  Log providers
    /// may change ultimate destination based on their own internal rules.  
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [DataContract(Namespace=CoreConfig.WcfNamespace)]
    public enum MessageLevel
    {
        /// <summary>
        /// Logging turned off or do not log
        /// </summary>
        [EnumMember]
        Off = 0,

        /// <summary>
        /// Verbose logging that is not generally available at production run-time.
        /// </summary>
        [EnumMember]
        Debug = 1,

        /// <summary>
        /// Similar to DEBUG but can be used for routine EnterMethod and ExitMethod tracing of application flow.  Usually turned off at production time.
        /// </summary>
        [EnumMember]
        Trace = 2,

        /// <summary>
        /// Miscellaneous text to be sent to logging store. Similar to Debug or Trace but is often turned ON at production time so care must be taken 
        /// of how much data is being persisted with this level.
        /// </summary>
        [EnumMember]
        Information = 4,

        /// <summary>
        /// Warning could be failure that effects a small number of users, or there is a work around.
        /// </summary>
        [EnumMember]
        Warning = 8,

        /// <summary>
        /// Error could be a medium failure that needs attention, but does not keep the system from running. (Default in most cases)
        /// </summary>
        [EnumMember]
        Error = 16,

        /// <summary>
        /// Critical could be a catastrophic failure that someone needs to be notified immediatly. Critial error or application crash.
        /// </summary>
        [EnumMember]
        Critical = 32,

    }
}
