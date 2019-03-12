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
using System.Runtime.Serialization;

namespace ScrimpNet.Cryptography
{
    /// <summary>
    /// Format of string return types
    /// </summary>
    [DataContract]
    public enum StringFormat
    {
        /// <summary>
        /// Base64 value.  Suitable for storing in text fields, .config etc.
        /// </summary>
        [EnumMember]
        Base64,

        /// <summary>
        /// Hex value. Suitable for storing in text fields, .config etc. Note: Hex takes more storage than Base64 but may be faster to generate/decode
        /// </summary>
        [EnumMember]
        Hex,

        /// <summary>
        /// Unicode (UTF-16) representation of bytes.  Suitable for storing in binary database fields.  Can be considered a 'binary' or 'raw' format or 'plain text'
        /// </summary>
        [EnumMember]
        Unicode
    }
}
