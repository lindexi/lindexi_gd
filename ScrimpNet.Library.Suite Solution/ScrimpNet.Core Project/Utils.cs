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
using System.Globalization;
using System.Data;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.Xml.Serialization;
using System.Collections;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ScrimpNet.Text;
using System.Reflection;
namespace ScrimpNet
{
    /// <summary>
    /// Miscellaneous functions
    /// </summary>
    public static partial class Utils
    {

        /// <summary>
        /// Expand an exception and all inner exceptions and return in formatted string
        /// </summary>
        /// <param name="ex">Expanded exception</param>
        /// <returns>Returned value</returns>
        public static string Expand(Exception ex)
        {
            return Expand(ex, 0);
        }

        /// <summary>
        /// Expand an exception and all inner exceptions and return in formatted string
        /// </summary>
        /// <param name="ex">Expanded exception</param>
        /// <param name="offSetIndex">Number of characters to insert at beginning of each line</param>
        /// <returns>Returned value</returns>
        public static string Expand(Exception ex, int offSetIndex)
        {
            StringBuilder sb = new StringBuilder();
            string paddingString = "";
            if (offSetIndex > 0)
                paddingString = new String(' ', offSetIndex);


            sb.AppendLine("{1}===== Begin {0} =====", ex.GetType().FullName, paddingString);
            _expandException(ex, 0, sb);
            sb.AppendLine("{1}===== End {0} =====", ex.GetType().FullName, paddingString);

            return sb.ToString().Replace(Environment.NewLine, Environment.NewLine + paddingString);
        }

        private static void _expandException(Exception ex, int offSet, StringBuilder sb)
        {
            //var props = ex.GetType().GetProperties();
            //foreach (var prop in props)
            //{
            //    switch (prop.Name)
            //    {
            //        case "TargetSite":
            //            sb.AppendLine("Target Site");
            //            break;
            //        case "Data":
            //            sb.AppendLine("data expansion");
            //            break;
            //        case "InternalException":
            //            sb.AppendLine(Expand(prop.GetValue(ex, null) as Exception, offSet + 4));
            //            break;
            //        default:
            //            sb.AppendLine("{0}:{1}", prop.Name, prop.GetValue(ex, null));
            //            break;
            //    }
            //}

            if (ex == null) return;
            Type t = ex.GetType();
            string paddingString = "";
            if (offSet > 1)
                paddingString = new String(' ', offSet);

            string replacementString = Environment.NewLine + paddingString + new String(' ', 14);
            var props = ex.GetType().GetProperties();
            foreach (var prop in props)
            {
                switch (prop.Name)
                {
                    case "Data":
                        if (ex.Data != null && ex.Data.Count > 0)
                        {
                            string dictString = expandData(prop.Name, ex.Data as IDictionary, offSet);
                            sb.AppendLine(dictString);
                        }

                        break;
                    case "InnerException":
                        if (ex.InnerException != null)
                        {
                            sb.AppendFormat("{0}    ----- Inner {1} (begin) -----{2}", paddingString,   ex.InnerException.GetType().FullName + "," + ex.InnerException.GetType().Assembly.GetName().Name, Environment.NewLine);
                            _expandException(ex.InnerException, offSet + 4, sb);
                            sb.AppendFormat("{0}    ----- Inner {1} (end) ------{2}", paddingString, ex.InnerException.GetType().FullName, Environment.NewLine);
                        }
                        break;
                    default:
                        var propValue = prop.GetValue(ex, null);
                        if (propValue == null) break;

                        if (propValue is IDictionary)
                        {
                            string dictString = expandData(prop.Name, propValue as IDictionary, offSet);
                            sb.AppendLine(dictString);
                        }
                        else if (propValue is ICollection)
                        {
                            ICollection exCollection = propValue as ICollection;
                            int x = 0;
                            foreach (var c in exCollection)
                            {
                                sb.AppendLine("{0}{1}[{2}][{3}]: {4}", paddingString, prop.Name,x++,c.GetType().Name,c);
                            }
                        }
                        else
                        {
                            var stringValue = string.Format("{0}", prop.GetValue(ex, null)).Replace(Environment.NewLine, replacementString);
                            sb.AppendLine("{2}{0}: {1}", prop.Name, stringValue, paddingString);
                        }

                        break;
                }
            }

        }

