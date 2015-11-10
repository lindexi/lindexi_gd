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
using System.ComponentModel;
using System.Text.RegularExpressions;
using ScrimpNet.Diagnostics;
using ScrimpNet;
using ScrimpNet.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;
using System.Reflection;
using System.Configuration.Internal;
using ScrimpNet.Configuration.ConfigSystems;
namespace ScrimpNet.Configuration
{
    /// <summary>
    /// Methods to retrieve values from connection strings.  Contains FSFH (fail-fast, fail-hard) logic and appropriate logging
    /// </summary>
    public sealed partial class ConfigManager
    {
        static bool _usingExternalSource = Initialize(); //force loading external configuration system before any other calls to ConfigManager
        /// <summary>
        /// Load external configuration sources, if configured
        /// </summary>
        /// <returns></returns>
        public static bool Initialize()
        {
            //// hack our proxy IInternalConfigSystem into the ConfigurationManager
            //try
            //{
            //    if (string.IsNullOrEmpty(CoreConfig.Configuration.ExternalSource) == true)
            //    {
            //        Log.LastChanceLog(LogLevel.Information, "Unable to determine external configuration setting source.  Using only .Net default .config settings");
            //        return true; //
            //    }
            //    // inject custom config setting source into ConfigurationManager
            //    FieldInfo s_configSystem = typeof(ConfigurationManager).GetField("s_configSystem", BindingFlags.Static | BindingFlags.NonPublic);
            //    IInternalConfigSystem externalSource = Reflection.ProviderFactory<IInternalConfigSystem>.GetInstance(CoreConfig.Configuration.ExternalSource, s_configSystem.GetValue(null));
            //    s_configSystem.SetValue(null, externalSource);
            //}
            //catch(Exception ex)
            //{
            //    Log.LastChanceLog(ex, "Unable to set external source of configuration data to '{0}'.  Be sure assembly exists and implements 'System.Configuration.IInternalConfigSystem'", CoreConfig.Configuration.ExternalSource);
            //}
            //return true; //doesn't matter what we return.  Value is ignored
            return true;
        }
        
        /// <summary>
        /// Get a required value from AppSettings section of the configuration pipeline.  Expands any embedded config metadata tags
        /// </summary>
        /// <typeparam name="T">Type to cast result to</typeparam>
        /// <param name="key">Key in AppSettings section containing value</param>
        /// <returns>Converted type</returns>
        /// <exception cref="ConfigurationErrorsException">Thrown if key cannot be found or converted into requested type</exception>
        public static T AppSetting<T>(string key)
        {
            string configValue = ConfigurationManager.AppSettings[ResolveValueSetting(key)];
            if (configValue == null)
            {
                string msg = string.Format("Unable to find required <AppSettings> key = '{0}' (resolved: {1})in .config file or configuration pipeline", key, ResolveValueSetting(key));
                Log.LastChanceLog( MessageLevel.Error,msg);
                throw new ConfigurationErrorsException(msg);
            }
            try
            {
                configValue = ResolveValueSetting(configValue);
                return configValue.ConvertTo<T>();
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unable to convert key '{2}[{0}]' to type of {1}", configValue, typeof(T).FullName, key);
                Log.LastChanceLog(ex, msg);
                throw new ConfigurationErrorsException(msg, ex);
            }
        }

        /// <summary>
        /// Get an optional value from AppSettings section of the configuration pipeline.  Expands any embedded config metadata tags
        /// </summary>
        /// <typeparam name="T">Type to cast result to</typeparam>
        /// <param name="key">Key in AppSettings section containing value</param>
        /// <param name="defaultValue">default to use if key is not found in configuration file</param>
        /// <returns>Converted type</returns>
        /// <exception cref="ConfigurationErrorsException">Throw when configuration value is provided but cannot be converted into T</exception>
        public static T AppSetting<T>(string key, T defaultValue)
        {

            var expandedKey = ResolveValueSetting(key);
            string configValue = ConfigurationManager.AppSettings[expandedKey];
            if (configValue == null)
            {
                string msg = string.Format("Unable to find <AppSettings> key='{0}' in .config or configuration pipeline.  Using default value '{1}' instead",
                    expandedKey, defaultValue.ToString());
                Log.LastChanceLog(MessageLevel.Warning, msg);
                return defaultValue;
            }
            try
            {
                configValue = ResolveValueSetting(configValue);
                return configValue.ConvertTo<T>();
            }
            catch (Exception ex)
            {
                string msg = string.Format("Unable to convert key '{2}[{0}]' to type of {1}.", configValue, typeof(T).FullName, key);
                Log.LastChanceLog(ex, msg);
                throw new ConfigurationErrorsException(msg, ex);
            }
        }

