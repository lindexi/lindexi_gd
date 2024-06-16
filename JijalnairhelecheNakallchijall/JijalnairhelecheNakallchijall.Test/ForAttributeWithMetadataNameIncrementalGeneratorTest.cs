using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JijalnairhelecheNakallchijall.Test;

[TestClass]
public class ForAttributeWithMetadataNameIncrementalGeneratorTest
{
    [TestMethod]
    public void Test()
    {
        var streamReader = new StreamReader(GetType().Assembly.GetManifestResourceStream("JijalnairhelecheNakallchijall.Test.TestCode.cs")!);
        var testCode = streamReader.ReadToEnd();
        var compilation = CreateCompilation(testCode);
        var generator = new ForAttributeWithMetadataNameIncrementalGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        Assert.IsTrue(diagnostics.IsEmpty);

        Assert.AreEqual(true, outputCompilation.ContainsSymbolsWithName("Program"));

        var syntaxTree = outputCompilation.SyntaxTrees.Last();
        var generatedCode = syntaxTree.GetText().ToString();
        Assert.AreEqual
        (
            """
            using System;

            namespace JijalnairhelecheNakallchijall
            {
                public static partial class Program
                {
                    public static void Hello()
                    {
                        Console.WriteLine($"Says: Hi from F1, F2");
                    }
                }
            }
            """
            , generatedCode);
    }

    private static CSharpCompilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[]
            {
                // 如果缺少引用，那将会导致单元测试有些符号无法寻找正确，从而导致解析失败
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}