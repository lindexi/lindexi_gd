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
using System.Reflection;
using System.Configuration;
using ScrimpNet.Diagnostics;

namespace ScrimpNet.Configuration
{
    /// <summary>
    /// Use reflection to read .config files and registered configuration pipleline sources and populate properties with the [ConfigSetting] attribute.
    /// </summary>
    /// <typeparam name="T">Your class that contains your configuration entries.  T is used to reflect and extract static properties</typeparam>
    /// <remarks>Uses ConfigUtils.AppSettings&lt;&gt;() to perform actual retrieval</remarks>
    public class ConfigBase<T> where T:ConfigBase<T>,new()
    {
        private static bool _isConfigLoaded = loadStaticProperties();  // this loads all 'static' child properties with [ConfigSetting] attributes on first access

        static ConfigBase()
        {
            //loadStaticProperties();
        }
        /// <summary>
        /// Force a load of all static configuration properties. This should be called in child class' static constructor
        /// </summary>
        /// <returns>Always true.  This method is a stub that forces static property initialization</returns>
        public static bool Initialize()
        {
           // dummy method that forces static variables to be loaded since calling static methods on child classes does not invoke initialization on parent classes
            if (_isConfigLoaded == false)
            {
                _isConfigLoaded = loadStaticProperties();
            }
            return true;
        }
        
        /// <summary>
        /// Default constructor that loads instance properties from configuration store(s)
        /// </summary>
        public ConfigBase()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{3}{0,-30} {1,-30} {2,-30}", "Key", "Property", "Value",Environment.NewLine);
            sb.AppendLine("{0}",new string('-',113));

            PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (var prop in properties)
            {
                List<ConfigSettingAttribute> attribs = prop.GetCustomAttributes<ConfigSettingAttribute>();  //check for [ConfigSetting] attribute
                if (attribs.Count == 0) continue;
                var attrib = attribs[0];
                if (string.IsNullOrEmpty(attrib.ConfigKey) == true) // no explicit key specified so use the name of the property
                {
                    attrib.ConfigKey = prop.Name;
                }
                if (string.IsNullOrEmpty(attrib.DefaultValue))
                {
                    prop.SetValue(this, getAppSetting(attrib.ConfigKey, prop.PropertyType), null); //set value on instance properties with no default
                }
                else
                {
                    prop.SetValue(this, getAppSetting(attrib.ConfigKey, prop.PropertyType,attrib.DefaultValue), null); //set value on instance properties using provided default
                }
                sb.AppendLine("{0,-30} {1,-30} {2,-30} {3,-20}", attrib.ConfigKey, prop.Name, prop.GetValue(this, null), attrib.KeyResolution.ToString());
            }
            Log.LastChanceLog(MessageLevel.Information, sb.ToString());
            //_isConfigLoaded = loadStaticProperties(this);
        }

