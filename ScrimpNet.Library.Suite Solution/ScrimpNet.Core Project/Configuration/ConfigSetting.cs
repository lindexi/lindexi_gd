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

namespace ScrimpNet.Configuration
{
    /// <summary>
    /// Defines a single atomic key within the configuration settings space.  A config setting has base setting (Application) and two optional variants (Environment, Machine).  
    /// Values can be set on all variants with the most specific (Application+Environment+Machine) overriding less specific (Application)
    /// </summary>
    /// <remarks>
    /// NOTE:
    /// </remarks>
    [DataContract]

    public class ConfigSetting:IComparable,IEqualityComparer 
    {
        /// <summary>
        /// Primary key within the corporate configuration space.  Often used to detect renames
        /// </summary>
        [DataMember]
        public Guid SettingGlobalId { get; set; }

        /// <summary>
        /// Name of key.  Similar to .config &lt;appSettings&gt; key.  Artificial FK (part4)
        /// </summary>
        [DataMember]
        public string SettingKey { get; set; }

        [DataMember]
        public string ApplicationName{ get; set; }

        /// <summary>
        /// Textual description of the key:  what the key controls, valid values, etc.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public ConfigVariantCollection Variants { get; set; }

        /// <summary>
        /// User identity that changed any one of the ..Value properties
        /// </summary>
        [DataMember]
        public string UpdatedBy { get; set; }

        /// <summary>
        /// DateTime of user that changed any one of the ...Value properties
        /// </summary>
        [DataMember]
        public DateTime UpdatedOn { get; set; }

        public int CompareTo(object obj)
        {
            return (base.Equals(obj) == true) ? 0 : -1;
        }

        public bool Equals(object x, object y)
        {
            return (x as ConfigSetting).CompareTo(y) == 0;
        }

        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", ApplicationName, SettingKey);
        }
    }
}
