using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class UserDictionaryTests
{
    [Fact]
    public void Learn_WhenCandidateSelectedThenPromotesItAboveSystemCandidates()
    {
        var dictionary = CreateDictionary();

        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("呢", "ni", 60), " NI "));
        var result = dictionary.Query("ni");

        Assert.Equal("呢", result[0].Text);
    }

    [Fact]
    public void Learn_WhenSameNormalizedReadingSelectedThenIncrementsOneEntry()
    {
        var dictionary = CreateDictionary();

        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("呢", "ni"), "NI"));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("呢", "ni"), " ni "));
        var entries = dictionary.GetEntries();

        Assert.Equal(2, entries[0].SelectionCount);
    }

    [Fact]
    public void Learn_WhenSameTextHasDifferentReadingThenKeepsSeparateEntries()
    {
        var dictionary = CreateDictionary();

        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("行", "xing"), "xing"));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("行", "hang"), "hang"));
        var entries = dictionary.GetEntries();

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void Forget_WhenMatchingUserWordExistsThenRemovesIt()
    {
        var dictionary = CreateDictionary();
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("自定义", "zidingyi"), "zidingyi"));

        var forgotten = dictionary.Forget("自定义", " ZiDingYi ");
        var result = dictionary.Query("zidingyi");

        Assert.True(forgotten);
        Assert.DoesNotContain(result, candidate => candidate.Text == "自定义");
        Assert.Empty(dictionary.GetEntries());
    }

    [Fact]
    public void Forget_WhenUserWordIsMissingThenReturnsFalse()
    {
        var dictionary = CreateDictionary();

        var forgotten = dictionary.Forget("自定义", "zidingyi");

        Assert.False(forgotten);
    }

    [Fact]
    public void Learn_WhenPhraseCandidateSelectedThenContinuousInputPromotesIt()
    {
        var dictionary = new UserDictionary(new InMemoryImeDictionary(
        [
            new ImeCandidate("你号", "ni hao", 200),
            new ImeCandidate("你好", "ni hao", 100),
        ]));

        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("你好", "ni hao", 100), "nihao"));
        var result = dictionary.Query("nihao");

        Assert.Equal("你好", result[0].Text);
    }

    [Fact]
    public void Query_WhenUserExactAndSystemPrefixThenUserExactIsFirst()
    {
        var dictionary = new UserDictionary(new InMemoryImeDictionary(
        [
            new ImeCandidate("你", "ni", 100),
            new ImeCandidate("你好", "ni hao", 200),
        ]));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("呢", "ni"), "ni"));

        var result = dictionary.Query(new ImeDictionaryQuery("ni", MatchMode: ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Collection(
            result,
            candidate => Assert.Equal("呢", candidate.Text),
            candidate => Assert.Equal("你", candidate.Text),
            candidate => Assert.Equal("你好", candidate.Text));
    }

    [Fact]
    public void Query_WhenUserAndSystemContainSameCandidateThenDoesNotDuplicateIt()
    {
        var dictionary = CreateDictionary();
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("你", "ni"), "ni"));

        var result = dictionary.Query("ni");

        Assert.Single(result, candidate => candidate.Text == "你");
    }

    [Fact]
    public void Query_WhenDoublePinyinUserEntryMatchesSystemCandidateThenDoesNotDuplicateIt()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("你好", "ni hao", 100)],
            packagePath,
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        var dictionary = new UserDictionary(DictionaryPackageLoader.Load(packagePath));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("你好", "ni hao"), "nihc"));

        var result = dictionary.Query("nihc");

        Assert.Single(result, candidate => candidate.Text == "你好");
    }

    [Fact]
    public void QueryByText_WhenUserWordExistsThenRanksUserReadingBeforeSystemCanonicalReading()
    {
        var dictionary = new UserDictionary(new InMemoryImeDictionary(
        [
            new ImeCandidate("小希", "xx", 100),
            new ImeCandidate("小希", "xiao xi", 100),
        ]));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("小希", "xx"), "xx"));

        var result = dictionary.QueryByText("小希");

        Assert.Collection(
            result,
            candidate =>
            {
                Assert.Equal("小希", candidate.Text);
                Assert.Equal("xx", candidate.Reading);
            },
            candidate =>
            {
                Assert.Equal("小希", candidate.Text);
                Assert.Equal("xiao xi", candidate.Reading);
            });
    }

    [Fact]
    public void QueryShape_WhenSystemDictionarySupportsShapeThenDelegates()
    {
        var dictionary = new UserDictionary(new MultiResourceDictionary());

        var result = dictionary.QueryShape("a");

        Assert.Collection(result, entry => Assert.Equal("你", entry.Text));
    }

    [Fact]
    public void QuerySymbols_WhenSystemDictionarySupportsSymbolsThenDelegates()
    {
        var dictionary = new UserDictionary(new MultiResourceDictionary());

        var result = dictionary.QuerySymbols("/xh");

        Assert.Collection(result, candidate => Assert.Equal("★", candidate.Text));
    }

    [Fact]
    public void Save_WhenLoadedThenPreservesEntries()
    {
        var path = CreatePath();
        var entries = new[] { new UserDictionaryEntry("呢", "ni", 3) };

        UserDictionaryStore.Save(path, entries);
        var result = UserDictionaryStore.Load(path);

        Assert.Equal(entries, result.Entries);
    }

    [Fact]
    public void Load_WhenFileIsCorruptThenIsolatesItAndReturnsEmptyEntries()
    {
        var path = CreatePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "not-json");

        var result = UserDictionaryStore.Load(path);

        Assert.Empty(result.Entries);
        Assert.NotNull(result.Error);
        Assert.NotNull(result.IsolatedCorruptFilePath);
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(result.IsolatedCorruptFilePath));
    }

    [Fact]
    public void DefaultFilePath_IsOutsideInstalledPackageDirectory()
    {
        var path = UserDictionaryLocations.GetDefaultFilePath();

        Assert.Equal(UserDictionaryLocations.FileName, Path.GetFileName(path));
        Assert.Contains(Path.DirectorySeparatorChar + UserDictionaryLocations.ApplicationDirectoryName + Path.DirectorySeparatorChar, path + Path.DirectorySeparatorChar);
        Assert.DoesNotContain("XiaoXiIme.DictionaryPackage", path, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.SystemDirectory, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Purge_WhenCorruptCopyExistsThenDeletesLearnedData()
    {
        var path = CreatePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "not-json");
        var isolatedPath = UserDictionaryStore.Load(path).IsolatedCorruptFilePath!;
        File.WriteAllText(path, "{}");

        UserDictionaryLocations.Purge(path);

        Assert.False(File.Exists(path));
        Assert.False(File.Exists(isolatedPath));
    }

    private sealed class MultiResourceDictionary : IImeDictionary, IShapeDictionary, ISymbolDictionary
    {
        public IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query) => [];

        public IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 9) =>
            [new ShapeDictionaryEntry("你", "ab", "亻尔")];

        public IReadOnlyList<ImeCandidate> FilterByShape(
            IReadOnlyList<ImeCandidate> candidates,
            string shapeCode,
            int maxCount = 9) => candidates.Take(maxCount).ToArray();

        public IReadOnlyList<ImeCandidate> QuerySymbols(string input, int maxCount = 9) =>
            [new ImeCandidate("★", input)];
    }

    private static UserDictionary CreateDictionary()
    {
        return new UserDictionary(new InMemoryImeDictionary(
        [
            new ImeCandidate("你", "ni", 100),
            new ImeCandidate("呢", "ni", 60),
        ]));
    }

    private static string CreatePath()
    {
        return Path.Combine(Path.GetTempPath(), $"XiaoXiIme_UserDictionary_{Guid.NewGuid():N}", "user-dictionary.json");
    }
}
