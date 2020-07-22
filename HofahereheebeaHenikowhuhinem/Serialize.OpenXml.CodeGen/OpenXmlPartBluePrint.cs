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
using System.Globalization;
using DocumentFormat.OpenXml.Packaging;

namespace Serialize.OpenXml.CodeGen
{
    /// <summary>
    /// Simple organization class used to keep track of all the OpenXmlPart details needed
    /// to complete a code generation request.
    /// </summary>
    internal sealed class OpenXmlPartBluePrint
    {
        #region Public Constructrs

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenXmlPartBluePrint"/> class
        /// with the required <see cref="OpenXmlPart"/> object and instance variable name.
        /// </summary>
        /// <param name="part">
        /// The <see cref="OpenXmlPart"/> object.
        /// </param>
        /// <param name="varName">
        /// Variable name that <paramref name="part"/> was initialized with in the
        /// generated code.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="part"/> is <see langword="null"/> or <paramref name="varName"/>
        /// is <see langword="null"/> or blank.
        /// </exception>
        public OpenXmlPartBluePrint(OpenXmlPart part, string varName)
        {
            Part = part ?? throw new ArgumentNullException(nameof(part));
            VariableName = varName ?? throw new ArgumentNullException(nameof(varName));
            MethodName = this.CreateMethodName(VariableName);
        }

        #endregion

        #region Public Instance Properties

        /// <summary>
        /// Gets the name to use when generating the code responsible for creating
        /// <see cref="Part"/>.
        /// </summary>
        public string MethodName { get; private set;}

        /// <summary>
        /// Gets the <see cref="OpenXmlPart"/> object that this instance represents.
        /// </summary>
        public OpenXmlPart Part { get; private set; }

        /// <summary>
        /// Gets the <see cref="Uri"/> for this object.
        /// </summary>
        public Uri Uri => Part?.Uri;

        /// <summary>
        /// Gets the variable name that <see cref="Part"/> was initialized with.
        /// </summary>
        public string VariableName { get; private set; }

        #endregion

        #region Private Instance Methods

        /// <summary>
        /// Create a method name for the current instance.
        /// </summary>
        /// <param name="varName">
        /// The variable name that this instance was initialized with.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> method name.
        /// </returns>
        private string CreateMethodName(string varName)
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            char[] chars = new char[varName.Length];

            for (int i = 0; i < varName.Length; i++)
            {
                if (i == 0)
                {
                    chars[i] = !Char.IsUpper(varName[i])
                        ? Char.ToUpper(varName[i], ci)
                        : varName[i];
                    continue;
                }
                chars[i] = varName[i];
            }
            return $"Generate{new string(chars)}";
        }

        #endregion
    }
}