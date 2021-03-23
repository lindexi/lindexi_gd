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
using System.Reflection;
using System.IO;
using ScrimpNet.IO;

namespace ScrimpNet.Reflection
{
    /// <summary>
    /// Resource file management extensions and helper methods
    /// </summary>
    public class Resource
    {
        /// <summary>
        /// Returns an embedded file that has beend compiled into the referenced assembly
        /// </summary>
        /// <param name="assembly">Assembly to return text from</param>
        /// <param name="fileName">Fully qualified name of file to get (case sensitive)</param>
        /// <returns>string representation of file</returns>
        /// <exception cref="IndexOutOfRangeException">if fileName not found in assembly</exception>
        public static string EmbeddedFile(Assembly assembly, string fileName)
        {
            using (Stream s = assembly.GetManifestResourceStream(fileName))
            {
                if (s == null)
                {
                    throw new IndexOutOfRangeException(string.Format("Unable to find file '{0} in assembly {1}.\nAvailable files are:{2}",
                        fileName, assembly.GetName().Name,string.Join(Environment.NewLine,GetResourceNames(assembly))));
                }
                return IOUtils.StreamToString(s);
            }
        }

        /// <summary>
        /// Load an embedded resource file as a binary array
        /// </summary>
        /// <param name="assembly">Assembly containing resource to get</param>
        /// <param name="fileName">Fully qualified name of embedded resource (cases sensitive)</param>
        /// <returns>Byte array or null if not found</returns>
        public static byte[] EmbeddedBytes(Assembly assembly, string fileName)
        {
            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(fileName))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    byte[] buffer = reader.ReadBytes((int)stream.Length);
                    reader.Close();
                    return buffer;
                }
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("File '{0}' cannot be found", fileName);
                sb.AppendLine("Available files are:");
                string[] resourceNames = GetResourceNames(assembly);
                foreach (string s in resourceNames)
                {
                    sb.AppendLine(s);
                }
                throw new FileNotFoundException(sb.ToString(), ex);
            }
        }

        /// <summary>
        /// Get an embedded text file from within the currently executing assembly
        /// </summary>
        /// <param name="fileName">Fully qualififed file name of manifest file (case sensitive)</param>
        /// <returns>String of context of files</returns>
        public static string EmbeddedFile(string fileName)
        {
            return EmbeddedFile(Assembly.GetExecutingAssembly(), fileName);
        }

        /// <summary>
        /// Get a list of resource names that are embedded in an assembly.  Generally this
        /// method is used in development/debugging to help parse out the exact
        /// path to the resource
        /// </summary>
        /// <param name="assembly">Assembly to get the resources from</param>
        /// <returns>List of full path and filenames for all embedded resources</returns>
        public static string[] GetResourceNames(Assembly assembly)
        {
            return assembly.GetManifestResourceNames();
        }
    }
}
