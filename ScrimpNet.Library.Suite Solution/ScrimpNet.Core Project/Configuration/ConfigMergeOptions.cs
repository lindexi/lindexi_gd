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

namespace ScrimpNet.Configuration
{
    /// <summary>
    /// Determines how merge operations should function
    /// </summary>
    [Flags]

    [DataContract]
    public enum ConfigMergeOptions
    {
        /// <summary>
        /// Take no action (default)
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Copy any missing keys from source to destination.  Copy keys only, not values.
        /// </summary>
        [EnumMember]
        CopyKeysToDestination = 1,

        /// <summary>
        /// Remove keys from destination that are not in source
        /// </summary>
        [EnumMember]
        RemoveOrphanDestinationKeys = 2,

        /// <summary>
        /// Copy all values (null and non-null) from source keys to existing destination keys, overwriting any destination values
        /// </summary>
        [EnumMember]
        CopyAllValuesToDestination = 4,

        /// <summary>
        /// Copy all non-null values from source to destination, overwriting any destintation values
        /// </summary>
        [EnumMember]
        CopyNonNullValuesToDestination = 8,

        /// <summary>
        /// Copy values from source to destination but only if destination value is null and source is not null
        /// </summary>
        [EnumMember]
        MergeValues = 16,
    }
}

