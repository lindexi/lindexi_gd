using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KonanohallreGonurliyage;

[Generator(LanguageNames.CSharp)]
public class IncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<GeneratorSyntaxContext> provider = context.SyntaxProvider.CreateSyntaxProvider((node, _) =>
        {
            return node.IsKind(SyntaxKind.ClassDeclaration);
        }, (syntaxContext, token) =>
        {
            return syntaxContext;
        });

        var pathProvider = provider.Combine(context.CompilationProvider).Select((tuple, _) =>
        {
            var (syntaxContext, compilation) = tuple;

            var path = compilation.Options.SourceReferenceResolver?.NormalizePath(syntaxContext.Node.SyntaxTree.FilePath,
                baseFilePath: null);
            // 找不到返回 path 为 null 的值
            return path;
        });

        context.RegisterSourceOutput(pathProvider, (productionContext, path) =>
        {
            
        });
    }
}

