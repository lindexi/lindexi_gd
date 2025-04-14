using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using WejeajaihurleyereRearchicobairnurjeyu.Analyzer.Properties;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                    foreach (var allInterface in namedTypeSymbol.AllInterfaces)
                    {
                        
                    }
                }

                return "";
            })
            .Where(t => t != null)
            .Select((t, _) => t!);

        context.RegisterImplementationSourceOutput(incrementalValuesProvider,
            (SourceProductionContext productionContext, string generatedCode) =>
            {

            });
    }
}
