using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(ProductionDictionaryCollection))]
public class ProductionDictionaryKeystrokeTests(ProductionDictionaryFixture fixture)
{
    [Theory(Timeout = 30_000)]
    [InlineData("nihao", "你好")]
    [InlineData("zhongguo", "中国")]
    [InlineData("women", "我们")]
    [InlineData("shijie", "世界")]
    [InlineData("shurufa", "输入法")]
    [InlineData("xiaoxishurufa", "小希输入法")]
    [InlineData("xiaoxiaimuyi", "XiaoXiIme")]
    public async Task FullPinyin_WhenKeysAreProcessedThroughIpcThenExpectedFirstCandidateIsCommitted(
        string input,
        string expectedText)
    {
        await AssertInputCommitsExpectedTextAsync(
            input,
            expectedText,
            DictionaryPackageLocations.FullPinyinInputScheme);
    }

    [Theory(Timeout = 30_000)]
    [InlineData("nihc", "你好")]
    [InlineData("vsgo", "中国")]
    [InlineData("womf", "我们")]
    [InlineData("uijp", "世界")]
    [InlineData("uurufa", "输入法")]
    [InlineData("xnxiuurufa", "小希输入法")]
    [InlineData("xnxiaimuyi", "XiaoXiIme")]
    public async Task XiaoheDoublePinyin_WhenKeysAreProcessedThroughIpcThenExpectedFirstCandidateIsCommitted(
        string input,
        string expectedText)
    {
        await AssertInputCommitsExpectedTextAsync(
            input,
            expectedText,
            DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
    }

    [Fact(Timeout = 30_000)]
    public async Task FullPinyin_WhenPrefixBecomesExactThenExactWordMovesToFirstCandidate()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_ProductionDictionary_{Guid.NewGuid():N}");
        using var host = fixture.CreateHost(options, DictionaryPackageLocations.FullPinyinInputScheme);
        using var client = new XiaoXiImeIpcClient(options);
        host.Start();
        await client.ConnectAsync();

        var prefix = await ProcessCharactersAsync(client, "xiaoxiaimuy");
        var exact = await ProcessCharactersAsync(client, "i");

        Assert.Contains(prefix.Snapshot.Candidates, candidate => candidate.Text == "XiaoXiIme");
        Assert.Equal("XiaoXiIme", exact.Snapshot.Candidates[0].Text);
    }

    [Fact(Timeout = 30_000)]
    public async Task FullPinyin_WhenNBecomesNiThenCompositionAndCandidatesUpdateWithoutCommit()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_ProductionDictionary_{Guid.NewGuid():N}");
        using var host = fixture.CreateHost(options, DictionaryPackageLocations.FullPinyinInputScheme);
        using var client = new XiaoXiImeIpcClient(options);
        host.Start();
        await client.ConnectAsync();

        var prefix = await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var exact = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        var status = await client.GetHostStatusAsync();

