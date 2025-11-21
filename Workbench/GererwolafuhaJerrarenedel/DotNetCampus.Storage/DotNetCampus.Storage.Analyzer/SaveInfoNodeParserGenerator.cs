using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace DotNetCampus.Storage.Analyzer;

[Generator(LanguageNames.CSharp)]
public class SaveInfoNodeParserGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 查找所有带有 SaveInfoContract 特性的类
        var classDeclarationProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSaveInfoCandidate(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // 为什么不用 ForAttributeWithMetadataName 方式，因为考虑到特性的命名空间会有不同，通过名称判断效果更好
        //context.SyntaxProvider.ForAttributeWithMetadataName("")

        classDeclarationProvider = classDeclarationProvider.Combine(context.AnalyzerConfigOptionsProvider)
            .Select((tuple, _) =>
            {
                var classInfo = tuple.Left;
                if (classInfo is null)
                {
                    return null;
                }

                var provider = tuple.Right;

                if (provider.GlobalOptions.TryGetValue("build_property.GenerateSaveInfoNodeParser", out var generateSaveInfoNodeParser)
                    && bool.TryParse(generateSaveInfoNodeParser, out var shouldGenerateSaveInfoNodeParser)
                    && shouldGenerateSaveInfoNodeParser)
                {
                    if (provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
                    {
                        return classInfo with
                        {
                            Namespace = rootNamespace
                        };
                    }

                    return classInfo;
                }
                else
                {
                    return null;
                }
            });

        // 生成代码
        context.RegisterSourceOutput(classDeclarationProvider, GenerateParseCode);

        // 再获取引用程序集的编译信息，以防需要用到
        var referencedAssemblySaveInfoClassInfoProvider = context.CompilationProvider.Select((t, _) =>
        {
            var referencedAssemblyDictionary = new Dictionary<IAssemblySymbol, ReferencedAssemblySymbolInfo>();

            foreach (IAssemblySymbol referencedAssemblySymbol in t.SourceModule.ReferencedAssemblySymbols)
            {
                // 获取引用程序集中的 SaveInfo 类信息
                // 可选先判断程序集是否存在存储库的引用，用来提升性能，减少遍历无效的程序集
                if (!IsSaveInfoAssemblyCandidate(referencedAssemblySymbol, referencedAssemblyDictionary))
                {
                    continue;
                }

                // 此时还不急处理，等待遍历完成，直接处理 Dictionary 好了
            }

            return new object();
        });

        //context.RegisterSourceOutput(compilationAndClasses,
        //    static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsSaveInfoAssemblyCandidate(IAssemblySymbol assemblySymbol, Dictionary<IAssemblySymbol, ReferencedAssemblySymbolInfo> dictionary)
    {
        dictionary.Add(assemblySymbol, new ReferencedAssemblySymbolInfo(assemblySymbol, null));

        bool isCandidate = false;

        foreach (var assemblySymbolModule in assemblySymbol.Modules)
        {
            foreach (var referencedAssemblySymbol in assemblySymbolModule.ReferencedAssemblySymbols)
            {
                if (dictionary.TryGetValue(referencedAssemblySymbol, out var referencedAssemblyInfo))
                {
                    if (referencedAssemblyInfo.IsCandidate is true)
                    {
                        isCandidate = true;
                    }
                    else
                    {
                        // 此程序集已经判断过不是候选项，跳过
                    }
                    continue;
                }

                if (referencedAssemblySymbol.Name == "DotNetCampus.Storage")
                {
                    dictionary[assemblySymbol] = new ReferencedAssemblySymbolInfo(assemblySymbol, true);

                    isCandidate = true;
                }

                if (IsSaveInfoAssemblyCandidate(referencedAssemblySymbol, dictionary))
                {
                    isCandidate = true;
                }
            }
        }

        return isCandidate;
    }

    private static bool IsSaveInfoCandidate(SyntaxNode node)
    {
        // 查找带有特性的类声明
        if (node is ClassDeclarationSyntax classDeclaration
            && classDeclaration.AttributeLists.Count > 0)
        {
            if (classDeclaration.Modifiers.Any(t => t.IsKind(SyntaxKind.AbstractKeyword)))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax) context.Node;
        var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclaration);

        if (symbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        return TryGetSaveInfoClassInfo(classSymbol);
    }

    private static ClassInfo? TryGetSaveInfoClassInfo(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.IsAbstract)
        {
            return null;
        }

        // 检查是否继承自 SaveInfo
        if (!InheritsFromSaveInfo(classSymbol))
        {
            return null;
        }

        // 查找 SaveInfoContract 特性
        var contractAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SaveInfoContractAttribute");

        if (contractAttribute == null)
        {
            return null;
        }

        // 获取特性中的名称参数
        var contractName = contractAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
        if (string.IsNullOrEmpty(contractName))
        {
            return null;
        }

        // 获取所有带有 SaveInfoMember 特性的属性，包括继承的属性
        var properties = new List<PropertyInfo>();
        var currentType = classSymbol;

        // 遍历继承链，收集所有带有 SaveInfoMemberAttribute 特性的属性
        while (currentType != null)
        {
            foreach (var member in currentType.GetMembers().OfType<IPropertySymbol>())
            {
                // 这是当前程序集内的，忽略此情况
                //if (member.DeclaredAccessibility != Accessibility.Public)
                //{
                //    // 非公共属性跳过
                //    continue;
                //}

                var memberAttribute = member.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "SaveInfoMemberAttribute");

                if (memberAttribute == null)
                {
                    continue;
                }

                var memberName = memberAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
                if (string.IsNullOrEmpty(memberName))
                {
                    continue;
                }

                // 避免添加重复的属性（子类可能覆盖基类属性）
                if (properties.All(p => p.PropertyName != member.Name))
                {
                    properties.Add(new PropertyInfo
                    {
                        PropertyName = member.Name,
                        PropertyType = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        StorageName = memberName!,
                        IsNullable = member.Type.NullableAnnotation == NullableAnnotation.Annotated
                    });
                }
            }

            // 移动到基类，但在到达 SaveInfo 时停止（不包括 SaveInfo 本身的属性）
            currentType = currentType.BaseType;
            if (currentType?.Name == "SaveInfo")
            {
                break;
            }
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
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "SaveInfo")
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static void GenerateParseCode(SourceProductionContext sourceProductionContext, ClassInfo? classInfo)
    {
        if (classInfo is null)
        {
            return;
        }

        var source = GenerateNodeParser(classInfo);
        sourceProductionContext.AddSource($"{classInfo.ClassName}NodeParser.g.cs", source);
    }

    private static string GenerateNodeParser(ClassInfo classInfo)
    {
        var parseCoreMethod = GenerateParseCore(classInfo);
        var deparseCoreMethod = GenerateDeparseCore(classInfo);

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

            {{parseCoreMethod}}

            {{deparseCoreMethod}}
            }
            """;
    }

    private static string GenerateParseCore(ClassInfo classInfo)
    {
        var sb = new StringBuilder();

        foreach (var property in classInfo.Properties)
        {
            var propertyVarName = $"propertyNameFor{property.PropertyName}";
            sb.AppendLine($$"""
                            var {{propertyVarName}} = "{{property.StorageName}}";
                            if (currentName.Equals({{propertyVarName}}, StringComparison.Ordinal))
                            {
                                var typeOf{{property.PropertyName}} = typeof({{property.PropertyType}});
                                var nodeParserFor{{property.PropertyName}} = parserManager.GetNodeParser(typeOf{{property.PropertyName}});
                                var valueFor{{property.PropertyName}} = nodeParserFor{{property.PropertyName}}.Parse(storageNode, context);
                                result.{{property.PropertyName}} = ({{property.PropertyType}}) valueFor{{property.PropertyName}};
                                continue;
                            }

            """);
        }

        var propertiesCode = sb.ToString();

        return $$"""
                protected override {{classInfo.ClassFullName}} ParseCore(StorageNode node, in ParseNodeContext context)
                {
                    StorableNodeParserManager parserManager = context.ParserManager;

                    // 决定不支持 init 的情况，这样才能更好地保留默认值
                    var result = new {{classInfo.ClassFullName}}();

                    if (node.Children is { } children)
                    {
                        List<StorageNode>? unknownNodeList = null;
                        foreach (var storageNode in children)
                        {
                            var currentName = storageNode.Name.AsSpan();

            {{propertiesCode}}
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
        var sb = new StringBuilder();

        foreach (var property in classInfo.Properties)
        {
            var propertyVarName = $"propertyNameFor{property.PropertyName}";
            sb.AppendLine($$"""
                    var {{propertyVarName}} = "{{property.StorageName}}";
                    var typeOf{{property.PropertyName}} = typeof({{property.PropertyType}});
                    var nodeParserFor{{property.PropertyName}} = parserManager.GetNodeParser(typeOf{{property.PropertyName}});
                    object? valueFor{{property.PropertyName}} = obj.{{property.PropertyName}};
                    if (valueFor{{property.PropertyName}} is not null)
                    {
                        tempContext = context with
                        {
                            NodeName = {{propertyVarName}}
                        };
                        var childNodeFor{{property.PropertyName}} = nodeParserFor{{property.PropertyName}}.Deparse(valueFor{{property.PropertyName}}, tempContext);
                        storageNode.Children.Add(childNodeFor{{property.PropertyName}});
                    }

            """);
        }

        var propertiesCode = sb.ToString().TrimEnd('\r', '\n');

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
    }

    private readonly record struct ReferencedAssemblySymbolInfo(IAssemblySymbol AssemblySymbol, bool? IsCandidate)
    {
        public override int GetHashCode()
        {
            return AssemblySymbol.GetHashCode();
        }

        public bool Equals(ReferencedAssemblySymbolInfo? other)
        {
            return AssemblySymbol.Equals(other?.AssemblySymbol, SymbolEqualityComparer.Default);
        }
    }
}
