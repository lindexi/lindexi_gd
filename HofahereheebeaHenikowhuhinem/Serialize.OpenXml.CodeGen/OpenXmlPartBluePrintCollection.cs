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
using System.Collections.ObjectModel;

namespace Serialize.OpenXml.CodeGen
{
    /// <summary>
    /// Collection class used to organize all of the <see cref="OpenXmlPartBluePrint"/> objects
    /// for a single request.
    /// </summary>
    internal class OpenXmlPartBluePrintCollection : KeyedCollection<Uri, OpenXmlPartBluePrint>
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenXmlPartBluePrintCollection"/> class
        /// that is empty.
        /// </summary>
        public OpenXmlPartBluePrintCollection() : base(new UriEqualityComparer()) { }

        #endregion

        #region Protected Instance Methods

        /// <inheritdoc/>
        protected override Uri GetKeyForItem(OpenXmlPartBluePrint item) => item.Uri;

        #endregion
    }
}