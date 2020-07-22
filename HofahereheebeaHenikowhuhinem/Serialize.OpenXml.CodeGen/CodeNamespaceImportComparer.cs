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
using System.Collections.Generic;
using System.CodeDom;

namespace Serialize.OpenXml.CodeGen
{
    /// <summary>
    /// Comparer based class used to sort <see cref="CodeNamespaceImport"/> objects.
    /// </summary>
    public class CodeNamespaceImportComparer 
        : Comparer<CodeNamespaceImport>, 
        IEqualityComparer<CodeNamespaceImport>
    {
        #region Private Static Fields

        /// <summary>
        /// Holds the comparer object to use for all string comparison operations.
        /// </summary>
        private static readonly StringComparer _cmpr = StringComparer.Ordinal;

        #endregion

        #region Public Instance Methods

        /// <inheritdoc/>
        public override int Compare(CodeNamespaceImport x, CodeNamespaceImport y)
        {
            if (x.Namespace.Contains("=") && !y.Namespace.Contains("="))
            {
                return 1;
            }
            if (!x.Namespace.Contains("=") && y.Namespace.Contains("="))
            {
                return -1;
            }

            // Check the namespace name first.
            if (!_cmpr.Equals(x.Namespace, y.Namespace))
            {
                return _cmpr.Compare(x.Namespace, y.Namespace);
            }

            // See if any of the LinePragma properties are null and return the appropriate code.
            if (x.LinePragma is null && y.LinePragma is null) return 0;
            if (x.LinePragma is null) return 1;
            if (y.LinePragma is null) return -1;

            // Compare linepragma properties if both parameters have them.
            if (!_cmpr.Equals(x.LinePragma.FileName, y.LinePragma.FileName))
            {
                return _cmpr.Compare(x.LinePragma.FileName, y.LinePragma.FileName);
            }
            if (!x.LinePragma.LineNumber.Equals(y.LinePragma.LineNumber))
            {
                return x.LinePragma.LineNumber.CompareTo(y.LinePragma.LineNumber);
            }
            return 0;
        }

        /// <inheritdoc/>
        public bool Equals(CodeNamespaceImport x, CodeNamespaceImport y)
        {
            // Check the namespace name first.
            if (!_cmpr.Equals(x.Namespace, y.Namespace)) return false;

            // Check the linepragma properties next
            if (x.LinePragma != null)
            {
                if (y.LinePragma == null) return false;

                if (!_cmpr.Equals(x.LinePragma.FileName, y.LinePragma.FileName)) return false;
                if (!x.LinePragma.LineNumber.Equals(y.LinePragma.LineNumber)) return false;
            }
            else
            {
                if (y.LinePragma != null) return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public int GetHashCode(CodeNamespaceImport obj)
        {
            if (obj is null) return 0;

            unchecked
            {
                int hash = unchecked((int)2166136261);
                const int prime = 16777619;

                hash = (hash ^ obj.Namespace.GetHashCode()) * prime;

                if (obj.LinePragma != null)
                {
                    hash = (hash ^ obj.LinePragma.FileName.GetHashCode()) * prime;
                    hash = (hash ^ obj.LinePragma.LineNumber.GetHashCode()) * prime;
                }

                return hash;
            }
        }

        #endregion
    }
}