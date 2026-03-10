using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Linq;

namespace SimpleWrite.Business.TextEditors.Highlighters.CodeHighlighters;

public class CsharpCodeHighlighter : ICodeHighlighter
{
    public void ApplyHighlight(in HighlightCodeContext context)
    {
        var code = context.PlainCode;
        var colorCode = context.ColorCode;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var rootSyntaxNode = tree.GetRoot();

        var colorSegments = BuildColorSegments(rootSyntaxNode, code.Length);
        foreach (var (span, scope) in colorSegments)
        {
#if DEBUG
            if (span.End < code.Length)
            {
                var currentText = code.Substring(span.Start, span.Length);
                GC.KeepAlive(currentText);

                if (scope == ScopeType.Invocation)
                {
                    
                }
            }
#endif

            colorCode.FillCodeColor(span, scope);
        }
    }


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

        foreach (SyntaxNode syntaxNode in root.DescendantNodes(descendIntoTrivia: false))
        {
            if (syntaxNode is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                var lastToken
                    = invocationExpressionSyntax.Expression.GetLastToken();
                result.Add((lastToken.Span, ScopeType.Invocation));
            }
            else if (syntaxNode is LocalDeclarationStatementSyntax localDeclarationStatementSyntax)
            {
                var variableDeclarationSyntax = localDeclarationStatementSyntax.Declaration;
                TypeSyntax typeSyntax = variableDeclarationSyntax.Type;
                if (typeSyntax.IsVar)
                {
                    result.Add((typeSyntax.Span, ScopeType.Keyword));
                }
                else
                {
                    result.Add((typeSyntax.Span, ScopeType.DeclarationTypeSyntax));
                }
            }
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

        if (token.IsKind(SyntaxKind.InvocationExpression))
        {
            return ScopeType.Invocation;
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
}