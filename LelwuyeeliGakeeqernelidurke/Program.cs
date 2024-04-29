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

foreach (var memberDeclarationSyntax in memberDeclarationSyntaxes.OfType<PropertyDeclarationSyntax>())
{
    var syntaxToken = memberDeclarationSyntax.ChildTokens().First();
    if (!syntaxToken.IsKind(SyntaxKind.PublicKeyword))
    {
        continue;
    }

    string? summary = null;
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
                }
            }
        }
    }
}

while (true)
{
    Console.Read();
}

string ReadXmlElementSyntaxText(XmlElementSyntax xmlElementSyntax)
{
    var stringBuilder = new StringBuilder();
    foreach (var xmlTextSyntax in xmlElementSyntax.Content.OfType<XmlTextSyntax>())
    {
        foreach (var textToken in xmlTextSyntax.TextTokens)
        {
            stringBuilder.Append(textToken.Text);
        }
    }

    return stringBuilder.ToString();
}