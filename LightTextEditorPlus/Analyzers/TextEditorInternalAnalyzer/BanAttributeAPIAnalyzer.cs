using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace TextEditorInternalAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BanAttributeAPIAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(analysisContext =>
        {
            if (analysisContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("BanAttributeAPI",out var value))
            {
                
            }
        });
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
}
