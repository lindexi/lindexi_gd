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
using System.Xml.Serialization;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using SysDrawing = System.Drawing;
using ScrimpNet.Reflection;
using System.Drawing;

namespace ScrimpNet.Net
{
    /// <summary>
    /// Foundational wrapper class that exposes the MIME library
    /// </summary>
    [XmlRoot("MimeMap", Namespace = "")]

    public class Mime
    {
        /// <summary>
        /// Finds the image codec for a particular mime type
        /// </summary>
        /// <param name="mimeType">W3C standard MIME type to evaluate</param>
        /// <returns>Codec when found</returns>
        /// <exception cref="Exception">Throw when mimeType is not a member of the valid list</exception>       
        public static ImageCodecInfo Encoder(string mimeType)
        {
            // [Citation("200803142233", AcquiredDate = "2008-03-14", Author = "Kevin Spencer", Source = "http://www.msnewsgroups.net/group/microsoft.public.dotnet.languages.csharp/topic33691.aspx", SourceDate = "2006-12-03")]
            int intCt;

            ImageCodecInfo[] aryEncoders = ImageCodecInfo.GetImageEncoders();
            for (intCt = 0; intCt < aryEncoders.Length; intCt++)
            {
                if (aryEncoders[intCt].MimeType == mimeType)
                    return aryEncoders[intCt];
            }
            throw new ArgumentException("MimeType '" + mimeType + "' not found");
        }

        /// <summary>
        /// Finds the image decoder for a particular mimeType
        /// </summary>
        /// <param name="mimeType">MimeType that is needing decoded</param>
        /// <returns>Decoder, if found</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="mimeType"/> is not found</exception>
        public static ImageCodecInfo Decoder(string mimeType)
        {
            int intCt;

            ImageCodecInfo[] aryEncoders = ImageCodecInfo.GetImageDecoders();
            for (intCt = 0; intCt < aryEncoders.Length; intCt++)
            {
                if (aryEncoders[intCt].MimeType == mimeType)
                    return aryEncoders[intCt];
            }
            throw new ArgumentException("MimeType '" + mimeType + "' not found");
        }
        /// <summary>
        /// Get the W3C standard MIME type for this image
        /// </summary>
        /// <param name="image">Image to parse</param>
        /// <returns>Image MIME type or 'image/unknown' if not found</returns>
        public static string GetMIMEType(SysDrawing.Image image)
        {
            // [Citation("200803142256", AcquiredDate = "2008-03-14", Author = "Chris Hynes", Source = "http://chrishynes.net/blog/archive/2008/01/17/Get-the-MIME-type-of-a-System.Drawing-Image.aspx", SourceDate = "2008-01-17")]
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
            {
                if (codec.FormatID == image.RawFormat.Guid)
                    return codec.MimeType;
            }
            return Mime.Map[""].MediaType;
        }

        /// <summary>
        /// Return a type/subtype string that mataches the extension this file
        /// </summary>
        /// <param name="fileName">Full filename.ext</param>
        /// <returns>Standard ICAAN MIME type (if found)</returns>
        public static string GetMIMEType(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            return mimeTypeForExtension(extension).MediaType;
        }

        /// <summary>
        /// Return a hydrated MimeInfo that matches the extension of this file
        /// </summary>
        /// <param name="fileName">Full filename.ext</param>
        /// <returns>Filled Mime object</returns>
        public static MimeInfo GetMimeInfo(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            return mimeTypeForExtension(extension);
        }

        private static MimeInfoList _map = null;

        static Mime()
        {            
            string s = Resource.EmbeddedFile(Assembly.GetExecutingAssembly(), "ScrimpNet.Net.MimeTypes.xml");
            _map = Serialize.From.Xml<MimeInfoList>(s);
            _map.initilizeLookupTables();
        }

