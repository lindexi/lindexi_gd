using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WejeajaihurleyereRearchicobairnurjeyu.Analyzer;

[Generator(LanguageNames.CSharp)]
public class FooIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var incrementalValuesProvider = context.SyntaxProvider.CreateSyntaxProvider((node, _) => node.IsKind(SyntaxKind.ClassDeclaration),
            (syntaxContext, _) =>
            {
                var declaredSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);
                if (declaredSymbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    return null;
                }

                if (namedTypeSymbol.Name == "F1")
                {
                    var typeName = namedTypeSymbol.Name;
                    var allInterfaces = string.Join(";", namedTypeSymbol.AllInterfaces.Select(t => t.Name));

                    return $$"""
                           namespace WejeajaihurleyereRearchicobairnurjeyu;

                           public static class NamedTypeSymbolHelper
                           {
                               public static void OutputAllInterfaces()
                               {
                                   Console.WriteLine($"AllInterfaces of '{{typeName}}' is {{allInterfaces}}");
                               }
                           }
                           """;
                }

                return null;
            })
            .Where(t => t != null)
            .Select((t, _) => t!);

        context.RegisterSourceOutput(incrementalValuesProvider,
            (SourceProductionContext productionContext, string generatedCode) =>
            {
                productionContext.AddSource("GeneratedCode.cs", generatedCode);
            });
    }
}

