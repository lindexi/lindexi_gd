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

namespace Serialize.OpenXml.CodeGen
{
    /// <summary>
    /// Class used to organize namespace import information
    /// during the OpenXml document reflection process.
    /// </summary>
    public class NamespaceAliasOptions
    {
        #region Public Static Fields

        /// <summary>
        /// Gets a <see cref="NamespaceAliasOptions"/> object to use
        /// with .NET languages that do not support namespace aliasing.
        /// </summary>
        public static readonly NamespaceAliasOptions Empty
            = new NamespaceAliasOptions();

        /// <summary>
        /// Gets a <see cref="NamespaceAliasOptions"/> object to use
        /// with the first class Microsoft .NET languages, C# and
        /// VB.NET.
        /// </summary>
        public static readonly NamespaceAliasOptions Default
            = new NamespaceAliasOptions()
        {
            Order = NamespaceAliasOrder.AliasFirst,
            AssignmentOperator = "="
        };

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceAliasOptions"/>
        /// class that is empty.
        /// </summary>
        public NamespaceAliasOptions()
        {
            AssignmentOperator = String.Empty;
            Order = NamespaceAliasOrder.None;
        }

        #endregion

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the text used to assign an alias to a namespace.
        /// </summary>
        public string AssignmentOperator
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates which order the namespace alias assignment is
        /// specified.
        /// </summary>
        public NamespaceAliasOrder Order
        {
            get;
            set;
        }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Builds a new <see cref="CodeNamespaceImport"/> object based
        /// on the settings of the current object.
        /// </summary>
        /// <param name="ns">
        /// The namespace to build the new <see cref="CodeNamespaceImport"/>
        /// object.
        /// </param>
        /// <param name="alias">
        /// The alias name to use when building the new <see cref="CodeNamespaceImport"/>
        /// object.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeNamespaceImport"/> object.
        /// </returns>
        public virtual CodeNamespaceImport BuildNamespaceImport(string ns, string alias)
        {
            if (String.IsNullOrWhiteSpace(alias) ||
                Order == NamespaceAliasOrder.None)
                return new CodeNamespaceImport(ns);

            const string assignment = "{0} {1} {2}";
            string name = String.Empty;

            switch (Order)
            {
                case NamespaceAliasOrder.NamespaceFirst:
                    name = String.Format(assignment,
                        ns.Trim(),
                        AssignmentOperator.Trim(),
                        alias.Trim());
                    break;
                case NamespaceAliasOrder.AliasFirst:
                    name = String.Format(assignment,
                        alias.Trim(),
                        AssignmentOperator.Trim(),
                        ns.Trim());
                    break;
            }
            return new CodeNamespaceImport(name);
        }

        /// <summary>
        /// Builds a new <see cref="CodeNamespaceImport"/> object based
        /// on the settings of the current object.
        /// </summary>
        /// <param name="ns">
        /// The <see cref="Type"/> object containing the namespace to build
        /// the new <see cref="CodeNamespaceImport"/> object.
        /// </param>
        /// <param name="alias">
        /// The alias name to use when building the new <see cref="CodeNamespaceImport"/>
        /// object.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeNamespaceImport"/> object.
        /// </returns>
        public CodeNamespaceImport BuildNamespaceImport(Type ns, string alias)
        {
            return BuildNamespaceImport(ns.Namespace, alias);
        }

        #endregion
    }
}