        /// <summary>
        /// Gets a connection string from .config or configuration pipeline
        /// </summary>
        /// <param name="key">Key in connection string to get</param>
        /// <returns>Connection string as found in .config file</returns>
        /// <exception cref="ConfigurationException">If key cannot be found in .config file</exception>
        public static string ConnectionString(string key)
        {
            string expandedKey = ResolveValueSetting(key);
            ConnectionStringSettings configValue = ConfigurationManager.ConnectionStrings[expandedKey];
            if (configValue == null)
            {
                string msg = string.Format("Unable to find <ConnectionStrings> key = '{0}' in .config file or configuration pipeline",
                    expandedKey);
                Log.LastChanceLog(msg);
                throw new ConfigurationErrorsException(msg);
            }
            return ResolveValueSetting(configValue.ConnectionString);
        }

        /// <summary>
        /// Gets a SQL connection from .config or configuration pipeline
        /// </summary>
        /// <param name="key">Key in connection string to get</param>
        /// <returns>An instance of a provider's connection</returns>
        /// <exception cref="ConfigurationErrorsException">If key cannot be found in .config file</exception>
        public static IDbConnection Connection(string key)
        {
            string expandedKey = ResolveValueSetting(key);
            ConnectionStringSettings configValue = ConfigurationManager.ConnectionStrings[expandedKey];
            if (configValue == null)
            {
                string msg = string.Format("Unable to find <ConnectionStrings> key = \"{0}\" in .config file or configuration pipeline",
                    expandedKey);
                Log.LastChanceLog(msg);
                throw new ConfigurationErrorsException(msg);
            }

            // DbProviderFactory DBProvider = DbProviderFactories.GetFactory(configValue.ProviderName);
            return (IDbConnection)new SqlConnection(configValue.ConnectionString);
            //return DBProvider.CreateConnection();
        }


        /// <summary>
        /// Method to expand tokens within a key
        /// </summary>
        /// <param name="tokenizedString">String with one of these tokens {env}, {app}, {machine}, {user} </param>
        /// <returns>A string with tokens expanded</returns>
        /// <remarks>
        /// <para>{env} - active environment</para>
        /// <para>{app} - application key</para>
        /// <para>{machine} - machine name</para>
        /// <para>{user} - user name</para>
        /// </remarks>
        public static string ResolveKeyTokens(string tokenizedString) //EXTEND
        {
            if (tokenizedString == null) return tokenizedString;
            
            return tokenizedString.Replace("{machine}", Environment.MachineName).Replace("{env}", CoreConfig.ActiveEnvironment).Replace("{app}", CoreConfig.ApplicationKey).Replace("{user}", Environment.UserName);
        }

        /// <summary>
        /// '{%'
        /// </summary>
        private const string START_TOKEN = "{%"; //CoreConfig.Configuration.StartToken;
        /// <summary>
        /// '%}'
        /// </summary>
        private const string STOP_TOKEN = "%}"; //CoreConfig.Configuration.StopToken;

        private static string settingWithDefault(string key, string defaultValue)
        {
            string configValue = ConfigurationManager.AppSettings[key];
            if (configValue == null)
            {
                return defaultValue;
            }
            return configValue;
        }

