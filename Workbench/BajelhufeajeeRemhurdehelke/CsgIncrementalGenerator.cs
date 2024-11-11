using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;

namespace BajelhufeajeeRemhurdehelke;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class DoNotUseEndOfStreamInAsyncMethodsAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var streamReaderType = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIOStreamReader);

        if (streamReaderType is null)
        {
            return;
        }

        var endOfStreamProperty = streamReaderType.GetMembers(EndOfStream)
            .OfType<IPropertySymbol>()
            .FirstOrDefault();

        if (endOfStreamProperty is null)
        {
            return;
        }

        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);

        void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            var operation = (IPropertyReferenceOperation) context.Operation;

            if (!SymbolEqualityComparer.Default.Equals(endOfStreamProperty, operation.Member))
            {
                return;
            }

            var containingSymbol = operation.TryGetContainingAnonymousFunctionOrLocalFunction() ?? context.ContainingSymbol;

            if (containingSymbol is IMethodSymbol containingMethod && containingMethod.IsAsync)
            {
                context.ReportDiagnostic(operation.CreateDiagnostic(Rule, operation.Syntax.ToString()));
            }
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    private const string RuleId = "CA2024";
    private const string EndOfStream = nameof(EndOfStream);

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId, EndOfStream, EndOfStream,
        "Warning", DiagnosticSeverity.Warning, true);

}
