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
using System.Text;

namespace ScrimpNet.Net
{
    /// <summary>
    /// List of primary MIME families based on ICAAN standards
    /// </summary>
    public enum MimeTypes
    {
        /// <summary>
        /// application/....
        /// </summary>
        Application,

        /// <summary>
        /// audio/....
        /// </summary>
        Audio,

        /// <summary>
        /// image/...
        /// </summary>
        Image,

        /// <summary>
        /// multipart/...
        /// </summary>
        Multipart,

        /// <summary>
        /// test/...
        /// </summary>
        Text,

        /// <summary>
        /// video...
        /// </summary>
        Video,

        /// <summary>
        /// xconference/...
        /// </summary>
        XConference,

        /// <summary>
        /// xworld/....
        /// </summary>
        XWorld
    }
}
