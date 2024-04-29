// See https://aka.ms/new-console-template for more information

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var file = @"c:\lindexi\Code\Configuration.cs";

var code = File.ReadAllText(file);
SyntaxTree tree = CSharpSyntaxTree.ParseText(code);

SyntaxNode root = tree.GetRoot();

var syntaxTrivias = root.DescendantTrivia(node=> node is PropertyDeclarationSyntax).ToList();
var syntaxNodes = root.ChildNodes().ToList();

foreach (var descendantNode in root.DescendantNodes(node => node is PropertyDeclarationSyntax, descendIntoTrivia: true))
{

}

while (true)
{
    Console.Read();
}
