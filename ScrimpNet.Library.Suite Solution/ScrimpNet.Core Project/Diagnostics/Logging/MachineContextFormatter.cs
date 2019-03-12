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
using System.Threading;

namespace ScrimpNet.Diagnostics.Logging
{
    /// <summary>
    /// Specifies how the machine context will be converted into a string
    /// </summary>
    public sealed class MachineContextFormatter : IFormatProvider, ICustomFormatter 
    {
        #region IFormatProvider Members

    
        /// <summary>
        /// String.Format calls this method to get an instance of an
        /// ICustomFormatter to handle the formatting
        /// </summary>
        /// <param name="formatType">Object that might have a formatter in it</param>
        /// <returns>Formatter</returns>
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return this;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region ICustomFormatter Members

        /// <summary>
        /// Parses a format string element and returns the appropriate machine context object
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg">Fully hydrated RuntimeContext argument</param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Separate fields with ^.  </para>
        /// <para>AD app domain</para>
        /// <para>HN host (machine) name</para>
        /// <para>ID user identity</para>
        /// <para>IP comma separated list of host IP's on this computer</para>
        /// <para>MN method name</para>
        /// <para>OS version</para>
        /// <para>PN process name</para>
        /// <para>TI thread id</para>
        /// <para>TN thread name</para>        
        /// <para>UD user domain</para>
        /// <para>UN user name</para>        
        /// 
        /// <para>AS fully qualified assembly name</para>
        /// <para>as only assembly name</para>
        /// <para>CN class name (full)</para>
        /// <para>CV CLR runtime version</para>        
        /// <para>ME[format] memory for process</para>
        /// <para>NL insert new line</para>        
        /// <para>PI process id</para>        
        /// <para>SP[count] insert count spaces</para>
        /// <para>ST stack trace</para>
        /// <para>ZZ[field separator] all.  ZZNLSP4 inserts new line after each field and spaces 4 characters</para>        
        /// </remarks>
        public string Format(string format, object arg, IFormatProvider formatProvider)

        {
            
            if (string.IsNullOrEmpty(format) && arg != null)
            {
                return String.Format("{0}", arg);
            }
            if (arg == null)
            {
                return format;
            }
            string retval = "";

            MachineContext ctx = arg as MachineContext;
            
            switch (format)
            {
                case "AD": retval = ctx.AppDomainName;
                    break;
                case "CN": retval = ctx.ClassName;
                    break;
                case "HN": retval = ctx.HostName;
                    break;
                case "ID": retval = ctx.Identity;
                    break;
                case "IP": retval = ctx.IPAddressList;
                    break;
                case "MN": retval = ctx.MethodName;
                    break;
                case "OS": retval = Environment.OSVersion.VersionString;
                    break;
                case "PN": retval = ctx.ProcessName;
                    break;
                case "TI": retval = Thread.CurrentThread.ManagedThreadId.ToString();
                    break;
                case "TN": retval = ctx.ManagedThreadName;
                    break;
                case "UN": retval = Environment.UserName;
                    break;
                case "UD": retval = ctx.UserDomainName;
                    break;
                default:
                    //If the object to be formatted supports the IFormattable
                    //interface, pass the format specifier to the 
                    //objects ToString method for formatting.
                    if (arg is IFormattable)
                    {
                        retval = ((IFormattable)arg).ToString(format, formatProvider);
                    }
                    //If the object does not support IFormattable, 
                    //call the objects ToString method with no additional
                    //formatting. 
                    else if (arg != null)
                    {
                        retval= arg.ToString();
                    }
                    break;
            }
            return retval;
        }


        #endregion
    }
}
