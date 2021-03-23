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

namespace ScrimpNet.Configuration
{
    /// <summary>
    /// WARNING:  Use only on non-nullable .Net native types also including DateTime and Guid
    /// </summary>
    /// <remarks>
    /// Table of .Net Types: http://msdn.microsoft.com/en-us/library/ya5y69ds.aspx
    /// </remarks>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple=false,Inherited = false)]
    public class ConfigSettingAttribute:Attribute 
    {
        /// <summary>
        /// Use the property name as the configuration key.  NOTE:  This
        /// is strongly discouraged as it requries knowledge of how the configuration
        /// system works for one to understand what key belongs to which property.
        /// (Default: ConfigResolutionEnum.OverwriteRemoteWithLocal)
        /// </summary>
        public ConfigSettingAttribute()
        {
            KeyResolution = ConfigScopeEnumeration.OverwriteRemoteWithLocal;
        }

        /// <summary>
        /// Use ConfigResolutionEnum.OverwriteRemoteWithLocal
        /// </summary>
        /// <param name="configKey">Key in configuration store whose value will be retireved.  Note:  May use any of the standard {config} meta tags</param>
        public ConfigSettingAttribute(string configKey):this(configKey, ConfigScopeEnumeration.OverwriteRemoteWithLocal)
        {
        }

        /// <summary>
        /// Use ConfigResolutionEnum.OverwriteRemoteWithLocal
        /// </summary>
        /// <param name="configKey">Key in configuration store whose value will be retireved.  Note:  May use any of the standard {config} meta tags</param>
        /// <param name="defaultValue">Value to use if <paramref name="configKey"/> cannot be found</param>
        public ConfigSettingAttribute(string configKey, string defaultValue)
            : this(configKey, defaultValue, ConfigScopeEnumeration.OverwriteRemoteWithLocal)
        {
        }


        /// <summary>
        /// Use ConfigResolutionEnum.OverwriteRemoteWithLocal
        /// </summary>
        /// <param name="configKey">Key in configuration store whose value will be retireved.  Note:  May use any of the standard {config} meta tags</param>
        /// <param name="defaultValue">Value to use if <paramref name="configKey"/> cannot be found</param>
        /// <param name="resolution">Determines tie breaking rules if key is discovered in both remote store and local .config</param>
        public ConfigSettingAttribute(string configKey, string defaultValue,ConfigScopeEnumeration resolution)
        {
            ConfigKey = configKey;
            KeyResolution = resolution;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Use ConfigResolutionEnum.OverwriteRemoteWithLocal
        /// </summary>
        /// <param name="configKey">Key in configuration store whose value will be retireved.  Note:  May use any of the standard {config} meta tags</param>
        /// <param name="resolution">Determines tie breaking rules if key is discovered in both remote store and local .config</param>
        public ConfigSettingAttribute(string configKey, ConfigScopeEnumeration resolution):this(configKey, null,resolution)
        {  
        }

        /// <summary>
        /// Key in configuration store whose value will be retireved.  Note:  May use any of the standard {config} meta tags
        /// </summary>
        public string ConfigKey { get; set; }

        /// <summary>
        /// Determines tie breaking rules if key is discovered in both remote store and local .config
        /// </summary>
        public ConfigScopeEnumeration  KeyResolution { get; set; }

        /// <summary>
        /// Default value if key is not found in configuration pipeline.  If null or empty then throw exception on missing key
        /// </summary>
        public string DefaultValue { get; set; }
    }
}
