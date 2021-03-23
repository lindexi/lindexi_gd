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
using System.Collections;
using System.Collections.ObjectModel;

namespace ScrimpNet.Cryptography
{
    /// <summary>
    /// List of key-value properties (also called 'attributes' or 'metadata') associated with an object.  Property keys may be any value the implementation desires but Crypto.PropertyTypes define those used in the library
    /// </summary>
    [Serializable]
    [CollectionDataContract(Name = "Properties", ItemName = "Property")]
    public class KeyProperties : IList<KeyProperty>, ICollection<KeyProperty>, IEnumerable<KeyProperty>, IEnumerable
    {
        private List<KeyProperty> _items=new List<KeyProperty>();

        /// <summary>
        /// List of properties associated with this class.  Used primarly for serialization purposes and as the backing store for interface implemenation
        /// </summary>
        [DataMember]
        public List<KeyProperty> Items
        {
            get
            {
                return _items;
            }
            internal set
            {
                _items = value;
            }
        }


        public int IndexOf(string propertyType)
        {
            return _items.FindIndex(prop => string.Compare(prop.PropertyType, propertyType, false) == 0);
        }
        public int IndexOf(KeyProperty item)
        {
            return IndexOf(item.PropertyType);
        }

        public void Insert(int index, KeyProperty item)
        {
            _items.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        public KeyProperty this[int index]
        {
            get
            {
                return Items[index];
            }
            set
            {
                Items[index] = value;
            }
        }

        public KeyProperty this[string key]
        {
            get
            {
                int index = IndexOf(key);
                if (index < 0)
                {
                    throw ExceptionFactory.New<IndexOutOfRangeException>("Unable to find property key '{0}'", key);
                }
                return this[index];
            }
            set
            {

            }
        }

        public void Add(KeyProperty item)
        {
            if (IndexOf(item.PropertyType) >= 0)
            {
                throw ExceptionFactory.New<InvalidOperationException>("Key '{0}' already exists in property collection", item.PropertyType);
            }
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(KeyProperty item)
        {
            return _items.Contains(item);
        }
        public bool Contains(string propertyType)
        {
            return IndexOf(propertyType) > 0;
        }

        public void CopyTo(KeyProperty[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyProperty item)
        {
            return _items.Remove(item);
        }

        public IEnumerator<KeyProperty> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }

    /// <summary>
    /// Defines a unit of meta data used in the library
    /// </summary>
    [DataContract]
    [Serializable]
    public class KeyProperty:IComparable,IComparable<KeyProperty>
    {
        /// <summary>
        /// A string constant that specifies what kind of property this is (e.g. name, effective date, etc).  Use (and extend)
        /// one of the Crypto.PropertyTypes contants or any implementation defined values.  NOTE: This is the 'Key' of the logical key-value pair
        /// but is named propertyType to avoid naming confusion with the idea of a 'key' within the crypto library.
        /// </summary>
        [DataMember]
        public string PropertyType { get; set; }

        /// <summary>
        /// A description of the data type contained in the property value.  This will give implementations hints for deserializing
        /// value to the appropriate native type.  Use (and extend) one of the Crypto.PropertyValueTypes.
        /// </summary>
        [DataMember]
        public string ValueType { get; set; }

        /// <summary>
        /// Value to assoicate with this property (e.g Name='Mack', UsageCount='6').  This is the 'Value' of the logical key-value pair.
        /// </summary>
        [DataMember]
        public string Value { get; set; }

        /// <summary>
        /// Comparer based on contents of PropertyType (case sensitive)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>0 if this and <paramref name="obj"/>contain the same case sensitive PropertyType values</returns>
        public int CompareTo(object obj)
        {
            return string.Compare(this.PropertyType, ((KeyProperty)obj).PropertyType, false);
        }

        /// <summary>
        /// Comparer based on contents of PropertyType (case sensitive)
        /// </summary>
        /// <param name="other"></param>
        /// <returns>0 if this and <paramref name="other"/>contain the same case sensitive PropertyType values</returns>
        public int CompareTo(KeyProperty other)
        {
            return CompareTo(other);
        }

        /// <summary>
        /// Compares to ensure both properties and values are case sensitive the same
        /// </summary>
        /// <param name="other">Property to comape with this instance</param>
        /// <returns>0 if this and <paramref name="other"/>contain the same case sensitive PropertyType values and values</returns>
        public int Same(KeyProperty other)
        {
            int retVal = string.Compare(this.PropertyType, other.PropertyType, false);
            if (retVal != 0) return retVal;
            return string.Compare(this.Value, other.Value,false);
        }
    }


}
