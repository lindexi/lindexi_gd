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
using System.Configuration;
using System.Reflection;

namespace ScrimpNet.Configuration
{
	public partial class ConfigManager
	{
        /// <summary>
        /// Modify underlaying ConnectionStrings parameters at runtime, bypassing inherent ReadOnly status
        /// </summary>
        public class ConnectionStrings
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            /// <param name="connectionString"></param>
            /// <param name="providerName"></param>
            public static void Add(string name, string connectionString, string providerName)
            {
                Add(new ConnectionStringSettings(name,connectionString,providerName));
            }
            /// <summary>
            /// Add a new string to collection 
            /// </summary>
            /// <param name="setting"></param>
            public static void Add(ConnectionStringSettings setting)
            {
                var readonlyField = typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                readonlyField.SetValue(ConfigurationManager.ConnectionStrings, false);
                ConfigurationManager.ConnectionStrings.Add(setting);
                readonlyField.SetValue(ConfigurationManager.ConnectionStrings, true);
            }

            /// <summary>
            /// Clear all exsist keys from collection
            /// </summary>
            public static void Clear()
            {
                var readonlyField = typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                readonlyField.SetValue(ConfigurationManager.ConnectionStrings, false);
                ConfigurationManager.ConnectionStrings.Clear();
                readonlyField.SetValue(ConfigurationManager.ConnectionStrings, true);
            }

            public static void Set(string name, string connectionString, string providerName)
            {
                Set(new ConnectionStringSettings(name, connectionString,providerName));
            }

            /// <summary>
            /// Update an existing key in the collection
            /// </summary>
            /// <param name="setting"></param>
            public static void Set(ConnectionStringSettings setting)
            {
                Remove(setting.Name);
                Add(setting);   
            }

            /// <summary>
            /// Remove an existing key from collection
            /// </summary>
            /// <param name="name"></param>
            public static void Remove(string name)
            {
                var readonlyField = typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
                readonlyField.SetValue(ConfigurationManager.ConnectionStrings, false);
                ConfigurationManager.ConnectionStrings.Remove(name);
                readonlyField.SetValue(ConfigurationManager.ConnectionStrings, true);
            }
        }
	}
}
