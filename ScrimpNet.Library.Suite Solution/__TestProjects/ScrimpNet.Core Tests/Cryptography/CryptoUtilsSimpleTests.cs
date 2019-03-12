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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScrimpNet.Cryptography;

namespace CoreTests.Cryptography
{
    [TestClass]
    public class CryptoUtilsSimpleTests
    {
        // NOTE:  These tests do NOT exercise the byte[] variants because the byte[] variants are implicitly called by the other overloaded methods
        [TestMethod]
        public void Simple_Encode_String()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainText = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString(); // or "Open Sesasme"

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var result = CryptoUtils.Simple.Encode(plainText, password);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, result);
        }

        [TestMethod]
        public void Simple_EncodeDecode_String()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainText = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString(); // or "Open Sesasme"

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded = CryptoUtils.Simple.Encode(plainText, password);
            var result = CryptoUtils.Simple.Decode(encoded, password);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, encoded);
            Assert.AreEqual<string>(plainText, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Simple_EncodeDecode_DifferentPasswords_Fail()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainText = Guid.NewGuid().ToString();
            var password1 = Guid.NewGuid().ToString(); // or "Open Sesasme"
            var password2 = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded = CryptoUtils.Simple.Encode(plainText, password1);
            var result = CryptoUtils.Simple.Decode(encoded, password2);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Simple_EncodeDecode_DifferentPasswords_Exception()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainText = Guid.NewGuid().ToString();
            var password1 = Guid.NewGuid().ToString(); // or "Open Sesasme"
            var password2 = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded1 = CryptoUtils.Simple.Encode(plainText, password1);
            var encoded2 = CryptoUtils.Simple.Decode(encoded1, password2);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, encoded1);
            Assert.AreNotEqual<string>(plainText, encoded2);
            Assert.AreNotEqual<string>(encoded2, encoded1);
        }

        [TestMethod]
        public void Simple_EncodeDecode_Object()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = 45,
                StringProp = "now is the time for all good me to come to the aid of their country"
            };

            var password1 = Guid.NewGuid().ToString(); // or "Open Sesasme"


            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encoded = CryptoUtils.Simple.Encode(plainObject, password1);
            var result = CryptoUtils.Simple.Decode<TestObject>(encoded, password1);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreEqual<DateTime>(plainObject.DateProp, result.DateProp);
            Assert.AreEqual<int>(plainObject.IntProp, result.IntProp);
            Assert.AreEqual<string>(plainObject.StringProp, result.StringProp);
        }

        [TestMethod]
        public void Simple_Hash_String_NoSalt()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainText = Guid.NewGuid().ToString();
            var plainText2 = Guid.NewGuid().ToString();


            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var result1 = CryptoUtils.Simple.Hash(plainText);
            var result2 = CryptoUtils.Simple.Hash(plainText);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, result1);
            Assert.AreNotEqual<string>(plainText, result2);
            Assert.AreEqual<string>(result1, result2);
        }

        [TestMethod]
        public void Simple_Hash_String_Salt()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainText = Guid.NewGuid().ToString();
            var salt = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var saltResult = CryptoUtils.Simple.Hash(plainText, salt);
            var noSaltResult = CryptoUtils.Simple.Hash(plainText);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, saltResult);
            Assert.AreNotEqual<string>(plainText, noSaltResult);
            Assert.AreNotEqual<string>(saltResult, noSaltResult);
        }

        [TestMethod]
        public void Simple_Hash_SameString_DifferentSalts()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainText = Guid.NewGuid().ToString();
            var salt1 = Guid.NewGuid().ToString();
            var salt2 = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var salt1Result = CryptoUtils.Simple.Hash(plainText, salt1);
            var salt2Result = CryptoUtils.Simple.Hash(plainText, salt2);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, salt1Result);
            Assert.AreNotEqual<string>(plainText, salt2Result);
            Assert.AreNotEqual<string>(salt1Result, salt2Result);
        }

        [TestMethod]
        public void Simple_Hash_DifferentStrings_SameSalt()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainText1 = Guid.NewGuid().ToString();
            var plainText2 = Guid.NewGuid().ToString();
            var salt = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var text1Result = CryptoUtils.Simple.Hash(plainText1, salt);
            var text2Result = CryptoUtils.Simple.Hash(plainText2, salt);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText1, text1Result);
            Assert.AreNotEqual<string>(plainText2, text2Result);
            Assert.AreNotEqual<string>(text1Result, text2Result);
        }

        [TestMethod]
        public void Simple_Hash_Object_NoSalt()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = 5642,
                StringProp = "We did not cherish freedom dearly enough"
            };


            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var result1 = CryptoUtils.Simple.Hash(plainObject);
            var result2 = CryptoUtils.Simple.Hash(plainObject);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(result1, result2);

        }

        [TestMethod]
        public void Simple_Hash_Object_Salt()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = 5642,
                StringProp = "We did not cherish freedom dearly enough"
            };
            var salt = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var saltResult = CryptoUtils.Simple.Hash(plainObject, salt);
            var noSaltResult = CryptoUtils.Simple.Hash(plainObject);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(saltResult, noSaltResult);
        }

        [TestMethod]
        public void Simple_Hash_SameObject_DifferentSalts()
        {

            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = 5642,
                StringProp = "We did not cherish freedom dearly enough"
            }; 
            var salt1 = Guid.NewGuid().ToString();
            var salt2 = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var salt1Result = CryptoUtils.Simple.Hash(plainObject, salt1);
            var salt2Result = CryptoUtils.Simple.Hash(plainObject, salt2);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(salt1Result, salt2Result);
        }

        [TestMethod]
        public void Simple_Hash_DifferentObject_SameSalt()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var plainObject1 = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = 5642,
                StringProp = "We did not cherish freedom dearly enough"
            };
            var plainObject2 = new TestObject()
            {
                DateProp = DateTime.Now.AddDays(-45),
                IntProp = 45366,
                StringProp = "We did not cherish freedom dearly enough"
            };
            
            var salt = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var saltResult1 = CryptoUtils.Simple.Hash(plainObject1, salt);
            var saltResult2 = CryptoUtils.Simple.Hash(plainObject2,salt);

            //-------------------------------------------------------
            // assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(saltResult1, saltResult2);

        }
    }
}
