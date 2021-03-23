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

namespace ScrimpNet.Cryptography
{
    /// <summary>
    /// A key is simply a container of named bytes that are grouped together and can be transported together.  Different encryption algorithms and deploy strategies have different component requirements (single, double, n-count)
    /// and this container
    /// </summary>
    [Serializable]
    [DataContract]
    public abstract class CryptoKeyBase
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public CryptoKeyBase()
        {
            Id = GetKeyId();
            Properties = new KeyProperties();
            Segments = new KeySegments();
        }

        /// <summary>
        /// An unique identifier of this key.  Default: Guid.  Override this method to supply a different kind of identifier
        /// </summary>
        /// <returns>new Guid string</returns>
        protected virtual string GetKeyId()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Named series of bytes (e.g. password, salts, etc). Often will be key/initializationVector pairs.  A key can contain any number of binary segments.  This value will be null after object is deserialized.  If not null then
        /// SegmentBytes field will be null, and vice-versa.
        /// </summary>
        /// <remarks>This collection is NEVER serialized.</remarks>
        [XmlIgnore]
        [IgnoreDataMemberAttribute]
        public KeySegments Segments { get; set; }

        /// <summary>
        /// Named series of properties that give additional information about this key.  It might contain encryption type, life span, or any other implementation specific information.  NOTE:  It is likely these properties are not
        /// natively encrypted on serialization operations.  Do not include senstitive information here.
        /// </summary>
        [DataMember]
        public KeyProperties Properties { get; set; }

        /// <summary>
        /// Unique identifier of this key.  Used in key management scenarios, reporting, and advanced caching scenarios.
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// Access a segment (named series of bytes) by name
        /// </summary>
        /// <param name="segmentType">One of the Crypto.SegmentTypes constants or any name implementation desires for their implementation</param>
        /// <returns>Named byte array</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="segmentType"/> is not found in collection</exception>
        [XmlIgnore]
        [IgnoreDataMemberAttribute]
        public byte[] this[string segmentType]
        {
            get
            {
                if (Segments.ContainsKey(segmentType)==false)
                {
                    throw ExceptionFactory.New<IndexOutOfRangeException>("Unable to find segment key '{0}'", segmentType);
                }
                return Segments[segmentType];
            }
            set
            {
                Segments[segmentType] = value;
            }
        }

        private byte[] _segmentBytes;
        /// <summary>
        /// Serialized verion of Segment dictionary.  Hyrdated during serialization, and cleared (null) after deserialization.  Used primarily for serialization purposes.  NOTE: if this property has value
        /// then Segments property will be null or empty.
        /// </summary>
        [DataMember]
        [SoapAttribute(DataType="Base64")]
        public byte[] SegmentBytes
        {
            get
            {
                return _segmentBytes;
            }
            set
            {
                _segmentBytes = value;
            }
        }

        [OnSerializing]
        private void onSerializing(StreamingContext context)
        {
            _segmentBytes = Serialize.To.Binary(Segments);
            Segments = null;

            //if (Properties.Contains(Crypto.PropertyTypes.HMAC) == true)
            //{
            //    KeyProperty kp = Properties[Crypto.PropertyTypes.HMAC];
            //    string currentDigest = kp.Value;
            //    string hashDigest = Segments.CurrentDigest(Crypto.HashProviders.HMAC512);
            //    kp.Value = hashDigest;
            //}
            //else
            //{
            //    Properties.Add(new KeyProperty()
            //    {
            //        PropertyType = Crypto.PropertyTypes.HMAC,
            //        Value = Segments.CurrentDigest(Crypto.HashProviders.HMAC512),
            //        ValueType = Crypto.PropertyValueTypes.Object
            //    });
            //}

            //if (Info.MasterKey != null) // there is an instance of a master key attached to this key
            //{
            //    if (DataKeysBytes == null) // data keys are not already locked
            //    {
            //        LockDataKeys(Info.MasterKey);

            //        Info.MasterKey = null;
            //    }
            //}
            //verifyIsLocked();
        }

        [OnSerialized]
        private void onSerialized(StreamingContext context)
        {
            if (Segments != null)
            {
                Segments.Clear();
            }
            //if (Keys != null)
            //{
            //    Keys.Dispose();
            //    Keys = null;
            //}
        }
        [OnDeserializing]
        private void onDeserializing(StreamingContext context)
        {

        }

        /// <summary>
        /// Pass context.Context=[masterKey] to automatically unlock data keys on deserialization
        /// </summary>
        /// <param name="context"></param>
        [OnDeserialized]
        private void onDeserialized(StreamingContext context)
        {
            if (_segmentBytes != null)
            {
                Segments = Serialize.From.Binary<KeySegments>(_segmentBytes);
            }
            _segmentBytes = null;
            //if (context.Context != null && context.Context is Key123)
            //{
            //    Key123 masterKey = context.Context as Key123;
            //    if (masterKey != null)
            //    {
            //        UnlockDataKeys(masterKey);
            //    }
            //}
        }



        /// <summary>
        /// 2011.07.14
        /// </summary>
        [DataMember]
        public virtual string Version
        {
            get
            {
                return "2011.07.14";
            }
            set
            {
                //for serialization purposes only
            }
        }

        
    }
}
