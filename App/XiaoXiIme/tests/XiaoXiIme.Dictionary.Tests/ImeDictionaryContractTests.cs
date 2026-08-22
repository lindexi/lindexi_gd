using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class ImeDictionaryContractTests
{
    [Fact]
    public void Query_WhenExactAndPrefixThenExactCandidateIsFirst()
    {
        var dictionary = new InMemoryImeDictionary(
        [
            new ImeCandidate("你", "ni", 10),
            new ImeCandidate("你好", "nihao", 100),
        ]);

        var candidates = dictionary.Query(new ImeDictionaryQuery("ni", MatchMode: ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Equal("你", candidates[0].Text);
    }

    [Fact]
    public void Query_WhenExactAndPrefixThenRespectsMaxCount()
    {
        var dictionary = new InMemoryImeDictionary(
        [
            new ImeCandidate("你", "ni", 100),
            new ImeCandidate("你好", "nihao", 90),
        ]);

        var candidates = dictionary.Query(new ImeDictionaryQuery("ni", 1, ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Single(candidates);
    }

    [Fact]
    public void Query_WhenExactAndPrefixThenUsesStableOrdering()
    {
        var dictionary = new InMemoryImeDictionary(
        [
            new ImeCandidate("你们", "nimen", 80),
            new ImeCandidate("你好", "nihao", 80),
        ]);

        var candidates = dictionary.Query(new ImeDictionaryQuery("ni", MatchMode: ImeDictionaryMatchMode.ExactAndPrefix));

        Assert.Collection(
            candidates,
            candidate => Assert.Equal("你们", candidate.Text),
            candidate => Assert.Equal("你好", candidate.Text));
    }

    [Fact]
    public void Query_WhenRequestIsNullThenThrows()
    {
        var dictionary = new InMemoryImeDictionary([]);

        Assert.Throws<ArgumentNullException>(() => dictionary.Query((ImeDictionaryQuery)null!));
    }

    [Fact]
    public void PackageManifest_UsesCurrentContractDefaults()
    {
        var manifest = new DictionaryPackageManifest();

        Assert.Equal(DictionaryPackageManifest.CurrentFormatVersion, manifest.FormatVersion);
    }

    [Fact]
    public void PackageManifest_UsesExpectedPackageKindByDefault()
    {
        var manifest = new DictionaryPackageManifest();

        Assert.Equal(DictionaryPackageManifest.ExpectedPackageKind, manifest.PackageKind);
    }

    [Fact]
    public void SourceFormat_UsesPhoneticTsvExtension()
    {
        Assert.Equal(".phonetic.tsv", ImeDictionarySourceFormat.PhoneticFileExtension);
    }
}
