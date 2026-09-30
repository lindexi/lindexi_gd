using System.Text.Json;

namespace XiaoXiIme.Dictionary.Tests;

public class DictionaryPackageTests
{
    [Fact]
    public void CompileAndLoad_WhenPackageIsValidThenQueriesExactAndPrefixCandidates()
    {
        var packagePath = CreatePackage(
        [
            new PhoneticDictionaryEntry("你", "ni", 100),
            new PhoneticDictionaryEntry("呢", "ni", 80),
            new PhoneticDictionaryEntry("你好", "ni hao", 200),
        ]);
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.Query(new ImeDictionaryQuery("NI", MatchMode: ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Collection(
            candidates,
            candidate => Assert.Equal("你", candidate.Text),
            candidate => Assert.Equal("呢", candidate.Text),
            candidate => Assert.Equal("你好", candidate.Text));
    }

    [Fact]
    public void CompileAndLoad_WhenConcatenatedReadingIsCanonicalizedThenXiaoheQueryMatches()
    {
        using var reader = new StringReader("均订\tjunding\t50");
        var entries = PhoneticDictionarySourceParser.Parse(reader, "core.phonetic.tsv");
        var packagePath = CreatePackage(entries, new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.Query(new ImeDictionaryQuery("jydk"));

        Assert.Equal("均订", Assert.Single(candidates).Text);
        Assert.Equal("jun ding", candidates[0].Reading);
    }

    [Fact]
    public void CompileAndLoad_WhenSingleTokenAbbreviationCannotBeProjectedThenUsesRawLookupKey()
    {
        var packagePath = CreatePackage(
            [new PhoneticDictionaryEntry("小希", "xx", 100)],
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.Query(new ImeDictionaryQuery("xx"));

        Assert.Equal("小希", Assert.Single(candidates).Text);
        Assert.Equal("xx", candidates[0].Reading);
    }

    [Fact]
    public void CompileAndLoad_WhenPhraseReadingContainsSyllableSpacesThenContinuousInputMatchesExactly()
    {
        var packagePath = CreatePackage(
        [
            new PhoneticDictionaryEntry("你好", "ni hao", 200),
        ]);
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.Query(new ImeDictionaryQuery("nihao"));

        Assert.Single(candidates);
        Assert.Equal("你好", candidates[0].Text);
    }

    [Fact]
    public void CompileAndLoad_WhenContinuousInputIsPrefixThenReturnsPhraseCandidate()
    {
        var packagePath = CreatePackage(
        [
            new PhoneticDictionaryEntry("你好", "ni hao", 200),
        ]);
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.Query(new ImeDictionaryQuery("nih", MatchMode: ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Single(candidates);
        Assert.Equal("你好", candidates[0].Text);
    }

    [Fact]
    public void CompileAndLoad_WhenXiaoheDoublePinyinThenQueriesExactAndPrefixCandidates()
    {
        var packagePath = CreatePackage(
            [
                new PhoneticDictionaryEntry("你好", "ni hao", 200),
                new PhoneticDictionaryEntry("中国", "zhong guo", 180),
            ],
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var exactCandidates = dictionary.Query(new ImeDictionaryQuery("nihc"));
        var prefixCandidates = dictionary.Query(new ImeDictionaryQuery("vs", MatchMode: ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Equal("你好", Assert.Single(exactCandidates).Text);
        Assert.Equal("中国", Assert.Single(prefixCandidates).Text);
    }

    [Fact]
    public void CompileAndLoad_WhenQueryByTextThenReturnsCanonicalReading()
    {
        var packagePath = CreatePackage(XiaoXiImeProjectTerms.Parse());
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.QueryByText("XiaoXiIme");

        Assert.Equal("xiao xi ai mu yi", Assert.Single(candidates).Reading);
        Assert.Equal("XiaoXiIme", candidates[0].Text);
    }

    [Fact]
    public void Compile_WhenXiaoheDoublePinyinThenManifestRecordsInputSchemeAndCandidateKeepsFullPinyinReading()
    {
        var packagePath = CreatePackage(
            [new PhoneticDictionaryEntry("你好", "ni hao", 200)],
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        var manifest = JsonSerializer.Deserialize<DictionaryPackageManifest>(
            File.ReadAllText(Path.Combine(packagePath, "manifest.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        Assert.Equal("xiaoheDoublePinyin", manifest!.Parameters.InputScheme);
        Assert.Equal("xiaoheDoublePinyin", dictionary.InputScheme);
        Assert.Equal("ni hao", Assert.Single(dictionary.Query(new ImeDictionaryQuery("nihc"))).Reading);
    }

    [Theory]
    [InlineData("zhong", "vs")]
    [InlineData("chi", "ii")]
    [InlineData("shi", "ui")]
    [InlineData("ang", "ah")]
    [InlineData("er", "er")]
    public void CompileAndLoad_WhenXiaoheSyllableIsSupportedThenUsesExpectedKey(string reading, string input)
    {
        var packagePath = CreatePackage(
            [new PhoneticDictionaryEntry("字", reading, 100)],
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.Query(new ImeDictionaryQuery(input));

        Assert.Single(candidates);
    }

    [Fact]
    public void Compile_WhenReadingCannotBeProjectedThenReportsReadingAndInputScheme()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"));

        var exception = Assert.Throws<ArgumentException>(() => DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("嗯好", "ng hao", 100)],
            path,
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" }));

        Assert.Contains("'ng hao'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'xiaoheDoublePinyin'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_WhenInputSchemeIsUnsupportedThenRejectsParameters()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"));

        Assert.Throws<ArgumentException>(() => DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("你", "ni", 100)],
            path,
            new DictionaryPackageParameters { InputScheme = "unsupported" }));
    }

    [Fact]
    public void Load_WhenManifestInputSchemeIsUnsupportedThenRejectsPackage()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        UpdateManifest(packagePath, root => root["parameters"]!["inputScheme"] = "unsupported");

        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(packagePath));
    }

    [Fact]
    public void Inspect_WhenPackageIsValidThenReturnsManifestWithoutKeepingFilesOpen()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);

        var inspection = DictionaryPackageLoader.Inspect(packagePath);

        Assert.Equal("XiaoXiImeDictionary", inspection.Manifest.PackageKind);
        Assert.Equal(DictionaryPackageManifest.CurrentCompilerVersion, inspection.Manifest.CompilerVersion);
        Assert.Equal(1, inspection.Manifest.Counts.Candidates);
        File.Delete(Path.Combine(packagePath, "candidates.bin"));
        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(packagePath));
    }

    [Fact]
    public void Load_WhenCompilerVersionIsUnsupportedThenRejectsPackage()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        UpdateManifest(packagePath, root => root["compilerVersion"] = "unsupported");

        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(packagePath));
    }

    [Fact]
    public void CompileAndLoad_WhenShapeAndSymbolsArePresentThenQueriesIndependentResources()
    {
        var packagePath = CreatePackage(
            [
                new PhoneticDictionaryEntry("你", "ni", 100),
                new PhoneticDictionaryEntry("妮", "ni", 90),
            ],
            shapeEntries:
            [
                new ShapeDictionaryEntry("妮", "vbg", "女尔"),
                new ShapeDictionaryEntry("你", "wqiy", "亻尔"),
            ],
            symbolEntries:
            [
                new SymbolDictionaryEntry("fh", ["，", "。"]),
            ]);
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var shapeCandidates = dictionary.QueryShape("V");
        var filteredCandidates = dictionary.FilterByShape(dictionary.Query(new ImeDictionaryQuery("ni")), "w");
        var symbols = dictionary.QuerySymbols("FH");

        Assert.Equal("妮", Assert.Single(shapeCandidates).Text);
        Assert.Equal("你", Assert.Single(filteredCandidates).Text);
        Assert.Equal(["，", "。"], symbols.Select(candidate => candidate.Text));
    }

    [Fact]
    public void Compile_WhenInputOrderChangesThenWritesIdenticalShards()
    {
        var firstPath = CreatePackage(
        [
            new PhoneticDictionaryEntry("呢", "ni", 80),
            new PhoneticDictionaryEntry("你好", "ni hao", 200),
            new PhoneticDictionaryEntry("你", "ni", 100),
        ]);
        var secondPath = CreatePackage(
        [
            new PhoneticDictionaryEntry("你", "ni", 100),
            new PhoneticDictionaryEntry("呢", "ni", 80),
            new PhoneticDictionaryEntry("你好", "ni hao", 200),
        ]);

        var first = ReadShardBytes(firstPath);
        var second = ReadShardBytes(secondPath);

        Assert.Equal(first, second);
    }

    [Fact]
    public void CompileAndLoad_WhenPrefixCandidatesShareFrequencyThenOrdersByLookupLengthThenText()
    {
        var packagePath = CreatePackage(
        [
            new PhoneticDictionaryEntry("你们", "ni men", 80),
            new PhoneticDictionaryEntry("你好", "ni hao", 80),
            new PhoneticDictionaryEntry("泥", "ni", 80),
        ]);
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.Query(new ImeDictionaryQuery("ni", MatchMode: ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Collection(
            candidates,
            candidate => Assert.Equal("泥", candidate.Text),
            candidate => Assert.Equal("你们", candidate.Text),
            candidate => Assert.Equal("你好", candidate.Text));
    }

    [Fact]
    public void Compile_WhenPrefixLimitIsOneThenBoundsPrefixCandidates()
    {
        var packagePath = CreatePackage(
            [
                new PhoneticDictionaryEntry("你好", "ni hao", 100),
                new PhoneticDictionaryEntry("你们", "ni men", 90),
            ],
            new DictionaryPackageParameters { MaxPrefixCandidatesPerKey = 1 });
        var dictionary = DictionaryPackageLoader.Load(packagePath);

        var candidates = dictionary.Query(new ImeDictionaryQuery("ni", MatchMode: ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Single(candidates);
    }

    [Fact]
    public void Load_WhenShardIsMissingThenRejectsPackage()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        File.Delete(Path.Combine(packagePath, "candidates.bin"));

        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(packagePath));
    }

    [Fact]
    public void Load_WhenShardLengthDiffersThenRejectsPackage()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        File.AppendAllBytes(Path.Combine(packagePath, "candidates.bin"), [0]);

        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(packagePath));
    }

    [Theory]
    [InlineData("formatVersion", 999)]
    [InlineData("packageKind", "OtherDictionary")]
    public void Load_WhenManifestContractIsUnsupportedThenRejectsPackage(string propertyName, object value)
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        UpdateManifest(packagePath, root => root[propertyName] = JsonSerializer.SerializeToNode(value));

        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(packagePath));
    }

    [Fact]
    public void Load_WhenShardPathEscapesPackageThenRejectsPackage()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        UpdateManifest(packagePath, root => root["shards"]![0]!["path"] = "../candidates.bin");

        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(packagePath));
    }

    [Fact]
    public void Load_WhenCandidateCountExceedsLimitThenRejectsBeforeReadingShard()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        UpdateManifest(packagePath, root => root["counts"]!["candidates"] = int.MaxValue);

        Assert.Throws<DictionaryPackageException>(() => DictionaryPackageLoader.Load(packagePath));
    }

    private static string CreatePackage(
        IReadOnlyList<PhoneticDictionaryEntry> entries,
        DictionaryPackageParameters? parameters = null,
        IReadOnlyList<ShapeDictionaryEntry>? shapeEntries = null,
        IReadOnlyList<SymbolDictionaryEntry>? symbolEntries = null)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(
            entries,
            path,
            parameters,
            shapeEntries: shapeEntries,
            symbolEntries: symbolEntries);
        return path;
    }

    private static byte[] ReadShardBytes(string packagePath)
    {
        return Directory.EnumerateFiles(packagePath, "*.bin")
            .OrderBy(path => path, StringComparer.Ordinal)
            .SelectMany(File.ReadAllBytes)
            .ToArray();
    }

    private static void UpdateManifest(string packagePath, Action<System.Text.Json.Nodes.JsonObject> update)
    {
        var manifestPath = Path.Combine(packagePath, "manifest.json");
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
        update(root);
        File.WriteAllText(manifestPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
