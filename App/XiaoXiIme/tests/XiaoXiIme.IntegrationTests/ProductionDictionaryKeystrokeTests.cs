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
        CompilePackage(DictionaryPackageLocations.FullPinyinInputScheme);
        CompilePackage(DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);
    }

    public string HostRoot { get; }

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
