using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TextEditorInternalAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BanAttributeAPIAnalyzer : DiagnosticAnalyzer
{
    public BanAttributeAPIAnalyzer()
    {
        SupportedDiagnostics = new []
        {
            new DiagnosticDescriptor("Ban1", "CallBanAPI", "不能调用禁用的 API 哦，方法 {0} 被标记 {1} 不可在本项目使用", "Error", DiagnosticSeverity.Error, true)
        }.ToImmutableArray();
    }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(analysisContext =>
        {
            if (analysisContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.BanAttributeAPI", out var banAttributeName))
            {
                analysisContext.RegisterSyntaxNodeAction(nodeAnalysisContext =>
                {
                    var invocationExpression = (InvocationExpressionSyntax) nodeAnalysisContext.Node;
                    if (nodeAnalysisContext.SemanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol is not { } methodSymbol)
                    {
                        return;
                    }

                    if (methodSymbol.GetAttributes().Any(t=>t.AttributeClass?.Name== banAttributeName))
                    {
                        nodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0], Location.Create(nodeAnalysisContext.Node.SyntaxTree, nodeAnalysisContext.Node.FullSpan), methodSymbol.Name, banAttributeName));
                    }

                }, SyntaxKind.InvocationExpression);
            }
        });
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
}
