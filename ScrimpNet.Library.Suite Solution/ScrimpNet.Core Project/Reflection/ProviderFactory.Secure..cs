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
using System.Security.Permissions;
using System.Reflection;
using System.Security;

namespace ScrimpNet.Reflection
{
       public static partial class ProviderFactory<T>
        {
        /// <summary>
        /// Invoke methods and classes after a security check
        /// </summary>
        [Citation("http://labs.developerfusion.co.uk/SourceViewer/browse.aspx?assembly=SSCLI&namespace=System.Windows.Forms&type=SecurityUtils")]
        public class Secure
        {
            /// <summary>
            /// Create an instance of T using a set of arguments
            /// </summary>
            /// <param name="args">Arguments to supply to constructor</param>
            /// <returns>Newly created instance of null</returns>
            public static T CreateInstance(object[] args)
            {
                Type type = typeof(T).GetType();
                if (((type.Assembly == typeof(ProviderFactory<T>).Assembly) && !type.IsPublic) && !type.IsNestedPublic)
                {
                    new ReflectionPermission(PermissionState.Unrestricted).Demand();
                }
                return (T)Activator.CreateInstance(type, args);
            }
            
            /// <summary>
            /// Creates an intance of a class of type T
            /// </summary>
            /// <param name="argTypes">List of Types that match the argument types of the constructor</param>
            /// <param name="args">List of values that match the arguments of the constructor</param>
            /// <param name="allowNonPublic">Can invoke a constructor that is not public</param>
            /// <returns>Instantiated class or null if not able to be created</returns>
            public static T ConstructorInvoke(Type[] argTypes, object[] args, bool allowNonPublic)
            {
                
                return (T)ConstructorInvoke(argTypes, args, allowNonPublic, BindingFlags.Default);
            }

            /// <summary>
            /// Creates an intance of a class of type T
            /// </summary>
            /// <param name="argTypes">List of Types that match the argument types of the constructor</param>
            /// <param name="args">List of values that match the arguments of the constructor</param>
            /// <param name="allowNonPublic">Can invoke a constructor that is not public</param>
            /// <param name="extraFlags">Customized binding flags to shape the constructor context</param>
            /// <returns>Instantiated class or null if not able to be created</returns>
            public static T ConstructorInvoke(Type[] argTypes, object[] args, bool allowNonPublic, BindingFlags extraFlags)
            {
                Type type = typeof(T).GetType();
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }
                BindingFlags bindingAttr = (BindingFlags.Public | BindingFlags.Instance) | extraFlags;
                if (type.Assembly == typeof(ProviderFactory<T>).Assembly)
                {
                    if (!type.IsPublic && !type.IsNestedPublic)
                    {
                        new ReflectionPermission(PermissionState.Unrestricted).Demand();
                    }
                    else if (allowNonPublic && !hasReflectionPermission)
                    {
                        allowNonPublic = false;
                    }
                }
                if (allowNonPublic)
                {
                    bindingAttr |= BindingFlags.NonPublic;
                }
                ConstructorInfo info = type.GetConstructor(bindingAttr, null, argTypes, null);
                if (info != null)
                {
                    return (T) info.Invoke(args);
                }
                throw ExceptionFactory.New<InvalidOperationException>("Unable to create instance of {0}", typeof(T).GetType().FullName);
            }
            /// <summary>
            /// Creates an intance of a class of type T
            /// </summary>
            /// <param name="args">List of values that match the arguments of the constructor</param>
            /// <param name="allowNonPublic">Can invoke a constructor that is not public</param>
            /// <returns>Instantiated class or null if not able to be created</returns>
            public static T CreateInstance(object[] args, bool allowNonPublic)
            {
                Type type = typeof(T).GetType();
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }
                BindingFlags bindingAttr = BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance;
                if (type.Assembly == typeof(ProviderFactory<T>).Assembly)
                {
                    if (!type.IsPublic && !type.IsNestedPublic)
                    {
                        new ReflectionPermission(PermissionState.Unrestricted).Demand();
                    }
                    else if (allowNonPublic && !hasReflectionPermission)
                    {
                        allowNonPublic = false;
                    }
                }
                if (allowNonPublic)
                {
                    bindingAttr |= BindingFlags.NonPublic;
                }
                return (T)Activator.CreateInstance(type, bindingAttr, null, args, null);
            }

            // Properties
            private static bool hasReflectionPermission
            {
                get
                {
                    try
                    {
                        new ReflectionPermission(PermissionState.Unrestricted).Demand();
                        return true;
                    }
                    catch (SecurityException)
                    {
                    }
                    return false;
                }
            }
        } //class Secure
    } 
 

}
