/*
// ScrimpNet.Core Library
// Copyright © 2005-2011
//
// This module is Copyright © 2005-2011 Steve Powell
// All rights reserved.
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the Microsoft Public License (Ms-PL)
// 
// This library is distributed in the hope that it will be
// useful, but WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
// details.
//
// You should have received a copy of the Microsoft Public License (Ms-PL)
// License along with this library; if not you may 
// find it here: http://www.opensource.org/licenses/ms-pl.html
//
// Steve Powell, spowell@scrimpnet.com
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;


namespace ScrimpNet.Reflection
{
    [DataContract(Namespace = CoreConfig.WcfNamespace)]
    public partial class AssemblyVersion
    {
        /// <summary>
        /// Properties of caller.
        /// </summary>
        [DataContract(Namespace = CoreConfig.WcfNamespace)]
        public class CallerInfo
        {
            /// <summary>
            /// Gets the AssemblyCompany attribute value of assembly containing this call
            /// </summary>
            [DataMember]
            public string Company;

            /// <summary>
            /// Parent method that called
            /// </summary>
            [DataMember]
            public string MethodName;

            /// <summary>
            /// Parent class that called
            /// </summary>
            [DataMember]
            public string ClassName;

            /// <summary>
            /// Parent assembly that called
            /// </summary>
            [DataMember]
            public string AssemblyName;

            /// <summary>
            /// AssemblyVersion attribute
            /// </summary>
            [DataMember]
            public string AssemblyVersion;

            /// <summary>
            /// How many layers between active method and the caller method
            /// </summary>
            [DataMember]
            public int CallDepth;

            /// <summary>
            /// Default constructor
            /// </summary>
            public CallerInfo() { }

            /// <summary>
            /// Reflect through the stack building appropriate values
            /// </summary>
            /// <param name="sf">Stack position of calling method</param>
            public CallerInfo(StackFrame sf)
                : this(sf.GetMethod())
            {

            }

            /// <summary>
            /// Constructor.  
            /// </summary>
            /// <param name="method">Method for which to get information about</param>
            public CallerInfo(MethodBase method)
            {
                ClassName = method.ReflectedType.FullName;
                MethodName = method.Name;
                Assembly asm = method.ReflectedType.Assembly;
                AssemblyName = asm.GetName().Name;                
                AssemblyVersion = asm.GetName().Version.ToString();
                //object[] attrs = asm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true);
                //if (attrs != null && attrs.Length > 0)
                //{
                //    AssemblyInformationalVersionAttribute attr = attrs[0] as AssemblyInformationalVersionAttribute;
                //    if (attr != null)
                //    {
                //        PublishName = attr.InformationalVersion;
                //    }
                //}
                string Location = asm.Location;
                
                //AssemblyCompanyAttribute;
                //AssemblyCopyrightAttribute;
                //AssemblyDescriptionAttribute;
                //AssemblyFileVersionAttribute;
                //AssemblyInformationalVersionAttribute;
                //AssemblyProductAttribute;
                //AssemblyTitleAttribute;
                //AssemblyTrademarkAttribute;
                
            }


            /// <summary>
            /// Builds a common representation for this class
            /// </summary>
            /// <returns>Hydrated string</returns>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("   Class Name: {0}{1}", this.ClassName, Environment.NewLine);
                sb.AppendFormat("   Method Name: {0}{1}", this.MethodName, Environment.NewLine);
                sb.AppendFormat("   Assembly Name: {0}{1}", this.AssemblyName,Environment.NewLine);
                sb.AppendFormat("   Assembly Version: {0}{1}", this.AssemblyVersion, Environment.NewLine);

                return sb.ToString();
            }
        }

    }
}