        /// <summary>
        /// Expand ScrimpNet configuration metadata tokens
        /// </summary>
        /// <param name="configValue">String containing one or more tokens</param>
        /// <returns>Fully expanded value</returns>
        public static string ResolveValueSetting(string configValue)
        {
            try
            {
                //-------------------------------------------------------
                // key was not found or value was empty string
                //-------------------------------------------------------
                if (string.IsNullOrEmpty(configValue) == true)
                {
                    return configValue;
                }

                configValue = ResolveKeyTokens(configValue); //{machine}, {env}, {app}, {user}

                //-------------------------------------------------------
                // look for any expansion indicators 
                //-------------------------------------------------------
                var tokenPattern = new Regex(START_TOKEN + @"([^" + START_TOKEN + "]+):([^" + STOP_TOKEN + "]+)" + STOP_TOKEN, RegexOptions.Compiled);
                var matches = tokenPattern.Matches(configValue);
                if (matches.Count == 0)  // no expansion needed
                {
                    return configValue;
                }

                //-------------------------------------------------------
                // recursively expand all CONFIG_TOKEN elements
                // as defined by START_TOKEN and STOP_TOKEN
                //-------------------------------------------------------
                for (var x = matches.Count - 1; x >= 0; x--)
                {
                    Match match = matches[x];
                    var fullKey = match.Groups[0].Value;
                    var sectionIdentifier = match.Groups[1].Value.ToLower();
                    string configKey = match.Groups[2].Value;
                    string resolvedValue = "";
                    switch (sectionIdentifier)
                    {
                        case "app":
                            configValue = configValue.Replace(fullKey, configKey);
                            resolvedValue = ConfigurationManager.AppSettings[configValue];
                            resolvedValue = ResolveValueSetting(resolvedValue);
                            break;
                        case "connection":
                            resolvedValue =
                                resolvedValue = ConfigurationManager.ConnectionStrings[ResolveKeyTokens(configKey)].ConnectionString;
                            resolvedValue = ResolveValueSetting(resolvedValue);
                            break;
                        default:
                            throw new ConfigurationErrorsException(TextUtils.StringFormat("Unable to resolve key: '{0}'", match.Groups[0].Value));
                    }
                    if (string.IsNullOrEmpty(resolvedValue) == false)
                    {
                        resolvedValue = ResolveKeyTokens(resolvedValue);
                        return resolvedValue;
                    }
                    int tokenPosition = configValue.IndexOf(fullKey);  //replace CONFIG_TOKEN with actual resolved value
                    configValue = configValue.Remove(tokenPosition, fullKey.Length);
                    configValue = configValue.Insert(tokenPosition, resolvedValue);
                }
                return configValue;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionStringName"></param>
        /// <param name="connectionString"></param>
        /// <param name="providerName">(e.g. "System.Data.SqlClient")</param>
        public static void AddConnectionString(string connectionStringName, string connectionString, string providerName)
        {
            var csSetting = new ConnectionStringSettings(connectionStringName, connectionString, providerName);
            var readonlyField = typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            readonlyField.SetValue(ConfigurationManager.ConnectionStrings, false);

            var baseAddMethod = typeof(ConfigurationElementCollection).GetMethod("BaseAdd", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(ConfigurationElement) }, null);
            baseAddMethod.Invoke(ConfigurationManager.ConnectionStrings, new object[] { csSetting });

            readonlyField.SetValue(ConfigurationManager.ConnectionStrings, true);
        }

        /// <summary>
        /// Return a formatted string of all application settings.  Each line is delimited by Environment.NewLine
        /// </summary>
        /// <returns>List of all AppSettings</returns>
        public static string EffectiveAppSettings()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                sb.AppendLine("AppSettings['{0}'] = '{1}'", key, ConfigurationManager.AppSettings[key]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Return a formatted string of all connection strings.  Each line is delimited by Environment.NewLine
        /// </summary>
        /// <returns>List of connection strings</returns>
        public static string EffectiveConnectionStrings()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ConnectionStringSettings setting in ConfigurationManager.ConnectionStrings)
            {
                sb.AppendLine("ConnectionString['{0}'] = '{1}'",
                    setting.Name,
                    setting.ConnectionString);
            }
            return sb.ToString();
        }
    }
}
