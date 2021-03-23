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
using System.Drawing.Imaging;
using System.Xml.Serialization;


namespace ScrimpNet.Net
{
    /// <summary>
    /// Common meta data for use with MIME types.  Meta data is define in the MimeTypes.xml file
    /// </summary>

    public class MimeInfo
    {
        /// <summary>
        /// Returns enumeration of primary MIME type.  Helper method only
        /// </summary>
        public MimeTypes MimeType
        {
            get
            {
                return Transform.ConvertValue<MimeTypes>(PrimaryType);
            }
        }

        string _primaryType = string.Empty;
        /// <summary>
        /// Returns string of the first part of the MIME content disposition (e.g. "image/jpg")
        /// </summary>
        public string PrimaryType
        {
            get
            {
                if (string.IsNullOrEmpty(_primaryType) == false) return _primaryType;
                if (string.IsNullOrEmpty(MediaType)==false)
                {
                    string[] parts = MediaType.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        _primaryType = parts[0];
                    }
                    if (parts.Length > 1)
                    {
                        _seconaryType = parts[1];
                    }
                }
                return _primaryType;
            }
        }
        string _seconaryType = string.Empty;
        /// <summary>
        /// Returns string of the second part of the MIME content disposition (e.g. "image/jpg")
        /// </summary>
        public string SubType
        {
            get
            {
                if (string.IsNullOrEmpty(_seconaryType) == false) return _seconaryType;
                if (string.IsNullOrEmpty(MediaType) == false)
                {
                    string[] parts = MediaType.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        _primaryType = parts[0];
                    }
                    if (parts.Length > 1)
                    {
                        _seconaryType = parts[1];
                    }
                }
                return _seconaryType;
            }
        }

        private string _fileExtensions;
        /// <summary>
        /// File extension that is commonly mapped to this MIME type.  There are overlaps of file-extension to MIME type.  This class does not
        /// attempt to resolve them. Extensions are lower case and do not contain prefix characters or punctuation(optional)
        /// </summary>
        public string FileExtension
        {
            get { return _fileExtensions; }
            set { _fileExtensions = value; }
        }

        private string _contentDisposition;


        /// <summary>
        /// Official MIME string (required) (e.g image/jpg, application/msword)
        /// </summary>
        [XmlElement("ContentDisposition")]
        public string MediaType
        {
            get { return _contentDisposition; }
            set { _contentDisposition = value; }
        }


        private string _description;

        /// <summary>
        /// English description of the MIME type.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        private readonly MimeIconList _iconList = new MimeIconList();
        /// <summary>
        /// Name of embedded resource file that represents this MIME type
        /// </summary>
        public MimeIconList Icons
        {
            get
            {
                return _iconList;
            }
        }

        /// <summary>
        /// Gets the Codec that is installed on the executing machine.  Returns similar information to
        /// MimeInfo.  MimeInfo can return information about Codedec's that are not installed on the operating system
        /// </summary>
        public ImageCodecInfo CodecEncoder
        {
            get
            {
                return Mime.Encoder(FileExtension);
            }
        }

        /// <summary>
        /// Hydrated codec for this particular MIME type.
        /// </summary>
        public ImageCodecInfo CodecDecoder
        {
            get
            {
                return Mime.Decoder(FileExtension);
            }
        }

        private MimeExtension _extension = MimeExtension.unknown;
        /// <summary>
        /// Strongly typed extension for this MIME type
        /// </summary>
        public MimeExtension Extension
        {
            get
            {
                if (_extension != MimeExtension.unknown)
                {
                    return _extension;
                }
                
                    try
                    {
                        _extension = (MimeExtension)Enum.Parse(typeof(MimeExtension), FileExtension, true);
                    }
                    catch
                    {
                        //swallow exception
                    }
                
                return _extension;
            }
        }
    }
}
