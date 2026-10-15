using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class TextTokenizerTests
{
    [DataTestMethod]
    [DataRow("Hello world!")]
    [DataRow("递归网络训练与生成。")]
    [DataRow("代码 C#\nemoji 😀\t123")]
    public async Task RealVocabularyRoundTripsTextAsync(string text)
    {
        var tokenizer = await LoadAsync();
        Assert.AreEqual(text, tokenizer.Decode(tokenizer.Encode(text)));
    }

    [TestMethod]
    public async Task SameAssetsProduceSameIdentityAsync()
    {
        var first = await LoadAsync();
        var second = await LoadAsync();
        Assert.AreEqual(first.Identity, second.Identity);
    }

    [TestMethod]
    public async Task SpecialTokensUseConfiguredIdentityAsync()
    {
        var tokenizer = await LoadAsync();
        Assert.AreEqual(tokenizer.EosTokenId, tokenizer.Encode("<|im_end|>")[0]);
    }

    [TestMethod]
    public async Task DatasetExplicitlyAppendsEosAsync()
    {
        var tokenizer = await LoadAsync();
        var dataset = tokenizer.CreateDataset(new[] { "Hello" });
        Assert.AreEqual(tokenizer.EosTokenId, dataset[0][^1]);
    }

    [TestMethod]
    public async Task EncodingHonorsTokenLimitAsync()
    {
        var tokenizer = await LoadAsync();
        Assert.AreEqual(1, tokenizer.Encode("Hello world!", 1).Count);
    }

    [TestMethod]
    public async Task FileBudgetIsEnforcedAsync()
    {
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxFileBytes: 1));
    }

    [TestMethod]
    public async Task TextBudgetIsEnforcedAsync()
    {
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxTextCharacters: 2);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tokenizer.Encode("Hello"));
    }

    [TestMethod]
    public async Task RealTextDatasetTrainsAndRestoresAsync()
    {
        var tokenizer = await LoadAsync();
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        var network = new RecursiveNetwork(graph);
        var encoding = Enumerable.Range(0, tokenizer.VocabularySize)
            .Select(id => (IReadOnlyList<double>)new[] { 2.0 * id / tokenizer.VocabularySize - 1 }).ToArray();
        network.BindText(new TokenBindings(tokenizer.Identity, new[] { "n" }, new[] { "n" }, encoding));
        var dataset = tokenizer.CreateDataset(new[] { "Hello", "你好" }, 16);
        network.ConfigureTrainingJob(dataset);
        network.TrainNextSample(dataset);
        var restored = RecursiveNetwork.ImportCheckpoint(network.ExportCheckpoint());
        network.TrainNextSample(dataset);
        restored.TrainNextSample(dataset);
        CollectionAssert.AreEqual(network.ExportCheckpoint(), restored.ExportCheckpoint());
    }

    [TestMethod]
    public async Task CacheEntryLimitSurvivesDistinctInputsAsync()
    {
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxCacheEntries: 2);
        foreach (var text in new[] { "alpha", "beta", "gamma", "delta", "中文" }) tokenizer.Encode(text);
        Assert.IsTrue(tokenizer.CachedEntryCount <= 2);
    }

    [TestMethod]
    public async Task CacheCharacterBudgetIsEnforcedAsync()
    {
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxCacheCharacters: 8);
        foreach (var text in new[] { "ab", "cd", "ef", "abcdefghi" }) tokenizer.Encode(text);
        Assert.IsTrue(tokenizer.CachedCharacterCount <= 8);
    }

    [TestMethod]
    public async Task OversizedCacheEntryIsNotStoredAsync()
    {
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxCacheCharacters: 2);
        tokenizer.Encode("abcdefgh");
        Assert.AreEqual(0, tokenizer.CachedEntryCount);
    }

    [TestMethod]
    public async Task DisabledCacheProducesSameTokensAsync()
    {
        var cached = await LoadAsync();
        var uncached = await MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxCacheEntries: 0);
        CollectionAssert.AreEqual(cached.Encode("你好 world 😀").ToArray(), uncached.Encode("你好 world 😀").ToArray());
    }

    [TestMethod]
    public async Task EvictionDoesNotChangeEncodingAsync()
    {
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxCacheEntries: 1);
        var expected = tokenizer.Encode("alpha").ToArray();
        tokenizer.Encode("beta gamma delta");
        CollectionAssert.AreEqual(expected, tokenizer.Encode("alpha").ToArray());
    }

    [TestMethod]
    public async Task CacheSettingsDoNotChangeTokenizerIdentityAsync()
    {
        var cached = await LoadAsync();
        var uncached = await MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxCacheEntries: 0);
        Assert.AreEqual(cached.Identity, uncached.Identity);
    }

    private static string Asset(string name) => Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", name);
    private static Task<MiniMindTextTokenizer> LoadAsync() => MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"));
}
