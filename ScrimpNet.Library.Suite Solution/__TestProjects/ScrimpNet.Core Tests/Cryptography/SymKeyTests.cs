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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScrimpNet.Cryptography;
using ScrimpNet;
using System.Security.Cryptography;

namespace CoreTests.Cryptography
{
    /// <summary>
    /// Summary description for SymKeyTests
    /// </summary>
    [TestClass]
    public class SymKeyTests
    {
        public SymKeyTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void SymmetricKey_DefaultConstructor()
        {
            //-------------------------------------------------------
            //  arrange
            //-------------------------------------------------------
            SymmetricKey key1 = SymmetricKey.Create();
            var plainText = Guid.NewGuid().ToString();
            var plainBytes = Encoding.Unicode.GetBytes(plainText);

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded = key1.Encode(plainBytes);
            var decoded = key1.Decode(encoded);

            var result = Encoding.Unicode.GetString(decoded);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(plainText, result);
      }

        [TestMethod]
        public void SymmetricKey_Constructor_SpecifiedProvider()
        {
            //-------------------------------------------------------
            //  arrange
            //-------------------------------------------------------
            SymmetricKey key1 = SymmetricKey.Create(Crypto.EncodeModes.TripleDES);
            var plainText = Guid.NewGuid().ToString();
            var plainBytes = Encoding.Unicode.GetBytes(plainText);

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded = key1.Encode(plainBytes);
            var decoded = key1.Decode(encoded);

            var result = Encoding.Unicode.GetString(decoded);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(plainText, result);
        }

        [TestMethod]
        public void SymmetricKey_Constructor_SpecifiedProviderSpecifiedKey()
        {
            //-------------------------------------------------------
            //  arrange
            //-------------------------------------------------------
            SymmetricKey key1 = SymmetricKey.Create(Crypto.EncodeModes.TripleDES);
            SymmetricKey key2 = SymmetricKey.Create(Crypto.EncodeModes.TripleDES,key1.KeyBytes,key1.IVBytes);
            var plainText = Guid.NewGuid().ToString();
            var plainBytes = Encoding.Unicode.GetBytes(plainText);

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded1 = key1.Encode(plainBytes);
            var encoded2 = key2.Encode(plainBytes);

            var result1 = CryptoUtils.FromBytes(encoded1, StringFormat.Base64);
            var result2 = CryptoUtils.FromBytes(encoded2, StringFormat.Base64);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(result1,result2);
        }

        [TestMethod]
        public void SymmetricKey_Constructor_SpecifiedProviderSpecifiedKey_ComparePlainText()
        {
            //-------------------------------------------------------
            //  arrange
            //-------------------------------------------------------
            SymmetricKey key1 = SymmetricKey.Create(Crypto.EncodeModes.TripleDES);
            SymmetricKey key2 = SymmetricKey.Create(Crypto.EncodeModes.TripleDES, key1.KeyBytes, key1.IVBytes);
            var plainText = Guid.NewGuid().ToString();
            var plainBytes = Encoding.Unicode.GetBytes(plainText);

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded1 = key1.Encode(plainBytes);
            var encoded2 = key2.Encode(plainBytes);

            var decoded1 = key1.Decode(encoded2);
            var decoded2 = key2.Decode(encoded1);

            var result1 = CryptoUtils.FromBytes(decoded1, StringFormat.Base64);
            var result2 = CryptoUtils.FromBytes(decoded2, StringFormat.Base64);
            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(result1, result2);
        }

        [TestMethod]
        public void SymmetricKey_Serialized_EncodeDecode()
        {
            //-------------------------------------------------------
            //  arrange
            //-------------------------------------------------------
            SymmetricKey originalKey = SymmetricKey.Create();
            var plainText = Guid.NewGuid().ToString();
            var plainBytes = Encoding.Unicode.GetBytes(plainText);
            
            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded1 = originalKey.Encode(plainBytes);

            var serializedKey = Serialize.To.DataContract(originalKey);
            var deserializedKey = Serialize.From.DataContract<SymmetricKey>(serializedKey);

            var decoded1 = deserializedKey.Decode(encoded1);
            var result1 = CryptoUtils.FromBytes(decoded1, StringFormat.Unicode);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(plainText, result1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SymmetricKey_NoOperationAfterSerialization()
        {
            //-------------------------------------------------------
            //  arrange
            //-------------------------------------------------------
            SymmetricKey originalKey = SymmetricKey.Create();
            var plainText = Guid.NewGuid().ToString();
            var plainBytes = Encoding.Unicode.GetBytes(plainText);

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded1 = originalKey.Encode(plainBytes);

            var serializedKey = Serialize.To.DataContract(originalKey);
            var result = originalKey.Decode(encoded1);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.Fail("Symmetric key should always throw an exception for operations after participating in serializing operations");
        }




    }
}
