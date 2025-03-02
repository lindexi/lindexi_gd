using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NelbecarballReanallyerhohe.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BanAPIAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(analysisContext =>
        {
            if (analysisContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                    "build_property.BanAPIFileName", out var fileName))
            {
                var file = analysisContext.Options.AdditionalFiles.FirstOrDefault(t =>
                    Path.GetFileName(t.Path) == fileName);
                if (file != null)
                {
                    ImmutableHashSet<string> banSet =
                        file.GetText()?.Lines.Select(t => t.ToString()).ToImmutableHashSet() ??
                        ImmutableHashSet<string>.Empty;

                    analysisContext.RegisterSyntaxNodeAction(nodeAnalysisContext =>
                    {
                        var invocationExpression = (InvocationExpressionSyntax) nodeAnalysisContext.Node;
                        var symbolInfo = nodeAnalysisContext.SemanticModel.GetSymbolInfo(invocationExpression);
                        if (symbolInfo.Symbol is not IMethodSymbol symbol)
                        {
                            return;
                        }

                        var containingType = symbol.ContainingType;

                        var symbolDisplayFormat = new SymbolDisplayFormat
                        (
                            // 带上命名空间和类型名
                            SymbolDisplayGlobalNamespaceStyle.Omitted,
                            // 命名空间之前加上 global 防止冲突
                            SymbolDisplayTypeQualificationStyle
                                .NameAndContainingTypesAndNamespaces
                        );

                        var containingTypeName = containingType.ToDisplayString(symbolDisplayFormat);
                        var name = symbol.Name;
                        var methodName = $"{containingTypeName}.{name}";

                        if (banSet.Contains(methodName))
                        {
                            var location = Location.Create(nodeAnalysisContext.Node.SyntaxTree, nodeAnalysisContext.Node.FullSpan);

                            nodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0],
                                location,
                                messageArgs: new object[] { symbol.Name, fileName }));
                        }
                    }, SyntaxKind.InvocationExpression);
                }
            }
        });
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = new[]
    {
        new DiagnosticDescriptor("Ban01", "CallBanAPI", "不能调用禁用的 API 哦，{0} 被 {1} 标记禁用", "Error",
            DiagnosticSeverity.Error, true)
    }.ToImmutableArray();
}