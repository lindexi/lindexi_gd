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

namespace ScrimpNet.Cryptography
{
    public partial class CryptoUtils
    {
        /// <summary>
        /// Encryption with reasonable defaults using user provided password as the key.  See Remarks encryption details and configuration options
        /// </summary>
        /// <remarks>Encryption Provider: Rijndael
        /// <para>Key Size: 256bits</para>
        /// <para>Initialization Vector: 256bits</para>
        /// <para>Internal Salt: 64bits</para>
        /// <para>Change constants at top of class to adjust internal behavior</para>
        /// </remarks>
        public class Simple
        {
            /// <summary>
            /// 32
            /// </summary>
            private static int KEYSIZEBYTES = 32;

            /// <summary>
            /// 32
            /// </summary>
            private static int IVSIZEBYTES = 32;

            /// <summary>
            /// Crypto.EncodeModes.Rijindael
            /// </summary>
            private static string _defaultEncryptionMode = Crypto.EncodeModes.Rijindael; //EXTENSION sets which encryption mode will be the default.

            /// <summary>
            /// Used to generate encryption keys based on a string password. See makeKey(password) method.  Must be a fixed value so passwords will always generate the same (deterministic) encryption key/IV pair.
            /// </summary>
            private static byte[] _internalSalt = new byte[] { 130, 231, 249, 7, 25, 99, 87, 8, 209, 136, 151, 199, 217, 196 };
            //EXTENSION: You should always change _internalSalt to something unique for you before using this library in production.  You don't want other people to use the same salt.  The value doesn't really matter.  Just change it. Use CrytopUtils.Generate.RandomBytesCS!

            /// <summary>
            /// Type of string to return from encode/hash methods, expected as parameter to decode methods.  (StringFormat.Base64)
            /// </summary>
            private static StringFormat _defaultStringFormat = StringFormat.Base64;

            /// <summary>
            /// Used to generate salts without any salt adjustments (Crypto.HashModesSimple.SHA256)
            /// </summary>
            private static string _defaultSimpleHashMode = Crypto.HashModesSimple.SHA256;

            /// <summary>
            /// Used to generate salts with salt adjustments (Crypto.HashModesSalted.HMACSHA256)
            /// </summary>
            private static string _defaultSaltedHashMode = Crypto.HashModesSalted.HMACSHA256;


            /// <summary>
            /// Encrypt text with a given password.  Returns string in Base64 format.  (Internally use RijindaelManaged encryptor)
            /// </summary>
            /// <param name="plainText">Text that will be encrypted</param>
            /// <param name="password">Password will be used to encrypt the text</param>
            /// <returns>Encrypted text in Base64 format</returns>
            public static string Encode(string plainText, string password)
            {
                byte[] plainBytes = CryptoUtils.ToBytes(plainText, StringFormat.Unicode);
                byte[] encryptedBytes = Encode(plainBytes, password);
                return CryptoUtils.FromBytes(encryptedBytes, _defaultStringFormat);
            }

            /// <summary>
            /// Encrypt object with a given password.  Returns string in Base64 format.  (Internally use RijindaelManaged encryptor)
            /// </summary>
            /// <param name="objectToEncrypt">Serializable object to encrypt</param>
            /// <param name="password">Password will be used to encrypt the text</param>
            /// <returns>Encrypted text in Base64 format</returns>
            public static string Encode(object objectToEncrypt, string password)
            {
                var key = makeKey(password);
                byte[] plainBytes = Serialize.To.Binary(objectToEncrypt);
                byte[] encryptedBytes = Encode(plainBytes, password);
                return CryptoUtils.FromBytes(encryptedBytes, _defaultStringFormat);
            }



            /// <summary>
            /// Encrypt series of bytes with a given password.  (Internally uses RijindaelManaged encryptor)
            /// </summary>
            /// <param name="plainBytes">Non-null list of bytes to encrypt</param>
            /// <param name="password">Password to use when encrypting this set of bytes</param>
            /// <returns>Encrypted set of bytes.  Does not modify <paramref name="plainBytes"/></returns>
            public static byte[] Encode(byte[] plainBytes, string password)
            {
                var key = makeKey(password);
                return key.Encode(plainBytes);
            }

            /// <summary>
            /// Decrypt text with a given password
            /// </summary>
            /// <param name="encryptedText">Encrypted text in Base64 format that was encrypted with one of the Encode (or compatible) methods</param>
            /// <param name="password">Password that was used to encrypt the text</param>
            /// <returns>Plain text</returns>
            public static string Decode(string encryptedText, string password)
            {
                var key = makeKey(password);
                byte[] encryptedBytes = CryptoUtils.ToBytes(encryptedText, _defaultStringFormat);
                byte[] plainBytes = Decode(encryptedBytes, password);
                return CryptoUtils.FromBytes(plainBytes, StringFormat.Unicode);
            }