        /// <summary>
        /// Generate a string that represents an enumeration of all MimeTypes defined in MimeTypes.xml.  This property
        /// is expected to be run from  debugging code and the returned value cut/pasted into the MimeFormat class
        /// </summary>
        /// <remarks>
        /// DESIGN: can use string instead of StringBuilder since this code is should never be executed at runtime.  
        /// Performance doesn't matter
        /// </remarks>
        public static string MimeFormatEnumGenenerator
        {
            get
            {
                string retVal = "///<summary>"+Environment.NewLine;
                retVal += "/// List of industry standandard file extensions, applications, and Mime Types." +Environment.NewLine;
                retVal += "///</summary>" + Environment.NewLine;
                retVal+= "///<remarks"+Environment.NewLine;
                retVal += "/// autogenerated using Mime.MimeFormatEnumGenenerator and MimeTypes.xml.  Modify MimeTypes.xml and regenerate when adding new MimeTypes." + Environment.NewLine;
                retVal += "///</remarks>"+Environment.NewLine;
                retVal += "public enum MimeExtensions" + Environment.NewLine + "{" + Environment.NewLine;
                retVal += "///<summary>"+Environment.NewLine;
                retVal += "/// unknown or undefined (0)"+Environment.NewLine;
                retVal += "///</summary>"+Environment.NewLine;
                retVal += "unknown=0," + Environment.NewLine;
                int mimeCount = 0;
                foreach (MimeInfo info in _map)
                {
                    if (string.IsNullOrEmpty(info.FileExtension) == true) continue;
                    string s1 = "///<summary>" + Environment.NewLine;
                    s1 += "/// ";
                    if (string.IsNullOrEmpty(info.Description) == false)
                    {
                        s1 += info.Description;
                    }
                    if (string.IsNullOrEmpty(info.MediaType) == false)
                    {
                        s1 += "(" + info.MediaType + ")";
                    }
                    s1 += string.Format("({0})",++mimeCount); 
                    s1 += Environment.NewLine;
                    s1 += "///</summary>" + Environment.NewLine;
                    s1 += info.FileExtension + "="+mimeCount.ToString()+"," + Environment.NewLine;
                    retVal += s1;
                }
                retVal += "} //enumeration MimeFormat"+Environment.NewLine;

                Dictionary<string, Dictionary<string,MimeInfo>> mediaList = new Dictionary<string, Dictionary<string,MimeInfo>>();
                foreach (MimeInfo mi in _map)
                {
                    try
                    {
                        string[] mediaParts = mi.MediaType.Split(new char[] { '/' });
                        string contentType = mediaParts[0];
                        string subType = mediaParts[1];

                        if (mediaList.ContainsKey(contentType) == false)
                        {
                            mediaList.Add(contentType, new Dictionary<string, MimeInfo>());
                        }
                        mediaList[contentType][mi.FileExtension] = mi;
                    }
                    catch (Exception)
                    {
                        continue; // ignore errors
                    }
                }
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("public class MimeMediaType");
                sb.AppendLine("{");
                foreach(KeyValuePair<string,Dictionary<string,MimeInfo>> kvpMedia in mediaList)
                {
                    sb.AppendLine("public class "+makeSafeIdentifier(kvpMedia.Key));
                    sb.AppendLine("{");
                    foreach (KeyValuePair<string, MimeInfo> kvpSubType in kvpMedia.Value)
                    {
                        sb.AppendLine("///<summary>");
                        sb.AppendLine("/// "+kvpSubType.Value.Description +"("+kvpSubType.Value.MediaType+")");
                        sb.AppendLine("///</summary>");
                        sb.AppendLine("public const string " + makeSafeIdentifier(kvpSubType.Value.FileExtension) + "=\""+kvpSubType.Value.MediaType+"\";");
                    }
                    sb.AppendLine("}// class " + kvpMedia.Key);
                }
                sb.AppendLine("} //class MediaTypes");
                return retVal+Environment.NewLine+sb.ToString();
            }
        }
        private static string makeSafeIdentifier(string unsafeIdentifier)
        {
            return unsafeIdentifier.Replace(".","").Replace("+","").Replace("-","");
        }

        /// <summary>
        /// Singleton constructor
        /// </summary>
        public static Mime Map = new Mime();

        private static MimeInfo mimeTypeForExtension(string fileExtension)
        {
            MimeInfo returnInfo = _map[fileExtension.Trim().ToLower().Replace(".", "")];
            return returnInfo;
        }

        /// <summary>
        /// Return MIME information about a particular file extension
        /// </summary>
        /// <param name="fileExtension">file extension (with or without leading '.')</param>
        /// <returns>Hydrated MIME information about this particular extension</returns>
        public MimeInfo this[string fileExtension]
        {
            get
            {
                return mimeTypeForExtension(fileExtension);
            }
        }
        /// <summary>
        /// Return MIME informatin about a particular file, based on it's file name
        /// </summary>
        /// <param name="fileInfo">Hydrated object</param>
        /// <returns>MIME information about the filename of this object</returns>
        public MimeInfo this[FileInfo fileInfo]
        {
            get
            {
                return this[fileInfo.Extension];
            }
        }

        /// <summary>
        /// Attempt to identify MIME information directly from the bytes of an image.
        /// </summary>
        /// <param name="image">Hydrated image to get MIME information from</param>
        /// <returns>MIME information about the image</returns>
        /// <remarks>NOTE: Codecs must be installed on computer running this assembly or .Net will not be able to determine the correct MIME information</remarks>
        public MimeInfo this[Image image]
        {
            get
            {
                foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders()) //EXTENSION Codec could be cached into a hashtable
                {
                    if (codec.FormatID == image.RawFormat.Guid)
                    {
                        return this[codec.FilenameExtension];

                    }
                }
                throw new IndexOutOfRangeException(string.Format("Cannot determine MimeInfo for '{0}'", image.RawFormat.ToString()));
            }
        }

    }
}
