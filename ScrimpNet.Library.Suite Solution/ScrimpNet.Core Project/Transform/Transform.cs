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
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Xml.Serialization;
using ScrimpNet.Text;

namespace ScrimpNet
{
    /// <summary>
    /// Change the type of an object from one time to another.  'Transform' is used
    /// because System.Convert is already taken by the .Net framework.  Logically they are the same
    /// </summary>
    public static partial class Transform
    {
        /// <summary>
        /// Convert a datarow field to a datetime value
        /// </summary>
        /// <param name="columnName">column in daterow to delete</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(DateTime) if not found</returns>
        public static DateTime ToDateTime(string columnName, IDataReader  dr)
        {
            return ToDateTime(dr[columnName]);
        }

        /// <summary>
        /// Convert a datarow field to a datetime value
        /// </summary>
        /// <param name="columnName">column in daterow to delete</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(DateTime) if not found</returns>
        public static DateTime ToDateTime(string columnName, DataRow dr)
        {
            return ToDateTime(dr[columnName]);
        }
        /// <summary>
        /// Convert an object into DateTime.  Return default(DateTime) if null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted DateTime or default(DateTime) if null</returns>
        public static DateTime ToDateTime(object value)
        {
            return ConvertValue<System.DateTime>(value);
        }

        /// <summary>
        /// Convert an object into T
        /// </summary>
        /// <typeparam name="T">Type to convert value into</typeparam>
        /// <param name="value">Value that will be converted into T</param>
        /// <param name="defaultValue">return value if not able to convert value into T</param>
        /// <returns>value converted into T or defaultValue if any errors or NULL</returns>
        public static T ConvertValue<T>(object value, T defaultValue)
        {
            try
            {
                return ConvertValue<T>(value);
            }
            catch(Exception)
            {
                return defaultValue; //always return default value if any error occurs
            }
        }
        /// <summary>
        /// Convert an object into T.
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted T</returns>
        /// <exception cref="InvalidCastException">Thrown when value is null</exception>
        /// <exception cref="NotImplementedException">Thrown when value is not able to be converted into T</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static T ConvertValue<T>(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                throw new InvalidCastException(TextUtils.StringFormat("Unable to cast a NULL value to '{0}'", typeof(T).FullName));
            }