        Assert.True(prefix.Handled);
        Assert.True(prefix.Snapshot.IsComposing);
        Assert.Equal("n", prefix.Snapshot.Composition.Reading);
        Assert.Contains(prefix.Snapshot.Candidates, candidate => candidate.Text == "你");
        Assert.Null(prefix.CommitText);
        Assert.True(exact.Handled);
        Assert.True(exact.Snapshot.IsComposing);
        Assert.Equal("ni", exact.Snapshot.Composition.Reading);
        Assert.Equal("你", exact.Snapshot.Candidates[0].Text);
        Assert.Null(exact.CommitText);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.FullPinyinInputScheme, status.DictionaryInputScheme);
    }

    [Fact(Timeout = 30_000)]
    public async Task FullPinyin_WhenEscapeCancelsNiThenCompositionClearsWithoutCommit()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_ProductionDictionary_{Guid.NewGuid():N}");
        using var host = fixture.CreateHost(options, DictionaryPackageLocations.FullPinyinInputScheme);
        using var client = new XiaoXiImeIpcClient(options);
        host.Start();
        await client.ConnectAsync();
        await ProcessCharactersAsync(client, "ni");

        var cancel = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Escape));
        var snapshot = await client.GetSnapshotAsync();
        var status = await client.GetHostStatusAsync();

        Assert.True(cancel.Handled);
        Assert.Null(cancel.CommitText);
        Assert.False(cancel.Snapshot.IsComposing);
        Assert.Equal(string.Empty, cancel.Snapshot.Composition.Reading);
        Assert.Empty(cancel.Snapshot.Candidates);
        Assert.False(snapshot.IsComposing);
        Assert.Equal(string.Empty, snapshot.Composition.Reading);
        Assert.Empty(snapshot.Candidates);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.FullPinyinInputScheme, status.DictionaryInputScheme);
    }

    [Fact(Timeout = 30_000)]
    public async Task FullPinyin_WhenNiIsRetypedAfterCancelThenSpaceCommitsOnce()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_ProductionDictionary_{Guid.NewGuid():N}");
        using var host = fixture.CreateHost(options, DictionaryPackageLocations.FullPinyinInputScheme);
        using var client = new XiaoXiImeIpcClient(options);
        host.Start();
        await client.ConnectAsync();
        await ProcessCharactersAsync(client, "ni");
        var cancel = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Escape));

        var composing = await ProcessCharactersAsync(client, "ni");
        var commit = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));
        var secondSpace = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));
        var status = await client.GetHostStatusAsync();

        Assert.Null(cancel.CommitText);
        Assert.Null(composing.CommitText);
        Assert.Equal("ni", composing.Snapshot.Composition.Reading);
        Assert.Equal("你", composing.Snapshot.Candidates[0].Text);
        Assert.True(commit.Handled);
        Assert.Equal("你", commit.CommitText);
        Assert.False(commit.Snapshot.IsComposing);
        Assert.Equal(string.Empty, commit.Snapshot.Composition.Reading);
        Assert.Empty(commit.Snapshot.Candidates);
        Assert.Null(secondSpace.CommitText);
        Assert.False(secondSpace.Snapshot.IsComposing);
        Assert.Empty(secondSpace.Snapshot.Candidates);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(DictionaryPackageLocations.FullPinyinInputScheme, status.DictionaryInputScheme);
    }

    private async Task AssertInputCommitsExpectedTextAsync(string input, string expectedText, string inputScheme)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_ProductionDictionary_{Guid.NewGuid():N}");
        using var host = fixture.CreateHost(options, inputScheme);
        using var client = new XiaoXiImeIpcClient(options);
        host.Start();
        await client.ConnectAsync();

        var result = await ProcessCharactersAsync(client, input);
        var status = await client.GetHostStatusAsync();
        var commit = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));

        Assert.True(result.Handled);
        Assert.Equal(input, result.Snapshot.Composition.Reading);
        Assert.Equal(expectedText, result.Snapshot.Candidates[0].Text);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Equal(inputScheme, status.DictionaryInputScheme);
        Assert.Equal(expectedText, commit.CommitText);
        Assert.False(commit.Snapshot.IsComposing);
    }

    private static async Task<ImeProcessResult> ProcessCharactersAsync(XiaoXiImeIpcClient client, string input)
    {
        ImeProcessResult result = default!;
        foreach (var character in input)
        {
            result = await client.ProcessKeyAsync(ImeKey.FromCharacter(character));
        }

        return result;
    }
}

[CollectionDefinition(nameof(ProductionDictionaryCollection), DisableParallelization = true)]
public sealed class ProductionDictionaryCollection : ICollectionFixture<ProductionDictionaryFixture>;

public sealed class ProductionDictionaryFixture : IDisposable
{
    private readonly string userDictionaryRoot;

    public ProductionDictionaryFixture()
    {
        HostRoot = Path.Combine(Path.GetTempPath(), "XiaoXiIme.IntegrationTests", Guid.NewGuid().ToString("N"));
        userDictionaryRoot = Path.Combine(HostRoot, "users");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        CompilePackage(DictionaryPackageLocations.FullPinyinInputScheme);
        FullPinyinBuildTime = watch.Elapsed;
        watch.Restart();
        CompilePackage(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
        XiaoheBuildTime = watch.Elapsed;
    }

    public string HostRoot { get; }
    public TimeSpan FullPinyinBuildTime { get; }
    public TimeSpan XiaoheBuildTime { get; }

    public ImeHostService CreateHost(XiaoXiImeIpcOptions options, string inputScheme)
    {
        return new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: HostRoot,
            userDictionaryPath: Path.Combine(userDictionaryRoot, $"{Guid.NewGuid():N}.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(HostRoot))
        {
            Directory.Delete(HostRoot, recursive: true);
        }
    }

    private void CompilePackage(string inputScheme)
    {
        var sourceRoot = Path.Combine(AppContext.BaseDirectory, "DictionarySources");
        var phoneticSources = Directory.GetFiles(sourceRoot, "*.phonetic.tsv", SearchOption.AllDirectories);
        var shapeSources = Directory.GetFiles(sourceRoot, "*.shape.tsv", SearchOption.AllDirectories);
        var symbolSources = Directory.GetFiles(sourceRoot, "*.symbols.tsv", SearchOption.AllDirectories);
        var packagePath = DictionaryPackageLocations.ResolvePackageDirectory(
            inputScheme: inputScheme,
            baseDirectory: HostRoot);

        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate
            {
                PhoneticSourcePaths = phoneticSources,
                ShapeSourcePaths = shapeSources,
                SymbolSourcePaths = symbolSources,
                Parameters = new DictionaryPackageParameters { InputScheme = inputScheme },
            },
            packagePath);
    }
}
