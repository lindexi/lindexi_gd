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
        _lostConstraintMemberDiagnosticDescriptor = new DiagnosticDescriptor("APIConstraint01", "API约束不满足，成员丢失", "此类型不实现标记的 {0} 里的 {1} 成员", "Error", DiagnosticSeverity.Error, true);

        _constraintMemberOrderErrorDiagnosticDescriptor = new DiagnosticDescriptor("APIConstraint02", "API约束不满足，成员定义顺序错误", "此类型实现标记的 {0} 的成员 {1} 顺序错误，正确顺序应该是 {2} ，实际顺序是 {3}", "Error", DiagnosticSeverity.Error, true);

        _missConstraintFileNameDiagnosticDescriptor = new DiagnosticDescriptor("APIConstraint03", "未写明约束文件", "没有在特性上标记约束文件名。当前可用约束文件列表： {0}", "Error", DiagnosticSeverity.Error, true);

        _canNotFindConstraintFileDiagnosticDescriptor = new DiagnosticDescriptor("APIConstraint03", "标记的约束文件找不到", "在特性上标记约束文件名 {0} 无法找到。当前可用约束文件列表： {1}", "Error", DiagnosticSeverity.Error, true);

        SupportedDiagnostics = new[]
        {
            _lostConstraintMemberDiagnosticDescriptor,
            _constraintMemberOrderErrorDiagnosticDescriptor,
            _missConstraintFileNameDiagnosticDescriptor,
            _canNotFindConstraintFileDiagnosticDescriptor,
        }.ToImmutableArray();
    }

    private readonly DiagnosticDescriptor _lostConstraintMemberDiagnosticDescriptor;
    private readonly DiagnosticDescriptor _constraintMemberOrderErrorDiagnosticDescriptor;
    private readonly DiagnosticDescriptor _missConstraintFileNameDiagnosticDescriptor;
    private readonly DiagnosticDescriptor _canNotFindConstraintFileDiagnosticDescriptor;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(analysisContext =>
        {
            var analysisContextNode = (TypeDeclarationSyntax) analysisContext.Node;

            if (analysisContextNode.Identifier.Text == "TextEditor")
            {
                if (analysisContextNode.SyntaxTree.FilePath.EndsWith("TextEditor.Edit.ava.cs"))
                {

                }
            }

            string attributeName = "global::LightTextEditorPlus.APIConstraintAttribute";
            if (!ContainAttribute(analysisContextNode, attributeName))
            {
                // 先走一次语法，速度快
                return;
            }

            INamedTypeSymbol? namedTypeSymbol = analysisContext.SemanticModel.GetDeclaredSymbol(analysisContextNode);
            if (namedTypeSymbol == null)
            {
                return;
            }

            // 经过以上步骤，绝大部分的代码都不会进入这里。因此不怕性能问题

            // 只加上标记是属于 API 约束的文件
            var constraintAdditionalTextList = new List<AdditionalText>();

            ImmutableArray<AdditionalText> additionalFileArray = analysisContext.Options.AdditionalFiles;

            foreach (AdditionalText additionalFile in additionalFileArray)
            {
                AnalyzerConfigOptions analyzerConfigOptions = analysisContext.Options.AnalyzerConfigOptionsProvider.GetOptions(additionalFile);
                if (analyzerConfigOptions.TryGetValue("build_metadata.AdditionalFiles.APIConstraint", out var isAPIConstraintFile))
                {
                    // 有标记的才能加上
                    if (string.Equals(isAPIConstraintFile, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                    {
                        constraintAdditionalTextList.Add(additionalFile);
                    }
                }
            }

            FrozenDictionary<string, AdditionalText> dictionary = constraintAdditionalTextList.ToFrozenDictionary(t => Path.GetFileName(t.Path));

            foreach (AttributeData constraintAttribute in GetAttributeList(namedTypeSymbol, attributeName))
            {
                var constraintFileName = constraintAttribute.ConstructorArguments[0].Value?.ToString();
                string? ignoreOrderText = constraintAttribute.ConstructorArguments[1].Value?.ToString();
                // 是否忽略顺序
                bool ignoreOrder;
                if (!bool.TryParse(ignoreOrderText, out ignoreOrder))
                {
                    ignoreOrder = false;
                }

                if (string.IsNullOrEmpty(constraintFileName))
                {
                    Location location = Location.Create(analysisContextNode.SyntaxTree, analysisContextNode.AttributeLists.FullSpan);
                    Diagnostic diagnostic = Diagnostic.Create(_missConstraintFileNameDiagnosticDescriptor, location,
                        string.Join("\n", dictionary.Select(t => t.Key)));
                    analysisContext.ReportDiagnostic(diagnostic);
                    continue;
                }

                Debug.Assert(constraintFileName != null, nameof(constraintFileName) + " != null");
                if (dictionary.TryGetValue(constraintFileName!, out var additionalText))
                {
                    List<MemberConstraint> memberConstraintList = ReadMemberConstraintList(additionalText);
                    var memberList = GetAllMember(namedTypeSymbol, includeBaseClass: ignoreOrder/*只有忽略顺序的情况，才能包含基类的成员*/);

                    var memberConstraintIndex = 0;
                    var memberIndex = 0;

                    bool isAnyMiss = false;
                    for (; memberConstraintIndex < memberConstraintList.Count; memberConstraintIndex++)
                    {
                        MemberConstraint memberConstraint = memberConstraintList[memberConstraintIndex];

                        bool canFind = false;

                        for (; memberIndex < memberList.Count && !canFind; memberIndex++)
                        {
                            ISymbol member = memberList[memberIndex];
                            if (IsMatch(member, memberConstraint))
                            {
                                canFind = true;
                            }
                        }

                        if (!canFind)
                        {
                            // 找不到可能是顺序错了
                            var isOrderError = memberList.Any(t => IsMatch(t, memberConstraint));

                            Location location = Location.Create(analysisContextNode.SyntaxTree, analysisContextNode.FullSpan);

                            Diagnostic diagnostic;
                            if (isOrderError)
                            {
                                if (ignoreOrder)
                                {
                                    // 如果可以忽略顺序，那就不报错
                                    continue;
                                }

                                if (isAnyMiss)
                                {
                                    // 已经有缺少的成员了，那就不再报错了
                                    // 可能是因为缺少成员，才出现的没有按照顺序
                                    continue;
                                }

                                diagnostic = Diagnostic.Create(_constraintMemberOrderErrorDiagnosticDescriptor, location, constraintFileName, memberConstraint.Name, string.Join(";", memberConstraintList.Select(t => t.Name)), string.Join(";", memberList.Select(t => t.Name)));
                            }
                            else
                            {
                                diagnostic = Diagnostic.Create(_lostConstraintMemberDiagnosticDescriptor, location, constraintFileName, memberConstraint.Name);
                            }

                            analysisContext.ReportDiagnostic(diagnostic);

                            isAnyMiss = true;

                            if (isOrderError)
                            {
                                // 如果是顺序错误，那就停止，否则可能后面的都是顺序错误的，导致淹没
                                break;
                            }
                            else
                            {
                                // 不是顺序错误，那就是缺少了，一次性给出所有缺少的成员
                                continue;
                            }
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
                    Diagnostic diagnostic = Diagnostic.Create(_canNotFindConstraintFileDiagnosticDescriptor, location, constraintFileName,
                        string.Join("\n", dictionary.Select(t => t.Key)));
                    analysisContext.ReportDiagnostic(diagnostic);
                    continue;
                }
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

    static IEnumerable<AttributeData> GetAttributeList(INamedTypeSymbol symbol, string attributeName)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass is { } attributeClass)
            {
                if (attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == attributeName)
                {
                    yield return attributeData;
                }
            }
        }
    }

    static IReadOnlyList<ISymbol> GetAllMember(INamedTypeSymbol symbol, bool includeBaseClass)
    {
        var memberList = new List<ISymbol>();
        memberList.AddRange(symbol.GetMembers());

        if (includeBaseClass)
        {
            var currentType = symbol.BaseType;
            while (currentType != null)
            {
                memberList.AddRange(currentType.GetMembers());
                currentType = currentType.BaseType;
            }
        }

        return memberList;
    }
}
