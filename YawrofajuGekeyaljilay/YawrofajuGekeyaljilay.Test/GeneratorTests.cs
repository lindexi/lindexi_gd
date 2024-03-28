using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace YawrofajuGekeyaljilay.Test;

[TestClass]
public class GeneratorTests
{
    [TestMethod]
    public void SimpleGeneratorTest()
    {
        Compilation inputCompilation = CreateCompilation(@"
namespace YawrofajuGekeyaljilay
{
    public static class Program
    {
        public static void Main(string[] args)
        {
        }
    }
}
");
        var codeCollectionIncrementalGenerator = new CodeCollectionIncrementalGenerator();
        var driver = CSharpGeneratorDriver.Create(codeCollectionIncrementalGenerator);
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        Assert.AreEqual(true, outputCompilation.ContainsSymbolsWithName("HelloFrom"));

        foreach (var outputCompilationSyntaxTree in outputCompilation.SyntaxTrees)
        {
            var text = outputCompilationSyntaxTree.GetText();
        }
    }

    private static CSharpCompilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}

