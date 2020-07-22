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

using DocumentFormat.OpenXml;
using Serialize.OpenXml.CodeGen;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Serialize.OpenXml.CodeGen.Extentions
{
    /// <summary>
    /// Collection of extension methods for the <see cref="Type"/> class
    /// specific to the generating code dom representations of
    /// OpenXml objects.
    /// </summary>
    internal static class TypeExtensions
    {
        #region Private Static Fields

        /// <summary>
        /// Holds all of the OpenXml SDK namespaces that require aliases when
        /// building the <see cref="CodeNamespaceImport"/> objects for the
        /// given project.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> _namespaceAliases;

        /// <summary>
        /// Holds all known types that are considered simple values.
        /// </summary>
        private static readonly IReadOnlyList<Type> _simpleValueTypes;

        #endregion

        #region Static Constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static TypeExtensions()
        {
            // Setup the namespace alias collection
            _namespaceAliases = new Dictionary<string, string>(StringComparer.Ordinal)
            {                
                {"DocumentFormat.OpenXml.ExtendedProperties", "AP"},
                {"DocumentFormat.OpenXml.VariantTypes", "VT"},
                {"DocumentFormat.OpenXml.CustomProperties", "OP"},
                {"DocumentFormat.OpenXml.Drawing", "A"},
                {"DocumentFormat.OpenXml.Drawing.ChartDrawing", "CDR"},
                {"DocumentFormat.OpenXml.Drawing.Charts", "C"},
                {"DocumentFormat.OpenXml.Drawing.Diagrams", "DGM"},
                {"DocumentFormat.OpenXml.Drawing.LegacyCompatibility", "COMP"},
                {"DocumentFormat.OpenXml.Drawing.LockedCanvas", "LC"},
                {"DocumentFormat.OpenXml.Drawing.Pictures", "PIC"},
                {"DocumentFormat.OpenXml.Drawing.Spreadsheet", "XDR"},
                {"DocumentFormat.OpenXml.Drawing.Wordprocessing", "WP"},
                {"DocumentFormat.OpenXml.Office2010.CustomUI", "MSO14"},
                {"DocumentFormat.OpenXml.Office2010.Drawing", "A14"},
                {"DocumentFormat.OpenXml.Office2010.Drawing.ChartDrawing", "CDR14"},
                {"DocumentFormat.OpenXml.Office2010.Drawing.Charts", "C14"},
                {"DocumentFormat.OpenXml.Office2010.Drawing.Diagram", "DGM14"},
                {"DocumentFormat.OpenXml.Office2010.Drawing.LegacyCompatibility", "COM14"},
                {"DocumentFormat.OpenXml.Office2010.Drawing.Pictures", "PIC14"},
                {"DocumentFormat.OpenXml.Office2010.Drawing.Slicer", "SLE"},
                {"DocumentFormat.OpenXml.Office2010.Excel", "X14"},
                {"DocumentFormat.OpenXml.Office2010.Excel.Drawing", "XDR14"},
                {"DocumentFormat.OpenXml.Office2010.ExcelAc", "X12AC"},
                {"DocumentFormat.OpenXml.Office2010.Ink", "MSINK"},
                {"DocumentFormat.OpenXml.Office2010.PowerPoint", "P14"},
                {"DocumentFormat.OpenXml.Office2010.Word", "W14"},
                {"DocumentFormat.OpenXml.Office2010.Word.Drawing", "WP14"},
                {"DocumentFormat.OpenXml.Office2010.Word.DrawingCanvas", "WPC"},
                {"DocumentFormat.OpenXml.Office2010.Word.DrawingGroup", "WPG"},
                {"DocumentFormat.OpenXml.Office2010.Word.DrawingShape", "WPS"},
                {"DocumentFormat.OpenXml.Office2013.Drawing", "A15"},
                {"DocumentFormat.OpenXml.Office2013.Drawing.Chart", "C15"},
                {"DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle", "CS"},
                {"DocumentFormat.OpenXml.Office2013.Drawing.TimeSlicer", "TSLE"},
                {"DocumentFormat.OpenXml.Office2013.Excel", "X15"},
                {"DocumentFormat.OpenXml.Office2013.ExcelAc", "X15AC"},
                {"DocumentFormat.OpenXml.Office2013.PowerPoint", "P15"},
                {"DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming", "PROAM"},
                {"DocumentFormat.OpenXml.Office2013.Theme", "THM15"},
                {"DocumentFormat.OpenXml.Office2013.WebExtension", "WE"},
                {"DocumentFormat.OpenXml.Office2013.WebExtentionPane", "WETP"},
                {"DocumentFormat.OpenXml.Office2013.Word", "W15"},
                {"DocumentFormat.OpenXml.Office2013.Word.Drawing", "WP15"}
            };

            // Now setup the simple type collection.
            var simpleTypes = new Type[]
            {
                typeof(StringValue),
                typeof(OpenXmlSimpleValue<uint>),
                typeof(OpenXmlSimpleValue<int>),
                typeof(OpenXmlSimpleValue<byte>),
                typeof(OpenXmlSimpleValue<sbyte>),
                typeof(OpenXmlSimpleValue<short>),
                typeof(OpenXmlSimpleValue<long>),
                typeof(OpenXmlSimpleValue<ushort>),
                typeof(OpenXmlSimpleValue<ulong>),
                typeof(OpenXmlSimpleValue<float>),
                typeof(OpenXmlSimpleValue<double>),
                typeof(OpenXmlSimpleValue<decimal>),
                typeof(OpenXmlSimpleValue<bool>),
                typeof(OpenXmlSimpleValue<DateTime>)
            };

            _simpleValueTypes = simpleTypes.ToList();
        }

        #endregion

        #region Public Static Properties

        /// <summary>
        /// Gets the collection of OpenXml namespaces and their corrisponding
        /// import aliases.
        /// </summary>
        public static IReadOnlyDictionary<string, string> NamespaceAliases 
            => _namespaceAliases;

        #endregion

        #region Public Static methods

        /// <summary>
        /// Generates a variable name to use when generating the appropriate
        /// CodeDom objects for a given <see cref="Type"/>.
        /// </summary>
        /// <param name="t">
        /// The <see cref="Type"/> to generate the variable name for.
        /// </param>
        /// <param name="typeCount">
        /// The <see cref="IDictionary{TKey, TValue}"/> object that tracks 
        /// the number of times a given type has been generated.
        /// </param>
        /// <returns>
        /// A new variable name to use to represent <paramref name="t"/>.
        /// </returns>
        public static string GenerateVariableName(this Type t, IDictionary<Type, int> typeCount)
        {
            string tmp;  // Hold the generated name
            string nsPrefix = String.Empty;

            // Include the namespace alias as part of the variable name
            if (NamespaceAliases.ContainsKey(t.Namespace))
            {
                nsPrefix = NamespaceAliases[t.Namespace].ToLowerInvariant();
            }

            // Simply return the generated name if the current
            // type is not considered generic.
            if (!t.IsGenericType)
            {
                tmp = String.Concat(nsPrefix, t.Name).ToCamelCase();
                if (typeCount != null && typeCount.ContainsKey(t))
                {
                    return String.Concat(tmp, ++typeCount[t]);
                }
                typeCount.Add(t, 0);
                return tmp;
            }

            // Include the generic types as part of the var name.
            var sb = new StringBuilder();
            foreach (var item in t.GenericTypeArguments)
            {
                sb.Append(item.Name.RetrieveUpperCaseChars().ToTitleCase());
            }
            tmp = t.Name;

            if (typeCount != null && typeCount.ContainsKey(t))
            {
                return String.Concat(nsPrefix,
                    tmp.Substring(0, tmp.IndexOf("`")),
                    sb.ToString(),
                    ++typeCount[t]).ToCamelCase();
            }

            typeCount.Add(t, 0);
            return String.Concat(nsPrefix,
                tmp.Substring(0, tmp.IndexOf("`")),
                sb.ToString()).ToCamelCase();
        }

        /// <summary>
        /// Builds a new <see cref="CodeNamespaceImport"/> object based on the
        /// namespace of <paramref name="t"/>.
        /// </summary>
        /// <param name="t">
        /// The <see cref="Type"/> object to get the namespace from.
        /// </param>
        /// <param name="options">
        /// The <see name="NamespaceAliasOptions"/> object to generate the
        /// <see cref="CodeNamespaceImport"/> object with.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeNamespaceImport"/> for the specified type.
        /// </returns>
        public static CodeNamespaceImport GetCodeNamespaceImport(this Type t, NamespaceAliasOptions options)
        {
            return t.Namespace.GetCodeNamespaceImport(options);
        }

        /// <summary>
        /// Creates a class name <see cref="String"/> to use when generating
        /// source code.
        /// </summary>
        /// <param name="t">
        /// The <see cref="Type"/> object containing the class name to evaluate.
        /// </param>
        /// <param name="order">
        /// The <see cref="NamespaceAliasOrder"/> value to evaluate when building the
        /// appropriate class name.
        /// </param>
        /// <returns>
        /// The class name to use when building a new <see cref="CodeObjectCreateExpression"/>
        /// object.
        /// </returns>
        public static string GetObjectTypeName(this Type t, NamespaceAliasOrder order)
        {
            if (_namespaceAliases.ContainsKey(t.Namespace))
            {
                return order == NamespaceAliasOrder.None ? t.FullName :
                    $"{_namespaceAliases[t.Namespace]}.{t.Name}";
            }
            return t.Name;
        }

        /// <summary>
        /// Gets all of the <see cref="PropertyInfo"/> objects that inherit from
        /// the <see cref="OpenXmlSimpleType"/> class in <paramref name="t"/>.
        /// </summary>
        /// <param name="t">
        /// The <see cref="Type"/> object to retrieve the <see cref="PropertyInfo"/>
        /// objects from.
        /// </param>
        /// <returns>
        /// A collection of <see cref="PropertyInfo"/> objects that inherit from
        /// the <see cref="OpenXmlSimpleType"/> class.
        /// </returns>
        /// <remarks>
        /// All necessary OpenXml object properties inherit from the
        /// <see cref="OpenXmlSimpleType"/> class.  This makes it easier to
        /// iterate through.
        /// </remarks>
        public static IReadOnlyList<PropertyInfo> GetOpenXmlSimpleTypeProperties(this Type t)
        {
            return t.GetOpenXmlSimpleTypeProperties(true);
        }

        /// <summary>
        /// Gets all of the <see cref="PropertyInfo"/> objects that inherit from
        /// the <see cref="OpenXmlSimpleType"/> class in <paramref name="t"/>.
        /// </summary>
        /// <param name="t">
        /// The <see cref="Type"/> object to retrieve the <see cref="PropertyInfo"/>
        /// objects from.
        /// </param>
        /// <param name="includeSimpleValueTypes">
        /// Include properties with types that inherit from <see cref="OpenXmlSimpleValue{T}"/>
        /// type.
        /// </param>
        /// <returns>
        /// A collection of <see cref="PropertyInfo"/> objects that inherit from
        /// the <see cref="OpenXmlSimpleType"/> class.
        /// </returns>
        /// <remarks>
        /// All necessary OpenXml object properties inherit from the
        /// <see cref="OpenXmlSimpleType"/> class.  This makes it easier to
        /// iterate through.
        /// </remarks>
        public static IReadOnlyList<PropertyInfo> GetOpenXmlSimpleTypeProperties(
            this Type t, bool includeSimpleValueTypes)
        {
            var props = t.GetProperties();
            var result = new List<PropertyInfo>();

            // Collect all properties that are of type or subclass of
            // OpenXmlSimpleType
            foreach (var p in props)
            {
                if (p.PropertyType.Equals(typeof(OpenXmlSimpleType)) ||
                    p.PropertyType.IsSubclassOf(typeof(OpenXmlSimpleType)))
                {
                    if (includeSimpleValueTypes || !p.PropertyType.IsSimpleValueType())
                    {
                        result.Add(p);
                    }
                }
            }
            // Return the collected
            return result;
        }

        /// <summary>
        /// Gets all of the <see cref="PropertyInfo"/> objects that inherit from
        /// the <see cref="OpenXmlSimpleValue{T}"/> class in <paramref name="t"/>.
        /// </summary>
        /// <param name="t">
        /// The <see cref="Type"/> object to retrieve the <see cref="PropertyInfo"/>
        /// objects from.
        /// </param>
        /// <returns>
        /// A collection of <see cref="PropertyInfo"/> objects that inherit from
        /// the <see cref="OpenXmlSimpleValue{T}"/> class.
        /// </returns>
        /// <remarks>
        /// All necessary OpenXml object properties inherit from the
        /// <see cref="OpenXmlSimpleValue{T}"/> class.  This makes it easier to
        /// iterate through.
        /// </remarks>
        public static IReadOnlyList<PropertyInfo> GetOpenXmlSimpleValuesProperties(this Type t)
        {
            var props = t.GetProperties();
            var result = new List<PropertyInfo>();

            // Collect all properties that are of type or subclass of
            // OpenXmlSimpleType
            foreach (var p in props)
            {
                foreach (var item in _simpleValueTypes)
                {
                    if (p.PropertyType.Equals(item) ||
                        p.PropertyType.IsSubclassOf(item))
                    {
                        result.Add(p);
                        break;
                    }
                }
            }
            // Return the collected properties
            return result;
        }

        /// <summary>
        /// Gets all <see cref="PropertyInfo"/> objects from <paramref name="t"/>
        /// of type <see cref="StringValue"/>.
        /// </summary>
        /// <param name="t">
        /// The <see cref="Type"/> to retrieve the <see cref="PropertyInfo"/> objects
        /// from.
        /// </param>
        /// <returns>
        /// A read only collection of <see cref="PropertyInfo"/> objects with a
        /// <see cref="StringValue"/> property type.
        /// </returns>
        public static IReadOnlyList<PropertyInfo> GetStringValueProperties(this Type t)
            => t.GetProperties().Where(s => s.PropertyType == typeof(StringValue)).ToList();

        /// <summary>
        /// Checks to see if <paramref name="t"/> is EnumValue`1.
        /// </summary>
        /// <param name="t">
        /// The <see cref="PropertyInfo"/> to evaluate.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the type of <paramref name="t"/>
        /// is EnumValue`1; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsEnumValueType(this Type t) =>
          t.Name.Equals("EnumValue`1", StringComparison.Ordinal);

        /// <summary>
        /// Indicates whether or not <paramref name="t"/> is considered
        /// a <see cref="OpenXmlSimpleValue{T}"/> type.
        /// </summary>
        /// <param name="t">
        /// The <see cref="Type"/> to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="t"/> is derived
        /// from an OpenXmlSimpleValue type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsSimpleValueType(this Type t)
        {
            foreach (var item in _simpleValueTypes)
            {
                if (t.Equals(item) || t.IsSubclassOf(item))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}