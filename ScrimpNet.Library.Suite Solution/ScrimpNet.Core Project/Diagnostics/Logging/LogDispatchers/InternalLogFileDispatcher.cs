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
using System.Threading;
using System.IO;
using System.Text;

namespace ScrimpNet.Diagnostics
{
    /// <summary>
    /// An asynchronous, thread-safe rolling file writer that logging sub-system will use if another logging store is not configured
    /// </summary>
    public partial class InternalLogFileDispatcher : RollingLogBase
    {
        /// <summary>
        /// Format a message based on custom formatting.  Override this method to create custom formats. Default value is msg.ToString()
        /// </summary>
        /// <param name="sb">Propulated StringBuilder to add message to</param>
        /// <param name="msg">Hydrated message.  Cast this value to a custom LogMessage if using any.</param>
        protected override void FormatMessage(StringBuilder sb, LogMessage msg)
        {
            sb.Append(msg.ToString()+(new string(' ',70))+Environment.NewLine);
        }
    }


}
