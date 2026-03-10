// See https://aka.ms/new-console-template for more information

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

string code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrrluujHlcdyqa
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""hellow"");
        }
    }

    class Foo
    {
        public string KiqHns { get; set; }
    }
}";

SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
var rootSyntaxNode = tree.GetRoot();

// 代码着色程序
// 根据 SyntaxTree 解析内容，为 code 代码进行着色
// 着色方法是调用 FillCodeColor 方法完成着色，传入的就是每个代码字符范围和代码类别

var colorSegments = BuildColorSegments(rootSyntaxNode, code.Length);
foreach (var (span, scope) in colorSegments)
{
    FillCodeColor(span, scope);
}

Console.WriteLine("Hello, World!");

static IReadOnlyList<(TextSpan Span, ScopeType Scope)> BuildColorSegments(SyntaxNode root, int textLength)
{
    var highlightedSegments = new List<(TextSpan Span, ScopeType Scope)>();

    foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
    {
        if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
            || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
            || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
        {
            highlightedSegments.Add((trivia.Span, ScopeType.Comment));
        }
    }

    foreach (var token in root.DescendantTokens(descendIntoTrivia: false))
    {
        var scope = GetTokenScope(token);
        if (scope is not null)
        {
            highlightedSegments.Add((token.Span, scope.Value));
        }
    }

    var ordered = highlightedSegments
        .OrderBy(t => t.Span.Start)
        .ThenBy(t => t.Span.Length)
        .ToList();

    var result = new List<(TextSpan Span, ScopeType Scope)>();
    var position = 0;

    foreach (var segment in ordered)
    {
        var start = segment.Span.Start;
        var end = segment.Span.End;

        if (end <= position)
        {
            continue;
        }

        if (start > position)
        {
            result.Add((TextSpan.FromBounds(position, start), ScopeType.PlainText));
        }

        if (start < position)
        {
            start = position;
        }

        result.Add((TextSpan.FromBounds(start, end), segment.Scope));
        position = end;
    }

    if (position < textLength)
    {
        result.Add((TextSpan.FromBounds(position, textLength), ScopeType.PlainText));
    }

    return result;
}

static ScopeType? GetTokenScope(SyntaxToken token)
{
    if (token.IsKeyword())
    {
        return ScopeType.Keyword;
    }

    if (token.IsKind(SyntaxKind.StringLiteralToken)
        || token.IsKind(SyntaxKind.CharacterLiteralToken)
        || token.IsKind(SyntaxKind.InterpolatedStringStartToken)
        || token.IsKind(SyntaxKind.InterpolatedStringTextToken)
        || token.IsKind(SyntaxKind.InterpolatedStringEndToken))
    {
        return ScopeType.String;
    }

    if (token.IsKind(SyntaxKind.NumericLiteralToken))
    {
        return ScopeType.Number;
    }

    if (token.IsKind(SyntaxKind.OpenBraceToken)
        || token.IsKind(SyntaxKind.CloseBraceToken)
        || token.IsKind(SyntaxKind.OpenParenToken)
        || token.IsKind(SyntaxKind.CloseParenToken)
        || token.IsKind(SyntaxKind.OpenBracketToken)
        || token.IsKind(SyntaxKind.CloseBracketToken)
        || token.IsKind(SyntaxKind.LessThanToken)
        || token.IsKind(SyntaxKind.GreaterThanToken))
    {
        return ScopeType.Brackets;
    }

    if (!token.IsKind(SyntaxKind.IdentifierToken))
    {
        return null;
    }

    if (token.Parent is TypeDeclarationSyntax typeDeclaration
        && typeDeclaration.Identifier == token)
    {
        return ScopeType.ClassName;
    }

    if (token.Parent is MethodDeclarationSyntax method && method.Identifier == token
        || token.Parent is PropertyDeclarationSyntax property && property.Identifier == token
        || token.Parent is EventDeclarationSyntax eventDeclaration && eventDeclaration.Identifier == token
        || token.Parent is ConstructorDeclarationSyntax constructor && constructor.Identifier == token
        || token.Parent is VariableDeclaratorSyntax memberVariable
            && memberVariable.Identifier == token
            && memberVariable.Parent?.Parent is FieldDeclarationSyntax)
    {
        return ScopeType.ClassMember;
    }

    if (token.Parent is ParameterSyntax parameter && parameter.Identifier == token
        || token.Parent is VariableDeclaratorSyntax variable && variable.Identifier == token
            && variable.Parent?.Parent is not FieldDeclarationSyntax
        || token.Parent is ForEachStatementSyntax forEach && forEach.Identifier == token
        || token.Parent is SingleVariableDesignationSyntax designation && designation.Identifier == token)
    {
        return ScopeType.Variable;
    }

    return ScopeType.PlainText;
}

void FillCodeColor(TextSpan span, ScopeType scope)
{
    // 此方法将会被替换为外部库调用完成着色
}


enum ScopeType
{
    Comment,
    ClassName,
    ClassMember,
    Keyword,
    PlainText,
    String,
    Number,
    Brackets,
    Variable,
}