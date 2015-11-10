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
using System.Collections;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Security.Cryptography;

namespace ScrimpNet.Cryptography
{
    public partial class Crypto
    {
        public class SegmentTypes
        {
            public const string SymmetricKey = "scrimpnet.com/cryptography/segment/key";
            public const string SymmetricIv = "scrimpnet.com/cryptography/segment/initializationVector";
            public const string HashSalt = "scrimpnet.com/cryptography/segment/hashSalt";
        }

        public class PropertyValueTypes
        {
            public const string String = "scrimpnet.com/cryptography/propertyValue/string";
            public const string DateTime = "scrimpnet.com/cryptography/propertyValue/datetime";
            public const string UniqueIdentifier = "scrimpnet.com/cryptography/propertyValue/uniqueIdentifier";
            public const string Boolean = "scrimpnet.com/cryptography/propertyValue/boolean";
            public const string Number = "scrimpnet.com/cryptography/propertyValue/number";
            public const string Object = "scrimpnet.com/cryptography/propertyValue/base64";
        }

        public class PropertyTypes
        {
        //    public const string ValidFrom = "scrimpnet.com/cryptography/property/validFrom";
        //    public const string ValidTo = "scrimpnet.com/cryptography/property/validTo";
        //    public const string IsEffective = "scrimpnet.com/cryptography/property/isEffective";
        //    public const string ExternalReference = "scrimpnet.com/cryptography/property/externalReference";
        //    public const string SplitReference = "scrimpnet.com/cryptography/property/splitReference";
            public const string HashSaltedMode = "scrimpnet.com/cryptography/property/saltedHashMode";
            public const string HashMode = "scrimpnet.com/cryptography/property/simpleHashMode";
            public const string EncodeMode = "scrimpnet.com/cryptography/property/encodeMode";
        }

        public class EncodeModes
        {
            public const string AES = "scrimpnet.com/cryptography/mode/aes";
            public const string DES = "scrimpnet.com/cryptography/mode/des";            
            public const string RC2 = "scrimpnet.com/cryptography/mode/rc2";
            public const string Rijindael = "scrimpnet.com/cryptography/mode/rijindael";
            public const string TripleDES = "scrimpnet.com/cryptography/mode/tripleDES";
        }

        public class HashModesSimple
        {
            public const string MD5 = "scrimpnet.com/cryptography/hash/MD5";
            public const string SHA1 = "scrimpnet.com/cryptography/hash/SHA1";
            public const string SHA256 = "scrimpnet.com/cryptography/hash/SHA256";
            public const string SHA384 = "scrimpnet.com/cryptography/hash/SHA384";
            public const string SHA512 = "scrimpnet.com/cryptography/hash/SHA512";
            public const string RIPEMD160 = "scrimpnet.com/cryptography/hash/RIPEMD160";   
        }

        public class HashModesSalted
        {
            public const string HMACSHA1 = "scrimpnet.com/cryptography/hashkeyed/HMACSHA1";
            public const string HMACSHA256 = "scrimpnet.com/cryptography/hashkeyed/HMACSHA256";
            public const string HMACSHA384 = "scrimpnet.com/cryptography/hashkeyed/HMACSHA384";
            public const string HMACSHA512 = "scrimpnet.com/cryptography/hashkeyed/HMACSHA512";
            public const string HMACMD5 = "scrimpnet.com/cryptography/hashkeyed/HMACMD5";
            public const string HMACRIPEMD160 = "scrimpnet.com/cryptography/hashkeyed/HMACRIPEMD160";
            public const string MACTrippleDES = "scrimpnet.com/cryptography/hashkeyed/MACTripleDES";
        }
    }
}
