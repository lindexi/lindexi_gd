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
	public partial class Log
	{
        /// <summary>
        /// Write a message to DEBUG log.  Replace message's internal LogLevel status
        /// </summary>
        /// <param name="message">message to write</param>
        public delegate void LogDebug(LogMessage message);

        /// <summary>
        /// Write a message to TRACE log.  Replace message's internal LogLevel status
        /// </summary>
        /// <param name="message">message to write</param>
        public delegate void LogTrace(LogMessage message);

        /// <summary>
        /// Write a message to INFO log.  Replace message's internal LogLevel status
        /// </summary>
        /// <param name="message">message to write</param>
        public delegate void LogInformation(LogMessage message);

        /// <summary>
        /// Write a message to WARNING log.  Replace message's internal LogLevel status
        /// </summary>
        /// <param name="message">message to write</param>
        public delegate void LogWarning(LogMessage message);

        /// <summary>
        /// Write a message to ERROR log.  Replace message's internal LogLevel status
        /// </summary>
        /// <param name="message">message to write</param>
        public delegate void LogError(LogMessage message);

        /// <summary>
        /// Write a message to Critical log.  Replace message's internal LogLevel status
        /// </summary>
        /// <param name="message">message to write</param>
        public delegate void LogCritical(LogMessage message);

        /// <summary>
        /// Write a message to log using message's internal LogLevel status
        /// </summary>
        /// <param name="message">message to write</param>
        public delegate void LogWrite(LogMessage message);
 

        /// <summary>
        /// Fired when provider wants to write a debug message
        /// </summary>
        public event LogDebug OnLogDebugEventHandler;

        /// <summary>
        /// Fired when a provider wants to write a warning message
        /// </summary>
        public event LogWarning OnLogWarningEventHandler;

        /// <summary>
        /// Fired when a provider wants to write a trace message
        /// </summary>
        public event LogTrace OnLogTraceEventHandler;

        /// <summary>
        /// Fired when a provider wants to write an info message
        /// </summary>
        public event LogInformation OnLogInformationEventHandler;

        /// <summary>
        /// Fired when a provider wants to write an error message
        /// </summary>
        public event LogError OnLogErrorEventHandler;

        /// <summary>
        /// Fired when a provider wants to write a Critial message
        /// </summary>
        public event LogCritical OnLogCriticalEventHandler;

   	}
}
