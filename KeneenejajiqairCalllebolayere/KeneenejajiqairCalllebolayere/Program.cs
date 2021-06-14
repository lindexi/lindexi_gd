using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KeneenejajiqairCalllebolayere
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"..\..\..\Program.cs";

            file = Path.GetFullPath(file);
            var text = File.ReadAllText(file);

            var tree = CSharpSyntaxTree.ParseText(text);

            var modelCollector = new ModelCollector();
            modelCollector.Visit(tree.GetRoot());
        }
    }

    class ModelCollector : CSharpSyntaxWalker
    {
        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            Debug.WriteLine(node.Name.ToFullString());
            base.VisitUsingDirective(node);
        }
    }
}
