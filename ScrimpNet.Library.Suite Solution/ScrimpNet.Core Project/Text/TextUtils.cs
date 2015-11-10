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
using System.IO;
using System.Globalization;
using ScrimpNet.IO;
using System.Text.RegularExpressions;

namespace ScrimpNet.Text
{
    /// <summary>
    /// Miscellanous string handling methods
    /// </summary>
    public class TextUtils
    {
        /// <summary>
        /// Replaces the format item in a specified System.String with the text equivalent
        ///     of the value of a corresponding System.Object instance in a specified array.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An System.Object array containing zero or more objects to format.</param>
        /// <param name="provider">Provider that will be responsible for inserting args into formatString</param>
        /// <returns> A copy of format in which the format items have been replaced by the System.String
        ///     equivalent of the corresponding instances of System.Object in args.
        /// </returns>
        /// <exception cref="ArgumentNullException">formatString is null</exception>
        /// <exception cref="FormatException">format is invalid.-or- The number indicating an argument to format is less
        ///     than zero, or greater than or equal to the length of the args array.
        ///</exception>
        public static string StringFormat(IFormatProvider provider, string format, params object[] args)
        {

            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException("TextUtils.StringFormat::format (message) should never be null.");
            string retval = "";
            if (args != null && args.Length > 0 && format != null)
            {
                try
                {

                    return string.Format(provider, format, args);
                }
                catch (Exception formatEx)
                {
                    try
                    {
                        retval = "Error Formatting String" + Environment.NewLine;
                        retval += "   Error Message:    " + formatEx.Message + Environment.NewLine;
                        retval += "   Format String:    " + (!string.IsNullOrEmpty(format) ? format : "Format string not specified") + Environment.NewLine;
                        retval += "   Format Arguments: " + (args == null ? "(null)" : args.Length.ToString()) + Environment.NewLine;
                        for (int x = 0; x < args.Length; x++)
                        {
                            retval += string.Format("        Argument[{0}]: {1}",
                                x, (args == null ? "(null)" : "Type: " + args[x].GetType().Name));
                            retval += " Value: ";
                            retval = string.Format("{0}", (args == null ? "" : args[x].ToString()));
                        }
                        retval += "   Format Exception: " + Transform.ToString(formatEx);
                    }
                    catch (Exception ex)
                    {
                        retval += Environment.NewLine + "Error creating error message: " + Transform.ToString(ex);
                    }
                }
                return retval;
            }
            else
            {
                return format;
            }
        }

        /// <summary>
        /// Replaces the format item in a specified System.String with the text equivalent
        ///     of the value of a corresponding System.Object instance in a specified array.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An System.Object array containing zero or more objects to format.</param>
        /// <returns> A copy of format in which the format items have been replaced by the System.String
        ///     equivalent of the corresponding instances of System.Object in args.
        /// </returns>
        /// <exception cref="ArgumentNullException">formatString is null</exception>
        /// <exception cref="FormatException">format is invalid.-or- The number indicating an argument to format is less
        ///     than zero, or greater than or equal to the length of the args array.
        ///</exception>
        public static string StringFormat(string format, params object[] args)
        {
            return StringFormat(CultureInfo.InvariantCulture, format, args);
        }

        /// <summary>
        /// Creates a string from a stream.  Alias function for IOUtils.StreamToString()
        /// </summary>
        /// <param name="stream">stream to make into string</param>
        /// <returns>String or null</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public static string StreamToString(Stream stream)
        {
            return IOUtils.StreamToString(stream);
        }



        /// <summary>
        /// Test a string against standard operating system wild cards
        /// </summary>
        /// <param name="testValue">String being searched</param>
        /// <param name="searchPattern">? or * in a text string (PH*)</param>
        /// <returns>True if seachPattern matches testValue</returns>
        [Citation("200805100135", AcquiredDate = "200805100135", Author = "reinux", License = "none specified", SourceDate = "2005-09-15", Source = "http://www.codeproject.com/KB/recipes/wildcardtoregex.aspx")]
        public static bool MatchesWildCard(string testValue, string searchPattern)
        {

            Regex regEx = new Regex("^" + Regex.Escape(searchPattern).
              Replace("\\*", ".*").
              Replace("\\?", ".") + "$");

            return regEx.IsMatch(testValue);

        }

        /// <summary>
        /// Remove all characters (and blanks) from beginning and end of string.  Convenience extension for TextUtils.Trim
        /// </summary>
        /// <param name="inputString">String that will have leading and trailing characters removed</param>
        /// <param name="removalCharacters">List of non-blank characters to remove from ends of string</param>
        /// <returns>Modified string</returns>
        public static string Trim(string inputString, string removalCharacters)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return inputString;
            }
            return inputString.Trim().Trim(removalCharacters.ToCharArray()).Trim();
        }

        /// <summary>
        /// Search number of matchStrings are in input string
        /// </summary>
        /// <param name="inputString">String to search</param>
        /// <param name="matchString">Value being searched for</param>
        /// <returns>Number of <paramref name="matchString"/> occurances in <paramref name="inputString"/></returns>
        public static int Count(string inputString, string matchString)
        {
            if (string.IsNullOrEmpty(inputString)) return 0;
            if (string.IsNullOrEmpty(matchString))
            {
                throw new ArgumentException("::matchString must not be null or empty");
            }
            int count = (int)(inputString.Length - inputString.Replace(matchString,"").Length)/matchString.Length;
            return count;
        }
    }
}
