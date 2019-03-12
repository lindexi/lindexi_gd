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

namespace CoreTests.Cryptography
{
    /// <summary>
    /// Summary description for HashKeySimpleTests
    /// </summary>
    [TestClass]
    public class HashKeySimpleTests
    {
        public HashKeySimpleTests()
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
        public void HashKeySimple_Create_NoParameter()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var key = HashKeySimple.Create();
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
        public void HashKeySimple_Create_SpecificHashMode()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var key = HashKeySimple.Create(Crypto.HashModesSimple.RIPEMD160);
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
        public void HashKeySimple_Create_DualHashers()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var key1 = HashKeySimple.Create(Crypto.HashModesSimple.RIPEMD160);
            var key2 = HashKeySimple.Create(Crypto.HashModesSimple.RIPEMD160);
            
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
    }
}
