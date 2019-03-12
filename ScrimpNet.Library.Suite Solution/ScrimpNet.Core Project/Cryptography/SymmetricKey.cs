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
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO;

namespace ScrimpNet.Cryptography
{
    /// <summary>
    /// Properties about a single symmetric crytographic key.  To create an instance of this class call Symmetric.Create() factory methods
    /// </summary>
    [Serializable]
    [DataContract]
    public class SymmetricKey : CryptoKeyBase
    {
        /// <summary>
        /// Default constructor
        /// </summary>        
        internal SymmetricKey()
            : base()
        {

        }

        /// <summary>
        /// Constructor called by Factory methods.  Sets common default values for Symmetric key encryption
        /// </summary>
        /// <param name="encryptorMode">One of the Crypto.PropertyTypes.EncodeMode constants or can be any specific user implementation</param>
        /// <param name="key">Array of bytes appropriate for this mode</param>
        /// <param name="IV">(also called InitializationVector) Array of bytes appropriate for this mode</param>
        private SymmetricKey(string encryptorMode, byte[] key, byte[] IV)
            : this()
        {
            Properties.Add(new KeyProperty()
            {
                PropertyType = Crypto.PropertyTypes.EncodeMode,
                Value = encryptorMode,
                ValueType = Crypto.PropertyValueTypes.String
            });

            Segments[Crypto.SegmentTypes.SymmetricKey] = key;
            Segments[Crypto.SegmentTypes.SymmetricIv] = IV;
        }

        /// <summary>
        /// Create a key that uses rijindael encryption with default key lengths
        /// </summary>
        /// <returns>Hydrated key</returns>
        public static SymmetricKey Create()
        {
            return Create(Crypto.EncodeModes.Rijindael);
        }

        /// <summary>
        /// Create a key for a specific encryption mode using random default key lengths
        /// </summary>
        /// <param name="encryptoMode">One of the EncodeModes constants</param>
        /// <returns>Hydrated key</returns>
        public static SymmetricKey Create(string encryptoMode)
        {
            using (SymmetricAlgorithm algorithm = findProvider(encryptoMode))
            {
                algorithm.GenerateKey();
                algorithm.GenerateIV();
                return Create(encryptoMode, algorithm.Key, algorithm.IV);
            }
        }

        /// <summary>
        /// Create a key for a specific encryption mode using a specific key and initialization vector
        /// </summary>
        /// <param name="encryptoMode">One of the EncodeModes constants</param>
        /// <param name="key">Hyrdated key that is valid for this specific encryption mode</param>
        /// <param name="IV">Hydrated initialization vector that is valid for this specific encryption mode</param>
        /// <returns>Hydrated key</returns>
        public static SymmetricKey Create(string encryptoMode, byte[] key, byte[] IV)
        {
            return new SymmetricKey(encryptoMode, key, IV);
        }

        /// <summary>
        /// Primary encryption key used for encryption.  Exact size is dependent on algorithm consuming these values.  Non-serialized.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [XmlIgnore]
        [IgnoreDataMember]
        [SoapIgnore]
        public byte[] KeyBytes
        {
            get
            {
                if (Segments == null || Segments.Count == 0)
                {
                    throw ExceptionFactory.New<InvalidOperationException>("This key does not have any byte segments defined.  Keys may not be used after they participation in a serializing operation");
                }
                //if (IsEffective)
                //{
                return Segments[Crypto.SegmentTypes.SymmetricKey];
                //}
                //throw ExceptionFactory.New<InvalidOperationException>("This key file is valid only from {0:yyyy-MM-dd HH:mm:ss.ff} UTC to {1:yyyy-MM-dd HH:mm:ss.ff} UTC",
                //    ValidFromUtc, ValidToUtc);
            }
        }

        /// <summary>
        /// Primary offset (some algorithms call this 'InsertionVector').  Exact size is dependent on algorithm.  Non-serialized
        /// </summary>
        [XmlIgnore]
        [IgnoreDataMember]
        [SoapIgnore]
        public byte[] IVBytes
        {
            get
            {
                return Segments[Crypto.SegmentTypes.SymmetricIv];
            }
        }

