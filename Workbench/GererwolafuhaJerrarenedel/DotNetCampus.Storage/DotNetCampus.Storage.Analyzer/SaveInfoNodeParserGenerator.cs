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
            if (symbol is not { } classSymbol)
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

                    if (storageName is null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(storageName))
                    {
                        continue;
                    }

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

                    var isListType = IsListOfSaveInfo(member.Type);

                    if (properties.All(p => p.PropertyName != member.Name))
                    {
                        properties.Add(new PropertyInfo
                        {
                            PropertyName = member.Name,
                            PropertyType = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            StorageName = storageName,
                            IsNullable = member.Type.NullableAnnotation == NullableAnnotation.Annotated,
                            Aliases = aliases,
                            IsListType = isListType
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

        private static bool IsListOfSaveInfo(ITypeSymbol type)
        {
            // Check if the type is List<T> where T is SaveInfo or inherits from SaveInfo
            if (type is INamedTypeSymbol namedType &&
                namedType.IsGenericType &&
                namedType.ConstructUnboundGenericType().ToDisplayString() == "System.Collections.Generic.List<>")
            {
                var typeArgument = namedType.TypeArguments.FirstOrDefault();
                if (typeArgument != null)
                {
                    return InheritsFromSaveInfo(typeArgument as INamedTypeSymbol) || 
                           typeArgument.Name == "SaveInfo";
                }
            }
            return false;
        }

        private static bool InheritsFromSaveInfo(INamedTypeSymbol? classSymbol)
        {
            if (classSymbol == null) return false;
            
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
                     using System.Linq;
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
            var preDeclarations = string.Join("\n", classInfo.Properties.Select(prop =>
            {
                var aliasesInitialization = prop.Aliases is null
                    ? "null"
                    : "new string[] { " + string.Join(", ", prop.Aliases.Select(a => "\"" + a.Replace("\"", "\\\"") + "\"")) + " }";

                return $$"""
                        bool isNotSet{{prop.PropertyName}} = true;
                        var propertyNameFor{{prop.PropertyName}} = "{{prop.StorageName}}";
                        string[]? aliasesFor{{prop.PropertyName}} = {{aliasesInitialization}};
                        
                        """;
            }));
            preDeclarations = SetIndent(preDeclarations, 3);

            // Generate match logic for each property
            var matchLogic = string.Join("\n", classInfo.Properties.Select(prop =>
            {
                var isNotSet = "isNotSet" + prop.PropertyName;
                var propNameVar = "propertyNameFor" + prop.PropertyName;
                var aliasesVar = "aliasesFor" + prop.PropertyName;

                if (prop.IsListType)
                {
                    // Special handling for List<SaveInfo> properties
                    return $$"""
                           // Parse property {{prop.PropertyName}} (List type)
                           if ({{isNotSet}})
                           {
                               if (currentName.Equals({{propNameVar}}, StringComparison.Ordinal) || IsMatchAliases(currentName, {{aliasesVar}}))
                               {
                                   result.{{prop.PropertyName}} = ParseElementOfList(storageNode.Children, context).OfType<SaveInfo>().ToList();
                                   {{isNotSet}} = false;
                                   continue;
                               }
                           }
                          
                           """;
                }
                else
                {
                    // Regular property handling
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
                }
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
            var propertiesCode = string.Join("\n", classInfo.Properties.Select(prop =>
            {
                var propNameVar = "propertyNameFor" + prop.PropertyName;

                if (prop.IsListType)
                {
                    // Special handling for List<SaveInfo> properties
                    return $$"""
                           // Generate code for property {{prop.PropertyName}} (List type)
                           var {{propNameVar}} = "{{prop.StorageName}}";
                           if (obj.{{prop.PropertyName}} is not null)
                           {
                               tempContext = context with
                               {
                                   NodeName = null
                               };
                               var childNodeFor{{prop.PropertyName}} = new StorageNode()
                               {
                                   Name = {{propNameVar}},
                                   Children = DeparseElementOfList(obj.{{prop.PropertyName}}, tempContext)
                               };
                               storageNode.Children.Add(childNodeFor{{prop.PropertyName}});
                           }
                           
                           """;
                }
                else
                {
                    // Regular property handling
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
                }
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
            public bool IsListType { get; init; }
        }
    }
}
