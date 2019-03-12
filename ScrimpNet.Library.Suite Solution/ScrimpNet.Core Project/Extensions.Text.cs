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
using ScrimpNet.Text;

namespace ScrimpNet
{
    public static partial class Extensions
    {
        /// <summary>
        /// Extension method since StringBuilder.AppendLine() does not support format arguments
        /// </summary>
        /// <param name="sb">String builder that updates string</param>
        /// <param name="format">Format of string to append</param>
        /// <param name="args">Arguments to supply to format string</param>
        /// <returns>Formatted string appended to existing string builder with new line appended</returns>
        public static StringBuilder AppendLine(this StringBuilder sb, string format, params object[] args)
        {
            sb.AppendFormat(format, args).AppendLine();
            return sb;
        }

        /// <summary>
        /// Extension method since StringBuilder.Append() does not support format arguments (but AppendFormat does support arguments)
        /// </summary>
        /// <param name="sb">String builder that updates string</param>
        /// <param name="format">Format of string to append</param>
        /// <param name="args">Arguments to supply to format string</param>
        /// <returns>Formatted string appended to existing string builder with new line appended</returns>
        public static StringBuilder Append(this StringBuilder sb, string format, params object[] args)
        {
            sb.AppendFormat(format, args);
            return sb;
        }

        /// <summary>
        /// (ScrimpNet.Core extension) Mask last 4 characters of a string
        /// </summary>
        /// <param name="plainText">Credit card number to be revealed</param>
        /// <returns>String with last four characters masked</returns>
        public static string MaskRight(this string plainText)
        {
            return MaskRight(plainText, 4);
        }

        /// <summary>
        /// (ScrimpNet.Core extension) Mask last X characters of a string
        /// </summary>
        /// <param name="plainText">String which to mask right X characters</param>
        /// <param name="places">Number of right hand characters</param>
        /// <returns>String with right X characters masked</returns>
        public static string MaskRight(this string plainText, int places)
        {
            return MaskRight(plainText, 4, '#');
        }

        /// <summary>
        /// (ScrimpNet.Core extension) Mask the right most X characters of a string with the specified character
        /// </summary>
        /// <param name="plainText">Text which right most characters should be masked</param>
        /// <param name="places">Number of characters to mask</param>
        /// <param name="character">Character to use as the mask character</param>
        /// <returns>Masked string or input value if null or empty</returns>
        public static string MaskRight(this string plainText, int places, char character)
        {
            if (string.IsNullOrEmpty(plainText) == true || plainText.Length == 0)
                return plainText;

            if (plainText.Length > places)
            {
                places = plainText.Length;
            }

            plainText = plainText.Substring(0, plainText.Length - places);
            return plainText.PadRight(places, character);

        }

        /// <summary>
        /// Return right most characters of string
        /// </summary>
        /// <param name="target">string that is being evaluated</param>
        /// <param name="characterCount">Number of characters from right end of string to return</param>
        /// <returns>Right most characters of string or entired string if characterCount > length</returns>
        public static string Right(this string target, int characterCount)
        {
            if (string.IsNullOrEmpty(target) == true || target.Length <= 1) return target;
            int startPos = target.Length - characterCount;
            if (startPos < 0) return target;
            return target.Substring(startPos);
        }

        /// <summary>
        /// Return the left most characters of a string
        /// </summary>
        /// <param name="target">string to get characters from</param>
        /// <param name="characterCount">Maximum number of characters to return including length of suffix</param>
        /// <param name="suffix">Characters to append to end of <paramref name="target"/> (e.g. ...).  Returned string+suffix will not be longer than <paramref name="characterCount"/></param>
        /// <returns>A string whose length is less than or equal to <paramref name="characterCount"/></returns>
        public static string Left(this string target, int characterCount, string suffix)
        {
            if (string.IsNullOrEmpty(target) == true) return target;

            if (characterCount > target.Length-suffix.Length)
            {
                characterCount = target.Length-suffix.Length;
            }
            
            string msg = target.Substring(0, characterCount);
            return msg + suffix;
        }
        /// <summary>
        /// Return the left most characters of a string
        /// </summary>
        /// <param name="target">string to get characters from</param>
        /// <param name="characterCount">Maximum number of characters to return</param>
        /// <returns>A string whose length is less than or equal to <paramref name="characterCount"/></returns>
        public static string Left(this string target, int characterCount)
        {
            return target.Left(characterCount, "");
        }

        /// <summary>
        /// Test a string against standard operating system wild cards.  Convenience extension for TextUtils.MatchesWildCard
        /// </summary>
        /// <param name="sourceString">String being searched</param>
        /// <param name="mask">? or * in a text string (PH*)</param>
        /// <returns>True if sourceString matches mask</returns>
        public static bool IsWildCardMatch(this string sourceString, string mask)
        {
            return TextUtils.MatchesWildCard(sourceString, mask);
        }

        /// <summary>
        /// Remove all characters (and blanks) from beginning and end of string.  Convenience extension for TextUtils.Trim
        /// </summary>
        /// <param name="inputString">String that will have leading and trailing characters removed</param>
        /// <param name="removalCharacters">List of non-blank characters to remove from ends of string</param>
        /// <returns>Modified string beginning with non-blank, non-<paramref name="removalCharacters"/></returns>
        public static string Trim(this string inputString, string removalCharacters)
        {
            return TextUtils.Trim(inputString, removalCharacters);
        }

        /// <summary>
        /// Search number of <paramref name="matchString"/>s are in input string
        /// </summary>
        /// <param name="inputString">String to search</param>
        /// <param name="matchString">Value being searched for</param>
        /// <returns>Number of <paramref name="matchString"/> occurances in <paramref name="inputString"/></returns>
        public static int Count(this string inputString, string matchString)
        {
            return TextUtils.Count(inputString, matchString);
        }

#if !FRAMEWORK_40
        /// <summary>
        /// Verify a string contains only whitespace characters.  Only compiles if FRAMEWORK_35 is a defined constant
        /// </summary>
        /// <param name="text">Text to evaluate</param>
        /// <returns>true if string contains only whitespace characters</returns>
        public static bool IsNullOrWhitespace(this string text)
        {
            if (string.IsNullOrEmpty(text) == true)
            {
                return true;
            }
            return text.Trim().Length == 0;
        }
#endif
    }
}
