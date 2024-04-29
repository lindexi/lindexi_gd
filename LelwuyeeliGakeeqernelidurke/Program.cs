// See https://aka.ms/new-console-template for more information

using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var file = @"c:\lindexi\Code\Configuration.cs";

var code = File.ReadAllText(file);
SyntaxTree tree = CSharpSyntaxTree.ParseText(code);

SyntaxNode root = tree.GetRoot();

var namespaceDeclarationSyntax = root.ChildNodes().OfType<BaseNamespaceDeclarationSyntax>().First();
var classDeclarationSyntax = namespaceDeclarationSyntax.ChildNodes().OfType<ClassDeclarationSyntax>().First();

var memberDeclarationSyntaxes = classDeclarationSyntax.Members;

foreach (var propertyDeclarationSyntax in memberDeclarationSyntaxes.OfType<PropertyDeclarationSyntax>())
{
    var syntaxToken = propertyDeclarationSyntax.ChildTokens().First();
    if (!syntaxToken.IsKind(SyntaxKind.PublicKeyword))
    {
        continue;
    }

    string? summary = null;
    string? example = null;
    foreach (var syntaxTrivia in syntaxToken.GetAllTrivia())
    {
        if (syntaxTrivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
        {
            if (syntaxTrivia.GetStructure() is DocumentationCommentTriviaSyntax documentationCommentTriviaSyntax)
            {
                foreach (var xmlElementSyntax in documentationCommentTriviaSyntax.Content.OfType<XmlElementSyntax>())
                {
                    if (xmlElementSyntax.StartTag.Name.LocalName.Text == "summary")
                    {
                        //summary = xmlElementSyntax.GetText().ToString()
                        //foreach (var xmlTextSyntax in xmlElementSyntax.Content.OfType<XmlTextSyntax>())
                        //{
                        //    foreach (var textToken in xmlTextSyntax.TextTokens)
                        //    {
                        //        summary += textToken.Text;
                        //    }
                        //}

                        summary = ReadXmlElementSyntaxText(xmlElementSyntax).Trim();
                    }
                    else if (xmlElementSyntax.StartTag.Name.LocalName.Text == "example")
                    {
                        example = ReadXmlElementSyntaxText(xmlElementSyntax).Trim();
                    }
                }
            }
        }
    }

    if (summary is null || example is null)
    {
        continue;
    }

    var propertyName = propertyDeclarationSyntax.Identifier.Text;
}

while (true)
{
    Console.Read();
}

string ReadXmlElementSyntaxText(XmlElementSyntax xmlElementSyntax)
{
    var stringBuilder = new StringBuilder();
    bool first = true;
    foreach (var xmlTextSyntax in xmlElementSyntax.Content.OfType<XmlTextSyntax>())
    {
        if (first)
        {
            stringBuilder.AppendLine();
            first = false;
        }

        foreach (var textToken in xmlTextSyntax.TextTokens)
        {
            stringBuilder.Append(textToken.Text);
        }
    }

    return stringBuilder.ToString();
}