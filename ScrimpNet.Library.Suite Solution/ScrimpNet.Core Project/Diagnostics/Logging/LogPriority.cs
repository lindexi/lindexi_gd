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

namespace ScrimpNet.Diagnostics
{
        /// <summary>
        ///     Specifies how urgent the sender of this message considers this message
        /// </summary>
        [DataContract]
        public enum LogPriority
        {
         
            /// <summary>
            ///  Lowest message priority. (0)
            /// </summary>
            [EnumMember]
            Lowest = 0,
            
            /// <summary>
            /// Between Low and Lowest message priority. (1)
            /// </summary>
            [EnumMember]
            VeryLow = 1,
            
            /// <summary>
            /// Low message priority. (2)
            /// </summary>
            [EnumMember]
            Low = 2,

            /// <summary>
            /// Normal message priority. (3)
            /// </summary>    
            [EnumMember]
            Normal = 3,

            /// <summary>
            /// Between High and Normal message priority. (4)
            /// </summary>
            [EnumMember]
            AboveNormal = 4,
            
            /// <summary>
            /// High message priority. (5)
            /// </summary>
            [EnumMember]
            High = 5,
            
            /// <summary>
            /// Between Highest and High message priority. (6)
            /// </summary>
            [EnumMember]
            VeryHigh = 6,
            
            /// <summary>
            /// Highest message priority. (7)
            /// </summary>
            [EnumMember]
            Highest = 7,        
    }
}