        private static SymmetricAlgorithm findProvider(string providerKey)
        {
            switch (providerKey)
            {
                case Crypto.EncodeModes.Rijindael: return new RijndaelManaged();
                case Crypto.EncodeModes.AES: return new AesManaged();
                case Crypto.EncodeModes.DES: return new DESCryptoServiceProvider();
                case Crypto.EncodeModes.TripleDES: return new TripleDESCryptoServiceProvider();
                case Crypto.EncodeModes.RC2: return new RC2CryptoServiceProvider();
            }
            throw ExceptionFactory.New<InvalidOperationException>("Unable to find encryption implemention for key '{0}'", providerKey);
        }

        /// <summary>
        /// Encrypt a series of bytes using algorithm and key/IV provisioned in this key
        /// </summary>
        /// <param name="plainData">non-null series of bytes to be encrypted</param>
        /// <returns>Encrypted data.  Does not modify <paramref name="plainData"/></returns>
        public byte[] Encode(byte[] plainData)
        {
            var providerKey = Properties[Crypto.PropertyTypes.EncodeMode].Value;
            using (var dotNetEncryptor = findProvider(providerKey))
            {
                return encode(dotNetEncryptor, plainData, KeyBytes, IVBytes);
            }
        }

        /// <summary>
        /// Decrypt a series of bytes using algorithm and key/IV provisioned in this key (or exactly compatible)
        /// </summary>
        /// <param name="encryptedData">Previous encrypted</param>
        /// <returns>Unencrypted bytes.  Does not modify <paramref name="encryptedData"/></returns>
        public byte[] Decode(byte[] encryptedData)
        {
            var providerKey = Properties[Crypto.PropertyTypes.EncodeMode].Value;
            using (var dotNetEncryptor = findProvider(providerKey))
            {
                return decode(dotNetEncryptor, encryptedData, KeyBytes, IVBytes);
            }
        }

