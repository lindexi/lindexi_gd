using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KonanohallreGonurliyage.Test;

[TestClass]
public class IncrementalGeneratorTest
{
    [TestMethod]
    public void Test()
    {
        var streamReader = new StreamReader(GetType().Assembly.GetManifestResourceStream("KonanohallreGonurliyage.Test.TestCode.cs")!);
        var testCode = streamReader.ReadToEnd();
        var compilation = CreateCompilation(testCode);
        var generator = new IncrementalGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        Assert.IsTrue(diagnostics.IsEmpty);
    }

    private static CSharpCompilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source, path: "Foo.cs") },
            new[]
            {
                // 如果缺少引用，那将会导致单元测试有些符号无法寻找正确，从而导致解析失败
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, sourceReferenceResolver: new SourceFileResolver([/*代码查找文件夹*/], @"C:\lindexi\Code")));
}