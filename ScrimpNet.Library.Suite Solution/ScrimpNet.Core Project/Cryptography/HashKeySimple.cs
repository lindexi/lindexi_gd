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
using System.Security.Cryptography;

namespace ScrimpNet.Cryptography
{
    /// <summary>
    /// Key suitable for generating hash values.  NOTE:  Use HashKeySalted for hashing with salt values
    /// </summary>
    public class HashKeySimple : CryptoKeyBase
    {
        /// <summary>
        /// Crypto.HashModesSimple.SHA256
        /// </summary>
        private static string _defaultHashProvider = Crypto.HashModesSimple.SHA256; //EXTENSION Change this for larger or smaller default hash values
        
        internal HashKeySimple()
            : base()
        {

        }

        /// <summary>
        /// Create a hash of a series of bytes
        /// </summary>
        /// <param name="plainBytes">Data to be hashed</param>
        /// <returns>Hashed value</returns>
        public byte[] Hash(byte[] plainBytes)
        {
            using (HashAlgorithm hasher = findHashPovider(Properties[Crypto.PropertyTypes.HashMode].Value))
            {
                return hasher.ComputeHash(plainBytes);
            }
        }

        private HashAlgorithm findHashPovider(string hashProviderConstant)
        {
            switch (hashProviderConstant)
            {
                case Crypto.HashModesSimple.MD5:
                    return new MD5Cng();
                case Crypto.HashModesSimple.SHA1:
                    return new SHA1Managed();
                case Crypto.HashModesSimple.SHA256:
                    return new SHA256Managed();
                case Crypto.HashModesSimple.SHA384:
                    return new SHA384Managed();
                case Crypto.HashModesSimple.SHA512:
                    return new SHA512Managed();
                case Crypto.HashModesSimple.RIPEMD160:
                    return new RIPEMD160Managed();
            }
            throw ExceptionFactory.New<InvalidOperationException>("Unable to find .Net hash provider for '{0}'", hashProviderConstant);
        }
        
        /// <summary>
        /// Create a key suitable for a hashing using a specific algorithm
        /// </summary>
        /// <param name="hashMode">One of the HashModesSimple constants</param>
        /// <returns>New hash key</returns>
        public static HashKeySimple Create(string hashMode)
        {
            var key = new HashKeySimple();
            key.Properties.Add(new KeyProperty()
            {
                PropertyType = Crypto.PropertyTypes.HashMode,
                Value = hashMode,
                ValueType = Crypto.PropertyValueTypes.String
            });

            return key;
        }

        /// <summary>
        /// Create a key using default (SHA256) hash provider.  NOTE: Default can be changed for smaller or larger hash values
        /// </summary>
        /// <returns>New hash key</returns>
        public static HashKeySimple Create()
        {
            return Create(_defaultHashProvider);
        }
    }
}
