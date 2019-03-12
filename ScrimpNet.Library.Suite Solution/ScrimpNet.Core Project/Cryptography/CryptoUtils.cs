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
using System.Security.Cryptography;
using ScrimpNet.Web;

namespace ScrimpNet.Cryptography
{
    /// <summary>
    /// Wrapper class for most ScrimpNet.Cryptography behaviors and functions.  Used to take advantage of Intellisense and to make routine encryption tasks easier
    /// </summary>
    public partial class CryptoUtils
    {
       
        /// <summary>
        ///  Convert a known string format to a series of bytes
        /// </summary>
        /// <param name="value">String that will be converted</param>
        /// <param name="inputFormat">Known format of string</param>
        /// <returns>Bytes as discovered by format</returns>
        public static byte[] ToBytes(string value, StringFormat inputFormat)
        {
            switch (inputFormat)
            {
                case StringFormat.Base64:
                    return Convert.FromBase64String(value);
                case StringFormat.Hex:
                    return Transform.HexStringToBytes(value);
                case StringFormat.Unicode:
                    return Transform.StringToBytes(value);
                default:
                    throw ExceptionFactory.New<InvalidOperationException>("'{0}' is an unknown string format", inputFormat.ToString());
            }
        }

        /// <summary>
        /// Convert a series of bytes into a known format.  These are generally lightweight wrappers around known .net functions
        /// </summary>
        /// <param name="source">non-null hydrated bytes to convert into a string type</param>
        /// <param name="outputFormat">How string will represent bytes of data</param>
        /// <returns>Byte values converted into a known formatted string</returns>
        public static string FromBytes(byte[] source, StringFormat outputFormat)
        {
            switch (outputFormat)
            {
                case StringFormat.Base64:
                    return Convert.ToBase64String(source);
                case StringFormat.Hex:
                    return Transform.BytesToHexString(source);
                case StringFormat.Unicode:
                    return Transform.StringFromBytes(source);
                default:
                    throw ExceptionFactory.New<InvalidOperationException>("'{0}' is an unknown string format", outputFormat.ToString());
            }
        }
    }
}