        private static string expandData(string keyName, System.Collections.IDictionary iDictionary, int offSet)
        {
            StringBuilder sb = new StringBuilder();
           // offSet += 4;
            string paddingString = "";
            if (offSet > 1)
                paddingString = new string(' ', offSet);

     //       sb.AppendFormat("{0}Total Data Entries: {1}{2}", paddingString, iDictionary.Count, Environment.NewLine);
            int counter = 0;
           // paddingString = new string(' ', paddingString.Length + 4);
            foreach (DictionaryEntry de in iDictionary)
            {
                sb.AppendFormat("{0}{4}[{1}][{2} {3}]: ", paddingString, counter++, de.Key.GetType().FullName, de.Key.ToString(),keyName);
                if (de.Value != null)
                    sb.AppendFormat("{0}", de.Value.ToString());
                else
                    sb.AppendFormat("{0}", "(null)");
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates a formatted string of database command properties
        /// </summary>
        /// <param name="cmd">Command to expand</param>
        /// <returns>Formatted string</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static string Expand(IDbCommand cmd)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.AppendLine("---- begin IDbCommand ----");
                if (cmd == null)
                {
                    sb.AppendLine("   cmd is null");
                    sb.AppendLine("---- end IDbCommand ----");
                    return sb.ToString();
                }
                if (cmd.Connection != null)
                {
                    sb.AppendFormat("Connection: {0}{1}", cmd.Connection.GetType().FullName, Environment.NewLine);
                    sb.AppendFormat("    CN String: {0}{1}", cmd.Connection.ConnectionString, Environment.NewLine);
                    sb.AppendFormat("    Time Out : {0}{1}", cmd.Connection.ConnectionTimeout, Environment.NewLine);
                    sb.AppendFormat("    Database : {0}{1}", cmd.Connection.Database, Environment.NewLine);
                    sb.AppendFormat("    State    : {0}{1}", cmd.Connection.State.ToString(), Environment.NewLine);
                }
                else
                {
                    sb.AppendFormat("Connection: {0}{1}", "Connection Not Set", Environment.NewLine);
                }
                sb.AppendFormat("Command: {0}{1}", cmd.GetType().FullName, Environment.NewLine);
                sb.AppendFormat("    Text     : {0}{1}", cmd.CommandText, Environment.NewLine);
                sb.AppendFormat("    Type     : {0}{1}", cmd.CommandType, Environment.NewLine);
                sb.AppendFormat("    RowSource: {0}{1}", cmd.UpdatedRowSource, Environment.NewLine);
                sb.AppendFormat("    Time Out : {0}{1}", cmd.CommandTimeout, Environment.NewLine);
                sb.AppendFormat("Parameters({0}){1}", cmd.Parameters.Count, Environment.NewLine);
                foreach (IDataParameter parm in cmd.Parameters)
                {
                    sb.AppendFormat("    Name     : {0}{1}", parm.ParameterName, Environment.NewLine);
                    sb.AppendFormat("    Db Type  : {0}{1}", parm.DbType, Environment.NewLine);
                    sb.AppendFormat("    Direction: {0}{1}", parm.Direction, Environment.NewLine);
                    sb.AppendFormat("    Nullable : {0}{1}", parm.IsNullable, Environment.NewLine);
                    sb.AppendFormat("Source Column: {0}{1}", parm.SourceColumn, Environment.NewLine);
                    sb.AppendFormat("   Source Ver: {0}{1}", parm.SourceVersion, Environment.NewLine);
                    sb.AppendFormat("        Value: ");
                    if (parm.Value == DBNull.Value || parm.Value == null)
                    {
                        sb.AppendLine("(null)");
                    }
                    else
                    {
                        sb.AppendFormat("{0}{1}", parm.Value.GetType().FullName, Environment.NewLine);
                        sb.AppendFormat("             : {0}{1}", parm.Value.ToString(), Environment.NewLine);
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("---- end IDbCommand ----");
            }
            catch (Exception ex)
            {
                sb.Append(Environment.NewLine);
                sb.Append(TextUtils.StringFormat("Error parsing command. Error: {0}", Transform.ToString(ex)));
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Convert an IDbComand into a string.  First tag is &lt;DbCommand&gt;
        /// </summary>
        /// <param name="cmd">Command to convert</param>
        /// <returns>Formatted string.</returns>
        public static string ToXml(IDbCommand cmd)
        {
            if (cmd == null)
                return @"<DbCommand />";
            StringBuilder sb = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(sb);
            writer.WriteStartElement("DbCommand");
            writer.WriteAttributeString("type=", cmd.GetType().ToString());
            writer.WriteStartElement("Connection");
            writer.WriteAttributeString("type", cmd.Connection.GetType().FullName);
            writer.WriteElementString("CN String", cmd.Connection.ConnectionString);
            writer.WriteElementString("TimeOut", cmd.Connection.ConnectionTimeout.ToString());
            writer.WriteElementString("DataBase", cmd.Connection.Database);
            writer.WriteElementString("State", cmd.Connection.State.ToString());
            writer.WriteEndElement(); //</DbConnection>
            writer.WriteStartElement("Command");
            writer.WriteAttributeString("type", cmd.GetType().FullName);
            writer.WriteElementString("Text", cmd.CommandText);
            writer.WriteElementString("Type", cmd.CommandType.ToString());
            writer.WriteElementString("RowSource", cmd.UpdatedRowSource.ToString());
            writer.WriteElementString("TimeOut", cmd.CommandTimeout.ToString());
            writer.WriteEndElement(); //</Command>
            writer.WriteStartElement("Parameters");
            if (cmd.Parameters != null)
            {
                writer.WriteAttributeString("count", cmd.Parameters.Count.ToString());
                foreach (IDataParameter param in cmd.Parameters)
                {
                    writer.WriteStartElement("Parameter");
                    writer.WriteAttributeString("name", param.ParameterName);
                    writer.WriteElementString("DbType", param.DbType.ToString());
                    writer.WriteElementString("Direction", param.Direction.ToString());
                    writer.WriteElementString("Nullable", param.IsNullable.ToString());
                    writer.WriteElementString("SourceColumn", param.SourceColumn.ToString());
                    writer.WriteElementString("SourceVersion", param.SourceVersion.ToString());
                    writer.WriteStartElement("Value");
                    if (param.Value == DBNull.Value || param.Value == null)
                    {
                        writer.WriteAttributeString("type", "");
                        writer.WriteCData("null or not define");
                    }
                    else
                    {
                        writer.WriteAttributeString("type", param.Value.GetType().FullName);
                        writer.WriteCData(param.Value.ToString());  //we don't know what we are writing out.  It could be Xml                        
                    }
                    writer.WriteEndElement(); //</Value>
                    writer.WriteEndElement(); //</Parameter>
                }
            }
            writer.WriteEndElement(); //</Parameters>

            writer.WriteEndElement(); //</DbCommand>
            return sb.ToString();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        public static T ToObject<T>(string serializedObject)
        {
            XmlSerializerNamespaces xmlnsEmpty = new XmlSerializerNamespaces();
            xmlnsEmpty.Add("", "");
            XmlSerializer xs = new XmlSerializer(typeof(T));

            using (MemoryStream memoryStream = new MemoryStream(StringToUTF8ByteArray(serializedObject)))
            {

                // XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                return (T)xs.Deserialize(memoryStream);
            }
        }

        /// <summary>
        /// To convert a Byte Array of Unicode values (UTF-8 encoded) to a complete String.
        /// </summary>
        /// <param name="characters">Unicode Byte Array to be converted to String</param>
        /// <returns>String converted from Unicode Byte Array</returns>
        public static String UTF8ByteArrayToString(Byte[] characters)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            String constructedString = encoding.GetString(characters);
            return constructedString;

        }

        /// <summary>
        /// Converts the String to UTF8 Byte array and is used in Deserialization
        /// </summary>
        /// <param name="valueToExpand"></param>
        /// <returns></returns>
        public static Byte[] StringToUTF8ByteArray(String valueToExpand)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(valueToExpand);
            return byteArray;
        }



        /// <summary>
        /// Return a byte array from a string
        /// </summary>
        /// <param name="strToConvert">string to convert</param>
        /// <returns>byte array</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
        public static byte[] StringToASCIIByteArray(string strToConvert)
        {
            return (new System.Text.ASCIIEncoding()).GetBytes(strToConvert);
        }


        /// <summary>
        /// Clone any object
        /// </summary>
        /// <param name="obj">object to copy</param>
        /// <returns>new created copy</returns>
        public static object Clone(object obj)
        {
            using (MemoryStream ms = new MemoryStream(500))
            {
                BinaryFormatter bf = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                object newObj;
                newObj = bf.Deserialize(ms);
                return newObj;
            }
        }

        /// <summary>
        /// Clone any object
        /// </summary>
        /// <param name="obj">object to copy</param>
        /// <typeparam name="T">Type of object to clone</typeparam>
        /// <returns>new created copy</returns>
        public static T Clone<T>(T obj)
        {
            return (T)Clone(obj);
        }

        /// <summary>
        /// Returns locale identifier of current thread "en-US", "fr-CA"
        /// </summary>
        public static string CurrentLocale
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.Name;
            }
        }


    }
}
