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
using System.Diagnostics;
using System.Threading;


namespace ScrimpNet.Diagnostics
{
    /// <summary>
    /// 
    /// </summary>
	public partial class Log 
	{
        private static Dictionary<string, Log> _dictionary = new Dictionary<string, Log>();
        //  (.Net 3.x locking) private static System.Threading.ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        private string _logName;
        /// <summary>
        /// Name of this particular log.
        /// </summary>
        public string LogName
        {
            get { return _logName; }
            set { _logName = value; }
        }
        /// <summary>
        /// Create a new log file with the application name and in execution folder.
        /// </summary>
        /// <returns></returns>
        public static Log NewLog()
        {
            return NewLog(findKey());
        }


        /// <summary>
        /// Create a default logger using full name of type (pattern from NHibernate)
        /// </summary>
        /// <param name="type">Type that will be using this logger</param>
        /// <returns>Instance of logger</returns>
        public static Log NewLog(Type type)
        {
            return NewLog(type.FullName);
            
        }

        /// <summary>
        /// Create a new log with a specific name.  LoggerLevel is set to 'Vital' (Information, Warning, Error, Critical)
        /// </summary>/// <param name="logName">Name of log (in logging system).  May not map to actual operating system file name</param>
        /// <returns>Created log</returns>
        public static Log NewLog(string logName)
        {
            return NewLog(logName, CoreConfig.Log.LoggerLevels);
        }

        /// <summary>
        /// Create a new log with a specific name and specific logging level.
        /// </summary>
        /// <param name="logName">Name of log (in logging system).  May not map to actual operating system file name</param>
        /// <param name="logLevels">Amount of logging this log will do</param>
        /// <returns>Created log</returns>
        public static Log NewLog(string logName, LoggerLevels logLevels)
        {
            if (_dictionary.ContainsKey(logName) == true)
            {
                _dictionary[logName].SetLogLevel(logLevels, true);
                return _dictionary[logName];
            }
            else
            {
                Log log = new Log(logName, logLevels);
                _dictionary[logName] = log;
                return log;
            }
        }

        /// <summary>
        /// Default constructor.  Hidden so instantiation must be through NewLog() methods
        /// </summary>
        static Log()
        {
            if (_lastChanceLock == null)
            {
                _lastChanceLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            }
        }

        private Log(string sourceName, LoggerLevels levels)
        {
            LogName = sourceName;

            //-------------------------------------------------------
            // wire-up default writers.  Applications might use
            // these events to hook into the event processing
            //  (e.g. have events log to a text box)
            //-------------------------------------------------------
            OnLogDebugEventHandler += writeDebug;
            OnLogErrorEventHandler += writeError;
            OnLogCriticalEventHandler += writeCritical;
            OnLogInformationEventHandler += writeInformation;
            OnLogTraceEventHandler += writeTrace;
            OnLogWarningEventHandler += writeWarning;
          
            SetLogLevel(levels, true); //turn on logging for Warning,Error,Critical
        }

	}
}

