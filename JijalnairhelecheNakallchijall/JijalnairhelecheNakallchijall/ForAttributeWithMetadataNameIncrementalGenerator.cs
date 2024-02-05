using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JijalnairhelecheNakallchijall;

[Generator(LanguageNames.CSharp)]
public class ForAttributeWithMetadataNameIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName("Lindexi.FooAttribute",
            // 进一步判断
            (node, _) => node.IsKind(SyntaxKind.ClassDeclaration),
            (syntaxContext, _) => syntaxContext.TargetSymbol.Name);

        context.RegisterSourceOutput(provider.Collect(), (productionContext, classNameList) =>
        {
            string helloName = string.Join(", ", classNameList);

            string source =
                $$"""
                  using System;

                  namespace JijalnairhelecheNakallchijall
                  {
                      public static partial class Program
                      {
                          public static void Hello()
                          {
                              Console.WriteLine($"Says: Hi from {{helloName}}");
                          }
                      }
                  }
                  """;
            productionContext.AddSource("HelloWorld.cs", source);
        });
    }
}

