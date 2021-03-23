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
    /// Determines which value will be used if a key is located in both local .config AND some other configuration store
    /// </summary>
    [DataContract]
    public enum ConfigScopeEnumeration
    {
        /// <summary>
        /// Only keys from .config will be considered
        /// </summary>
        [EnumMember]
        LocalOnly = 0,

        /// <summary>
        /// .config keys will be loaded from a non-config configuration store
        /// </summary>
        [EnumMember]
        RemoteOnly = 1,

        /// <summary>
        /// Get keys from .config first then from remote using remote value if found
        /// </summary>
        [EnumMember]
        OverwriteLocalWithRemote = 2,

        /// <summary>
        /// Get keys from remote then from .config using .config value if found. (default)
        /// </summary>
        [EnumMember]
        OverwriteRemoteWithLocal = 4
    }
}
