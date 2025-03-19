// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Configuration;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

var code = @"C:\lindexi\Work\Source\";
var slnFile = Directory.EnumerateFiles(code, "*.sln").First();

MSBuildWorkspace msBuildWorkspace = MSBuildWorkspace.Create();
Solution solution = await msBuildWorkspace.OpenSolutionAsync(slnFile,new Progress<ProjectLoadProgress>(progress =>
{
    Console.WriteLine($"打开进度：[{progress.Operation}] TargetFramework={progress.TargetFramework};FilePath={progress.FilePath};ElapsedTime={progress.ElapsedTime.TotalMilliseconds}ms");
}));

Console.WriteLine($"打开项目完成");

foreach (Project solutionProject in solution.Projects)
{
    Compilation? compilation = await solutionProject.GetCompilationAsync();
    if (compilation == null)
    {
        continue;
    }

    foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
    {
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

        if (!Path.GetFileName(syntaxTree.FilePath).Contains("Attribute"))
        {
            continue;
        }

        if (syntaxTree.TryGetRoot(out SyntaxNode? root))
        {
            if (root.Span.Length<390)
            {
                continue;
            }

            var modelCollector = new ModelCollector();
            modelCollector.Visit(root);
            SyntaxNode syntaxNode = root.FindNode(new TextSpan(390,1));

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
            {
                var namedTypeSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                if (namedTypeSymbol != null)
                {
                    var referencedSymbolList = await SymbolFinder.FindReferencesAsync(namedTypeSymbol,solution);

                    foreach (var referencedSymbol in referencedSymbolList)
                    {
                        
                    }
                }

            }
            
        }
        ImmutableArray<ISymbol> symbolArray = semanticModel.LookupSymbols(390);

        //SymbolFinder.FindReferencesAsync()
    }
}

Thread.Sleep(Timeout.Infinite);

class ModelCollector : CSharpSyntaxWalker
{

}