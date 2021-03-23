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
using System.Runtime.InteropServices;

namespace ScrimpNet
{
    public static partial class Utils
    {
        /// <summary>
        /// Windows operating system external calls
        /// </summary>
        public static class Win32
        {
            /// <summary>
            /// Win32 API Call
            /// </summary>
            /// <returns></returns>
            [DllImport("kernel32.dll")]
            public static extern int GetCurrentProcessId();

            /// <summary>
            /// Win32 API Call
            /// </summary>
            /// <param name="hModule"></param>
            /// <param name="lpFilename"></param>
            /// <param name="nSize"></param>
            /// <returns></returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [PreserveSig]
            public static extern int GetModuleFileName([In] IntPtr hModule, [Out] StringBuilder lpFilename, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

            /// <summary>
            /// Win32 API Call
            /// </summary>
            /// <param name="moduleName"></param>
            /// <returns></returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr GetModuleHandle(string moduleName);

            /// <summary>
            /// Win32 API Call
            /// </summary>
            /// <returns></returns>
            [DllImport("kernel32.dll")]
            public static extern int GetCurrentThreadId();
        }
    }
}
