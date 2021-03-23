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
using System.Reflection;

namespace ScrimpNet.Reflection
{
    //TODO Add multi-threaded locking on cache
    /// <summary>
    /// Create an instance of a class from an external assembly
    /// </summary>
    /// <typeparam name="T">Type of class to return</typeparam>
    public static partial class ProviderFactory<T>
    {
        private static Dictionary<string, Assembly> _asmCache = new Dictionary<string, Assembly>();

        /// <summary>
        /// Returns a new instance of a provider based class.  Executes default constructor of class
        /// </summary>
        /// <param name="typeName">Fully qualified type name for class</param>
        /// <param name="assemblyName">Fully qualified assembly name for class</param>
        /// <param name="isExternalReference">True if assemblyName is a fully qualified path name</param>
        /// <returns>Instantiated class</returns>
        public static T GetInstance(string typeName, string assemblyName, bool isExternalReference)
        {
            return GetInstance(typeName, assemblyName, isExternalReference, null);
        }

    

        /// <summary>
        /// Returns a new instance of a provider based class.  Executes the constructor
        /// of the class that contains a signature matching the arguments list.  If
        /// arguments is null, then execute default constructor
        /// </summary>
        /// <param name="typeName">Full.Type.Name to instantiate</param>
        /// <param name="assemblyName">Assembly.Name containing type to instantiate</param>
        /// <param name="arguments">Arguments to pass to type's constructor</param>
        /// <param name="isExternalReference">True if assemblyName is a fully qualified path name</param>
        /// <returns>Instantiated class</returns>
        public static T GetInstance(string typeName, string assemblyName, bool isExternalReference, params object[] arguments)
        {
            T type = (T) GetAssembly(assemblyName,isExternalReference).CreateInstance(typeName,
                true,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
                null, arguments, null, null);

            return type;
        }

        /// <summary>
        /// Returns a new instance of a provider based class.  Executes the constructor
        /// of the class that contains a signature matching the arguments list.  If
        /// arguments is null, then execute default constructor
        /// </summary>
        /// <param name="longName">Full.Type.Name,Assembly.Name</param>
        /// <param name="arguments">Arguments to pass to type's constructor</param>
        /// <returns></returns>
        public static T GetInstance(string longName, params object[] arguments)
        {
            string[] s = longName.Split(new char[] { ',' });
            if (s.Length != 2)
                throw new ArgumentException("longName must be of format: <fully qualified type name>,<assembly name>");
            return GetInstance(s[0], s[1], false,arguments);
        }

        /// <summary>
        /// Return a loaded assembly.  First from internal cache then from external assembly.  Generally a helper method.
        /// </summary>
        /// <param name="assemblyName">Assembly name for type,assembly combination</param>
        /// <param name="isExternalReference">True if assemblyName is a fully qualified path name</param>
        /// <returns>Loaded assembly.</returns>
        internal static Assembly GetAssembly(string assemblyName,bool isExternalReference)
        {
            string asmName = getAssemblyName(assemblyName);
            if (_asmCache.ContainsKey(asmName) == true)
                return _asmCache[asmName];
            if (isExternalReference == false)
            {
                _asmCache.Add(asmName, Assembly.Load(asmName)); // store in static list
            }
            else
            {
                _asmCache.Add(asmName, Assembly.LoadFile(asmName));
            }
            return GetAssembly(assemblyName,isExternalReference); // get assembly from cache
        }


        private static string getAssemblyName(string assemblyName)
        {
            string[] s = assemblyName.Split(new char[] { ',' }, StringSplitOptions.None);
            if (s.Length == 1) return s[0];  
            if (s.Length == 2) return s[1];
            throw new ArgumentException("Unable to find assemblyName in '" + assemblyName + "'");
        }
        private static string getTypeName(string typeName)
        {
            
            string[] s = typeName.Split(new char[] { ',' }, StringSplitOptions.None);
            if (s.Length >= 1) return s[0];

            throw new ArgumentException("Unable to find typeName in '" + typeName + "'");
        }
    } //ProviderFactory
} //namespace
