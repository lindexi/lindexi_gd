/* MIT License

Copyright (c) 2020 Ryan Boggs

Permission is hereby granted, free of charge, to any person obtaining a copy of this
software and associated documentation files (the "Software"), to deal in the Software
without restriction, including without limitation the rights to use, copy, modify,
merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be included in all copies
or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
*/

using System;
using System.CodeDom;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Serialize.OpenXml.CodeGen.Extentions
{
    /// <summary>
    /// Static class containing extension methods for the <see cref="String"/> class.
    /// </summary>
    internal static class StringExtensions
    {
        #region Public Static Methods

        /// <summary>
        /// Builds a new <see cref="CodeNamespaceImport"/> object based on the
        /// namespace of <paramref name="ns"/>.
        /// </summary>
        /// <param name="ns">
        /// The <see cref="String"/> namespace name.
        /// </param>
        /// <param name="options">
        /// The <see name="NamespaceAliasOptions"/> object to generate the
        /// <see cref="CodeNamespaceImport"/> object with.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeNamespaceImport"/> for the specified type.
        /// </returns>
        public static CodeNamespaceImport GetCodeNamespaceImport(this string ns, NamespaceAliasOptions options)
        {
            if (TypeExtensions.NamespaceAliases.ContainsKey(ns))
            {
                return options.BuildNamespaceImport(ns, 
                    TypeExtensions.NamespaceAliases[ns]);
            }
            return new CodeNamespaceImport(ns);
        }

        /// <summary>
        /// Strips out the standard header text that is included when a
        /// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.codedom.compiler.codedomprovider"/> 
        /// derived class
        /// generates dotnet code.
        /// </summary>
        /// <param name="raw">
        /// The raw code <see cref="string"/> produced by the 
        /// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.codedom.compiler.codedomprovider"/> 
        /// derived class.
        /// </param>
        /// <returns>
        /// A new code <see cref="string"/> value with the default headers removed.
        /// </returns>
        public static string RemoveOutputHeaders(this string raw)
        {
            var indicator = new string('-', 78);
            var sb = new StringBuilder();
            bool inHeader = false;

            using (var sr = new StringReader(raw))
            {
                string currentLine = sr.ReadLine();
                while (currentLine != null)
                {
                    if (currentLine.EndsWith(indicator))
                    {
                        if (inHeader) currentLine = sr.ReadLine();
                        inHeader = !inHeader;
                    }
                    if (Regex.IsMatch(currentLine, "\".*\\\'.*\""))
                    {
                        currentLine = currentLine.Replace("\\'", "'");
                    }
                    if (!inHeader) sb.AppendLine(currentLine);
                    currentLine = sr.ReadLine();
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Retrieves only the upper case characters from a given string.
        /// </summary>
        /// <param name="s">The <see cref="string"/> to analyze.</param>
        /// <returns>
        /// The upper case characters from <paramref name="s"/>.
        /// </returns>
        public static string RetrieveUpperCaseChars(this string s)
        {
            var cs = s.Where(c => Char.IsUpper(c)).ToArray();
            return new string(cs);
        }

        /// <summary>
        /// Ensures that the first letter of a given string is lowercase.
        /// </summary>
        /// <param name="s">
        /// The <see cref="String"/> to manipulate.
        /// </param>
        /// <returns>
        /// The same value as <paramref name="s"/> with the first character converted
        /// to lowercase.
        /// </returns>
        public static string ToCamelCase(this string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return s;
            var sArray = s.ToCharArray();

            sArray[0] = Char.ToLowerInvariant(sArray[0]);
            return new string(sArray);
        }

        /// <summary>
        /// Ensures that the only the first letter of <paramref name="s"/> is
        /// capitalized.
        /// </summary>
        /// <param name="s">
        /// The <see cref="string"/> to capitalize.
        /// </param>
        /// <returns>
        /// A new <see cref="string"/> with the value of <paramref name="s"/> that has only
        /// the first letter capitalized.
        /// </returns>
        public static string ToTitleCase(this string s)
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            char[] chars = new char[s.Length];

            for (int i = 0; i < s.Length; i++)
            {
                if (i == 0)
                {
                    chars[i] = !Char.IsUpper(s[i])
                        ? Char.ToUpper(s[i], ci)
                        : s[i];
                    continue;
                }
                chars[i] = !Char.IsWhiteSpace(s[i]) && !Char.IsLower(s[i])
                    ? Char.ToLower(s[i], ci)
                    : s[i];
            }
            return new string(chars);
        }

        #endregion
    }
}