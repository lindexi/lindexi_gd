using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ChunecilarkenaLibeewhemke.Test;

[TestClass]
public class IncrementalGeneratorTest
{
    [TestMethod]
    public void Test()
    {
        var testCode =
            """
            using System;
            using Lindexi;

            namespace ChunecilarkenaLibeewhemke.Test
            {
                [Foo]
                public class F1
                {
                }
            
                [FooAttribute]
                public class F2
                {
                }
            }

            namespace FooChunecilarkenaLibeewhemke
            {
                public class FooAttribute : Attribute
                {
                }
                
                [Foo]
                public class F3
                {
                }
            }
            """;

        var compilation = CreateCompilation(testCode);
        var generator = new IncrementalGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        foreach (var generatedTree in driver.GetRunResult().GeneratedTrees)
        {
            var generatedCode = generatedTree.ToString();
            Debug.WriteLine(generatedCode);

            if (generatedTree.FilePath.EndsWith("GeneratedCode.cs"))
            {
                var expected =
                    """
                     using System;
                     namespace ChunecilarkenaLibeewhemke
                     {
                         public static class GeneratedCode
                         {
                             public static void Print()
                             {
                                 Console.WriteLine("标记了 Foo 特性的类型有： F1,F2,");
                             }
                         }
                     }
                    """;
                // 防止拉取 git 时出现的 \r\n 不匹配问题。能够解决一些拉取 git 的奇怪的坑，也就是在我电脑上跑的好好的，但为什么在你电脑上就炸了
                expected = expected.Replace("\r\n", "\n");
                Assert.AreEqual(expected, generatedCode.Replace("\r\n", "\n"));
            }
        }
    }

    private static CSharpCompilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source, path: "Foo.cs") },
            new[]
            {
                // 如果缺少引用，那将会导致单元测试有些符号无法寻找正确，从而导致解析失败
                MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                sourceReferenceResolver: new SourceFileResolver([ /*代码查找文件夹*/], @"C:\lindexi\Code")));
}