        /// <summary>
        /// Load static properties from configuration store(s)
        /// </summary>
        /// <returns></returns>
        private static bool loadStaticProperties()
        {
            if (isMissingStaticConstructor() == true) //this check will catch only some use-cases.  If child class is all static methods the check won't execute
            {
                string errMsg = string.Format("Class '{0}' must have a static constructor that should contain 'ConfigBase<{0}>.Initialize();' to initialize static properties",
                    typeof(T).GetType().Name);
                throw new ConfigurationErrorsException(errMsg);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{3}{0,-30} {1,-30} {2,-30}", "Key", "Property", "Value", Environment.NewLine);
            sb.AppendLine("{0}", new string('-', 113));

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (var prop in properties)
            {
                List<ConfigSettingAttribute> attribs = prop.GetCustomAttributes<ConfigSettingAttribute>();  //check for [ConfigSetting] attribute
                if (attribs.Count == 0) continue;
                var attrib = attribs[0];
                if (string.IsNullOrEmpty(attrib.ConfigKey) == true) // no explicit key specified so use the name of the property
                {
                    attrib.ConfigKey = prop.Name;
                }
                if (string.IsNullOrEmpty(attrib.DefaultValue))
                {
                    prop.SetValue(null, getAppSetting(attrib.ConfigKey, prop.PropertyType), BindingFlags.Static, null, null, null); //set value on static properties with no default
                }
                else
                {
                    prop.SetValue(null, getAppSetting(attrib.ConfigKey, prop.PropertyType, attrib.DefaultValue), null);  //set value on static properties using provided default
                }

                sb.AppendLine("{0,-30} {1,-30} {2,-30}{3,-20}", attrib.ConfigKey, prop.Name, prop.GetValue(null,null), attrib.KeyResolution.ToString());
            }

            Log.LastChanceLog(MessageLevel.Information, sb.ToString());
            return true;
        }

        /// <summary>
        /// Check to see if child class has a static constructor if 1) there are static properties and 2) the static properties have the [ConfigSetting] attribute
        /// </summary>
        /// <returns></returns>
        private static bool isMissingStaticConstructor()
        {
            bool needsConstructor = false;

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (var prop in properties)
            {
                List<ConfigSettingAttribute> attribs = prop.GetCustomAttributes<ConfigSettingAttribute>();  //check for [ConfigSetting] attribute
                if (attribs.Count == 0) continue;
                needsConstructor = true;
                break;
            }
        
            if (needsConstructor == false) return false; // no static [ConfigSetting] property no constructor needed

            ConstructorInfo[] ctors = typeof(T).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (ctors.Length == 0 && needsConstructor) return true; //no static constructors but there are static [ConfigSetting] properties

            return false; // do not need static constructor
        }

        /// <summary>
        /// Call ConfigUtils.AppSetting<>() to get a value
        /// </summary>
        /// <param name="configKey">setting key to retrieve</param>
        /// <param name="t">Type to convert setting value into</param>
        /// <returns>Converted setting</returns>
        /// <remarks>Reflection is needed to invoke ConfigUtils.AppSetting since generics do not take a specific type</remarks>
        private static object getAppSetting(string configKey, Type t)
        {
            Type configType = typeof(ConfigManager);
            var methodList = configType.GetMethods();
            var mi = methodList.FirstOrDefault(mi4 => mi4.GetParameters().Length == 1 && mi4.Name == "AppSetting");
            MethodInfo miGeneric = mi.MakeGenericMethod(t);
            object[] args = { configKey};
            return miGeneric.Invoke(null, args);
        }

        /// <summary>
        /// Call ConfigUtils.AppSetting<>() to get a value or default to parameter
        /// </summary>
        /// <param name="configKey">setting key to retrieve</param>
        /// <param name="defaultValue">String representation of default value</param>
        /// <param name="t">Type to convert setting value into</param>
        /// <returns>Converted setting</returns>
        /// <remarks>Reflection is needed to invoice ConfigUtils.AppSetting since generics do not take a specific type</remarks>
        private static object getAppSetting(string configKey, Type t, string defaultValue)
        {
            Type configType = typeof(ConfigManager);
            var methodList = configType.GetMethods();
            var mi = methodList.FirstOrDefault(mi4 => mi4.GetParameters().Length == 2 && mi4.Name == "AppSetting");
            MethodInfo miGeneric = mi.MakeGenericMethod(t);
            object defaultObject = internalTransform(t, defaultValue);
            object[] args = { configKey, defaultObject};
            return miGeneric.Invoke(null, args);
        }

        /// <summary>
        /// Call Transform.ConvertTo<>() to convert default value into a strong type.  Used in retrieving default values.
        /// </summary>
        /// <param name="t">Type string will be converted into</param>
        /// <param name="defaultValue">Value that will be converted into Type t</param>
        /// <returns>Converted value</returns>
        private static object internalTransform(Type t, string defaultValue)
        {
            Type configType = typeof(Transform);
            var methodList = configType.GetMethods();
            var mi = methodList.FirstOrDefault(mi4 => mi4.GetParameters().Length == 1 && mi4.Name == "ConvertValue");
            MethodInfo miGeneric = mi.MakeGenericMethod(t);
            object[] args = { defaultValue };
            return miGeneric.Invoke(null, args);
        }
    }
}
