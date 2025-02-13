using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NinahajawhuLairfoheahurcee.Analyzer;

[Generator(LanguageNames.CSharp)]
public class IncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 先注册一个特性给到业务方使用
        context.RegisterPostInitializationOutput(initializationContext =>
        {
            initializationContext.AddSource("FooAttribute.cs",
                """
                namespace Lindexi;

                public class FooAttribute : Attribute
                {
                }
                """);
        });

        IncrementalValueProvider<ImmutableArray<string>> targetClassNameArrayProvider = context.SyntaxProvider.ForAttributeWithMetadataName("Lindexi.FooAttribute",
            // 进一步判断
            (node, _) => node.IsKind(SyntaxKind.ClassDeclaration),
            (syntaxContext, _) => syntaxContext.TargetSymbol.Name)
            .Collect();

        context.RegisterSourceOutput(targetClassNameArrayProvider, (productionContext, classNameArray) =>
        {
            productionContext.AddSource("GeneratedCode.cs",
                $$"""
                using System;
                namespace NinahajawhuLairfoheahurcee
                {
                    public static class GeneratedCode
                    {
                        public static void Print()
                        {
                            Console.WriteLine("标记了 Foo 特性的类型有： {{string.Join(",", classNameArray)}}");
                        }
                    }
                }
                """);
        });
    }
}

