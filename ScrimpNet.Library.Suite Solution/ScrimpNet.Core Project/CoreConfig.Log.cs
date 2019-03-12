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
using ScrimpNet.Configuration;
using ScrimpNet.Diagnostics;
using ScrimpNet.Diagnostics.Logging;
using System.IO;
using ScrimpNet.Reflection;
using ScrimpNet.Text;
using DotNetConfig = System.Configuration;
using System.Configuration;
using System.Web;
namespace ScrimpNet
{
    /// <summary>
    /// Configuration values for ScrimpNet.Core classes
    /// </summary>
    public partial class CoreConfig
    {
        /// <summary>
        /// Logging configuration for LastChance and global logging boot strapping values
        /// </summary>
        public static class Log
        {

            /// <summary>
            /// {app}.Log.{ts}
            /// </summary>
            private const string FILE_PREFIX = "{app}.{ts}";

            /// <summary>
            /// 5
            /// </summary>
            private const int MAX_FILE_SIZE_MB = 5;

            /// <summary>
            /// log
            /// </summary>
            private const string FILE_EXTENSION = "log";

            /// <summary>
            /// yyyyMMdd
            /// </summary>
            private const string TIMESTAMP_FORMAT = "yyyyMMdd";

            private static LoggerLevels? _loggerLevels;
            /// <summary>
            /// Determines which levels of debugging will be active.  More than one element in Application.LogLevels can be comma delimited. (Key: Logging.LogLevels, Default: Vital)
            /// </summary>
            public static LoggerLevels LoggerLevels
            {
                get
                {
                    if (_loggerLevels.HasValue == true)
                    {
                        return _loggerLevels.Value;
                    }
                    _loggerLevels = ConfigManager.AppSetting<LoggerLevels>("ScrimpNet.Diagnostics.LoggerLevels", LoggerLevels.Vital);
                    return _loggerLevels.Value;
                }
            }

            /// <summary>
            /// Determines if LastChanceLog is enabled or not.  (Key:Logging.IsLastChanceLogEnabled.  Default:true)
            /// </summary>
            /// <remarks>
            /// Using native ConfigurationManager calls here since ConfigUtils...calls IsLastChangeLogEnabled
            /// </remarks>
            internal static bool IsLastChanceLogEnabled
            {
                get
                {
                    string configValue = ConfigurationManager.AppSettings["ScrimpNet.Logging.IsLastChanceLogEnabled"];
                    if (string.IsNullOrEmpty(configValue) == true)
                    {
                        return true;
                    }
                    else
                    {
                        try
                        {
                            return Transform.ConvertValue<bool>(configValue);
                        }
                        catch
                        {
                            return true;  //default to true if error converting Logging.IsLastChanceLogEnabled
                        }
                    }
                }
            }

            /// <summary>
            /// Fully qualified name where last chance logging messages will be written to.key=Log.LastChanceLogFolder. Default: executingFolder/appKey.LastChance.yyyyMMdd.log
            /// </summary>
            internal static string LastChanceLogFile
            {
                get
                {
                    string fileName = ConfigurationManager.AppSettings["ScrimpNet.Logging.LastChanceLogFile"];
                    if (string.IsNullOrEmpty(fileName) == true)
                    {
                        fileName = "{app}.LastChance.{ts}.log";
                    }
                    
                    fileName = ConfigManager.ResolveValueSetting(fileName);

                    fileName = Path.Combine(LastChanceLogFolder, ReplaceTokens(fileName));
                    fileName = fileName.Replace("{ts}", string.Format("{0:" + TimeStampFormat + "}", DateTime.Now));
                    return fileName;
                }
            }

            static string _lastChanceLogFolder;
            /// <summary>
            /// Folder where last chance log file will be stored.  If ScrimpNet.Logging.LastChanceLogFolder key cannot be found default to /bin folder
            /// </summary>
            internal static string LastChanceLogFolder
            {
                get
                {
                    if (string.IsNullOrEmpty(_lastChanceLogFolder) == false) return _lastChanceLogFolder;

                     _lastChanceLogFolder = ConfigurationManager.AppSettings["ScrimpNet.Logging.LastChanceLogFolder"];
                    if (string.IsNullOrEmpty(_lastChanceLogFolder) == true || Directory.Exists(_lastChanceLogFolder) == false)
                    {
                        if (HttpContext.Current != null)  //web application
                        {
                            _lastChanceLogFolder = HttpContext.Current.Server.MapPath("~/app_data");
                        }
                        else //forms application
                        {
                            _lastChanceLogFolder = System.AppDomain.CurrentDomain.RelativeSearchPath;
                            if (string.IsNullOrEmpty(_lastChanceLogFolder))
                            {
                                _lastChanceLogFolder = Directory.GetCurrentDirectory();
                            }
                        }
                    }
                    
                    _lastChanceLogFolder = ReplaceTokens(_lastChanceLogFolder).Replace("{ts}", string.Format("{0:" + TimeStampFormat + "}", DateTime.Now));
                    return _lastChanceLogFolder;
                }
            }

