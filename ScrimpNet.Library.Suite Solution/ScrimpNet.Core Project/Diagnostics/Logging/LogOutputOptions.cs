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
    /// Sets which fields the caller wishes to persist to logging store.  It is up to
    /// listener providers to honor the caller's request.  Default setting is adequate in most cases.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue"), 
    Citation(".Net Framework 2.0")]
    [Flags]
    public enum LogOutputOptions
    {
        /// <summary>
        /// Use a simplified version or LogMessage.MessageFormat if specified.  Used in object's ToString() method or use
        /// output options defined in TraceListener.
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Include the embedded exception's ToString()
        /// </summary>
        IncludeException = 1,

        /// <summary>
        /// Include all public properties of exception; including DataCollection calling ToString() on each of it's embedded elements
        /// </summary>
        ExceptionDetail = 2,

        /// <summary>
        /// Recurse all InnerExceptions. 
        /// </summary>
        ExceptionRecursive = 4,

        /// <summary>
        /// Include any Notes attached to message
        /// </summary>
        Notes = 8,

        /// <summary>
        /// Include any machine context information at the moment this message was created
        /// </summary>
        RuntimeMachine = 16,

        /// <summary>
        /// Include a complete HTTPRequest at the moment this message was created.
        /// </summary>
        RuntimeHttp = 32,

        /// <summary>
        /// Include list of data objects as stored in object
        /// </summary>
        IncludeData = 64,

        /// <summary>
        /// Include a list of all public fields and properties of object
        /// </summary>
        DataDetail = 128,

        /// <summary>
        /// Recurse each object's inheritance chain up to, but not including, System or Microsoft namespaces
        /// </summary>
        DataRecursion = 256,

        /// <summary>
        /// Serialize complete object to log store
        /// </summary>
        SerializeData = 512,

        /// <summary>
        /// Output string representation of log level
        /// </summary>
        LogLevel = 1024,

        /// <summary>
        /// Consider System.Diagnostics.TraceOptions
        /// </summary>
        TraceOutputOptions = 2048
    }
}
