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
using ScrimpNet;
using System.Xml.Serialization;
using System.Runtime.Serialization;
namespace ScrimpNet.Cryptography
{
    /// <summary>
    /// Contains necessary property and salt value for use in hash+salt operations.  Uses .Net HMAC providers.
    /// </summary>
    [Serializable]
    public class HashKeySalted : CryptoKeyBase
    {
        /// <summary>
        /// Crypto.HashModesSalted.HMACSHA256
        /// </summary>
        private static string _defaultHashProvider = Crypto.HashModesSalted.HMACSHA256; //EXTENSION Change this for larger or smaller default hash values

        /// <summary>
        /// Generate a hash value using the values provided in this key
        /// </summary>
        /// <param name="plainBytes">Bytes that will have their hash calculated</param>
        /// <returns>Hash value</returns>
        public byte[] Hash(byte[] plainBytes)
        {
            var salt = Segments[Crypto.SegmentTypes.HashSalt];
            using (KeyedHashAlgorithm hasher = findHashPovider(Properties[Crypto.PropertyTypes.HashSaltedMode].Value, salt))
            {
                return hasher.ComputeHash(plainBytes);
            }
        }

        [XmlIgnore]
        [SoapIgnore]
        [IgnoreDataMember]
        public byte[] Salt
        {
            get
            {
                return Segments[Crypto.SegmentTypes.HashSalt];
            }
            set
            {
                Segments[Crypto.SegmentTypes.HashSalt] = value;
            }
        }

        private KeyedHashAlgorithm findHashPovider(string hashProviderConstant, byte[] salt)
        {
            switch (hashProviderConstant)
            {
                case Crypto.HashModesSalted.HMACMD5:
                    return new HMACMD5(salt);
                case Crypto.HashModesSalted.HMACSHA1:
                    return new HMACSHA1(salt);
                case Crypto.HashModesSalted.HMACSHA256:
                    return new HMACSHA256(salt);
                case Crypto.HashModesSalted.HMACSHA384:
                    return new HMACSHA384(salt);
                case Crypto.HashModesSalted.HMACSHA512:
                    return new HMACSHA512(salt);
                case Crypto.HashModesSalted.HMACRIPEMD160:
                    return new HMACRIPEMD160(salt);
                case Crypto.HashModesSalted.MACTrippleDES:
                    return new MACTripleDES(salt);
            }
            throw ExceptionFactory.New<InvalidOperationException>("Unable to find .net keyed hash provider for '{0}'", hashProviderConstant);
        }

        /// <summary>
        /// Create a key using a specific Crypto.HashModesKeyed and random salt.
        /// </summary>
        /// <param name="hashMode">One of the Crypto.HashModesKeyed constants</param>
        /// <returns>Newly created key</returns>
        public static HashKeySalted Create(string hashMode)
        {
            int[] validSizes = new int[] { 8, 16, 24 };
            Random rdm = new Random();
            int actualSize = validSizes[rdm.Next(0, validSizes.Length)]; //MACTripleDES requires one of three salt sizes so apply same constraint to each of the keyed hash algorithms
            return Create(hashMode, CryptoUtils.Generate.RandomBytes(actualSize, actualSize));
        }

        /// <summary>
        /// Create a key using a specific Crypto.HashModesKeyed and specific salt.
        /// </summary>
        /// <param name="hashMode">One of the Crypto.HashModesKeyed constants</param>
        /// <param name="salt">Non-null salt value to use in hashing operations</param>
        /// <returns>Newly created key</returns>
        public static HashKeySalted Create(string hashMode, byte[] salt)
        {
            var key = new HashKeySalted();
            key.Properties.Add(new KeyProperty()
            {
                PropertyType = Crypto.PropertyTypes.HashSaltedMode,
                Value = hashMode,
                ValueType = Crypto.PropertyValueTypes.String
            });
            key.Segments.Add(Crypto.SegmentTypes.HashSalt, salt);
            return key;
        }

        /// <summary>
        /// Create a key suitiable for salted hashing operations using HMAC256 and a random salt
        /// </summary>
        /// <returns>Newly created key</returns>
        public static HashKeySalted Create()
        {
            return Create(_defaultHashProvider);
        }
    }
}
