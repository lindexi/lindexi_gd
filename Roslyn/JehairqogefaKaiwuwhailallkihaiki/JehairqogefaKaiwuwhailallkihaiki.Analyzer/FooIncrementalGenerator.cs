using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JehairqogefaKaiwuwhailallkihaiki.Analyzer;

[Generator(LanguageNames.CSharp)]
public class FooIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

        IncrementalValueProvider<ImmutableArray<string>> targetClassNameArrayProvider = context.SyntaxProvider
            .CreateSyntaxProvider((node, _) =>
            {
                if (node is not ClassDeclarationSyntax classDeclarationSyntax)
                {
                    return false;
                }

                // 为什么这里是 Attribute List 的集合？原因是可以写出这样的语法
                // ```csharp
                // [A1Attribute, A2Attribute]
                // [A3Attribute]
                // private void Foo()
                // {
                // }
                // ```
                foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        NameSyntax name = attributeSyntax.Name;
                        string nameText = name.ToFullString();
                        if (nameText == "Foo")
                        {
                            return true;
                        }

                        if (nameText == "FooAttribute")
                        {
                            return true;
                        }

                        // 可能还有 global::Lindexi.FooAttribute 的情况
                        if (nameText.EndsWith("Lindexi.FooAttribute"))
                        {
                            return true;
                        }

                        if (nameText.EndsWith("Lindexi.Foo"))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }, (syntaxContext, _) =>
            {
                ISymbol declaredSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);
                if (declaredSymbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    return (string) null;
                }

                ImmutableArray<AttributeData> attributeDataArray = namedTypeSymbol.GetAttributes();

                // 在通过语义判断一次，防止被骗了
                if (!attributeDataArray.Any(t =>
                        t.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                        "global::Lindexi.FooAttribute"))
                {
                    return (string) null;
                }

                return namedTypeSymbol.Name;
            }).Collect();

        context.RegisterSourceOutput(targetClassNameArrayProvider, (productionContext, classNameArray) =>
        {
            productionContext.AddSource("GeneratedCode.cs",
                $$"""
                  using System;
                  namespace JehairqogefaKaiwuwhailallkihaiki
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