        /// <summary>
        /// Encrypt an array of bytes using key and insertion vector (also called block size).  NOTE: This is the base
        /// method for encryption methods.  Other encryption methods are convenience overloads
        /// </summary>
        /// <param name="plainBytes">Non-null,non-zero length array of bytes to encrypt</param>
        /// <param name="keyBytes">Key for encryption algorithm. Must be 126, 128, or 256 bits (16, 24, 32 bytes) </param>
        /// <param name="ivBytes">Insertion vector (also called block size). Must be 126, 128, or 256 bits (16, 24, 32 bytes)</param>
        /// <param name="encryptor">.Net Library that will perform actual decryption</param>
        /// <returns>An encrypted byte array</returns>
        [Citation("http://www.codeproject.com/KB/security/DotNetCrypto.aspx", Notes = "Incorporated excellent comments")]
        private byte[] encode(SymmetricAlgorithm encryptor, byte[] plainBytes, byte[] keyBytes, byte[] ivBytes)
        {
            // Create a MemoryStream to accept the encrypted bytes 
            using (MemoryStream ms = new MemoryStream())
            {

                // Create a symmetric algorithm. 
                // We are going to use Rijndael because it is strong and
                // available on all platforms. 
                
                try
                {
                    // explicitly set block sizes since
                    // keys may not be default lengths
                    // Legal values are:
                    //      Key Size:   128, 192, 256
                    //      Block Size: 128, 192, 256
                    //      Bytes        16,  24,  32
                    encryptor.BlockSize = ivBytes.Length * 8;
                    encryptor.KeySize = keyBytes.Length * 8;

                    // Now set the key and the IV. 
                    // We need the IV (Initialization Vector) because
                    // the algorithm is operating in its default 
                    // mode called CBC (Cipher Block Chaining).
                    // The IV is XORed with the first block (8 byte) 
                    // of the data before it is encrypted, and then each
                    // encrypted block is XORed with the 
                    // following block of plaintext.
                    // This is done to make encryption more secure. 

                    // There is also a mode called ECB which does not need an IV,
                    // but it is much less secure. 

                    encryptor.Key = keyBytes;
                    encryptor.IV = ivBytes;

                    // Create a CryptoStream through which we are going to be
                    // pumping our data. 
                    // CryptoStreamMode.Write means that we are going to be
                    // writing data to the stream and the output will be written
                    // in the MemoryStream we have provided. 
                    using (CryptoStream cs = new CryptoStream(ms,
                       encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {

                        // Write the data and make it do the encryption 
                        cs.Write(plainBytes, 0, plainBytes.Length);

                        // Close the crypto stream (or do FlushFinalBlock). 
                        // This will tell it that we have done our encryption and
                        // there is no more data coming in, 
                        // and it is now a good time to apply the padding and
                        // finalize the encryption process. 
                        cs.Close();

                        // Now get the encrypted data from the MemoryStream.
                        // Some people make a mistake of using GetBuffer() here,
                        // which is not the right way. 
                        ms.Flush();
                        byte[] encryptedData = ms.ToArray();
                        ms.Close();
                        return encryptedData;
                    }
                }
                finally
                {
                    if (encryptor != null)
                    {
                        encryptor.Clear();
                    }
                }
            } //using memory stream
        }

        /// <summary>
        ///Decrypt an array of bytes using key and insertion vector (also called block size).  NOTE: This is the base
        /// method for decryption methods.  Other decryption methods are convenience overloads
        /// </summary>
        /// <param name="encryptedBytes">Non-null,non-zero length array of bytes to decrypt</param>
        /// <param name="keyBytes">Key for encryption algorithm. Must be 126, 128, or 256 bits (16, 24, 32 bytes) </param>
        /// <param name="ivBytes">Insertion vector (also called block size). Must be 126, 128, or 256 bits (16, 24, 32 bytes)</param>
        /// <param name="encryptor">.Net library that will perform actual decryption</param>
        /// <returns>An encrypted byte array</returns>
        private byte[] decode(SymmetricAlgorithm encryptor, byte[] encryptedBytes, byte[] keyBytes, byte[] ivBytes)
        {
            // Create a MemoryStream that is going to accept the
            // decrypted bytes 
            using (MemoryStream ms = new MemoryStream())
            {

                // Create a symmetric algorithm. 
                // We are going to use Rijndael because it is strong and
                // available on all platforms. 
                // You can use other algorithms, to do so substitute the next
                // line with something like 
                //     TripleDES alg = TripleDES.Create(); 
               
                try
                {
                    // explicitly set block sizes since
                    // keys may not be default lengths
                    // Legal values are:
                    //      Key Size:   128, 192, 256
                    //      Block Size: 128, 192, 256
                    //      Bytes        16,  24,  32
                    encryptor.BlockSize = ivBytes.Length * 8;
                    encryptor.KeySize = keyBytes.Length * 8;

                    // Now set the key and the IV. 
                    // We need the IV (Initialization Vector) because the algorithm
                    // is operating in its default 
                    // mode called CBC (Cipher Block Chaining). The IV is XORed with
                    // the first block (8 byte) 
                    // of the data after it is decrypted, and then each decrypted
                    // block is XORed with the previous 
                    // cipher block. This is done to make encryption more secure. 
                    // There is also a mode called ECB which does not need an IV,
                    // but it is much less secure. 
                    encryptor.Key = keyBytes;
                    encryptor.IV = ivBytes;

                    // Create a CryptoStream through which we are going to be
                    // pumping our data. 
                    // CryptoStreamMode.Write means that we are going to be
                    // writing data to the stream 
                    // and the output will be written in the MemoryStream
                    // we have provided. 
                    using (CryptoStream cs = new CryptoStream(ms,
                        encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {

                        // Write the data and make it do the decryption 
                        cs.Write(encryptedBytes, 0, encryptedBytes.Length);

                        // Close the crypto stream (or do FlushFinalBlock). 
                        // This will tell it that we have done our decryption
                        // and there is no more data coming in, 
                        // and it is now a good time to remove the padding
                        // and finalize the decryption process. 
                        cs.Close();

                        // Now get the decrypted data from the MemoryStream. 
                        // Some people make a mistake of using GetBuffer() here,
                        // which is not the right way. 
                        byte[] decryptedData = ms.ToArray();

                        return decryptedData;
                    }
                }
                finally
                {
                    if (encryptor != null)
                    {
                        encryptor.Clear(); //remove any resources this might have
                    }
                }
            } //using memory stream
        }

    }
}


///// <summary>
///// Use this key file to encrypt a data block.  Helper facade over CryptoUtils.Encrypt. RijndaelManaged-256
///// </summary>
///// <param name="plainBytes">Data block to encrypt</param>
///// <returns>Encrypted bytes</returns>
//public string Encrypt(byte[] plainBytes, StringFormat outputFormat)
//{
//    return CryptoUtils.Encrypt(Transform.ToBytes(plainBytes), this.KeyBytes, this.IVBytes, outputFormat);
//}

///// <summary>
///// Use this key file to decrypt a data block.  Helper facade over CryptoUtils.Decrypt.  RijndaelManaged-256
///// </summary>
///// <param name="encryptedBytes">Bytes that had previously been encrypted</param>
///// <returns>Plain text bytes</returns>
//public byte[] Decrypt(byte[] encryptedBytes)
//{
//    return CryptoUtils.Decrypt(encryptedBytes, this.KeyBytes, this.IVBytes);
//}

///// <summary>
///// Determines if this key file is effective at this instant in time
///// </summary>
//public bool IsEffective
//{
//    get
//    {
//        long utcTicks = DateTime.UtcNow.Ticks;
//        if (utcTicks < ValidFromUtc.Ticks) return false;
//        if (utcTicks > ValidToUtc.Ticks) return false;
//        return true;
//    }
//}



//private DateTime _validFrom = Utils.Date.SqlMaxDate; // default to NOT effective

///// <summary>
///// Instant in time before which this key file is not valid (UTC)
///// </summary>
//[DataMember]
//public DateTime ValidFromUtc
//{
//    get
//    {
//        return _validFrom;
//    }
//    set
//    {
//        _validFrom = value;
//    }
//}

//private DateTime _validTo = Utils.Date.SafeMinDate; // default to NOT effective

///// <summary>
///// Instant in time after which this keyfile is no longer valid (UTC)
///// </summary>
//[DataMember]
//public DateTime ValidToUtc
//{
//    get
//    {
//        return _validTo;
//    }
//    set
//    {
//        _validTo = value;
//    }
//}

///// <summary>
///// Encrypt this class, serialize it, and convert it to Base64 which is suitable for storing in .config file or database fields.
///// Encryption uses ActiveKey,ActiveIv (usually defaulting to internal keys of CryptoUtils class)
///// </summary>
///// <returns>Base64 string</returns>
//public string Pack()
//{
//    return Pack(CryptoUtils.ActiveKey);
//}

///// <summary>
///// Encrypt this class, serialize it, and convert it to Base64 which is suitable for storing in .config file or database fields.
///// Encryption uses ActiveKey,ActiveIv (usually defaulting to internal keys of CryptoUtils class)
///// </summary>
///// <returns>Base64 string</returns>
//public string Pack(SymKey activeKey)
//{
//    byte[] encryptedBytes = CryptoUtils.Encrypt(Serialize.To.Binary(this), CryptoUtils.ActiveKey.Key, CryptoUtils.ActiveKey.IV);
//    return Convert.ToBase64String(encryptedBytes);
//}


//public static SymKey Unpack(SymKey activeKey, string encryptedKeyFile)
//{
//    byte[] encryptedBytes = Convert.FromBase64String(encryptedKeyFile);
//    byte[] plainTextBytes = CryptoUtils.Decrypt(encryptedBytes, activeKey.KeyBytes, activeKey.IVBytes);
//    return Serialize.From.Binary<SymKey>(plainTextBytes);
//}

///// <summary>
///// Factory method to create a new hydrated key file
///// </summary>
///// <returns>Hydrated key file with 256bit values</returns>
//public static SymKey New()
//{
//    return new SymKey();
//}

///// <summary>
///// Create a new key file with random encryption components and is valid for a period of time
///// </summary>
///// <param name="effectiveFrom">Instant in time this key file becomes effective</param>
///// <param name="effectiveTo">Last instant in time this file is effective</param>
///// <returns></returns>
//public static SymKey New(DateTime effectiveFrom, DateTime effectiveTo)
//{
//    SymKey kf = new SymKey();
//    kf.ValidFromUtc = effectiveFrom;
//    kf.ValidToUtc = effectiveTo;            
//    return kf;
//}

//public string ToString(StringFormat outputFormat)
//{
//    byte[] bytes = Serialize.To.Binary(this);
//    return CryptoUtils.FromBytes(bytes, outputFormat);
//}
