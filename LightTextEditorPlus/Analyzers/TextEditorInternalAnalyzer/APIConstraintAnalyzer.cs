using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace TextEditorInternalAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class APIConstraintAnalyzer : DiagnosticAnalyzer
{
    public APIConstraintAnalyzer()
    {
        _diagnosticDescriptor01 = new DiagnosticDescriptor("APIConstraint01", "API约束不满足，成员丢失", "此类型不实现标记的 {0} 里的 {1} 成员", "Error", DiagnosticSeverity.Error, true);

        _diagnosticDescriptor02 = new DiagnosticDescriptor("APIConstraint02", "API约束不满足，成员定义顺序错误", "此类型实现标记的 {0} 的成员 {1} 顺序错误，正确顺序应该是 {2} ，实际顺序是 {3}", "Error", DiagnosticSeverity.Error, true);

        _diagnosticDescriptor03 = new DiagnosticDescriptor("APIConstraint03", "未写明约束文件", "没有在特性上标记约束文件名。当前可用约束文件列表： {0}", "Error", DiagnosticSeverity.Error, true);

        _diagnosticDescriptor04 = new DiagnosticDescriptor("APIConstraint03", "标记的约束文件找不到", "在特性上标记约束文件名 {0} 无法找到。当前可用约束文件列表： {1}", "Error", DiagnosticSeverity.Error, true);

        SupportedDiagnostics = new[]
        {
            _diagnosticDescriptor01,
            _diagnosticDescriptor02,
            _diagnosticDescriptor03,
            _diagnosticDescriptor04,
        }.ToImmutableArray();
    }

    private readonly DiagnosticDescriptor _diagnosticDescriptor01;
    private readonly DiagnosticDescriptor _diagnosticDescriptor02;
    private readonly DiagnosticDescriptor _diagnosticDescriptor03;
    private readonly DiagnosticDescriptor _diagnosticDescriptor04;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(analysisContext =>
        {
            var analysisContextNode = (TypeDeclarationSyntax) analysisContext.Node;

            string attributeName = "global::LightTextEditorPlus.APIConstraintAttribute";
            if (!ContainAttribute(analysisContextNode, attributeName))
            {
                // 先走一次语法，速度快
                return;
            }

            //SymbolInfo symbolInfo = analysisContext.SemanticModel.GetSymbolInfo(analysisContextNode);
            INamedTypeSymbol? namedTypeSymbol = analysisContext.SemanticModel.GetDeclaredSymbol(analysisContextNode);
            if (namedTypeSymbol == null)
            {
                return;
            }

            if (!TryGetAttribute(namedTypeSymbol, attributeName, out var constraintAttribute))
            {
                // 再走一次语义，确定是否有特性
                return;
            }

            var constraintFileName = constraintAttribute.ConstructorArguments[0].Value?.ToString();

            var constraintAdditionalTextList = new List<AdditionalText>();

            ImmutableArray<AdditionalText> additionalFileArray = analysisContext.Options.AdditionalFiles;

            foreach (AdditionalText additionalFile in additionalFileArray)
            {
                AnalyzerConfigOptions analyzerConfigOptions = analysisContext.Options.AnalyzerConfigOptionsProvider.GetOptions(additionalFile);
                if (analyzerConfigOptions.TryGetValue("build_metadata.AdditionalFiles.APIConstraint", out var isAPIConstraintFile))
                {
                    if (string.Equals(isAPIConstraintFile, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                    {
                        constraintAdditionalTextList.Add(additionalFile);
                    }
                }
            }

            FrozenDictionary<string, AdditionalText> dictionary = constraintAdditionalTextList.ToFrozenDictionary(t => Path.GetFileName(t.Path));

            if (string.IsNullOrEmpty(constraintFileName))
            {
                Location location = Location.Create(analysisContextNode.SyntaxTree, analysisContextNode.AttributeLists.FullSpan);
                Diagnostic diagnostic = Diagnostic.Create(_diagnosticDescriptor03, location,
                    string.Join("\n", dictionary.Select(t => t.Key)));
                analysisContext.ReportDiagnostic(diagnostic);
                return;
            }

            Debug.Assert(constraintFileName != null, nameof(constraintFileName) + " != null");
            if (dictionary.TryGetValue(constraintFileName!, out var additionalText))
            {
                List<MemberConstraint> memberConstraintList = ReadMemberConstraintList(additionalText);
                ImmutableArray<ISymbol> memberArray = namedTypeSymbol.GetMembers();

                var memberConstraintIndex = 0;
                var memberIndex = 0;

                for (; memberConstraintIndex < memberConstraintList.Count; memberConstraintIndex++)
                {
                    MemberConstraint memberConstraint = memberConstraintList[memberConstraintIndex];

                    bool canFind = false;

                    for (; memberIndex < memberArray.Length && !canFind; memberIndex++)
                    {
                        ISymbol member = memberArray[memberIndex];
                        if (IsMatch(member, memberConstraint))
                        {
                            canFind = true;
                        }
                    }

                    if (!canFind)
                    {
                        // 找不到可能是顺序错了
                        var isOrderError = memberArray.Any(t => IsMatch(t, memberConstraint));

                        Location location = Location.Create(analysisContextNode.SyntaxTree, analysisContextNode.FullSpan);

                        Diagnostic diagnostic;
                        if (isOrderError)
                        {
                            diagnostic = Diagnostic.Create(_diagnosticDescriptor02, location, constraintFileName, memberConstraint.Name, string.Join(";", memberConstraintList.Select(t => t.Name)), string.Join(";", memberArray.Select(t => t.Name)));
                        }
                        else
                        {
                            diagnostic = Diagnostic.Create(_diagnosticDescriptor01, location, constraintFileName, memberConstraint.Name);
                        }

                        analysisContext.ReportDiagnostic(diagnostic);
                        return;
                    }
                }

                static bool IsMatch(ISymbol member, MemberConstraint memberConstraint)
                {
                    return member.Name == memberConstraint.Name;
                }
            }
            else
            {
                Location location = Location.Create(analysisContextNode.SyntaxTree, analysisContextNode.AttributeLists.FullSpan);
                Diagnostic diagnostic = Diagnostic.Create(_diagnosticDescriptor04, location, constraintFileName,
                    string.Join("\n", dictionary.Select(t => t.Key)));
                analysisContext.ReportDiagnostic(diagnostic);
                return;
            }

        }, SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration);
    }

    private List<MemberConstraint> ReadMemberConstraintList(AdditionalText additionalText)
    {
        var memberConstraintList = new List<MemberConstraint>();

        SourceText? text = additionalText.GetText();
        if (text is not null)
        {
            foreach (TextLine textLine in text.Lines)
            {
                string line = textLine.ToString();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                
                if (line.StartsWith("//"))
                {
                    continue;
                }

                memberConstraintList.Add(new MemberConstraint(line));
            }
        }

        return memberConstraintList;
    }

    record MemberConstraint(string Name);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    static bool ContainAttribute(TypeDeclarationSyntax type, string attributeName)
    {
        foreach (var attributeListSyntax in type.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (attributeSyntax.Name is IdentifierNameSyntax identifierNameSyntax)
                {
                    if (attributeName.Contains(identifierNameSyntax.Identifier.Text))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    static bool TryGetAttribute(INamedTypeSymbol symbol, string attributeName, [NotNullWhen(true)] out AttributeData? hitAttributeData)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass is { } attributeClass)
            {
                if (attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == attributeName)
                {
                    hitAttributeData = attributeData;
                    return true;
                }
            }
        }

        hitAttributeData = null;
        return false;
    }
}