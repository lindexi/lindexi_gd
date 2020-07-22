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
using Serialize.OpenXml.CodeGen.Extentions;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Serialize.OpenXml.CodeGen
{
    /// <summary>
    /// Static class that converts <see cref="OpenXmlElement"/> elements
    /// into Code DOM objects.
    /// </summary>
    public static class OpenXmlElementExtensions
    {
        #region Public Static Methods

        /// <summary>
        /// Converts an <see cref="OpenXmlElement"/> into a <see cref="CodeCompileUnit"/> 
        /// object that can be used to build code in a given .NET language that would 
        /// build the referenced <see cref="OpenXmlElement"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="OpenXmlElement"/> object to generate source code for.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeCompileUnit"/> containing the instructions to build
        /// the referenced <see cref="OpenXmlElement"/>.
        /// </returns>
        public static CodeCompileUnit GenerateSourceCode(this OpenXmlElement element)
        {
            return element.GenerateSourceCode(NamespaceAliasOptions.Default);
        }

        /// <summary>
        /// Converts an <see cref="OpenXmlElement"/> into a <see cref="CodeCompileUnit"/> 
        /// object that can be used to build code in a given .NET language that would 
        /// build the referenced <see cref="OpenXmlElement"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="OpenXmlElement"/> object to generate source code for.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to apply to the resulting source code.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeCompileUnit"/> containing the instructions to build
        /// the referenced <see cref="OpenXmlElement"/>.
        /// </returns>
        public static CodeCompileUnit GenerateSourceCode(this OpenXmlElement element, NamespaceAliasOptions opts)
        {
            var result = new CodeCompileUnit();
            var eType = element.GetType();
            var typeCounts = new Dictionary<Type, int>();
            var namespaces = new SortedSet<string>();
            var mainNamespace = new CodeNamespace("OpenXmlSample");
            string tmpName = null;

            // Setup the main method
            var mainMethod = new CodeMemberMethod()
            {
                Name = $"Build{eType.Name}",
                ReturnType = new CodeTypeReference(eType.Name),
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            mainMethod.Statements.AddRange(BuildCodeStatements(element, opts, typeCounts, namespaces, out tmpName));
            mainMethod.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(tmpName)));

            // Setup the main class next
            var mainClass = new CodeTypeDeclaration($"{eType.Name}BuilderClass")
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };
            mainClass.Members.Add(mainMethod);
            
            // Setup the imports
            var codeNameSpaces = new List<CodeNamespaceImport>(namespaces.Count);
            foreach (var ns in namespaces)
            {
                codeNameSpaces.Add(ns.GetCodeNamespaceImport(opts));
            }
            codeNameSpaces.Sort(new CodeNamespaceImportComparer());

            mainNamespace.Imports.AddRange(codeNameSpaces.ToArray());
            mainNamespace.Types.Add(mainClass);

            // Finish up
            result.Namespaces.Add(mainNamespace);
            return result;
        }

        /// <summary>
        /// Converts an <see cref="OpenXmlElement"/> into a <see cref="string"/> representation
        /// of dotnet source code that can be compiled to build <paramref name="element"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="OpenXmlElement"/> object to generate source code for.
        /// </param>
        /// <param name="provider">
        /// The <see cref="CodeDomProvider"/> object to create the resulting source code.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> representation of the source code generated by 
        /// <paramref name="provider"/> that could create <paramref name="element"/> when compiled.
        /// </returns>
        public static string GenerateSourceCode(this OpenXmlElement element, CodeDomProvider provider)
        {
            return element.GenerateSourceCode(NamespaceAliasOptions.Default, provider);
        }

        /// <summary>
        /// Converts an <see cref="OpenXmlElement"/> into a <see cref="string"/> representation
        /// of dotnet source code that can be compiled to build <paramref name="element"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="OpenXmlElement"/> object to generate source code for.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to apply to the resulting source code.
        /// </param>
        /// <param name="provider">
        /// The <see cref="CodeDomProvider"/> object to create the resulting source code.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> representation of the source code generated by 
        /// <paramref name="provider"/> that could create <paramref name="element"/> when compiled.
        /// </returns>
        public static string GenerateSourceCode(this OpenXmlElement element, NamespaceAliasOptions opts, CodeDomProvider provider)
        {
            var codeString = new System.Text.StringBuilder();
            var code = element.GenerateSourceCode(opts);

            using (var sw = new System.IO.StringWriter(codeString))
            {
                provider.GenerateCodeFromCompileUnit(code, sw,
                    new CodeGeneratorOptions() { BracingStyle = "C" });
            }
            return codeString.ToString().RemoveOutputHeaders().Trim();
        }


        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Builds the appropriate code objects that would build the contents of
        /// <paramref name="e"/>.
        /// </summary>
        /// <param name="e">
        /// The <see cref="OpenXmlElement"/> object to codify.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to use during the variable naming 
        /// process.
        /// </param>
        /// <param name="typeCounts">
        /// A lookup <see cref="IDictionary{TKey, TValue}"/> object containing the
        /// number of times a given type was referenced.  This is used for variable naming
        /// purposes.
        /// </param>
        /// <param name="namespaces">
        /// Collection <see cref="ISet{T}"/> used to keep track of all openxml namespaces
        /// used during the process.
        /// </param>
        /// <param name="elementName">
        /// The variable name of the root <see cref="OpenXmlElement"/> object that was built
        /// from the <paramref name="e"/>.
        /// </param>
        /// <returns>
        /// A collection of code statements and expressions that could be used to generate
        /// a new <paramref name="e"/> object from code.
        /// </returns>
        internal static CodeStatementCollection BuildCodeStatements(
            OpenXmlElement e,
            NamespaceAliasOptions opts,
            IDictionary<Type, int> typeCounts,
            ISet<string> namespaces,
            out string elementName)
        {
            // argument validation
            if (e is null) throw new ArgumentNullException(nameof(e));
            if (opts is null) throw new ArgumentNullException(nameof(opts));
            if (typeCounts is null) throw new ArgumentNullException(nameof(typeCounts));
            if (namespaces is null) throw new ArgumentNullException(nameof(namespaces));

            // method vars
            var result = new CodeStatementCollection();
            var elementType = e.GetType();

            CodeStatement statement;
            CodeObjectCreateExpression createExpression;
            CodeMethodReferenceExpression methodReferenceExpression;
            CodeMethodInvokeExpression invokeExpression;
            CodeTypeReferenceCollection typeReferenceCollection;
            CodeTypeReference typeReference;

            // Dictionary used to map complex objects to element properties
            var simpleTypePropReferences = new Dictionary<string, string>();

            Type tmpType = null;
            string simpleName = null;
            string junk = null;
            object val = null;
            object propVal = null;
            PropertyInfo pi = null;

            // Start pulling out the properties of the current element.
            var sProperties = elementType.GetOpenXmlSimpleValuesProperties();
            var cProps = elementType.GetOpenXmlSimpleTypeProperties(false);
            IReadOnlyList<PropertyInfo> cProperties = cProps
                .Where(m => !m.PropertyType.IsEnumValueType())
                .ToList();
            IReadOnlyList<PropertyInfo> enumProperties = cProps
                .Where(m => m.PropertyType.IsEnumValueType())
                .ToList();

            // Add blank code line
            void addBlankLine() => result.Add(new CodeSnippetStatement(String.Empty));

            // Create a variable reference statement
            CodeAssignStatement primitivePropertyAssignment(
                string objName,
                string varName,
                string rVal,
                bool varIsRef = false)
            {
                var rValExp = (varIsRef
                    ? new CodeVariableReferenceExpression(rVal)
                    : new CodePrimitiveExpression(rVal) as CodeExpression);
                return new CodeAssignStatement(
                    new CodePropertyReferenceExpression(
                        new CodeVariableReferenceExpression(objName), varName),
                    rValExp);
            }

            // Add the current element type namespace to the set object
            namespaces.Add(elementType.Namespace);

            // Need to build the non enumvalue complex objects first before assigning
            // them as properties of the current element
            foreach (var complex in cProperties)
            {
                // Get the value of the current property
                val = complex.GetValue(e);

                // Skip properties that are null
                if (val is null) continue;

                // Add the complex property namespace to the set
                namespaces.Add(complex.PropertyType.Namespace);

                // Use the junk var to store the property type name
                junk = complex.PropertyType.Name;

                // Build the variable name
                simpleName = complex.PropertyType.GenerateVariableName(typeCounts);

                // Need to handle the generic properties special when trying
                // to build a variable name.
                if (complex.PropertyType.IsGenericType)
                {
                    // Setup necessary CodeDom objects
                    typeReferenceCollection = new CodeTypeReferenceCollection();

                    foreach (var gen in complex.PropertyType.GenericTypeArguments)
                    {
                        typeReferenceCollection.Add(gen.Name);
                    }

                    typeReference = new CodeTypeReference(junk);
                    typeReference.TypeArguments.AddRange(typeReferenceCollection);
                    createExpression = new CodeObjectCreateExpression(typeReference);
                    statement = new CodeVariableDeclarationStatement(typeReference, simpleName, createExpression);
                }
                else
                {
                    createExpression = new CodeObjectCreateExpression(junk);
                    statement = new CodeVariableDeclarationStatement(junk, simpleName, createExpression);
                }
                result.Add(statement);

                // Finish the variable assignment statement
                result.Add(primitivePropertyAssignment(simpleName, "InnerText", val.ToString()));
                addBlankLine();

                // Keep track of the objects to assign to the current element
                // complex properties
                simpleTypePropReferences.Add(complex.Name, simpleName);
            }

            // Initialize the mc attribute information, if available
            if (e.MCAttributes != null)
            {
                tmpType = e.MCAttributes.GetType();

                simpleName = tmpType.GenerateVariableName(typeCounts);

                createExpression = new CodeObjectCreateExpression(tmpType.Name);
                statement = new CodeVariableDeclarationStatement(tmpType.Name, simpleName, createExpression);
                result.Add(statement);

                foreach (var m in tmpType.GetStringValueProperties())
                {
                    val = m.GetValue(e.MCAttributes);
                    if (val != null)
                    {
                        statement = new CodeAssignStatement(
                            new CodePropertyReferenceExpression(
                                new CodeVariableReferenceExpression(simpleName), m.Name),
                                new CodePrimitiveExpression(val.ToString()));
                        result.Add(statement);
                    }
                }
                addBlankLine();
                simpleTypePropReferences.Add("MCAttributes", simpleName);
            }

            // Build the initializer for the current element
            elementName = elementType.GenerateVariableName(typeCounts);

            // Include the alias prefix if the current element belongs to a class
            // within the namespaces identified to needing an alias
            junk = elementType.GetObjectTypeName(opts.Order);

            // OpenXmlLeafTextElement classes have constructors that take in
            // one StringValue object as a parameter to populate the new
            // object's Text property.  This takes advantange of that knowledge.
            if (elementType.IsSubclassOf(typeof(OpenXmlLeafTextElement)))
            {
                var leafText = elementType.GetProperty("Text").GetValue(e);
                var param = new CodePrimitiveExpression(leafText);
                createExpression = new CodeObjectCreateExpression(junk, param);
            }
            else
            {
                createExpression = new CodeObjectCreateExpression(junk);
            }
            statement = new CodeVariableDeclarationStatement(junk, elementName, createExpression);
            result.Add(statement);

            // Don't forget to add any additional namespaces to the element
            if (e.NamespaceDeclarations != null && e.NamespaceDeclarations.Count() > 0)
            {
                addBlankLine();
                foreach (var ns in e.NamespaceDeclarations)
                {
                    methodReferenceExpression = new CodeMethodReferenceExpression(
                        new CodeVariableReferenceExpression(elementName),
                        "AddNamespaceDeclaration");
                    invokeExpression = new CodeMethodInvokeExpression(methodReferenceExpression,
                        new CodePrimitiveExpression(ns.Key),
                        new CodePrimitiveExpression(ns.Value));
                    result.Add(invokeExpression);
                }

                // Add a line break if namespace declarations were present and if the current
                // element has additional properties that need to be filled out.
                if (cProperties.Count > 0 || simpleTypePropReferences.Count > 0)
                {
                    addBlankLine();
                }
                else if (sProperties.Count > 0)
                {
                    foreach (var p in sProperties)
                    {
                        val = p.GetValue(e);
                        if (val != null)
                        {
                            addBlankLine();
                            break;
                        }
                    }
                }
            }

            // Now set the properties of the current variable
            foreach (var p in sProperties)
            {
                val = p.GetValue(e);
                if (val == null) continue;

                // Add the simple property type namespace to the set
                namespaces.Add(p.PropertyType.Namespace);

                propVal = val.GetType().GetProperty("Value").GetValue(val);

                statement = new CodeAssignStatement(
                    new CodePropertyReferenceExpression(
                        new CodeVariableReferenceExpression(elementName), p.Name),
                        new CodePrimitiveExpression(propVal));
                result.Add(statement);
            }

            if (simpleTypePropReferences.Count > 0)
            {
                foreach (var sProp in simpleTypePropReferences)
                {
                    statement = new CodeAssignStatement(
                        new CodePropertyReferenceExpression(
                            new CodeVariableReferenceExpression(elementName), sProp.Key),
                            new CodeVariableReferenceExpression(sProp.Value));
                    result.Add(statement);
                }
            }

            // Go through the list of complex properties again but include
            // EnumValue`1 type properties in the search.
            foreach (var cp in enumProperties)
            {
                val = cp.GetValue(e);
                simpleName = null;

                if (val is null) continue;

                pi = cp.PropertyType.GetProperty("Value");
                simpleName = pi.PropertyType.GetObjectTypeName(opts.Order);

                // Add the simple property type namespace to the set
                namespaces.Add(pi.PropertyType.Namespace);

                statement = new CodeAssignStatement(
                    new CodePropertyReferenceExpression(
                        new CodeVariableReferenceExpression(elementName), cp.Name),
                        new CodeFieldReferenceExpression(
                            new CodeVariableReferenceExpression(simpleName),
                            pi.GetValue(val).ToString()));
                result.Add(statement);
            }
            // Insert an empty line, if possible
            addBlankLine();

            // See if the current element has children and retrieve that information
            if (e.HasChildren)
            {
                foreach (var child in e)
                {
                    // use recursion to generate source code for the child elements
                    result.AddRange(
                        BuildCodeStatements(child, opts, typeCounts, namespaces, out string appendName));

                    methodReferenceExpression = new CodeMethodReferenceExpression(
                        new CodeVariableReferenceExpression(elementName),
                        "Append");
                    invokeExpression = new CodeMethodInvokeExpression(methodReferenceExpression, 
                        new CodeVariableReferenceExpression(appendName));
                    result.Add(invokeExpression);
                    addBlankLine();
                }
            }

            // Return all of the collected expressions and statements
            return result;
        }

        #endregion
    }
}
