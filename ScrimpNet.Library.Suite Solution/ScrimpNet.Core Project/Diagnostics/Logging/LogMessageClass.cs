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

namespace ScrimpNet.Diagnostics
{
    /// <summary>
    /// Different classes of log messages.
    /// </summary>
    public enum LogMessageClass
    {
        /// <summary>
        /// Logging for IIS logs
        /// </summary>
        IISLogging,
        /// <summary>
        /// WCF Logging
        /// </summary>
        WCFLogging,
        /// <summary>
        /// Logging for tracing
        /// </summary>
        TraceLogging,
        /// <summary>
        /// Logging for error message or debug messages.  Multi-purpose logging format
        /// </summary>
        ApplicationLogging,

        /// <summary>
        /// Logging of any generic object.  Specific content is indeterminant
        /// </summary>
        Binary,
    }


}
