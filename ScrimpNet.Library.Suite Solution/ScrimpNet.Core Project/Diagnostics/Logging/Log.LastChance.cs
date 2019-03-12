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
using System.IO;
using System.Diagnostics;
using ScrimpNet.Text;

namespace ScrimpNet.Diagnostics
{
    public sealed partial class Log
    {

        /// <summary>
        /// Write a message to last chance log.  Use LogLevel.Warning since this shouldn't happen in most cases
        /// </summary>
        /// <param name="message">Message text to log</param>
        /// <param name="args">List of optional arguements to message</param>
        public static void LastChanceLog(string message, params object[] args)
        {
            LastChanceLog(MessageLevel.Warning, message, args);
        }

        /// <summary>
        /// Write an exception to last chance log.  Use LogLevel.Error since in most cases logging exception to
        /// last chance log is due to extremely low level exceptions.
        /// </summary>
        public static void LastChanceLog(Exception ex)
        {
            LastChanceLog(MessageLevel.Error, Utils.Expand(ex)); 
        }
        /// <summary>
        /// Last chance logging if primary logging sink fails.  Usually used on application boot strapping.  Log file will be located in
        /// either the file location of Log.LastChangeLogFile or in the default folder of the running application.  NOTE:  Bypasses
        /// any configured log provider (log4Net, NLog, TraceLog, etc) and writes directly to text file.  Use with extreme care and is
        /// not configured for high performance.
        /// </summary>
        /// <param name="logLevel">Level this message will be logged at</param>
        /// <param name="message">Text of message</param>
        /// <param name="args">Arguments to supply to message text (if any)</param>
        public static void LastChanceLog(MessageLevel logLevel, string message, params object[] args)
        {
            string logMessage = "";
            try
            {
                _lastChanceLock.EnterWriteLock();

                if (CoreConfig.Log.IsLastChanceLogEnabled == false) return; // last chance logging not enabled

                logMessage = TextUtils.StringFormat("{0:yyyy-MM-dd HH:mm:ss.fff} {1} {2}", DateTime.Now, logLevel.ToString().ToUpper(), TextUtils.StringFormat(message, args));

                File.AppendAllText(CoreConfig.Log.LastChanceLogFile, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                try // write to event log if unable to log to last chance file.  This is really the last chance of the last chance!
                {
                    EventLog.WriteEntry("Application Error", string.Format(logMessage, args), EventLogEntryType.Error);
                    EventLog.WriteEntry("Application Error", string.Format(ex.ToString()), EventLogEntryType.Error);
                }
                catch (Exception ex2) //couldn't write log to last chance log file or event viewer so swallow exception
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("Error in LastChanceLog: {0}{1}{2}", ex2.Message, Environment.NewLine, logMessage));
                }
            }
            finally
            {
                _lastChanceLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Write an entry to LastChanceLog.  NOTE:  Application should rarely write information directly to LastChanceLog; except in special use-cases
        /// particularly around application bootstrapping and teardown.
        /// </summary>
        /// <param name="ex">Exception that should be logged</param>
        /// <param name="messageFormat">Message including standard string.format specifiers</param>
        /// <param name="args">Arguments, if any, that should be supplied to string</param>
        public static void LastChanceLog(Exception ex, string messageFormat, params object[] args)
        {
            LastChanceLog(MessageLevel.Error, "{0}\n{1}", Utils.Expand(ex), TextUtils.StringFormat(messageFormat, args));
        }

    }
}
