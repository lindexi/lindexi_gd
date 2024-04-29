// See https://aka.ms/new-console-template for more information

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var file = @"c:\lindexi\Code\Configuration.cs";

var code = File.ReadAllText(file);
SyntaxTree tree = CSharpSyntaxTree.ParseText(code);

SyntaxNode root = tree.GetRoot();

var namespaceDeclarationSyntax = root.ChildNodes().OfType<BaseNamespaceDeclarationSyntax>().First();
var classDeclarationSyntax = namespaceDeclarationSyntax.ChildNodes().OfType<ClassDeclarationSyntax>().First();

foreach (var descendantNode in root.DescendantNodes(node => node is PropertyDeclarationSyntax, descendIntoTrivia: true))
{

}

while (true)
{
    Console.Read();
}
