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

namespace CoreTests.Cryptography
{
    /// <summary>
    /// Summary description for HashKeySaltedTests
    /// </summary>
    [TestClass]
    public class HashKeySaltedTests
    {

        public HashKeySaltedTests()
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
        public void HashSaltedTests_DefaultConstructor()
        {
            //-------------------------------------------------------
            // arrange 
            //-------------------------------------------------------
            var key = HashKeySalted.Create();
            var plainData = Guid.NewGuid().ToByteArray();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var firstHash = key.Hash(plainData);
            var secondHash = key.Hash(plainData);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            var firstResult = Encoding.Unicode.GetString(firstHash);
            var secondResult = Encoding.Unicode.GetString(secondHash);

            Assert.AreEqual(firstResult, secondResult);
        }

        [TestMethod]
        public void HashKeySaltedTests_ParameterizedConstructor()
        {
            //-------------------------------------------------------
            // arrange 
            //-------------------------------------------------------
            var key1 = HashKeySalted.Create();
            var key2 = HashKeySalted.Create(Crypto.HashModesSalted.HMACSHA256,key1.Salt);
            var plainData = Guid.NewGuid().ToByteArray();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var firstHash = key1.Hash(plainData);
            var secondHash = key2.Hash(plainData);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            var firstResult = Encoding.Unicode.GetString(firstHash);
            var secondResult = Encoding.Unicode.GetString(secondHash);

            Assert.AreEqual(firstResult, secondResult);
        }

        [TestMethod]
        public void HashKeySalted_Serialization_DataContract()
        {
            var key1 = HashKeySalted.Create();
            var plainData = Guid.NewGuid().ToByteArray();
            var key1Hash = key1.Hash(plainData);
            var serializedKey = Serialize.To.DataContract(key1);
            var deserializedKey = Serialize.From.DataContract<HashKeySalted>(serializedKey);
            var key2Hash = deserializedKey.Hash(plainData);

            var key1HashResult = CryptoUtils.FromBytes(key1Hash, StringFormat.Base64);
            var key2HashResult = CryptoUtils.FromBytes(key2Hash, StringFormat.Base64);
            Assert.AreEqual<string>(key1HashResult, key2HashResult);
        }

        [TestMethod]
        public void HashKeySalted_Serialization_Json()
        {
            var key1 = HashKeySalted.Create();
            var plainData = Guid.NewGuid().ToByteArray();
            var key1Hash = key1.Hash(plainData);
            var serializedKey = Serialize.To.Json(key1);
            var deserializedKey = Serialize.From.Json<HashKeySalted>(serializedKey);
            var key2Hash = deserializedKey.Hash(plainData);

            var key1HashResult = CryptoUtils.FromBytes(key1Hash, StringFormat.Base64);
            var key2HashResult = CryptoUtils.FromBytes(key2Hash, StringFormat.Base64);
            Assert.AreEqual<string>(key1HashResult, key2HashResult);
        }

        [TestMethod]
        public void HashKeySalted_Serialization_Binary()
        {
            var key1 = HashKeySalted.Create();
            var plainData = Guid.NewGuid().ToByteArray();
            var key1Hash = key1.Hash(plainData);
            var serializedKey = Serialize.To.Binary(key1);
            var deserializedKey = Serialize.From.Binary<HashKeySalted>(serializedKey);
            var key2Hash = deserializedKey.Hash(plainData);

            var key1HashResult = CryptoUtils.FromBytes(key1Hash, StringFormat.Base64);
            var key2HashResult = CryptoUtils.FromBytes(key2Hash, StringFormat.Base64);
            Assert.AreEqual<string>(key1HashResult, key2HashResult);
        }
    }
}
