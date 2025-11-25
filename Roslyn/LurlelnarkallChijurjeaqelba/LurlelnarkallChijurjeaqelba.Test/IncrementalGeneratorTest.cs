using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LurlelnarkallChijurjeaqelba.Test;

[TestClass]
public class IncrementalGeneratorTest
{
    [TestMethod]
    public void Test()
    {
        var testCode =
            """
            using System;
            
            namespace LurlelnarkallChijurjeaqelba;
            """;

        var compilation = CreateCompilation(testCode);
        var generator = new IncrementalGenerator();

        // 创建 AnalyzerConfigOptions
        var configOptions = new Dictionary<string, string>
        {
            ["build_property.FooProperty"] = "Test",
        };
        var analyzerConfigOptionsProvider = new TestAnalyzerConfigOptionsProvider(configOptions);

        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], optionsProvider: analyzerConfigOptionsProvider);
        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        Assert.HasCount(1, runResult.GeneratedTrees);
        foreach (var generatedTree in runResult.GeneratedTrees)
        {
            var generatedCode = generatedTree.ToString();
            Debug.WriteLine(generatedCode);

            if (generatedTree.FilePath.EndsWith("GeneratedCode.cs"))
            {
                var expected =
                    """
                    using System;
                    
                    namespace LurlelnarkallChijurjeaqelba
                    {
                        public static class GeneratedCode
                        {
                            public static void Print()
                            {
                                Console.WriteLine("配置的属性 Test");
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
            new MetadataReference[]
            {
                // 如果缺少引用，那将会导致单元测试有些符号无法寻找正确，从而导致解析失败
                // 在这里添加你自己的依赖库的引用
            }
            // 加上整个 dotnet 的基础库
            .Concat(MetadataReferenceProvider.GetDotNetMetadataReferenceList()),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}


internal class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> configOptions)
    {
        var testAnalyzerConfigOptions = new TestAnalyzerConfigOptions(configOptions);
        GlobalOptions = testAnalyzerConfigOptions;
    }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
    {
        return GlobalOptions;
    }

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        return GlobalOptions;
    }

    public override AnalyzerConfigOptions GlobalOptions { get; }
}

internal class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    public TestAnalyzerConfigOptions(Dictionary<string, string> configOptions)
    {
        _configOptions = configOptions;
    }

    private readonly Dictionary<string, string> _configOptions;

    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        return _configOptions.TryGetValue(key, out value);
    }
}

internal static class MetadataReferenceProvider
{
    public static IReadOnlyList<MetadataReference> GetDotNetMetadataReferenceList()
    {
        if (_cacheList is not null)
        {
            return _cacheList;
        }

        var metadataReferenceList = new List<MetadataReference>();
        var assembly = Assembly.Load("System.Runtime");
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(assembly.Location)!, "*.dll"))
        {
            try
            {
                metadataReferenceList.Add(MetadataReference.CreateFromFile(file));
            }
            catch
            {
                // 忽略
            }
        }

        _cacheList = metadataReferenceList;
        return _cacheList;
    }

    private static IReadOnlyList<MetadataReference>? _cacheList;
}