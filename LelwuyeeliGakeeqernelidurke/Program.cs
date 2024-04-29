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
var propertyInfoList = new List<PropertyInfo>();

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

    propertyInfoList.Add(new PropertyInfo(propertyName, summary, example));
}

var codeText = new StringBuilder();
foreach (var propertyInfo in propertyInfoList)
{
    codeText.AppendLine
    (
        $"""
         <!-- {propertyInfo.Summary} -->
         <{propertyInfo.PropertyName}>{propertyInfo.Example}</{propertyInfo.PropertyName}>
         """
    );
    codeText.AppendLine();
}

code = codeText.ToString();
Console.WriteLine(code);

while (true)
{
    Console.Read();
}

string ReadXmlElementSyntaxText(XmlElementSyntax xmlElementSyntax)
{
    var stringBuilder = new StringBuilder();
    bool needAppendLine = false;
    foreach (var xmlNodeSyntax in xmlElementSyntax.Content)
    {
        if (xmlNodeSyntax is XmlTextSyntax xmlTextSyntax)
        {
            if (needAppendLine)
            {
                stringBuilder.AppendLine();
            }

            foreach (var textToken in xmlTextSyntax.TextTokens)
            {
                stringBuilder.Append(textToken.Text);
            }

            needAppendLine = true;
        }
        else
        {
            needAppendLine = false;

            if (xmlNodeSyntax is XmlEmptyElementSyntax xmlEmptyElementSyntax)
            {
                // <see cref="AppId"/>
                if (xmlEmptyElementSyntax.Name.LocalName.Text == "see")
                {
                    var xmlCrefAttributeSyntax = xmlEmptyElementSyntax.Attributes.OfType<XmlCrefAttributeSyntax>()
                        .FirstOrDefault();
                    if (xmlCrefAttributeSyntax != null)
                    {
                        stringBuilder.Append($"{xmlCrefAttributeSyntax.Cref}");
                    }
                }
            }
            else if (xmlNodeSyntax is XmlElementSyntax subXmlElementSyntax)
            {
                // <see cref="AppId"></see>
                var startTag = subXmlElementSyntax.StartTag;
                if (startTag.Name.LocalName.Text == "see")
                {
                    var xmlCrefAttributeSyntax = startTag.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault();
                    if (xmlCrefAttributeSyntax != null)
                    {
                        stringBuilder.Append($"{xmlCrefAttributeSyntax.Cref}");
                    }
                }
                else if (startTag.Name.LocalName.Text == "para")
                {
                    needAppendLine = true;
                }
            }
        }
    }

    return stringBuilder.ToString();
}

record PropertyInfo(string PropertyName, string Summary, string Example);