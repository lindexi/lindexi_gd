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
using System.Drawing.Imaging;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using System.Xml.Serialization;
using System.Reflection;
using System.IO;

namespace ScrimpNet.Net
{
   

    /// <summary>
    /// List of icons for a particular MIME type
    /// </summary>

    public class MimeIconList : List<MimeIconInfo>
    {
        /// <summary>
        /// Get's the icon image for this MIME type that has a particular group name
        /// </summary>
        /// <param name="groupName">Name of group (e.g. 32x32) of icon to return</param>
        /// <returns></returns>
        public MimeIconInfo this[string groupName]
        {
            get
            {
                MimeIconInfo icon = this.Find(delegate(MimeIconInfo pivotItem)
                {
                    return (string.Compare(pivotItem.GroupName, groupName, false) == 0);
                });
                return icon;
            }
        }
    }

    
    /// <summary>
    /// List of MIME types.  Generally deserialized from MimeTypes.xml
    /// </summary>

    [XmlRoot("MimeMap")]
    public class MimeInfoList : List<MimeInfo>
    {
        Dictionary<string, MimeInfo> _fileExtensionDictionary = new Dictionary<string, MimeInfo>();
        Dictionary<MimeExtension, MimeInfo> _extensionDictionary = new Dictionary<MimeExtension, MimeInfo>();
        internal void initilizeLookupTables()
        {
            foreach (MimeInfo info in this)
            {
                _fileExtensionDictionary[info.FileExtension.ToLower()] = info;
                _extensionDictionary[info.Extension] = info;
            }
        }

        /// <summary>
        /// Get a MIME info object based on a file extension
        /// </summary>
        /// <param name="fileExtension">File extension to get MIME info for.  May contain a leading '.'</param>
        /// <returns></returns>
        public MimeInfo this[string fileExtension]
        {
            get
            {
                return _fileExtensionDictionary[fileExtension];
            }
        }

        /// <summary>
        /// Get a MIME info object based on a strongly typed version of file extensions
        /// </summary>
        /// <param name="extension">File extension to get a MIME type for</param>
        /// <returns>Hydrated MimeInfo or IndexOutOfRange exception</returns>
        public MimeInfo this[MimeExtension extension]
        {
            get
            {
                return _extensionDictionary[extension];
            }
        }
    }

   
}
