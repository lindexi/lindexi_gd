using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using NayijainawNerkanekajawi.Analyzer.Properties;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NayijainawNerkanekajawi.Analyzer;

[Generator(LanguageNames.CSharp)]
public class FooIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(initializationContext =>
        {
            initializationContext.AddSource("CollectionAttribute.cs",
                """
                namespace Lindexi;

                internal class CollectionAttribute : Attribute
                {
                }
                """);
        });

        IncrementalValueProvider<ImmutableArray<CollectionExportMethodInfo>> collectionMethodInfoProvider = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                "Lindexi.CollectionAttribute", static (SyntaxNode node, CancellationToken _) =>
                {
                    if (node is MethodDeclarationSyntax methodDeclarationSyntax)
                    {
                        // 判断是否是 partial 分部方法
                        return methodDeclarationSyntax.Modifiers.Any(t => t.IsKind(SyntaxKind.PartialKeyword));
                    }

                    return false;
                },
                (GeneratorAttributeSyntaxContext syntaxContext, CancellationToken _) =>
                {
                    var methodSymbol = (IMethodSymbol) syntaxContext.TargetSymbol;
                    if (!methodSymbol.IsPartialDefinition)
                    {
                        return null;
                    }

                    ITypeSymbol returnType = methodSymbol.ReturnType;
                    // 这是一个泛型类型，我们需要获取泛型参数
                    // 预期是 IEnumerable<Func<IContext, IFoo>> 这样的类型
                    if (returnType is not INamedTypeSymbol methodSymbolReturnType)
                    {
                        return null;
                    }

                    var fullNameDisplayFormat = new SymbolDisplayFormat
                    (
                        // 带上命名空间和类型名
                        SymbolDisplayGlobalNamespaceStyle.Included,
                        // 命名空间之前加上 global 防止冲突
                        SymbolDisplayTypeQualificationStyle
                            .NameAndContainingTypesAndNamespaces
                    );
                    var returnTypeName = methodSymbolReturnType.ToDisplayString(fullNameDisplayFormat);

                    // 预期的返回值类型
                    const string exceptedReturnTypeName = "global::System.Collections.Generic.IEnumerable";

                    if (!string.Equals(returnTypeName, exceptedReturnTypeName, StringComparison.InvariantCulture))
                    {
                        return null;
                    }

                    if (!methodSymbolReturnType.IsGenericType)
                    {
                        // 不是泛型类型，不是我们想要的
                        return null;
                    }

                    if (methodSymbolReturnType.TypeArguments.Length != 1)
                    {
                        // 预期是 IEnumerable<Func> 这样的类型，在 IEnumerable 里面只有一个泛型参数
                        return null;
                    }

                    // 取出 IEnumerable<Func<IContext, IFoo>> 中的 Func<IContext, IFoo> 部分
                    if (methodSymbolReturnType.TypeArguments[0] is not INamedTypeSymbol funcTypeSymbol)
                    {
                        return null;
                    }

                    const string exceptedFuncTypeName = "global::System.Func";
                    var funcTypeName = funcTypeSymbol.ToDisplayString(fullNameDisplayFormat);

                    if (!string.Equals(funcTypeName, exceptedFuncTypeName, StringComparison.InvariantCulture))
                    {
                        // 如果不是 Func 类型的，则不是预期的
                        return null;
                    }

                    // 继续取出 Func<IContext, IFoo> 中的 IContext 和 IFoo 部分
                    if (funcTypeSymbol.TypeArguments.Length != 2)
                    {
                        return null;
                    }

                    // 取出 Func<IContext, IFoo> 中的 IContext 部分
                    ITypeSymbol constructorArgumentType = funcTypeSymbol.TypeArguments[0];
                    string constructorArgumentTypeName = constructorArgumentType.ToDisplayString(fullNameDisplayFormat);
                    // 取出 Func<IContext, IFoo> 中的 IFoo 部分
                    ITypeSymbol collectionType = funcTypeSymbol.TypeArguments[1];
                    var collectionTypeName = collectionType.ToDisplayString(fullNameDisplayFormat);

                    // 生成的代码的示例内容
                    //namespace NayijainawNerkanekajawi;
                    //
                    //public static partial class FooCollection
                    //{
                    //    public static partial IEnumerable<Func<IContext, IFoo>> GetFooCreatorList()
                    //    {
                    //        yield return context => new F1(context);
                    //        yield return context => new F2(context);
                    //        yield return context => new F3(context);
                    //    }
                    //}

                    INamedTypeSymbol containingType = methodSymbol.ContainingType;
                    string classNamespace = containingType.ContainingNamespace.Name;
                    string className = containingType.Name;

                    // Modifiers
                    Accessibility declaredAccessibility = containingType.DeclaredAccessibility;
                    var modifier = AccessibilityToString(declaredAccessibility);

                    // 将 MethodSymbol 转换为生成代码，减少 MethodSymbol 对象的传递。传递的过程由于缓存缘故，需要不断判断。换成生成代码字符串，可以减少一分钱损耗
                    var generatedCode =
                            $$"""
                              namespace {{classNamespace}};

                              {{modifier}}{{(containingType.IsStatic ? " static" : "")}} partial class {{className}}
                              {
                                  {{AccessibilityToString(methodSymbol.DeclaredAccessibility)}}{{(methodSymbol.IsStatic ? " static" : "")}} partial {{exceptedReturnTypeName}}<{{exceptedFuncTypeName}}<{{constructorArgumentTypeName}}, {{collectionTypeName}}>>
                              {{methodSymbol.Name}}()
                                  {
                                      yield return context => new F1(context);
                                  }
                              }
                              """
                        ;
                    // 当前的 generatedCode 变量的字符串内容如下
                    /*
                       namespace NayijainawNerkanekajawi;

                       public static partial class FooCollection
                       {
                           public static partial global::System.Collections.Generic.IEnumerable<global::System.Func<global::NayijainawNerkanekajawi.IContext, global::NayijainawNerkanekajawi.IFoo>>
                       GetFooCreatorList()
                           {
                               yield return context => new F1(context);
                           }
                       }
                     */

                    // 获取代码的位置，用于生成警告和错误。即告诉 Visual Studio 应该在哪里飘红
                    var location = syntaxContext.TargetNode.GetLocation();
                    // 使用 record 类型自带的相等判断，能够省心很多
                    return new CollectionExportMethodInfo(constructorArgumentType, collectionType,
                        new GeneratedCodeInfo(generatedCode, $"{className}.{methodSymbol.Name}"), location);
                })
            // 过滤掉不符合条件的情况
            .Where(t => t != null)
            .Collect()!;

        // 全项目里面的类型
        IncrementalValuesProvider<INamedTypeSymbol> wholeAssemblyClassTypeProvider
            = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (SyntaxNode node, CancellationToken _) => node.IsKind(SyntaxKind.ClassDeclaration),
                    static (GeneratorSyntaxContext generatorSyntaxContext, CancellationToken token) =>
                    {
                        var classDeclarationSyntax = (ClassDeclarationSyntax) generatorSyntaxContext.Node;
                        INamedTypeSymbol? assemblyClassTypeSymbol =
                            generatorSyntaxContext.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, token);

                        if (assemblyClassTypeSymbol is not null && !assemblyClassTypeSymbol.IsAbstract)
                        {
                            return assemblyClassTypeSymbol;
                        }

                        return null;
                    })
                .Where(t => t != null)!;

        IncrementalValuesProvider<ImmutableArray<ItemGeneratedCodeResult>> itemGeneratedCodeResultProvider =
            wholeAssemblyClassTypeProvider
                .Combine(collectionMethodInfoProvider)
                .Select(static ((INamedTypeSymbol Left, ImmutableArray<CollectionExportMethodInfo> Right) tuple,
                    CancellationToken token) =>
                {
                    INamedTypeSymbol assemblyClassTypeSymbol = tuple.Left;
                    var exportMethodReturnTypeCollectionResultArray = tuple.Right;

                    // 慢点创建列表，因为这里是每个类型都会进入一次的，进入次数很多。但大部分类型都不满足条件。因此不提前创建列表能减少很多对象的创建
                    List<ItemGeneratedCodeResult>? result = null;

                    foreach (CollectionExportMethodInfo exportMethodInfo in exportMethodReturnTypeCollectionResultArray)
                    {
                        // 一般进入循环的时候，都会加上这个判断。这个判断逻辑的作用是如开发者在 IDE 里面进行编辑文件的时候，那此文件对应的类型就需要重新处理，即类型对应的 token 将会被激活。此时在循环跑的逻辑就是浪费的，逻辑需要重跑，因此需要判断 token 是否被取消，减少循环里面的不必要的逻辑损耗
                        // check for cancellation so we don't hang the host
                        token.ThrowIfCancellationRequested();

                        // 判断当前的类型是否是我们需要的类型
                        if (!IsInherit(assemblyClassTypeSymbol, exportMethodInfo.CollectionType))
                        {
                            continue;
                        }

                        // 遍历其构造函数，找到感兴趣的构造函数
                        IMethodSymbol? candidateConstructorMethodSymbol = null;
                        foreach (IMethodSymbol constructorMethodSymbol in assemblyClassTypeSymbol.Constructors)
                        {
                            if (constructorMethodSymbol.Parameters.Length != 1)
                            {
                                continue;
                            }

                            // 判断参数的类型是否符合预期
                            IParameterSymbol parameterSymbol = constructorMethodSymbol.Parameters[0];
                            var parameterType = parameterSymbol.Type;

                            if (SymbolEqualityComparer.Default.Equals(parameterType,
                                    exportMethodInfo.ConstructorArgumentType))
                            {
                                candidateConstructorMethodSymbol = constructorMethodSymbol;
                                break;
                            }
                        }

                        result ??= new List<ItemGeneratedCodeResult>();
                        var fullNameDisplayFormat = new SymbolDisplayFormat
                        (
                            // 带上命名空间和类型名
                            SymbolDisplayGlobalNamespaceStyle.Included,
                            // 命名空间之前加上 global 防止冲突
                            SymbolDisplayTypeQualificationStyle
                                .NameAndContainingTypesAndNamespaces
                        );
                        var className = assemblyClassTypeSymbol.ToDisplayString(fullNameDisplayFormat);

                        if (candidateConstructorMethodSymbol is not null)
                        {
                            // context => new F1(context)
                            var generatedCode = $"context => new {className}(context)";

                            result.Add(new ItemGeneratedCodeResult(generatedCode, exportMethodInfo.GeneratedCodeInfo));
                        }
                        else
                        {
                            // 找不到满足条件的构造函数，给出分析警告
                            var diagnosticDescriptor = new DiagnosticDescriptor
                            (
                                id: nameof(Resources.Kaw001),
                                title: Localize(nameof(Resources.Kaw001)),
                                messageFormat: Localize(nameof(Resources.Kaw001_Message)),
                                category: "FooCompiler",
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault: true
                            );
                            // 无法从 {0} 类型中找到构造函数，期望构造函数的只有一个参数，且参数类型为 {1}
                            Diagnostic diagnostic = Diagnostic.Create(diagnosticDescriptor, exportMethodInfo.Location,
                                messageArgs:
                                [
                                    className,
                                    exportMethodInfo.ConstructorArgumentType.ToDisplayString(fullNameDisplayFormat)
                                ]);

                            result.Add(default(ItemGeneratedCodeResult) with
                            {
                                Diagnostic = diagnostic,
                            });
                        }
                    }

                    return result?.ToImmutableArray() ?? ImmutableArray<ItemGeneratedCodeResult>.Empty;
                })
                .Where(t => t != ImmutableArray<ItemGeneratedCodeResult>.Empty);

        // 过滤报告的内容，立刻就可以调用 RegisterSourceOutput 报告
        IncrementalValuesProvider<Diagnostic> diagnosticProvider =
            itemGeneratedCodeResultProvider.SelectMany((array, _) =>
                array.Select(t => t.Diagnostic).Where(t => t is not null))!;
        context.RegisterSourceOutput(diagnosticProvider,
            (SourceProductionContext productionContext, Diagnostic diagnostic) =>
            {
                productionContext.ReportDiagnostic(diagnostic);
            });

        IncrementalValuesProvider<GeneratedCodeInfo> generatedCodeInfoProvider = itemGeneratedCodeResultProvider
            .Collect()
            .SelectMany((ImmutableArray<ImmutableArray<ItemGeneratedCodeResult>> array, CancellationToken token) =>
            {
                IEnumerable<IGrouping<GeneratedCodeInfo, ItemGeneratedCodeResult>> group = array
                    .SelectMany(t => t)
                    // 这条是用来报告的，忽略
                    .Where(t => t.Diagnostic is null)
                    .GroupBy(t => t.ExportMethodGeneratedCodeInfo);

                var generatedCodeList = new List<GeneratedCodeInfo>();
                foreach (var temp in group)
                {
                    // 进行组装生成代码。在 Select 系列方法组装会比在 RegisterSourceOutput 更好，在这里更加方便被打断
                    var stringBuilder = new StringBuilder();
                    foreach (ItemGeneratedCodeResult itemGeneratedCodeResult in temp)
                    {
                        token.ThrowIfCancellationRequested();
                        //         yield return context => new F1(context);
                        stringBuilder.AppendLine($"         yield return {itemGeneratedCodeResult.ItemGeneratedCode};");
                    }

                    // 严谨一些，添加 break 语句。顺带解决收集不到任何一个类型的情况
                    stringBuilder.AppendLine("         yield break;");

                    // 这是用来替换的代码
                    var replacedCode = "        yield return context => new F1(context);";

                    GeneratedCodeInfo generatedCodeInfo = temp.Key;
                    var generatedCode = generatedCodeInfo.GeneratedCode.Replace(replacedCode, stringBuilder.ToString());
                    // 当前的 generatedCode 变量的内容大概如下
                    /*
                       namespace NayijainawNerkanekajawi;

                       public static partial class FooCollection
                       {
                           public static partial global::System.Collections.Generic.IEnumerable<global::System.Func<global::NayijainawNerkanekajawi.IContext, global::NayijainawNerkanekajawi.IFoo>>
                       GetFooCreatorList()
                           {
                                yield return context => new global::NayijainawNerkanekajawi.F1(context);
                                yield return context => new global::NayijainawNerkanekajawi.F2(context);
                                yield return context => new global::NayijainawNerkanekajawi.F3(context);

                           }
                       }
                     */
                    generatedCodeList.Add(new GeneratedCodeInfo(generatedCode, generatedCodeInfo.Name));
                }

                return generatedCodeList;
            });

        context.RegisterImplementationSourceOutput(generatedCodeInfoProvider,
            (SourceProductionContext productionContext, GeneratedCodeInfo generatedCodeInfo) =>
            {
                productionContext.AddSource($"{generatedCodeInfo.Name}.cs", generatedCodeInfo.GeneratedCode);
            });
    }

    private static string AccessibilityToString(Accessibility accessibility)
        => accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Protected => "protected",

            _ => string.Empty,
        };

    /// <summary>
    /// 判断类型继承关系
    /// </summary>
    /// <param name="currentType">当前的类型</param>
    /// <param name="requiredType">需要继承的类型</param>
    /// <returns></returns>
    public static bool IsInherit(ITypeSymbol currentType, ITypeSymbol requiredType)
    {
        var baseType = currentType.BaseType;
        while (baseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, requiredType))
            {
                // 如果基类型是的话
                return true;
            }

            // 否则继续找基类型
            baseType = baseType.BaseType;
        }

        foreach (var currentInheritInterfaceType in currentType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(currentInheritInterfaceType, requiredType))
            {
                // 如果继承的类型是的话
                return true;
            }
        }

        return false;
    }

    public static LocalizableString Localize(string key) =>
        new LocalizableResourceString(key, Resources.ResourceManager, typeof(Resources));
}

record CollectionExportMethodInfo
(
    ITypeSymbol ConstructorArgumentType,
    ITypeSymbol CollectionType,
    GeneratedCodeInfo GeneratedCodeInfo,
    Location Location
);

readonly record struct GeneratedCodeInfo(string GeneratedCode, string Name);

readonly record struct ItemGeneratedCodeResult
(
    string ItemGeneratedCode,
    GeneratedCodeInfo ExportMethodGeneratedCodeInfo
)
{
    // 值类型会让分析器更加开森
    // Use value types where possible: Value types are more amenable to caching and usually have well defined and easy to understand comparison semantics.
    public Diagnostic? Diagnostic { get; init; }
}