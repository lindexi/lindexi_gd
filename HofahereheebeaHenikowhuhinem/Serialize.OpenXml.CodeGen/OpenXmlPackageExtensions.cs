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
    /// Static class that converts <see cref="OpenXmlPackage">packages</see>
    /// into Code DOM objects.
    /// </summary>
    public static class OpenXmlPackageExtensions
    {
        #region Public Static Methods

        /// <summary>
        /// Converts an <see cref="OpenXmlPackage"/> into a CodeDom object that can be used
        /// to build code in a given .NET language to build the referenced <paramref name="pkg"/>.
        /// </summary>
        /// <param name="pkg">
        /// The <see cref="OpenXmlPackage"/> object to generate source code for.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeCompileUnit"/> containing the instructions to build
        /// the referenced <see cref="OpenXmlPackage"/>.
        /// </returns>
        public static CodeCompileUnit GenerateSourceCode(this OpenXmlPackage pkg)
        {
            return pkg.GenerateSourceCode(NamespaceAliasOptions.Default);
        }

        /// <summary>
        /// Converts an <see cref="OpenXmlPackage"/> into a CodeDom object that can be used
        /// to build code in a given .NET language to build the referenced <paramref name="pkg"/>.
        /// </summary>
        /// <param name="pkg">
        /// The <see cref="OpenXmlPackage"/> object to generate source code for.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to apply to the resulting source code.
        /// </param>
        /// <returns>
        /// A new <see cref="CodeCompileUnit"/> containing the instructions to build
        /// the referenced <see cref="OpenXmlPackage"/>.
        /// </returns>
        public static CodeCompileUnit GenerateSourceCode(this OpenXmlPackage pkg, NamespaceAliasOptions opts)
        {
            const string pkgVarName = "pkg";
            const string paramName = "stream";
            var result = new CodeCompileUnit();
            var pkgType = pkg.GetType();
            var pkgTypeName = pkgType.Name;
            var partTypeCounts = new Dictionary<string, int>();
            var namespaces = new SortedSet<string>();
            var mainNamespace = new CodeNamespace("OpenXmlSample");
            var bluePrints = new OpenXmlPartBluePrintCollection();
            CodeConditionStatement conditionStatement;
            CodeMemberMethod entryPoint;
            CodeMemberMethod createParts;
            CodeTypeDeclaration mainClass;
            CodeTryCatchFinallyStatement tryAndCatch;
            CodeFieldReferenceExpression docTypeVarRef = null;
            Type docTypeEnum = null;
            string docTypeEnumVal = null;
            KeyValuePair<string, Type> rootVarType;
            
            // Add all initial namespace names first
            namespaces.Add("System.IO");

            // The OpenXmlDocument derived parts all contain a property called "DocumentType"
            // but the property types differ depending on the derived part.  Need to get both
            // the enum name of selected value to use as a parameter for the Create statement
            if (pkg is PresentationDocument)
            {
                var docType = ((PresentationDocument)pkg).DocumentType;
                docTypeEnumVal = docType.ToString();
                docTypeEnum = docType.GetType();
            }
            else if (pkg is SpreadsheetDocument)
            {
                var docType = ((SpreadsheetDocument)pkg).DocumentType;
                docTypeEnumVal = docType.ToString();
                docTypeEnum = docType.GetType();
            }
            else if (pkg is WordprocessingDocument)
            {
                var docType = ((WordprocessingDocument)pkg).DocumentType;
                docTypeEnumVal = docType.ToString();
                docTypeEnum = docType.GetType();
            }

            // Create the entry method
            entryPoint = new CodeMemberMethod()
            {
                Name = "CreatePackage",
                ReturnType = new CodeTypeReference(),
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            entryPoint.Parameters.Add(
                new CodeParameterDeclarationExpression(typeof(Stream).Name, paramName) 
                { Direction = FieldDirection.Ref });
            
            // Create package declaration expression first
            entryPoint.Statements.Add(new CodeVariableDeclarationStatement(pkgTypeName, pkgVarName, 
                new CodePrimitiveExpression(null)));
            
            // Add the required DocumentType parameter here, if available
            if (docTypeEnum != null)
            {
                namespaces.Add(docTypeEnum.Namespace);

                var simpleFieldRef = new CodeVariableReferenceExpression(docTypeEnum.GetObjectTypeName(opts.Order));
                docTypeVarRef = new CodeFieldReferenceExpression(simpleFieldRef, docTypeEnumVal);
            }
            
            // initialize package var
            var pkgCreateMethod = new CodeMethodReferenceExpression(
                new CodeTypeReferenceExpression(pkgTypeName), 
                "Create");
            var pkgCreateInvoke = new CodeMethodInvokeExpression(pkgCreateMethod, 
                new CodeArgumentReferenceExpression(paramName),
                docTypeVarRef);
            var initializePkg = new CodeAssignStatement(
                new CodeVariableReferenceExpression(pkgVarName),
                pkgCreateInvoke);

            // Call CreateParts method
            var partsCreateMethod = new CodeMethodReferenceExpression(
                new CodeThisReferenceExpression(),
                "CreateParts");
            var partsCreateInvoke = new CodeMethodInvokeExpression(
                partsCreateMethod,
                new CodeDirectionExpression(FieldDirection.Ref,
                    new CodeVariableReferenceExpression(pkgVarName)));

            // Clean up pkg var
            var pkgDisposeMethod = new CodeMethodReferenceExpression(
                new CodeVariableReferenceExpression(pkgVarName),
                "Dispose");
            var pkgDisposeInvoke = new CodeMethodInvokeExpression(
                pkgDisposeMethod);

            // Setup the try/catch statement to properly initialize the pkg var
            tryAndCatch = new CodeTryCatchFinallyStatement();

            // Try statements
            tryAndCatch.TryStatements.Add(initializePkg);
            tryAndCatch.TryStatements.Add(new CodeSnippetStatement(String.Empty));
            tryAndCatch.TryStatements.Add(partsCreateInvoke);

            // If statement to ensure pkgVarName is not null before trying to dispose
            conditionStatement = new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression(pkgVarName),
                    CodeBinaryOperatorType.IdentityInequality,
                    new CodePrimitiveExpression(null)));

            conditionStatement.TrueStatements.Add(pkgDisposeInvoke);

            // Finally statements
            tryAndCatch.FinallyStatements.Add(conditionStatement);
            entryPoint.Statements.Add(tryAndCatch);

            // Create the CreateParts method
            createParts = new CodeMemberMethod()
            {
                Name = "CreateParts",
                ReturnType = new CodeTypeReference(),
                Attributes = MemberAttributes.Private | MemberAttributes.Final
            };
            createParts.Parameters.Add(new CodeParameterDeclarationExpression(pkgTypeName, pkgVarName) 
                { Direction = FieldDirection.Ref });

            // Add all of the child part references here
            if (pkg.Parts != null)
            {
                var customNewPartTypes = new Type[] { typeof(PresentationPart), typeof(WorkbookPart), typeof(MainDocumentPart) };
                OpenXmlPartBluePrint bpTemp = null;
                CodeMethodReferenceExpression referenceExpression = null;
                CodeMethodInvokeExpression invokeExpression = null;
                CodeMethodReferenceExpression methodReference = null;
                Type currentPartType = null;
                string varName = null;
                string initMethodName = null;
                string mainPartTypeName = null;

                foreach (var pair in pkg.Parts)
                {
                    // Need special handling rules for WorkbookPart, MainDocumentPart, and PresentationPart
                    // objects.  They cannot be created using the usual "AddNewPart" methods, unfortunately.
                    currentPartType = pair.OpenXmlPart.GetType();
                    if (customNewPartTypes.Contains(currentPartType))
                    {
                        namespaces.Add(currentPartType.Namespace);
                        mainPartTypeName = currentPartType.Name;
                        if (pair.OpenXmlPart is PresentationPart)
                        {
                            varName = "presentationPart";
                            initMethodName = "AddPresentationPart";
                        }
                        else if (pair.OpenXmlPart is WorkbookPart)
                        {
                            varName = "workbookPart";
                            initMethodName = "AddWorkbookPart";
                        }
                        else if (pair.OpenXmlPart is MainDocumentPart)
                        {
                            varName = "mainDocumentPart";
                            initMethodName = "AddMainDocumentPart";
                        }
                        rootVarType = new KeyValuePair<string, Type>(varName, currentPartType);

                        // Setup the blueprint
                        bpTemp = new OpenXmlPartBluePrint(pair.OpenXmlPart, varName);

                        // Setup the add new part statement for the current OpenXmlPart object
                        referenceExpression = new CodeMethodReferenceExpression(
                            new CodeArgumentReferenceExpression(pkgVarName), initMethodName);

                        invokeExpression = new CodeMethodInvokeExpression(referenceExpression);

                        createParts.Statements.Add(new CodeVariableDeclarationStatement(mainPartTypeName, varName, invokeExpression));

                        // Add the call to the method to populate the current OpenXmlPart object
                        methodReference = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), bpTemp.MethodName);
                        createParts.Statements.Add(new CodeMethodInvokeExpression(methodReference,
                            new CodeDirectionExpression(FieldDirection.Ref, new CodeVariableReferenceExpression(varName))));

                        // Add the current main part to the collection of blueprints to ensure that the appropriate 'Generate*'
                        // method is added to the collection of helper methods.
                        bluePrints.Add(bpTemp);

                        // Add a blank line for clarity
                        createParts.Statements.Add(new CodeSnippetStatement(String.Empty));

                        // now create the child parts for the current one an continue the loop to avoid creating
                        // an additional invalid 'AddNewPart' method for the current main part.
                        foreach (var child in pair.OpenXmlPart.Parts)
                        {
                            createParts.Statements.AddRange(
                                OpenXmlPartExtensions.BuildEntryMethodCodeStatements(
                                    child, opts, partTypeCounts, namespaces, bluePrints, rootVarType));
                        }
                        continue;
                    }

                    rootVarType = new KeyValuePair<string, Type>(pkgVarName, pkgType);
                    createParts.Statements.AddRange(
                        OpenXmlPartExtensions.BuildEntryMethodCodeStatements(
                            pair, opts, partTypeCounts, namespaces, bluePrints, rootVarType));
                }
            }

            // Setup the main class next
            mainClass = new CodeTypeDeclaration($"{pkgTypeName}BuilderClass")
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };

            // Setup the main class members
            mainClass.Members.Add(entryPoint);
            mainClass.Members.Add(createParts);
            mainClass.Members.AddRange(OpenXmlPartExtensions.BuildHelperMethods
                (bluePrints, opts, namespaces));

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
        /// Converts an <see cref="OpenXmlPackage"/> into a <see cref="string"/> representation
        /// of dotnet source code that can be compiled to build <paramref name="pkg"/>.
        /// </summary>
        /// <param name="pkg">
        /// The <see cref="OpenXmlPackage"/> object to generate source code for.
        /// </param>
        /// <param name="provider">
        /// The <see cref="CodeDomProvider"/> object to create the resulting source code.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> representation of the source code generated by <paramref name="provider"/>
        /// that could create <paramref name="pkg"/> when compiled.
        /// </returns>
        public static string GenerateSourceCode(this OpenXmlPackage pkg, CodeDomProvider provider)
        {
            return pkg.GenerateSourceCode(NamespaceAliasOptions.Default, provider);
        }

        /// <summary>
        /// Converts an <see cref="OpenXmlPackage"/> into a <see cref="string"/> representation
        /// of dotnet source code that can be compiled to build <paramref name="pkg"/>.
        /// </summary>
        /// <param name="pkg">
        /// The <see cref="OpenXmlPackage"/> object to generate source code for.
        /// </param>
        /// <param name="opts">
        /// The <see cref="NamespaceAliasOptions"/> to apply to the resulting source code.
        /// </param>
        /// <param name="provider">
        /// The <see cref="CodeDomProvider"/> object to create the resulting source code.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> representation of the source code generated by <paramref name="provider"/>
        /// that could create <paramref name="pkg"/> when compiled.
        /// </returns>
        public static string GenerateSourceCode(this OpenXmlPackage pkg, NamespaceAliasOptions opts, CodeDomProvider provider)
        {
            var codeString = new System.Text.StringBuilder();
            var code = pkg.GenerateSourceCode(opts);

            using (var sw = new StringWriter(codeString))
            {
                provider.GenerateCodeFromCompileUnit(code, sw,
                    new CodeGeneratorOptions() { BracingStyle = "C" });
            }
            return codeString.ToString().RemoveOutputHeaders().Trim();
        }

        #endregion
    }
}