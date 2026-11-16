using XiaoXiIme.Cli;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli.Tests;

public class DefaultDictionaryPackageBuilderTests
{
    [Fact]
    public void Build_WhenNativeSourcesAreValidThenCreatesFullPinyinPackage()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSources(root);
        var hostOutputDirectory = Path.Combine(root, "host");

        DefaultDictionaryPackageBuilder.Build(sourceDirectory, hostOutputDirectory);

        var dictionary = DictionaryPackageLoader.Load
            (Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName));
        Assert.Equal("你好", Assert.Single(dictionary.Query(new ImeDictionaryQuery("nihao"))).Text);
    }

    [Fact]
    public void Build_WhenNativeSourcesAreValidThenCreatesXiaoheDoublePinyinPackage()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSources(root);
        var hostOutputDirectory = Path.Combine(root, "host");

        DefaultDictionaryPackageBuilder.Build(sourceDirectory, hostOutputDirectory);

        var dictionary = DictionaryPackageLoader.Load
            (Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath));
        Assert.Equal("你好", Assert.Single(dictionary.Query(new ImeDictionaryQuery("nihc"))).Text);
    }

    [Fact]
    public void Build_WhenProjectTermUsesNormalizedLetterNameReadingThenCreatesBothPackages()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSources(root);
        var hostOutputDirectory = Path.Combine(root, "host");

        DefaultDictionaryPackageBuilder.Build(sourceDirectory, hostOutputDirectory);

        var fullPinyin = DictionaryPackageLoader.Load
            (Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName));
        var xiaohe = DictionaryPackageLoader.Load
            (Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath));
        Assert.Equal("IME", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("aimuyi"))).Text);
        Assert.Equal("IME", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("aimuyi"))).Text);
        Assert.Equal("XiaoXiIme", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("xiaoxiaimuyi"))).Text);
        Assert.Equal("XiaoXiIme", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("xnxiaimuyi"))).Text);
        Assert.Empty(xiaohe.Query(new ImeDictionaryQuery("xiaoxiaimuyi")));
    }

    [Fact]
    public void Build_WhenPhoneticSourceIsMissingThenDoesNotCreatePackage()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);
        var hostOutputDirectory = Path.Combine(root, "host");

        Assert.Throws<InvalidOperationException>
            (() => DefaultDictionaryPackageBuilder.Build(sourceDirectory, hostOutputDirectory));
        Assert.False
        (
            Directory.Exists
                (Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName))
        );
    }

    [Fact]
    public void EnsureRequiredSources_WhenRequiredTsvIsMissingThenReportsConvertCommand()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);

        var exception = Assert.Throws<InvalidOperationException>
            (() => DefaultDictionaryPackageBuilder.EnsureRequiredSources(sourceDirectory));

        Assert.Contains("dictionary-convert-sewzc", exception.Message);
    }

    [Theory]
    [InlineData("fullPinyin")]
    [InlineData("xiaoheDoublePinyin")]
    [InlineData("sogouDoublePinyin")]
    [InlineData("microsoftDoublePinyin")]
    [InlineData("abcDoublePinyin")]
    [InlineData("jiajiaDoublePinyin")]
    [InlineData("naturalCodeDoublePinyin")]
    public void CopyPackages_WhenPayloadPathContainsSpacesThenUiLoadsScheme(string scheme)
    {
        var root = Path.Combine(CreateRoot(), "experience output with spaces");
        var sources = CreateSources(root);
        var staging = Path.Combine(root, "staging");
        DefaultDictionaryPackageBuilder.Build(sources, staging);

        var error = IntegrationPayloadBuilder.CopyAndVerifyDictionaryPackages(staging, root);
        Assert.Null(error);
        var package = DictionaryPackageLocations.ResolvePackageDirectory
            (inputScheme: scheme, baseDirectory: Path.Combine(root, "app", "ui"));
        Assert.Equal(scheme, DictionaryPackageLoader.Load(package).InputScheme);
    }

    [Fact]
    public async Task Build_WhenDictionaryResourcesMissingThenFailsWithoutManifest()
    {
        var root = CreateRoot();
        var sources = Directory.CreateDirectory(Path.Combine(root, "missing sources")).FullName;
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await IntegrationPayloadBuilder.BuildAsync
        (
            new PayloadBuildOptions
            {
                NoBuild = true, StagingDirectory = root, DictionarySource = sources,
                Output = Path.Combine(root, "output")
            }, output, error
        );

        Assert.Equal(13, exitCode);
        Assert.False(File.Exists(Path.Combine(root, "output", IntegrationPayloadManifest.FileName)));
    }

    [Theory]
    [InlineData("XiaoXiIme.ImeUi.Avalonia.exe")]
    [InlineData("coreclr.dll")]
    [InlineData("Avalonia.Fonts.Inter.dll")]
    [InlineData("libSkiaSharp.dll")]
    public async Task Build_WhenUiResourceIsMissingThenFailsWithoutManifest(string missing)
    {
        var root = CreateRoot();
        var sources = CreateSources(root);
        var ui = Directory.CreateDirectory(Path.Combine(root, "app", "ui")).FullName;
        var required = new[]
        {
            "XiaoXiIme.ImeUi.Avalonia.exe", "XiaoXiIme.ImeUi.Avalonia.dll", "XiaoXiIme.ImeUi.Avalonia.deps.json",
            "XiaoXiIme.ImeUi.Avalonia.runtimeconfig.json", "coreclr.dll", "hostfxr.dll", "hostpolicy.dll",
            "System.Private.CoreLib.dll", "Avalonia.Controls.dll", "Avalonia.Fonts.Inter.dll",
            "Avalonia.Themes.Fluent.dll", "av_libglesv2.dll", "libSkiaSharp.dll"
        };
        foreach (var file in required.Where(file => file != missing))
        {
            File.WriteAllText(Path.Combine(ui, file), "fixture");
        }

        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await IntegrationPayloadBuilder.BuildAsync
        (
            new PayloadBuildOptions
            {
                Experience = true, NoBuild = true, StagingDirectory = root, DictionarySource = sources,
                Output = Path.Combine(root, "output")
            }, output, error
        );

        Assert.Equal(11, exitCode);
        Assert.False(File.Exists(Path.Combine(root, "output", IntegrationPayloadManifest.FileName)));
        Assert.Contains(missing, error.ToString());
    }

    private static string CreateSources(string root)
    {
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "phonetic"));
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "shape"));
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "symbols"));
        File.WriteAllText
            (Path.Combine(sourceDirectory, "phonetic", "sewzc-default.phonetic.tsv"), "你好\tni hao\t100\n你\tni\t90");
        XiaoXiImeProjectTerms.WriteTo(Path.Combine(sourceDirectory, XiaoXiImeProjectTerms.OutputRelativePath));
        File.WriteAllText(Path.Combine(sourceDirectory, "shape", "sewzc-moqi.shape.tsv"), "你\trb\t亻尔");
        File.WriteAllText(Path.Combine(sourceDirectory, "symbols", "sewzc-default.symbols.tsv"), "/xh\t★\t☆");
        return sourceDirectory;
    }

    private static string CreateRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Cli.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}