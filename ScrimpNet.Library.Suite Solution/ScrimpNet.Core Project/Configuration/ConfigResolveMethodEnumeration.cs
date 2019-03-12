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
    /// Determines what kind of settings a configuration provider should return (net value after hierarchial, only keys that match explict)
    /// </summary>
    [DataContract]
    public enum ConfigResolveMethod
    {
        /// <summary>
        /// Return the net result of application+environment+machine overrides
        /// </summary>
        [EnumMember]
        Resolve = 0,

        /// <summary>
        /// Return the actual setting without applying overrides
        /// </summary>
        [EnumMember]
        Explicit = 1
    }
}
