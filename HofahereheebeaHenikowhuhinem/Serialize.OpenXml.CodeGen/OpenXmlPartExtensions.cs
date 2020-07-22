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

using DocumentFormat.OpenXml.Packaging;
using Serialize.OpenXml.CodeGen.Extentions;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Serialize.OpenXml.CodeGen
{
    /// <summary>
    /// Static class that converts <see cref="OpenXmlPart">OpenXmlParts</see>
    /// into Code DOM objects.
    /// </summary>
    public static class OpenXmlPartExtensions
    {
        #region Private Static Fields

        /// <summary>
        /// The default parameter name for an <see cref="OpenXmlPart"/> object.
        /// </summary>
        private const string methodParamName = "part";

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Converts an <see cref="OpenXmlPart"/> into a CodeDom object that can be used
        /// to build code in a given .NET language to build the referenced <see cref="OpenXmlPart"/>.
        /// </summary>
        /// <param name="part">
        /// The <see cref="OpenXmlPart"/> object to generate source code for.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeCompileUnit"/> containing the instructions to build
        /// the referenced <see cref="OpenXmlPart"/>.
        /// </returns>
        public static CodeCompileUnit GenerateSourceCode(this OpenXmlPart part)
        {
            return part.GenerateSourceCode(NamespaceAliasOptions.Default);
        }

        /// <summary>
        /// Converts an <see cref="OpenXmlPart"/> into a CodeDom object that can be used
        /// to build code in a given .NET language to build the referenced <see cref="OpenXmlPart"/>.
        /// </summary>
        /// <param name="part">
        /// The <see cref="OpenXmlPart"/> object to generate source code for.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to apply to the resulting source code.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeCompileUnit"/> containing the instructions to build
        /// the referenced <see cref="OpenXmlPart"/>.
        /// </returns>
        public static CodeCompileUnit GenerateSourceCode(this OpenXmlPart part, NamespaceAliasOptions opts)
        {
            CodeMethodReferenceExpression methodRef = null;
            OpenXmlPartBluePrint mainBluePrint = null;
            var result = new CodeCompileUnit();
            var eType = part.GetType();
            var partTypeName = eType.Name;
            var partTypeFullName = eType.FullName; 
            var varName = eType.Name.ToCamelCase();
            var partTypeCounts = new Dictionary<string, int>();
            var namespaces = new SortedSet<string>();
            var mainNamespace = new CodeNamespace("OpenXmlSample");
            var bluePrints = new OpenXmlPartBluePrintCollection();

            // Assign the appropriate variable name
            if (partTypeCounts.ContainsKey(partTypeFullName))
            {
                varName = String.Concat(varName, partTypeCounts[partTypeFullName]++);
            }
            else
            {
                partTypeCounts.Add(partTypeFullName, 1);
            }

            // Generate a new blue print for the current part to help create the main
            // method reference then add it to the blue print collection
            mainBluePrint = new OpenXmlPartBluePrint(part, varName);
            bluePrints.Add(mainBluePrint);
            methodRef = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), mainBluePrint.MethodName);

            // Build the entry method
            var entryMethod = new CodeMemberMethod()
            {
                Name = $"Create{partTypeName}",
                ReturnType = new CodeTypeReference(),
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            entryMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(partTypeName, methodParamName)
                { Direction = FieldDirection.Ref });

            // Add all of the child part references here
            if (part.Parts != null)
            {
                var rootPartPair = new KeyValuePair<string, Type>(methodParamName, eType);
                foreach (var pair in part.Parts)
                {
                    entryMethod.Statements.AddRange(BuildEntryMethodCodeStatements(
                        pair, opts, partTypeCounts, namespaces, bluePrints, rootPartPair));
                }
            }

            entryMethod.Statements.Add(new CodeMethodInvokeExpression(methodRef, 
                new CodeArgumentReferenceExpression(methodParamName)));

            // Setup the main class next
            var mainClass = new CodeTypeDeclaration($"{eType.Name}BuilderClass")
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };
            mainClass.Members.Add(entryMethod);
            mainClass.Members.AddRange(BuildHelperMethods(bluePrints, opts, namespaces));
            
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
        /// Converts an <see cref="OpenXmlPart"/> into a <see cref="string"/> representation
        /// of dotnet source code that can be compiled to build <paramref name="part"/>.
        /// </summary>
        /// <param name="part">
        /// The <see cref="OpenXmlPart"/> object to generate source code for.
        /// </param>
        /// <param name="provider">
        /// The <see cref="CodeDomProvider"/> object to create the resulting source code.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> representation of the source code generated by <paramref name="provider"/>
        /// that could create <paramref name="part"/> when compiled.
        /// </returns>
        public static string GenerateSourceCode(this OpenXmlPart part, CodeDomProvider provider)
        {
            return part.GenerateSourceCode(NamespaceAliasOptions.Default, provider);
        }

        /// <summary>
        /// Converts an <see cref="OpenXmlPart"/> into a <see cref="string"/> representation
        /// of dotnet source code that can be compiled to build <paramref name="part"/>.
        /// </summary>
        /// <param name="part">
        /// The <see cref="OpenXmlPart"/> object to generate source code for.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to apply to the resulting source code.
        /// </param>
        /// <param name="provider">
        /// The <see cref="CodeDomProvider"/> object to create the resulting source code.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> representation of the source code generated by <paramref name="provider"/>
        /// that could create <paramref name="part"/> when compiled.
        /// </returns>
        public static string GenerateSourceCode(this OpenXmlPart part, NamespaceAliasOptions opts, CodeDomProvider provider)
        {
            var codeString = new System.Text.StringBuilder();
            var code = part.GenerateSourceCode(opts);

            using (var sw = new StringWriter(codeString))
            {
                provider.GenerateCodeFromCompileUnit(code, sw,
                    new CodeGeneratorOptions() { BracingStyle = "C" });
            }
            return codeString.ToString().RemoveOutputHeaders().Trim();
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Creates the appropriate code objects needed to create the entry method for the
        /// current request.
        /// </summary>
        /// <param name="part">
        /// The <see cref="OpenXmlPart"/> object and relationship id to build code for.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to use during the variable naming 
        /// process.
        /// </param>
        /// /// <param name="typeCounts">
        /// A lookup <see cref="IDictionary{TKey, TValue}"/> object containing the
        /// number of times a given type was referenced.  This is used for variable naming
        /// purposes.
        /// </param>
        /// <param name="namespaces">
        /// Collection <see cref="ISet{T}"/> used to keep track of all openxml namespaces
        /// used during the process.
        /// </param>
        /// <param name="blueprints">
        /// The collection of <see cref="OpenXmlPartBluePrint"/> objects that have already been
        /// visited.
        /// </param>
        /// <param name="rootVar">
        /// The root variable name and <see cref="Type"/> to use when building code
        /// statements to create new <see cref="OpenXmlPart"/> objects.
        /// </param>
        /// <returns>
        /// A collection of code statements and expressions that could be used to generate
        /// a new <paramref name="part"/> object from code.
        /// </returns>
        internal static CodeStatementCollection BuildEntryMethodCodeStatements(
            IdPartPair part,
            NamespaceAliasOptions opts,
            IDictionary<string, int> typeCounts,
            ISet<string> namespaces,
            OpenXmlPartBluePrintCollection blueprints,
            KeyValuePair<string, Type> rootVar)
        {
            // Argument validation
            if (part is null) throw new ArgumentNullException(nameof(part));
            if (opts is null) throw new ArgumentNullException(nameof(opts));
            if (blueprints is null) throw new ArgumentNullException(nameof(blueprints));
            if (String.IsNullOrWhiteSpace(rootVar.Key)) throw new ArgumentNullException(nameof(rootVar.Key));

            var result = new CodeStatementCollection();
            var partType = part.OpenXmlPart.GetType();
            var partTypeName = partType.Name;
            var partTypeFullName = partType.FullName;
            string varName = partType.Name.ToCamelCase();
            OpenXmlPartBluePrint bpTemp;
            CodeMethodReferenceExpression referenceExpression = null;
            CodeMethodInvokeExpression invokeExpression = null;
            CodeMethodReferenceExpression methodReference = null;
            bool useAddImgPart = part.OpenXmlPart is ImagePart &&
                rootVar.Value.GetMethods().Count(
                    m => m.Name.Equals("AddImagePart", StringComparison.OrdinalIgnoreCase)) > 0;
            
            // Add blank code line
            void addBlankLine() => result.Add(new CodeSnippetStatement(String.Empty));

            // Make sure that the namespace for the current part is captured
            namespaces.Add(partType.Namespace);

            // Assign the appropriate variable name
            if (typeCounts.ContainsKey(partTypeFullName))
            {
                varName = String.Concat(varName, typeCounts[partTypeFullName]++);
            }
            else
            {
                typeCounts.Add(partTypeFullName, 1);
            }

            // Setup the blueprint
            bpTemp = new OpenXmlPartBluePrint(part.OpenXmlPart, varName);

            // Need to evaluate the current OpenXmlPart type first to make sure the 
            // correct "Add" statement is used as not all Parts can be initialized
            // using the "AddNewPart"method
            
            // Check for image part methods
            if (useAddImgPart)
            {
                referenceExpression = new CodeMethodReferenceExpression(
                    new CodeVariableReferenceExpression(rootVar.Key), "AddImagePart");
            }
            else
            {
                // Setup the add new part statement for the current OpenXmlPart object
                referenceExpression = new CodeMethodReferenceExpression(
                    new CodeVariableReferenceExpression(rootVar.Key), "AddNewPart",
                    new CodeTypeReference(partTypeName));
            }

            // Create the invoke expression 
            invokeExpression = new CodeMethodInvokeExpression(referenceExpression);
            
            // Add content type to invoke method for embeddedpackage and image parts.
            if (part.OpenXmlPart is EmbeddedPackagePart || useAddImgPart)
            {
                invokeExpression.Parameters.Add(
                    new CodePrimitiveExpression(part.OpenXmlPart.ContentType));
            }
            invokeExpression.Parameters.Add(
                new CodePrimitiveExpression(part.RelationshipId));

            result.Add(new CodeVariableDeclarationStatement(partTypeName, varName, invokeExpression));

            // Add the call to the method to populate the current OpenXmlPart object
            methodReference = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), bpTemp.MethodName);
            result.Add(new CodeMethodInvokeExpression(methodReference,
                new CodeDirectionExpression(FieldDirection.Ref, 
                    new CodeVariableReferenceExpression(varName))));

            // Add the appropriate code statements if the current part
            // contains any hyperlink relationships
            if (part.OpenXmlPart.HyperlinkRelationships.Count() > 0)
            {
                // Add a line break first for easier reading
                addBlankLine();
                result.AddRange(
                    part.OpenXmlPart.HyperlinkRelationships.BuildHyperlinkRelationshipStatements(varName));
            }

            // Add the appropriate code statements if the current part
            // contains any non-hyperlink external relationships
            if (part.OpenXmlPart.ExternalRelationships.Count() > 0)
            {
                // Add a line break first for easier reading
                addBlankLine();
                result.AddRange(
                    part.OpenXmlPart.ExternalRelationships.BuildExternalRelationshipStatements(varName));
            }
            
            // put a line break before going through the child parts
            addBlankLine();

            // Add the current blueprint to the collection
            blueprints.Add(bpTemp);

            // Now check to see if there are any child parts for the current OpenXmlPart object.
            if (bpTemp.Part.Parts != null)
            {
                OpenXmlPartBluePrint childBluePrint = null;

                foreach (var p in bpTemp.Part.Parts)
                {
                    // If the current child object has already been created, simply add a reference to
                    // said object using the AddPart method.
                    if (blueprints.Contains(p.OpenXmlPart.Uri))
                    {
                        childBluePrint = blueprints[p.OpenXmlPart.Uri];

                        referenceExpression = new CodeMethodReferenceExpression(
                            new CodeVariableReferenceExpression(varName), "AddPart",
                            new CodeTypeReference(p.OpenXmlPart.GetType().Name));

                        invokeExpression = new CodeMethodInvokeExpression(referenceExpression,
                            new CodeVariableReferenceExpression(childBluePrint.VariableName),
                            new CodePrimitiveExpression(p.RelationshipId));

                        result.Add(invokeExpression);                        
                        continue;
                    }

                    // If this is a new part, call this method with the current part's details
                    result.AddRange(BuildEntryMethodCodeStatements(p, opts, typeCounts, namespaces, blueprints, 
                        new KeyValuePair<string, Type>(varName, partType)));
                }
            }
            
            return result;
        }

        /// <summary>
        /// Creates the appropriate helper methods for all of the <see cref="OpenXmlPart"/> objects 
        /// for the current request.
        /// </summary>
        /// <param name="bluePrints">
        /// The collection of <see cref="OpenXmlPartBluePrint"/> objects that have already been
        /// visited.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to use during the variable naming 
        /// process.
        /// </param>
        /// <param name="namespaces">
        /// <see cref="ISet{T}"/> collection used to keep track of all openxml namespaces
        /// used during the process.
        /// </param>
        /// <returns>
        /// A collection of code helper statements and expressions that could be used to generate a new 
        /// <see cref="OpenXmlPart"/> object from code.
        /// </returns>
        internal static CodeTypeMemberCollection BuildHelperMethods(
            OpenXmlPartBluePrintCollection bluePrints,
            NamespaceAliasOptions opts,
            ISet<string> namespaces)
        {
            if (bluePrints == null) throw new ArgumentNullException(nameof(bluePrints));
            var result = new CodeTypeMemberCollection();
            var localTypeCounts = new Dictionary<Type, int>();
            CodeMemberMethod method = null;
            Type rootElementType = null;

            // Add blank code line
            void addBlankLine() => method.Statements.Add(new CodeSnippetStatement(String.Empty));

            foreach (var bp in bluePrints)
            {
                // Setup the first method
                method = new CodeMemberMethod()
                {
                    Name = bp.MethodName,
                    Attributes = MemberAttributes.Private | MemberAttributes.Final,
                    ReturnType = new CodeTypeReference()
                };
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(bp.Part.GetType().Name, methodParamName)
                    { Direction = FieldDirection.Ref });

                // Code part elements next
                if (bp.Part.RootElement is null)
                {
                    // If the root element is not present (aka: null) then perform a simple feed
                    // dump of the part in the current method
                    const string memName = "mem";
                    const string b64Name = "base64";

                    // Add the necessary namespaces by hand to the namespace set
                    namespaces.Add("System");
                    namespaces.Add("System.IO");

                    using (var partStream = bp.Part.GetStream(FileMode.Open, FileAccess.Read))
                    {
                        using (var mem = new MemoryStream())
                        {
                            partStream.CopyTo(mem);
                            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), b64Name,
                                new CodePrimitiveExpression(Convert.ToBase64String(mem.ToArray()))));
                        }
                    }
                    addBlankLine();

                    var fromBase64 = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("Convert"),
                        "FromBase64String");
                    var invokeFromBase64 = new CodeMethodInvokeExpression(fromBase64, new CodeVariableReferenceExpression("base64"));
                    var createStream = new CodeObjectCreateExpression(new CodeTypeReference("MemoryStream"),
                        invokeFromBase64, new CodePrimitiveExpression(false));
                    var feedData = new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(methodParamName), "FeedData");
                    var invokeFeedData = new CodeMethodInvokeExpression(feedData, new CodeVariableReferenceExpression(memName));
                    var disposeMem = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression(memName), "Dispose");
                    var invokeDisposeMem = new CodeMethodInvokeExpression(disposeMem);

                    // Setup the try statement
                    var tryAndCatch = new CodeTryCatchFinallyStatement();
                    tryAndCatch.TryStatements.Add(invokeFeedData);
                    tryAndCatch.FinallyStatements.Add(invokeDisposeMem);

                    // Put all of the pieces together
                    method.Statements.Add(new CodeVariableDeclarationStatement("Stream", memName, createStream));
                    method.Statements.Add(tryAndCatch);
                }
                else
                {
                    rootElementType = bp.Part.RootElement?.GetType();

                    // Build the element details of the requested part for the current method
                    method.Statements.AddRange(
                        OpenXmlElementExtensions.BuildCodeStatements(bp.Part.RootElement,
                        opts, localTypeCounts, namespaces, out string rootElementVar));

                    // Now finish up the current method by assigning the OpenXmlElement code statements
                    // back to the appropriate property of the part parameter
                    if (rootElementType != null && !String.IsNullOrWhiteSpace(rootElementVar))
                    {
                        foreach (var paramProp in bp.Part.GetType().GetProperties())
                        {
                            if (paramProp.PropertyType == rootElementType)
                            {
                                var varRef = new CodeVariableReferenceExpression(rootElementVar);
                                var paramRef = new CodeArgumentReferenceExpression(methodParamName);
                                var propRef = new CodePropertyReferenceExpression(paramRef, paramProp.Name);
                                method.Statements.Add(new CodeAssignStatement(propRef, varRef));
                                break;
                            }
                        }
                    }
                }
                result.Add(method);
            }

            return result;
        }

        /// <summary>
        /// Creates a collection of code statements that describe how to add external relationships to 
        /// a <see cref="OpenXmlPartContainer"/> object.
        /// </summary>
        /// <param name="relationships">
        /// The collection of <see cref="ExternalRelationship"/> objects to build the code statements for.
        /// </param>
        /// <param name="parentName">
        /// The name of the <see cref="OpenXmlPartContainer"/> object that the external relationship
        /// assignments should be for.
        /// </param>
        /// <returns>
        /// A collection of code statements that could be used to generate and assign new
        /// <see cref="ExternalRelationship"/> objects to a <see cref="OpenXmlPartContainer"/> object.
        /// </returns>
        internal static CodeStatementCollection BuildExternalRelationshipStatements(
            this IEnumerable<ExternalRelationship> relationships, string parentName)
        {
            if (String.IsNullOrWhiteSpace(parentName)) throw new ArgumentNullException(nameof(parentName));

            var result = new CodeStatementCollection();

            // Return an empty code statement collection if the hyperlinks parameter is empty.
            if (relationships.Count() == 0) return result;

            CodeObjectCreateExpression createExpression;
            CodeMethodReferenceExpression methodReferenceExpression;
            CodeMethodInvokeExpression invokeExpression;
            CodePrimitiveExpression param;
            CodeTypeReference typeReference;

            foreach (var ex in relationships)
            {
                // Need special care to create the uri for the current object.
                typeReference = new CodeTypeReference(ex.Uri.GetType());
                param = new CodePrimitiveExpression(ex.Uri.ToString());
                createExpression = new CodeObjectCreateExpression(typeReference, param);

                // Create the AddHyperlinkRelationship statement
                methodReferenceExpression = new CodeMethodReferenceExpression(
                    new CodeVariableReferenceExpression(parentName),
                    "AddExternalRelationship");
                invokeExpression = new CodeMethodInvokeExpression(methodReferenceExpression,
                    createExpression,
                    new CodePrimitiveExpression(ex.IsExternal),
                    new CodePrimitiveExpression(ex.Id));
                result.Add(invokeExpression);
            }
            return result;
        }

        /// <summary>
        /// Creates a collection of code statements that describe how to add hyperlink relationships to 
        /// a <see cref="OpenXmlPartContainer"/> object.
        /// </summary>
        /// <param name="hyperlinks">
        /// The collection of <see cref="HyperlinkRelationship"/> objects to build the code statements for.
        /// </param>
        /// <param name="parentName">
        /// The name of the <see cref="OpenXmlPartContainer"/> object that the hyperlink relationship
        /// assignments should be for.
        /// </param>
        /// <returns>
        /// A collection of code statements that could be used to generate and assign new
        /// <see cref="HyperlinkRelationship"/> objects to a <see cref="OpenXmlPartContainer"/> object.
        /// </returns>
        internal static CodeStatementCollection BuildHyperlinkRelationshipStatements(
            this IEnumerable<HyperlinkRelationship> hyperlinks, string parentName)
        {
            if (String.IsNullOrWhiteSpace(parentName)) throw new ArgumentNullException(nameof(parentName));

            var result = new CodeStatementCollection();

            // Return an empty code statement collection if the hyperlinks parameter is empty.
            if (hyperlinks.Count() == 0) return result;

            CodeObjectCreateExpression createExpression;
            CodeMethodReferenceExpression methodReferenceExpression;
            CodeMethodInvokeExpression invokeExpression;
            CodePrimitiveExpression param;
            CodeTypeReference typeReference;

            foreach (var hl in hyperlinks)
            {
                // Need special care to create the uri for the current object.
                typeReference = new CodeTypeReference(hl.Uri.GetType());
                param = new CodePrimitiveExpression(hl.Uri.ToString());
                createExpression = new CodeObjectCreateExpression(typeReference, param);

                // Create the AddHyperlinkRelationship statement
                methodReferenceExpression = new CodeMethodReferenceExpression(
                    new CodeVariableReferenceExpression(parentName),
                    "AddHyperlinkRelationship");
                invokeExpression = new CodeMethodInvokeExpression(methodReferenceExpression,
                    createExpression,
                    new CodePrimitiveExpression(hl.IsExternal),
                    new CodePrimitiveExpression(hl.Id));
                result.Add(invokeExpression);
            }
            return result;
        }

        #endregion
    }
}