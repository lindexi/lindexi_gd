using XiaoXiIme.Cli;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli.Tests;

public class LocalDictionaryCommandsTests
{
    [Fact]
    public void Update_WhenSourcesAreValidThenCreatesPackage()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "custom.phonetic.tsv"), "你好\tni hao\t100");
        var packageDirectory = Path.Combine(root, "package");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions
            {
                SourceDirectory = sourceDirectory,
                PackageDirectory = packageDirectory,
                Scheme = "xiaoheDoublePinyin",
            },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("Dictionary package updated:", output.ToString());
        Assert.Equal("你好", Assert.Single(DictionaryPackageLoader.Load(packageDirectory).Query(new ImeDictionaryQuery("nihc"))).Text);
    }

    [Fact]
    public void Update_WhenSourcesAreUnchangedThenReusesPackage()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "custom.phonetic.tsv"), "你好\tni hao\t100");
        var packageDirectory = Path.Combine(root, "package");
        using var output = new StringWriter();
        using var error = new StringWriter();
        LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = sourceDirectory, PackageDirectory = packageDirectory },
            output,
            error);

        var exitCode = LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = sourceDirectory, PackageDirectory = packageDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("Dictionary package reused:", output.ToString());
        Assert.False(Directory.Exists($"{packageDirectory}.previous"));
    }

    [Fact]
    public void Inspect_WhenPackageIsValidThenReportsCounts()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "custom.phonetic.tsv"), "你好\tni hao\t100");
        var packageDirectory = Path.Combine(root, "package");
        using var output = new StringWriter();
        using var error = new StringWriter();
        LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = sourceDirectory, PackageDirectory = packageDirectory },
            output,
            error);

        var exitCode = LocalDictionaryCommands.Inspect(
            new DictionaryInspectOptions { PackageDirectory = packageDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("Validation: passed", output.ToString());
        Assert.Contains("Candidates: 1", output.ToString());
        Assert.Contains("Compiler version: 1", output.ToString());
        Assert.Contains("Attribution: SeWZC", output.ToString());
        Assert.Contains(DictionaryAttribution.Notice, output.ToString());
    }

    [Fact]
    public void Inspect_WhenJsonThenReportsSewzcAttribution()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "custom.phonetic.tsv"), "你好\tni hao\t100");
        var packageDirectory = Path.Combine(root, "package");
        using var output = new StringWriter();
        using var error = new StringWriter();
        LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = sourceDirectory, PackageDirectory = packageDirectory },
            output,
            error);

        var exitCode = LocalDictionaryCommands.Inspect(
            new DictionaryInspectOptions { PackageDirectory = packageDirectory, Json = true },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"attribution\": \"SeWZC\"", output.ToString());
        Assert.Contains(DictionaryAttribution.Notice, output.ToString());
    }

    [Fact]
    public void Inspect_WhenPackageIsMissingThenReturnsFailure()
    {
        var root = CreateRoot();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.Inspect(
            new DictionaryInspectOptions { PackageDirectory = Path.Combine(root, "missing") },
            output,
            error);

        Assert.Equal(4, exitCode);
    }

    [Fact]
    public void Rollback_WhenPreviousPackageExistsThenRestoresIt()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);
        var sourcePath = Path.Combine(sourceDirectory, "custom.phonetic.tsv");
        File.WriteAllText(sourcePath, "你\tni\t100");
        var packageDirectory = Path.Combine(root, "package");
        using var output = new StringWriter();
        using var error = new StringWriter();
        LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = sourceDirectory, PackageDirectory = packageDirectory },
            output,
            error);
        File.WriteAllText(sourcePath, "好\thao\t200");
        LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = sourceDirectory, PackageDirectory = packageDirectory },
            output,
            error);

        var exitCode = LocalDictionaryCommands.Rollback(
            new DictionaryRollbackOptions { PackageDirectory = packageDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packageDirectory).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Update_WhenPhoneticSourceIsMissingThenReturnsFailure()
    {
        var root = CreateRoot();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = root, PackageDirectory = Path.Combine(root, "package") },
            output,
            error);

        Assert.Equal(3, exitCode);
    }

    [Fact]
    public void ConvertSeWzc_WhenSnapshotIsValidThenWritesNativeSources()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSeWzcSnapshot(root);
        var targetDirectory = Path.Combine(root, "native");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.ConvertSeWzc(
            new DictionaryConvertSeWzcOptions { SourceDirectory = sourceDirectory, TargetDirectory = targetDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        var phoneticSource = File.ReadAllText(Path.Combine(targetDirectory, SeWzcDictionarySourceConverter.PhoneticOutputRelativePath));
        var projectTermsSource = File.ReadAllText(Path.Combine(targetDirectory, SeWzcDictionarySourceConverter.ProjectTermsOutputRelativePath));
        Assert.Contains("你好\tni hao\t100", phoneticSource);
        Assert.Contains("均订\tjun ding\t50", phoneticSource);
        Assert.DoesNotContain("小希\txx\t10000000", phoneticSource);
        Assert.Contains("小希\txiao xi\t10000000", projectTermsSource);
        Assert.Contains("小希\txx\t10000000", projectTermsSource);
        Assert.Contains("小希输入法\txiao xi shu ru fa\t10000000", projectTermsSource);
        Assert.Contains("XiaoXiIme\txiao xi ai mu yi\t10000000", projectTermsSource);
        Assert.Contains("输入法核心\tshu ru fa he xin\t10000000", projectTermsSource);
        Assert.Contains("候选窗口\thou xuan chuang kou\t10000000", projectTermsSource);
        Assert.Contains("用户词库\tyong hu ci ku\t10000000", projectTermsSource);
        Assert.Contains("词库编译\tci ku bian yi\t10000000", projectTermsSource);
        Assert.Contains("TSF\tti e si e fu\t10000000", projectTermsSource);
        Assert.Contains("IME\tai mu yi\t10000000", projectTermsSource);
    }

    [Fact]
    public void ConvertSeWzc_WhenSourceIsSeWzcRepositoryThenRejectsPath()
    {
        var root = CreateRoot();
        var repositoryRoot = Path.Combine(root, "SeWZC_IME");
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "src", "Ime.RimeShim.NativeAot"));
        File.WriteAllText(Path.Combine(repositoryRoot, "ImeLab.slnx"), string.Empty);
        var sourceDirectory = CreateSeWzcSnapshot(Path.Combine(repositoryRoot, "data"));
        var targetDirectory = Path.Combine(root, "native");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.ConvertSeWzc(
            new DictionaryConvertSeWzcOptions { SourceDirectory = sourceDirectory, TargetDirectory = targetDirectory },
            output,
            error);

        Assert.Equal(4, exitCode);
        Assert.Contains("does not accept a SeWZC_IME repository path", error.ToString());
        Assert.False(Directory.Exists(targetDirectory));
    }

    [Fact]
    public void BuildPackages_WhenNativeSourcesAreValidThenCreatesQueryablePackages()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateNativeSources(root);
        var hostOutputDirectory = Path.Combine(root, "host");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.BuildPackages(
            new DictionaryBuildPackagesOptions { SourceDirectory = sourceDirectory, HostOutputDirectory = hostOutputDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("Dictionary packages built:", output.ToString());
        var fullPinyin = DictionaryPackageLoader.Load(Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName));
        var xiaohe = DictionaryPackageLoader.Load(Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath));
        Assert.Equal("你好", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("nihao"))).Text);
        Assert.Equal("你好", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("nihc"))).Text);
        Assert.Equal("IME", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("aimuyi"))).Text);
        Assert.Equal("IME", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("aimuyi"))).Text);
        Assert.Equal("XiaoXiIme", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("xiaoxiaimuyi"))).Text);
        Assert.Equal("XiaoXiIme", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("xnxiaimuyi"))).Text);
        Assert.Empty(xiaohe.Query(new ImeDictionaryQuery("xiaoxiaimuyi")));
    }

    [Fact]
    public void ConvertSeWzc_WhenSnapshotIsConvertedThenBuildsQueryablePackages()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSeWzcSnapshot(root);
        var targetDirectory = Path.Combine(root, "native");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.ConvertSeWzc(
            new DictionaryConvertSeWzcOptions { SourceDirectory = sourceDirectory, TargetDirectory = targetDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        var hostOutputDirectory = Path.Combine(root, "host");
        DefaultDictionaryPackageBuilder.Build(targetDirectory, hostOutputDirectory);
        var fullPinyin = DictionaryPackageLoader.Load(Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName));
        var xiaohe = DictionaryPackageLoader.Load(Path.Combine(hostOutputDirectory, DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath));
        Assert.Equal("小希", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("xx"))).Text);
        Assert.Equal("小希", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("xx"))).Text);
        Assert.Equal("均订", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("jydk"))).Text);
        Assert.Equal("IME", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("aimuyi"))).Text);
        Assert.Equal("XiaoXiIme", Assert.Single(fullPinyin.Query(new ImeDictionaryQuery("xiaoxiaimuyi"))).Text);
        Assert.Equal("IME", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("aimuyi"))).Text);
        Assert.Equal("XiaoXiIme", Assert.Single(xiaohe.Query(new ImeDictionaryQuery("xnxiaimuyi"))).Text);
        Assert.Empty(xiaohe.Query(new ImeDictionaryQuery("xiaoxiaimuyi")));
    }

    [Fact]
    public void ConvertSeWzc_WhenSymbolInputHasTooManyCandidatesThenTruncatesDeterministically()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSeWzcSnapshot(root);
        var symbolsPath = Path.Combine(sourceDirectory, "symbols", "default.symbols.tsv");
        File.WriteAllText(symbolsPath, "/all\t" + string.Join('\t', Enumerable.Range(0, SymbolDictionarySourceParser.MaxCandidatesPerInput + 12).Select(index => $"候选{index}")));
        var targetDirectory = Path.Combine(root, "native");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.ConvertSeWzc(
            new DictionaryConvertSeWzcOptions { SourceDirectory = sourceDirectory, TargetDirectory = targetDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        using var reader = File.OpenText(Path.Combine(targetDirectory, SeWzcDictionarySourceConverter.SymbolOutputRelativePath));
        Assert.Equal(SymbolDictionarySourceParser.MaxCandidatesPerInput, Assert.Single(SymbolDictionarySourceParser.Parse(reader, symbolsPath)).Candidates.Count);
    }

    [Fact]
    public void ConvertSeWzc_WhenRequiredSourceIsMissingThenPreservesExistingTargets()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSeWzcSnapshot(root);
        File.Delete(Path.Combine(sourceDirectory, "shape", "moqi_chaifen.txt"));
        var targetDirectory = Path.Combine(root, "native");
        var existingPath = Path.Combine(targetDirectory, SeWzcDictionarySourceConverter.PhoneticOutputRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(existingPath)!);
        File.WriteAllText(existingPath, "保留");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.ConvertSeWzc(
            new DictionaryConvertSeWzcOptions { SourceDirectory = sourceDirectory, TargetDirectory = targetDirectory },
            output,
            error);

        Assert.Equal(4, exitCode);
        Assert.Equal("保留", File.ReadAllText(existingPath));
    }

    private static string CreateNativeSources(string root)
    {
        var sourceDirectory = Path.Combine(root, "native-sources");
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "phonetic"));
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "shape"));
        Directory.CreateDirectory(Path.Combine(sourceDirectory, "symbols"));
        File.WriteAllText(
            Path.Combine(sourceDirectory, "phonetic", "sewzc-default.phonetic.tsv"),
            "你好\tni hao\t100");
        XiaoXiImeProjectTerms.WriteTo(Path.Combine(sourceDirectory, XiaoXiImeProjectTerms.OutputRelativePath));
        File.WriteAllText(Path.Combine(sourceDirectory, "shape", "sewzc-moqi.shape.tsv"), "你\trb\t亻尔");
        File.WriteAllText(Path.Combine(sourceDirectory, "symbols", "sewzc-default.symbols.tsv"), "/xh\t★\t☆");
        return sourceDirectory;
    }

    private static string CreateSeWzcSnapshot(string root)
    {
        var sourceDirectory = Path.GetFileName(root) == "data"
            ? Path.Combine(root, "dictionaries")
            : Path.Combine(root, "snapshot");
        var phoneticDirectory = Path.Combine(sourceDirectory, "phonetic");
        var shapeDirectory = Path.Combine(sourceDirectory, "shape");
        var symbolDirectory = Path.Combine(sourceDirectory, "symbols");
        Directory.CreateDirectory(phoneticDirectory);
        Directory.CreateDirectory(shapeDirectory);
        Directory.CreateDirectory(symbolDirectory);
        File.WriteAllText(Path.Combine(phoneticDirectory, "rime-frost-8105.dict.yaml"), "---\n...\n你\tni\t10");
        File.WriteAllText(Path.Combine(phoneticDirectory, "rime-frost-base.dict.yaml"), "你好\tni hao\t100\n均订\tjunding\t50");
        File.WriteAllText(Path.Combine(phoneticDirectory, "rime-frost-corrections.dict.yaml"), "角色\tjiao se\t0\tjué sè");
        File.WriteAllText(Path.Combine(phoneticDirectory, "project-terms.dict.yaml"), "小希输入法\txiao xi shu ru fa\t1000");
        File.WriteAllText(Path.Combine(shapeDirectory, "moqi_chaifen.txt"), "你\trb\t亻尔");
        File.WriteAllText(Path.Combine(symbolDirectory, "default.symbols.tsv"), "/xh\t★\t☆");
        return sourceDirectory;
    }

    private static string CreateRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Cli.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
