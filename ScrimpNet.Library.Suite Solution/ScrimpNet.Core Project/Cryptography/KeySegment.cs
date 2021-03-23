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
using System.Xml.Serialization;
using System.Collections;
using System.Security.Cryptography;

namespace ScrimpNet.Cryptography
{
    /// <summary>
    /// List of 0..N byte arrays required by a key to perform a required cryptologic operation
    /// </summary>
    [Serializable]
    [CollectionDataContract
    (Name = "Segments",
    ItemName = "Segment",
    KeyName = "SegmentType",
    ValueName = "Bytes")]
    public class KeySegments : Dictionary<string, byte[]>,IXmlSerializable 
    {
        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public  KeySegments(SerializationInfo info, StreamingContext context):
        base(info, context)
    {

    }
    
        /// <summary>
        /// Default constructor
        /// </summary>
        public KeySegments()
            : base()
        {
        }
        ///// <summary>
        ///// Recalculate the hash code (digest) of all segments in this collection
        ///// </summary>
        ///// <param name="hashProvider">Any one of the standard .Net hash providers</param>
        ///// <param name="hashKey">Unique salt to be used for keyed hash algorithms (HMAC and TripleDESHMAC)</param>
        ///// <returns>Base64 encoded hash code</returns>
        //public string CurrentDigest(HashAlgorithm hashProvider,byte[] hashKey=null)
        //{
        //    byte[] bytes = Serialize.To.Binary(this);           
        //    string s = CryptoUtils.Hash.Compute(hashProvider, bytes, StringFormat.Base64,hashKey);           
        //    return s;
        //}        

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    //[DataContract]
    //[Serializable]
    //public class KeySegment
    //{
    //    [DataMember]
    //    public string SegmentType { get; set; }

    //    [DataMember]
    //    [SoapAttribute(DataType = "base64Binary")]
    //    public byte[] Bytes { get; set; }
    //}

}
