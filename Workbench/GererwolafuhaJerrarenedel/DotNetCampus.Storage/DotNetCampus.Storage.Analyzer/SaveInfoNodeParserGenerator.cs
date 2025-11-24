using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetCampus.Storage.Analyzer
{
    [Generator(LanguageNames.CSharp)]
    public class SaveInfoNodeParserGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsSaveInfoCandidate(node),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)
                .Select(static (m, _) => m!);

            context.RegisterSourceOutput(provider, (spc, classInfo) => GenerateParseCode(spc, classInfo));
        }

        private static bool IsSaveInfoCandidate(SyntaxNode node)
        {
            if (node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0)
            {
                if (cds.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclaration)
            {
                return null;
            }

            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
            if (symbol is not INamedTypeSymbol classSymbol)
            {
                return null;
            }

            return TryGetSaveInfoClassInfo(classSymbol);
        }

        private static ClassInfo? TryGetSaveInfoClassInfo(INamedTypeSymbol classSymbol)
        {
            if (classSymbol.IsAbstract)
                return null;

            if (!InheritsFromSaveInfo(classSymbol))
                return null;

            var contractAttribute = classSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "SaveInfoContractAttribute");
            if (contractAttribute == null)
                return null;

            var contractName = contractAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
            if (string.IsNullOrEmpty(contractName))
                return null;

            var properties = new List<PropertyInfo>();
            var current = classSymbol;
            while (current != null)
            {
                foreach (var member in current.GetMembers().OfType<IPropertySymbol>())
                {
                    var memberAttribute = member.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "SaveInfoMemberAttribute");
                    if (memberAttribute == null)
                        continue;

                    var storageName = memberAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
                    if (string.IsNullOrEmpty(storageName))
                        continue;

                    IReadOnlyList<string>? aliases = null;
                    foreach (var named in memberAttribute.NamedArguments)
                    {
                        if (named.Key == "Aliases")
                        {
                            var tc = named.Value;
                            if (tc.Kind == TypedConstantKind.Array && !tc.IsNull)
                            {
                                var items = new List<string>();
                                foreach (var element in tc.Values)
                                {
                                    if (element.Value is string s)
                                        items.Add(s);
                                }

                                if (items.Count > 0)
                                    aliases = items.ToArray();
                            }
                            else if (tc.Value is string single)
                            {
                                aliases = new[] { single };
                            }

                            break;
                        }
                    }

                    if (properties.All(p => p.PropertyName != member.Name))
                    {
                        properties.Add(new PropertyInfo
                        {
                            PropertyName = member.Name,
                            PropertyType = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            StorageName = storageName,
                            IsNullable = member.Type.NullableAnnotation == NullableAnnotation.Annotated,
                            Aliases = aliases
                        });
                    }
                }

                current = current.BaseType;
                if (current?.Name == "SaveInfo")
                    break;
            }

            return new ClassInfo
            {
                ClassName = classSymbol.Name,
                ClassFullName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
                ContractName = contractName!,
                Properties = properties
            };
        }

        private static bool InheritsFromSaveInfo(INamedTypeSymbol classSymbol)
        {
            var bt = classSymbol.BaseType;
            while (bt != null)
            {
                if (bt.Name == "SaveInfo")
                    return true;
                bt = bt.BaseType;
            }
            return false;
        }

        private static void GenerateParseCode(SourceProductionContext spc, ClassInfo? classInfo)
        {
            if (classInfo is null)
                return;

            var source = GenerateNodeParser(classInfo);
            spc.AddSource(classInfo.ClassName + "NodeParser.g.cs", source);
        }

        private static string GenerateNodeParser(ClassInfo classInfo)
        {
            return $$"""
                     // <auto-generated/>
                     #nullable enable
                     using System;
                     using System.Collections.Generic;
                     using DotNetCampus.Storage.Lib.Parsers;
                     using DotNetCampus.Storage.Lib.Parsers.Contexts;
                     using DotNetCampus.Storage.Lib.Parsers.NodeParsers;
                     using DotNetCampus.Storage.Lib.SaveInfos;
                     using DotNetCampus.Storage.Lib.StorageNodes;

                     namespace {{classInfo.Namespace}};

                     internal partial class {{classInfo.ClassName}}NodeParser : SaveInfoNodeParser<{{classInfo.ClassFullName}}>
                     {
                         public override SaveInfoContractAttribute ContractAttribute => _contractAttribute ??= new SaveInfoContractAttribute("{{classInfo.ContractName}}");
                         private SaveInfoContractAttribute? _contractAttribute;

                     {{GenerateParseCore(classInfo)}}

                     {{GenerateDeparseCore(classInfo)}}
                     }
                     """;
        }

        private static string GenerateParseCore(ClassInfo classInfo)
        {
            // Generate variable declarations for each property
            /*
               bool isNotSetFoo1Property = true;
               var propertyNameForFoo1Property = "Foo1Property";
               string[]? aliasesForFoo1Property = null;
             */
            var preDeclarations = string.Join("\n", classInfo.Properties.Select(prop =>
            {
                var aliasesInitialization = prop.Aliases is null
                    ? "null"
                    : "new string[] { " + string.Join(", ", prop.Aliases.Select(a => "\"" + a.Replace("\"", "\\\"") + "\"")) + " }";

                return $"""
                        bool isNotSet{prop.PropertyName} = true;
                        var propertyNameFor{prop.PropertyName} = "{prop.StorageName}";
                        string[]? aliasesFor{prop.PropertyName} = {aliasesInitialization};
                        
                        """;
            }));
            preDeclarations = SetIndent(preDeclarations, 3);

            // Generate match logic for each property
            /*
               if (isNotSetFoo1Property)
               {
                   if (currentName.Equals(propertyNameForFoo1Property, StringComparison.Ordinal) || IsMatchAliases(currentName, aliasesForFoo1Property))
                   {
                       var typeOfFoo1Property = typeof(bool);
                       var nodeParserForFoo1Property = parserManager.GetNodeParser(typeOfFoo1Property);
                       var valueForFoo1Property = nodeParserForFoo1Property.Parse(storageNode, context);
                       result.Foo1Property = (bool) valueForFoo1Property;
                       isNotSetFoo1Property = false;
                       continue;
                   }
               }
             */
            var matchLogic = string.Join("\n", classInfo.Properties.Select(prop =>
            {
                var isNotSet = "isNotSet" + prop.PropertyName;
                var propNameVar = "propertyNameFor" + prop.PropertyName;
                var aliasesVar = "aliasesFor" + prop.PropertyName;

                return $$"""
                       // Parse property {{prop.PropertyName}}
                       if ({{isNotSet}})
                       {
                           if (currentName.Equals({{propNameVar}}, StringComparison.Ordinal) || IsMatchAliases(currentName, {{aliasesVar}}))
                           {
                               var typeOf{{prop.PropertyName}} = typeof({{prop.PropertyType}});
                               var nodeParserFor{{prop.PropertyName}} = parserManager.GetNodeParser(typeOf{{prop.PropertyName}});
                               var valueFor{{prop.PropertyName}} = nodeParserFor{{prop.PropertyName}}.Parse(storageNode, context);
                               result.{{prop.PropertyName}} = ({{prop.PropertyType}}) valueFor{{prop.PropertyName}};
                               {{isNotSet}} = false;
                               continue;
                           }
                       }
                       
                       """;
            }));
            matchLogic = SetIndent(matchLogic, 4);

            return $$"""
                    protected override {{classInfo.ClassFullName}} ParseCore(StorageNode node, in ParseNodeContext context)
                    {
                        StorableNodeParserManager parserManager = context.ParserManager;

                        var result = new {{classInfo.ClassFullName}}();

                        if (node.Children is { } children)
                        {
                {{preDeclarations}}
                            List<StorageNode>? unknownNodeList = null;
                            foreach (var storageNode in children)
                            {
                                var currentName = storageNode.Name.AsSpan();

                {{matchLogic}}
                                unknownNodeList ??= new List<StorageNode>();
                                unknownNodeList.Add(storageNode);
                            }
                            if (unknownNodeList != null)
                            {
                                FillExtensionAndUnknownProperties(unknownNodeList, result, in context);
                            }
                        }

                        return result;
                    }
                """;
        }

        private static string GenerateDeparseCore(ClassInfo classInfo)
        {
            // Generate properties code using raw strings
            /*
               var propertyNameForFoo1Property = "Foo1Property";
               var typeOfFoo1Property = typeof(bool);
               var nodeParserForFoo1Property = parserManager.GetNodeParser(typeOfFoo1Property);
               object? valueForFoo1Property = obj.Foo1Property;
               if (valueForFoo1Property is not null)
               {
                   tempContext = context with
                   {
                       NodeName = propertyNameForFoo1Property
                   }; 
                   var childNodeForFoo1Property = nodeParserForFoo1Property.Deparse(valueForFoo1Property, tempContext);
                   storageNode.Children.Add(childNodeForFoo1Property);
               }
             */
            var propertiesCode = string.Join("\n", classInfo.Properties.Select(prop =>
            {
                var propNameVar = "propertyNameFor" + prop.PropertyName;

                return $$"""
                       // Generate code for property {{prop.PropertyName}}
                       var {{propNameVar}} = "{{prop.StorageName}}";
                       var typeOf{{prop.PropertyName}} = typeof({{prop.PropertyType}});
                       var nodeParserFor{{prop.PropertyName}} = parserManager.GetNodeParser(typeOf{{prop.PropertyName}});
                       object? valueFor{{prop.PropertyName}} = obj.{{prop.PropertyName}};
                       if (valueFor{{prop.PropertyName}} is not null)
                       {
                           tempContext = context with
                           {
                               NodeName = {{propNameVar}}
                           }; 
                           var childNodeFor{{prop.PropertyName}} = nodeParserFor{{prop.PropertyName}}.Deparse(valueFor{{prop.PropertyName}}, tempContext);
                           storageNode.Children.Add(childNodeFor{{prop.PropertyName}});
                       }
                       
                       """;
            }));
            propertiesCode = SetIndent(propertiesCode, 2);

            return $$"""
                    protected override StorageNode DeparseCore({{classInfo.ClassFullName}} obj, in DeparseNodeContext context)
                    {
                        StorableNodeParserManager parserManager = context.ParserManager;

                        var storageNode = new StorageNode();
                        const int saveInfoMemberCount = {{classInfo.Properties.Count}};
                        storageNode.Name = context.NodeName ?? TargetStorageName;
                        storageNode.Children = new List<StorageNode>(saveInfoMemberCount);

                        DeparseNodeContext tempContext;

                {{propertiesCode}}
                        AppendExtensionAndUnknownProperties(storageNode, obj, in context);
                        return storageNode;
                    }
                """;
        }

        private static string SetIndent(string code, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var indentedLines = lines.Select(line => indent + line);
            return string.Join("\r\n", indentedLines);
        }

        private record ClassInfo
        {
            public required string ClassName { get; init; }
            public required string ClassFullName { get; init; }
            public required string Namespace { get; init; }
            public required string ContractName { get; init; }
            public required List<PropertyInfo> Properties { get; init; }
        }

        private record PropertyInfo
        {
            public required string PropertyName { get; init; }
            public required string PropertyType { get; init; }
            public required string StorageName { get; init; }
            public required bool IsNullable { get; init; }
            public IReadOnlyList<string>? Aliases { get; init; }
        }
    }
}
