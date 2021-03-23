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
using System.Drawing;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;

namespace ScrimpNet.Net
{
    /// <summary>
    /// Definition of a MIME icon from the MIME library
    /// </summary>
    [Serializable]
    public class MimeIconInfo
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public MimeIconInfo()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">Name of file embedded into this assembly</param>
        /// <param name="groupName">Name of file group (often icon size 32x32, 64x64 etc.)</param>
        public MimeIconInfo(string fileName, string groupName)
        {
            FileName = fileName;
            GroupName = groupName;
        }
        private string _imageNotFoundPath;

        /// <summary>
        /// Name of 'extension' that will be used if this particular file cannot be found in assembly.
        /// Effectively setting a 'default' image should something happen to specific image
        /// </summary>
        public string ImageNotFoundPath
        {
            get { return _imageNotFoundPath; }
            set { _imageNotFoundPath = value; }
        }

        private string _fileName;

        /// <summary>
        /// Name of physical file in this assembly that contains an image for a MIME type
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        private string _groupName;

        /// <summary>
        /// Name of file group (often icon size 32x32, 64x64 etc.)
        /// </summary>
        public string GroupName
        {
            get { return _groupName; }
            set { _groupName = value; }
        }



        private Bitmap _bitmap;
        /// <summary>
        /// Loaded icon image.  Loads once per application domain
        /// </summary>
        [XmlIgnore]
        public Bitmap IconImage
        {
            get
            {
                if (_bitmap == null)
                {
                    Assembly myAssembly = Assembly.GetExecutingAssembly();
                    Stream myStream = myAssembly.GetManifestResourceStream(_fileName);
                    _bitmap = new Bitmap(myStream);
                }
                return _bitmap;
            }
        }
    }
}