            /// <summary>
            /// Decrypt object with a given password
            /// </summary>
            /// <param name="encryptedObjectString">Encrypted object (in Base64 format) that was encrypted with one of the Encode (or compatible) methods</param>
            /// <param name="password">Plain text password that was used to encrypt the text</param>
            /// <typeparam name="T">Type of object into which to serialize result</typeparam>
            /// <returns>Deserialized object</returns>
            public static T Decode<T>(string encryptedObjectString, string password) where T : class
            {
                var key = makeKey(password);
                byte[] encryptedBytes = CryptoUtils.ToBytes(encryptedObjectString, _defaultStringFormat);
                byte[] plainBytes = Decode(encryptedBytes, password);
                return Serialize.From.Binary<T>(plainBytes);
            }

            /// <summary>
            /// Decrypt a series of encrypted bytes that have been previously encrypted with one of Encode methods 
            /// </summary>
            /// <param name="encryptedBytes">Non-null encrypted list of bytes</param>
            /// <param name="password">Plain text password that was used to encrypt this bytes</param>
            /// <returns>Decrypted bytes.  Does not modify <paramref name="encryptedBytes"/>.</returns>
            public static byte[] Decode(byte[] encryptedBytes, string password)
            {
                var key = makeKey(password);
                try
                {
                    return key.Decode(encryptedBytes);
                }
                catch (CryptographicException ex)
                {
                    throw new InvalidOperationException("Encryption error occured.  This is often because decode password is not the same as the one used to encode the data, data is not encrypted, or data encrypted with a different algorithm.  See inner exception for exception from .Net", ex);
                }
            }

            /// <summary>
            /// Calculate a unique hash code (Base64) for a piece of non-null text. (Uses SHA256 by default)
            /// </summary>
            /// <param name="plainText">Text to use for calculating hash code</param>
            /// <param name="hashSalt">(optional) Additional text that you want to use to add addtional 'secrecy' to your hash.  If provided uses HMAC256 instead of SHA256</param>
            /// <returns>Base64 encoded string of hash.  The value is suitable for storing in databases but might need to be HtmlEncoded if used in query strings or displayed on web pages</returns>
            public static string Hash(string plainText, string hashSalt = null)
            {
                byte[] plainBytes = Encoding.Unicode.GetBytes(plainText);
                byte[] saltBytes = null;
                if (hashSalt != null)
                {
                    saltBytes = Encoding.Unicode.GetBytes(hashSalt);

                }
                byte[] hashBytes = Hash(plainBytes, saltBytes);
                return CryptoUtils.FromBytes(hashBytes, _defaultStringFormat);
            }

            /// <summary>
            /// Calculate a unique hash (Base64) code for a non-null object. (Uses SHA256 by default)
            /// </summary>
            /// <param name="objectToHash">Binary serializable object upon which to calculate hash</param>
            /// <param name="hashSalt">(optional) Additional text that you want to use to add addtional 'secrecy' to your hash.  If provided uses HMAC256 instead of SHA256</param>
            /// <returns>Base64 encoded string of hash.  The value is suitable for storing in databases but might need to be HtmlEncoded if used in query strings or displayed on web pages</returns>
            public static string Hash(object objectToHash, string hashSalt = null)
            {
                byte[] plainBytes = Serialize.To.Binary(objectToHash);
                byte[] saltBytes = null;
                if (hashSalt != null)
                {
                    saltBytes = Encoding.Unicode.GetBytes(hashSalt);
                }
                byte[] hashBytes = Hash(plainBytes, saltBytes);

                return CryptoUtils.FromBytes(hashBytes, _defaultStringFormat);

            }
            /// <summary>
            /// Calculate a unique hash for a non-null series of bytes (Uses SHA256 by default)
            /// </summary>
            /// <param name="bytesToHash">Non-null list of bytes upon which to calculate hash</param>
            /// <param name="hashSalt">(optional) Additional text that you want to use to add addtional 'secrecy' to your hash.  If provided uses HMAC256 instead of SHA256</param>
            /// <returns>Base64 encoded string of hash.  The value is suitable for storing in databases but might need to be HtmlEncoded if used in query strings or displayed on web pages</returns>
            public static byte[] Hash(byte[] bytesToHash, byte[] hashSalt = null)
            {
                if (hashSalt == null)
                {
                    var simpleKey = HashKeySimple.Create(_defaultSimpleHashMode);
                    return simpleKey.Hash(bytesToHash);
                }

                var keyedHasher = HashKeySalted.Create(_defaultSaltedHashMode, hashSalt);
                return keyedHasher.Hash(bytesToHash);
            }

            private static SymmetricKey makeKey(string password)
            {
                byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
                using (Rfc2898DeriveBytes byteFactory = new Rfc2898DeriveBytes(passwordBytes, _internalSalt, 10))
                {
                    byte[] encryptionKey = byteFactory.GetBytes(Simple.KEYSIZEBYTES);
                    byte[] iv = byteFactory.GetBytes(Simple.IVSIZEBYTES);
                    var key = SymmetricKey.Create(_defaultEncryptionMode, encryptionKey, iv);
                    return key;
                }
            }
        }
    }
}