            /// <summary>
            /// Returns the format string (e.g. yyyy-MM-dd) for time stamps within a folder or path.  This value
            /// is common for all logging paths and files. (Default: yyyyMMdd)
            /// </summary>
            public static string TimeStampFormat
            {
             get
             {
                 string configValue = ConfigurationManager.AppSettings["ScrimpNet.Logging.Default.TimestampFormat"];
                 if (string.IsNullOrEmpty(configValue)==true)
                 {
                     return TIMESTAMP_FORMAT;
                 }
                 return configValue;
                 //Logging.Default.TimestampFormat
             }
            }
            private static ILogDispatcher _activePersister;

            /// <summary>
            /// Load the configured persister for logging sub-system.  Default: Internal rolling log file persister
            /// </summary>
            /// <remarks>
            /// If not configured, probe the configuration file for known types
            /// </remarks>
            public static ILogDispatcher ActiveDispatcher
            {
                get
                {
                    if (_activePersister != null)
                    {
                        return _activePersister;
                    }
                    string persisterClassName = ConfigManager.AppSetting<string>("ScrimpNet.Logging.Dispatcher", "ScrimpNet.Diagnostics.InternalLogFileDispatcher,ScrimpNet.Core");
           
                    try
                    {
                        _activePersister = ProviderFactory<ILogDispatcher>.GetInstance(persisterClassName);
                        if (_activePersister == null)
                        {
                            ScrimpNet.Diagnostics.Log.LastChanceLog("Unable to create instance of {0} as ILogDispatcher", persisterClassName);
                            throw new DotNetConfig.ConfigurationErrorsException(TextUtils.StringFormat("Unable to create instance of {0} as {1}", persisterClassName,typeof(ILogDispatcher).FullName));
                        }
                    }
                    catch (Exception ex)
                    {
                        ScrimpNet.Diagnostics.Log.LastChanceLog(ex, "Unable to create instance of {0} as ILogDispatcher", persisterClassName);
                        throw new DotNetConfig.ConfigurationErrorsException(TextUtils.StringFormat("Unable to create instance of {0} as {1}", persisterClassName,typeof(ILogDispatcher).FullName), ex);
                    }

                    return _activePersister;
                }
                set
                {
                    _activePersister = value;
                }
            }

            private static string _logFolder;
            /// <summary>
            /// Default: applications' working folder.  Key:Logging.Default.LogFolder
            /// </summary>
            public static string LogFolder
            {
             get
             {
                 if (string.IsNullOrEmpty(_logFolder) == false) return _logFolder;

                 _logFolder =  ReplaceTokens(ConfigManager.AppSetting<string>("ScrimpNet.Logging.Default.LogFolder", CoreConfig.Log.LastChanceLogFolder));
                 if (Directory.Exists(_logFolder) == false)
                 {
                     _logFolder = Directory.GetCurrentDirectory();
                 }
                 return _logFolder;
             }
            }

            /// <summary>
            /// [app].Log.{ts} Key: Logging.Default.FilePrefix
            /// </summary>
            public static string LogFilePrefix
            {
             get
             {
                 return CoreConfig.Log.ReplaceTokens(ConfigManager.AppSetting<string>("ScrimpNet.Logging.Default.FilePrefix", FILE_PREFIX)); 
             }
            }

            /// <summary>
            /// Default: 5
            /// </summary>
            public static int MaximumFileSizeMb
            {
                get
                {
                    return ConfigManager.AppSetting<int>("ScrimpNet.Logging.Default.MaxFileSizeMb", MAX_FILE_SIZE_MB);
                }
            }
            /// <summary>
            /// Default: log
            /// </summary>
            public static string LogFileExtension
            {
                get
                {
                    return CoreConfig.Log.ReplaceTokens(ConfigManager.AppSetting<string>("ScrimpNet.Logging.Default.FileExtension", FILE_EXTENSION));
                }
            }
            /// <summary>
            /// Method to expand tokens within a string
            /// </summary>
            /// <param name="tokenizedString">String with one of these tokens {env}, {app}, {machine}, {user} </param>
            /// <returns>A string with tokens expanded</returns>
            /// <remarks>
            /// <para>{env} - active environment</para>
            /// <para>{app} - application key</para>
            /// <para>{machine} - machine name</para>
            /// <para>{user} - user name</para>
            /// <para>{ts} - timestamp format tokens (</para>
            /// </remarks>
            public static string ReplaceTokens(string tokenizedString)
            {
                return ConfigManager.ResolveKeyTokens(tokenizedString);
            }
        }
    }
}
