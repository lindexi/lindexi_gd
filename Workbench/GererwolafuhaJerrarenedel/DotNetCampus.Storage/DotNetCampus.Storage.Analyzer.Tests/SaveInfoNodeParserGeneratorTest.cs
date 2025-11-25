using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DotNetCampus.Storage.Analyzer.Tests;

[TestClass]
public sealed class SaveInfoNodeParserGeneratorTest
{
    [TestMethod]
    public void TestNullable()
    {
        var sourceCode =
            """
            using DotNetCampus.Storage.Lib.SaveInfos;

            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace DotNetCampus.Storage.Demo.SaveInfos;

            [SaveInfoContract("Foo")]
            public class FooSaveInfo : SaveInfo
            {
                [SaveInfoMember("FooNullProperty", Description = "This is a foo null property.")]
                public int? FooNullProperty { get; set; }
            }
            """;
        var compilation = CreateCompilation(sourceCode);
        var generator = new SaveInfoNodeParserGenerator();

        // 创建 AnalyzerConfigOptions
        var configOptions = new Dictionary<string, string>
        {
            ["build_property.GenerateSaveInfoNodeParser"] = "true",
            ["build_property.RootNamespace"] = "DotNetCampus.Storage.Demo.SaveInfos"
        };
        var analyzerConfigOptionsProvider = new TestAnalyzerConfigOptionsProvider(configOptions);

        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], optionsProvider: analyzerConfigOptionsProvider);
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        foreach (var generatedTree in runResult.GeneratedTrees)
        {
            var generatedCode = generatedTree.ToString();
            Debug.WriteLine(generatedCode);
        }
    }

    private static CSharpCompilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source, path: "Foo.cs") },
            new[]
            {
                // 如果缺少引用，那将会导致单元测试有些符号无法寻找正确，从而导致解析失败
                // 添加对于 Lib 的引用
                MetadataReference.CreateFromFile(typeof(DotNetCampus.Storage.Lib.SaveInfos.SaveInfo).GetTypeInfo().Assembly.Location),
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