            if (typeof(T).IsEnum == true)
            {
                return (T)(object)Enum.Parse(typeof(T), ConvertValue<System.String>(value), true);
            }
            string s = typeof(T).Name;
            switch (typeof(T).ToString())
            {
                case "System.Byte": return (T)(object)Convert.ToByte(value);
                case "System.SByte": return (T)(object)Convert.ToSByte(value);
                case "System.Char": return (T)(object)Convert.ToChar(value);
                case "System.Decimal": return (T)(object)Convert.ToDecimal(value); // Decimal.Parse(value.ToString());
                case "System.Double": return (T)(object)Convert.ToDouble(value);
                case "System.Single": return (T)(object)Convert.ToSingle(value);
                case "System.Int32": return (T)(object)Convert.ToInt32(value); // Int32.Parse(value);
                case "System.UInt32": return (T)(object)Convert.ToUInt32(value);
                case "System.Int64": return (T)(object)Convert.ToInt64(value); // Int64.Parse(value.ToString());
                case "System.UInt64": return (T)(object)Convert.ToUInt64(value);
                case "System.Int16": return (T)(object)Convert.ToInt16(value); // Int16.Parse(value.ToString());
                case "System.UInt16": return (T)(object)Convert.ToUInt16(value);
                case "System.Guid": return (T)(object)new System.Guid(value.ToString());
                case "System.DateTime": return (T)(object)DateTime.Parse(value.ToString());
                case "System.String": return (T)(object)value.ToString();
                case "System.Boolean":
                    Boolean boolRetVal;
                    if (Boolean.TryParse(value.ToString(), out boolRetVal) == true)
                        return (T)(object)boolRetVal;
                    switch (value.ToString().ToUpper())
                    {
                        case "ON":
                        case "1":
                        case "YES":
                        case "TRUE": return (T)(object)true;
                        case "OFF":
                        case "0":
                        case "NO":
                        case "FALSE": return (T)(object)false;
                        default:
                            throw new NotImplementedException(string.Format("Conversion Not Implemented for Type: '{0}' or could not convert value '{1}' to System.Boolean", typeof(T).Name,value));
                    }
                case "System.Byte[]":
                    try
                    {
                        return (T)(object)value; //attempt simple cast
                    }
                    catch
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bf.Serialize(ms, value);
                            ms.Seek(0, 0);
                            return (T)(object)ms.ToArray();
                        }
                    }
                default:
                    throw new NotImplementedException(string.Format("Conversion Not Implemented for Type: '{0}'", typeof(T).Name));
            }
        }

        /// <summary>
        /// Try to convert a value (follows .Net TryParse pattern)
        /// </summary>
        /// <typeparam name="T">Desired type to cast parameter to</typeparam>
        /// <param name="value">Value to try to convert</param>
        /// <param name="result">Result of conversion or Default(T) if conversion fails</param>
        /// <returns>True if conversion was successful and result contains new value</returns>
        public static bool TryConvert<T>(object value, out T result)
        {
            try
            {
                result = ConvertValue<T>(value);
                return true;
            }
            catch
            {
                result = default(T); 
                return false;
            }
        }

        /// <summary>
        /// Convert a datarow field to a datetime value
        /// </summary>
        /// <param name="columnName">column in daterow to delete</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(DateTime) if not found</returns>
        public static byte[] ToBytes(string columnName, DataRow dr)
        {
            if (dr[columnName] == DBNull.Value) return null;

            return ToBytes(dr[columnName]);
        }

        /// <summary>
        /// Convert an object into an array of bytes or null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted value or null</returns>
        public static byte[] ToBytes(object value)
        {
            if (value == null) return null;
            return ConvertValue<Byte[]>(value);
        }

        /// <summary>
        /// Convert an datareader value into a double
        /// </summary>
        /// <param name="p">data reader parameter to read value from</param>
        /// <param name="dr">data reader containing value</param>
        /// <returns>Double that was converted</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static double ToDouble(string p, IDataReader dr)
        {
            return ConvertValue<double>(dr[p]);
        }

        /// <summary>
        /// Convert a file into a array of bytes
        /// </summary>
        /// <param name="fileName">fileName</param>
        /// <returns>Populated array</returns>
        public static byte[] FileToByteArray(string fileName)
        {
            return FileToByteArray(new FileInfo(fileName));
        }

        /// <summary>
        /// Convert a file into a array of bytes
        /// </summary>
        /// <param name="file">File to read into bytes</param>
        /// <returns>Populated array</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public static byte[] FileToByteArray(FileInfo file)
        {
            long numBytes = file.Length;
            using (FileStream fStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {

                using (BinaryReader br = new BinaryReader(fStream))
                {
                    byte[] data = br.ReadBytes((int)numBytes);
                    br.Close();                  
                    return data;
                }
            }
            
        }



        /// <summary>
        /// Convert a datarow field to a Enumeration value
        /// </summary>
        /// <param name="columnName">column in daterow to delete</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(Enumeration) if not found</returns>
        public static T ToEnum<T>(string columnName, DataRow dr)
        {
            return ToEnum<T>(dr[columnName]);
        }

        /// <summary>
        /// Convert a DataReader field to a Enumeration value
        /// </summary>
        /// <param name="columnName">column in DataReader to convert</param>
        /// <param name="dr">DataReader containing field to convert</param>
        /// <returns>Converted value or default(Enumeration) if not found</returns>
        public static T ToEnum<T>(string columnName, IDataReader dr)
        {
            return ToEnum<T>(dr[columnName].ToString());
        }

        /// <summary>
        /// Convert an object into an enumeration
        /// </summary>
        /// <typeparam name="T">Type of enumeration to convert object into</typeparam>
        /// <param name="value">Value of enumeration that will be converted</param>
        /// <returns>Enumeration value</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static T ToEnum<T>(object value)
        {
            return ConvertValue<T>(value);
        }

        /// <summary>
        /// Convert a datarow field to a Guid value
        /// </summary>
        /// <param name="columnName">column in daterow to read</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(Guid) if not found</returns>
        public static Guid ToGuid(string columnName, DataRow dr)
        {
            return ToGuid(dr[columnName]);
        }

        /// <summary>
        /// Convert a datareader field to a Guid value
        /// </summary>
        /// <param name="columnName">column in DataReader to read</param>
        /// <param name="dr">DataReader containing field to convert</param>
        /// <returns>Converted value or default(Guid) if not found</returns>
        public static Guid ToGuid(string columnName, IDataReader dr)
        {
            return ToGuid(dr[columnName]);
        }
        /// <summary>
        /// Convert an object into Guid.  Return default(Guid) if null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted Guid or default(Guid) if null</returns>
        public static Guid ToGuid(object value)
        {
            return ConvertValue<System.Guid>(value);
        }

        /// <summary>
        /// Convert a datareader field to a String value
        /// </summary>
        /// <param name="columnName">column in datareader to read</param>
        /// <param name="dr">datareader containing field to convert</param>
        /// <returns>Converted value or default(String) if not found</returns>
        public static String ToString(string columnName, IDataReader dr)
        {
            return ToString(dr[columnName]);
        }

        /// <summary>
        /// Convert a DataRow field to a String value
        /// </summary>
        /// <param name="columnName">column in DataRow to read</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(String) if not found</returns>
        public static String ToString(string columnName, DataRow dr)
        {
            return ToString(dr[columnName]);
        }
        /// <summary>
        /// Convert an object into String.  Return default(String) if null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted String or default(String) if null</returns>
        public static String ToString(object value)
        {
            return ConvertValue<System.String>(value);
        }

        /// <summary>
        /// Convert an object into Int32.  Return default(Int32) if null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted Int32 or default(Int32) if null</returns>
        public static Int32 ToInt32(object value)
        {
            return ConvertValue<System.Int32>(value);
        }

        /// <summary>
        /// Convert a datarow field to a Int32 value
        /// </summary>
        /// <param name="columnName">column in daterow to delete</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(Int32) if not found</returns>
        public static Int32 ToInt32(string columnName, DataRow dr)
        {
            return ToInt32(dr[columnName]);
        }
        /// <summary>
        /// Convert a DataReader field to a Int32 value
        /// </summary>
        /// <param name="columnName">column in DataReader to convert</param>
        /// <param name="dr">DataReader containing field to convert</param>
        /// <returns>Converted value or default(Int32) if not found</returns>
        public static Int32 ToInt32(string columnName, IDataReader dr)
        {
            return ToInt32(dr[columnName]);
        }
        /// <summary>
        /// Convert an object into Int64.  Return default(Int64) if null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted Int64 or default(Int64) if null</returns>
        public static Int64 ToInt64(object value)
        {
            return ConvertValue<System.Int64>(value);
        }

        /// <summary>
        /// Convert a datarow field to a Int64 value
        /// </summary>
        /// <param name="columnName">column in daterow to delete</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(Int64) if not found</returns>
        public static Int64 ToInt64(string columnName, DataRow dr)
        {
            return ToInt64(dr[columnName]);
        }


        /// <summary>
        /// Convert an object into Long.  Return default(Long) if null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted Long or default(Long) if null</returns>
        public static long ToLong(object value)
        {
            return (long)ConvertValue<System.Int64>(value);
        }
        /// <summary>
        /// Convert a datarow field to a Boolean value
        /// </summary>
        /// <param name="columnName">column in daterow to delete</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(Boolean) if not found</returns>
        public static Boolean ToBoolean(string columnName, DataRow dr)
        {
                return ToBoolean(dr[columnName]);
        }

        ///<summary>
        /// Convert a datareader field to a Boolean value
        /// </summary>
        /// <param name="columnName">column in datereader to delete</param>
        /// <param name="dr">DataReader containing field to convert</param>
        /// <returns>Converted value or default(Boolean) if not found</returns>
        public static Boolean ToBoolean(string columnName, IDataReader dr)
        {
                return ToBoolean(dr[columnName]);

        }
        /// <summary>
        /// Convert an object into Boolean.  Return default(Boolean) if null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted Boolean or default(Boolean) if null</returns>
        public static Boolean ToBoolean(object value)
        {
            return (Boolean)ConvertValue<System.Boolean>(value);         
        }

        #region ToDecimal
        /// <summary>
        /// Convert an object into Decimal.  Return default(Decimal) if null
        /// </summary>
        /// <param name="value">value to convert</param>
        /// <param name="ignoreError">True if conversion should return default(Decimal) on error</param>
        /// <returns>Converted Decimal or default(Decimal)</returns>
        public static Decimal ToDecimal(object value, bool ignoreError)
        {
            try
            {
                return ToDecimal(value);
            }
            catch
            {
                if (ignoreError)
                    return default(decimal);
                else
                    throw;
            }
        }
        /// <summary>
        /// Convert an object into Decimal.  Return default(Decimal) if null
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted Decimal or default(Decimal) if null</returns>
        public static Decimal ToDecimal(object value)
        {
            //if (value is string)
            //{
            //    string s = value as string;
            //    if (s.Contains(".") == false) s += ".0";
            //    return Convert<Decimal>(s);
            //}
            return ConvertValue<System.Decimal>(value);
        }

        /// <summary>
        /// Convert a datarow field to a Decimal value
        /// </summary>
        /// <param name="columnName">column in daterow to delete</param>
        /// <param name="dr">DataRow containing field to convert</param>
        /// <returns>Converted value or default(Decimal) if not found</returns>
        public static Decimal ToDecimal(string columnName, DataRow dr)
        {
            return ToDecimal(dr[columnName]);
        }
        /// <summary>
        /// Convert a DataReader field to a Decimal value
        /// </summary>
        /// <param name="columnName">column in DataReader to convert</param>
        /// <param name="dr">DataReader containing field to convert</param>
        /// <returns>Converted value or default(Decimal) if not found</returns>
        public static Decimal ToDecimal(string columnName, IDataReader dr)
        {
            return ToDecimal(dr[columnName]);
        }
        #endregion ToDecimal

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
        /// Creates a string from a stream
        /// </summary>
        /// <param name="stream">stream to make into string</param>
        /// <returns>String or null</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public static string StreamToString(Stream stream)
        {
            string s = null;
            stream.Seek(0, SeekOrigin.Begin);
            using (StreamReader reader = new StreamReader(stream))
            {
                s = reader.ReadToEnd();
            }
            return s;
        }

        /// <summary>
        /// Converts a list of bytes into a Hex encoded string
        /// </summary>
        /// <param name="data">Data that is being converted</param>
        /// <returns>Hex string of bytes</returns>
        [Citation("http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa-in-c")]
        public static string BytesToHexString(byte[] data)
        {
            byte b;
            int i, j, k;
            int l = data.Length;
            char[] r = new char[l * 2];
            for (i = 0, j = 0; i < l; ++i)
            {
                b = data[i];
                k = b >> 4;
                r[j++] = (char)(k > 9 ? k + 0x37 : k + 0x30);
                k = b & 15;
                r[j++] = (char)(k > 9 ? k + 0x37 : k + 0x30);
            }
            return new string(r);
        }

        /// <summary>
        /// Convert a string of hex characters into actual byte values
        /// </summary>
        /// <param name="hex">Hex string to convert</param>
        /// <returns>Hydrated byte array</returns>
        [Citation("http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa-in-c")]
        public static byte[] HexStringToBytes(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            int bl = bytes.Length;
            for (int i = 0; i < bl; ++i)
            {
                bytes[i] = (byte)((hex[2 * i] > 'F' ? hex[2 * i] - 0x57 : hex[2 * i] > '9' ? hex[2 * i] - 0x37 : hex[2 * i] - 0x30) << 4);
                bytes[i] |= (byte)(hex[2 * i + 1] > 'F' ? hex[2 * i + 1] - 0x57 : hex[2 * i + 1] > '9' ? hex[2 * i + 1] - 0x37 : hex[2 * i + 1] - 0x30);
            }
            return bytes;
        }

        /// <summary>
        /// Convert a byte array into an UTF8 string
        /// </summary>
        /// <param name="utf16Bytes">byte array to convert into string</param>
        /// <returns>String as converted from byte array</returns>
        public static string StringFromBytes(byte[] utf16Bytes)
        {
            System.Text.Encoding enc = new System.Text.UnicodeEncoding();
            return enc.GetString(utf16Bytes);
        }

        /// <summary>
        /// Convert a string into a byte array using UTF8 encoding
        /// </summary>
        /// <param name="utf16String">String that will be converted into a byte array</param>
        /// <returns>A byte representation of this string</returns>
        public static byte[] StringToBytes(string utf16String)
        {
            System.Text.Encoding encoding = new UnicodeEncoding();
            return encoding.GetBytes(utf16String);
        }
    }
}
