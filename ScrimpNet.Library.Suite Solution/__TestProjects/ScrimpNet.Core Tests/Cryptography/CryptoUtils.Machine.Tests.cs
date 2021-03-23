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
using ScrimpNet.Text;
using System.Web.Security;

namespace CoreTests.Cryptography
{
    [Serializable]
    public class TestObject
    {
        public string StringProp { get; set; }
        public DateTime DateProp { get; set; }
        public int IntProp { get; set; }
    }

    [TestClass]
    public class CryptoUtilsMachineTests
    {

    
        [TestMethod]
        public void Encode_Machine_PlainText_HappyPath()
        {
            var s = CryptoUtils.Generate.RandomBytesCS(8, 16);
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            string plainText = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            string encryptedText = CryptoUtils.Machine.Encode(plainText);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, encryptedText);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Encode_Machine_PlainText_Null_ArgumentNullException()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            string plainText = null;

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            string encryptedText = CryptoUtils.Machine.Encode(plainText);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, encryptedText);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Encode_Machine_Bytes_Null_ArgumentNullException()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            byte[] plainBytes = null;

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encryptedText = CryptoUtils.Machine.Encode(plainBytes);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.Fail("Did not throw expected exception");
        }

        [TestMethod]
        public void Encode_Machine_PlainText_EmptyString_NoException()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            string plainText = string.Empty;

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            string encryptedText = CryptoUtils.Machine.Encode(plainText);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, encryptedText);
        }

        [TestMethod]
        public void Encode_Machine_Object_Success()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var testObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = int.MaxValue,
                StringProp = Guid.NewGuid().ToString()
            };

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            byte[] s = Serialize.To.Binary(testObject);
            var response = CryptoUtils.Machine.Encode(s);
            var responseObject1 = CryptoUtils.Machine.Decode<TestObject>(response);

            var response2 = CryptoUtils.Machine.Encode(s);
            var responseObject2 = CryptoUtils.Machine.Decode<TestObject>(response2);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(response, response2); //multiple calls to encoding will return different values but will encrypt to same value
            Assert.AreEqual<DateTime>(responseObject1.DateProp, responseObject2.DateProp);
            Assert.AreEqual<string>(responseObject2.StringProp, responseObject2.StringProp);
            Assert.AreEqual<int>(responseObject1.IntProp, responseObject2.IntProp);
        }

        [TestMethod]
        public void Decode_Machine_String_Success()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            string plainText = Guid.NewGuid().ToString();
            string encodedString = CryptoUtils.Machine.Encode(plainText);

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var response = CryptoUtils.Machine.Decode(encodedString);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            //var unencryptedText = UnicodeEncoding.Unicode.GetString(response);
            Assert.AreEqual<string>(plainText, response);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Decode_Machine_String_Null_ArgumentNullException()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            string plainText = Guid.NewGuid().ToString();
            string encodedString = null;

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var response = CryptoUtils.Machine.Decode(encodedString);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.Fail("Did not throw expected ArgumentNullException");
        }

        [TestMethod]
        public void Decode_Machine_Object_GenericVersion_Success()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var testObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = int.MaxValue,
                StringProp = Guid.NewGuid().ToString()
            };
            string encodedString = CryptoUtils.Machine.Encode(testObject);

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var unencryptedObject = CryptoUtils.Machine.Decode<TestObject>(encodedString);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreEqual<DateTime>(testObject.DateProp, unencryptedObject.DateProp);
            Assert.AreEqual<string>(testObject.StringProp, unencryptedObject.StringProp);
            Assert.AreEqual<int>(testObject.IntProp, unencryptedObject.IntProp);
        }

        [TestMethod]
        public void Hash_Machine_PlainText_HappyPath()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            string plainText = Guid.NewGuid().ToString();

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            string encryptedText = CryptoUtils.Machine.Hash(plainText);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, encryptedText);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Hash_Machine_PlainText_Null_ArgumentNullException()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            string plainText = null;

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            string encryptedText = CryptoUtils.Machine.Hash(plainText);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, encryptedText);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Hash_Machine_Bytes_Null_ArgumentNullException()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            byte[] plainBytes = null;

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var encryptedText = CryptoUtils.Machine.Hash(plainBytes);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.Fail("Did not throw expected exception");
        }

        [TestMethod]
        public void Hash_Machine_PlainText_EmptyString_NoException()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            string plainText = string.Empty;

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            string encryptedText = CryptoUtils.Machine.Hash(plainText);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(plainText, encryptedText);
        }

        [TestMethod]
        public void Hash_Machine_Object_Success()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var testObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = int.MaxValue,
                StringProp = Guid.NewGuid().ToString()
            };

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var response1 = CryptoUtils.Machine.Hash(testObject);
            var response2 = CryptoUtils.Machine.Hash(testObject);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(response1, response2);
        }

        [TestMethod]
        public void Hash_ObjectDuplicate_EqualHash()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var testObject1 = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = int.MaxValue,
                StringProp = Guid.NewGuid().ToString()
            };
            var testObject2 = new TestObject()
            {
                DateProp = testObject1.DateProp,
                IntProp = testObject1.IntProp,
                StringProp = testObject1.StringProp
            };

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var response1 = CryptoUtils.Machine.Hash(testObject1);
            var response2 = CryptoUtils.Machine.Hash(testObject2);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(response1, response2);
        }
        [TestMethod]
        public void Hash_Machine_ObjectContentChanged_NotEqual()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var testObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = int.MaxValue,
                StringProp = Guid.NewGuid().ToString()
            };

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var response1 = CryptoUtils.Machine.Hash(testObject);
            testObject.DateProp = DateTime.Now.AddDays(1);
            var response2 = CryptoUtils.Machine.Hash(testObject);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(response1, response2);
        }

        [TestMethod]
        public void Hash_Machine_String_SameContentEqual_Success()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var string1 = Guid.NewGuid().ToString();
            var string2 = string1;

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var response1 = CryptoUtils.Machine.Hash(string1);
            var response2 = CryptoUtils.Machine.Hash(string2);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreEqual<string>(response1, response2);
        }

        [TestMethod]
        public void Decode_Machine_Object_ChangedContent_Success()
        {
            //-------------------------------------------------------
            // arrange
            //-------------------------------------------------------
            var testObject = new TestObject()
            {
                DateProp = DateTime.Now,
                IntProp = int.MaxValue,
                StringProp = Guid.NewGuid().ToString()
            };

            //-------------------------------------------------------
            // act
            //-------------------------------------------------------
            var response1 = CryptoUtils.Machine.Hash(testObject);
            testObject.DateProp = DateTime.Now.AddDays(1); //change content
            var response2 = CryptoUtils.Machine.Hash(testObject);

            //-------------------------------------------------------
            //  assert
            //-------------------------------------------------------
            Assert.AreNotEqual<string>(response1, response2);
        }
    }
}

