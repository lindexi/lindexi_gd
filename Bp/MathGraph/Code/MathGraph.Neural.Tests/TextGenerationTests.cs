using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class TextGenerationTests
{
    [TestMethod]
    public async Task TextSessionMatchesTokenSessionAsync()
    {
        var tokenizer = await LoadAsync();
        var executor = Create(tokenizer).CreateTokenExecutor();
        var text = executor.CreateTextSession(tokenizer, "Hello", 2, seed: 7);
        var tokens = executor.CreateGenerationSession(tokenizer.Encode("Hello"), 2, seed: 7, eosToken: tokenizer.EosTokenId);
        Assert.AreEqual(tokens.NextToken(), text.NextToken());
    }

    [TestMethod]
    public async Task TextRestorePreservesNextTokenAsync()
    {
        var tokenizer = await LoadAsync();
        var model = Create(tokenizer);
        var original = model.CreateTokenExecutor().CreateTextSession(tokenizer, "你好", 3, seed: 31);
        var restored = RecursiveNetwork.ImportCheckpoint(model.ExportCheckpoint()).CreateTokenExecutor()
            .RestoreTextSession(tokenizer, original.ExportCheckpoint());
        Assert.AreEqual(original.NextToken(), restored.NextToken());
    }

    [TestMethod]
    public async Task TextDecodeExcludesPromptAsync()
    {
        var tokenizer = await LoadAsync();
        var session = Create(tokenizer).CreateTokenExecutor().CreateTextSession(tokenizer, "Hello", 2);
        Assert.AreEqual(string.Empty, session.GetGeneratedText());
    }

    [TestMethod]
    public async Task TextDecodeMatchesWholeGeneratedSequenceAsync()
    {
        var tokenizer = await LoadAsync();
        var session = Create(tokenizer).CreateTokenExecutor().CreateTextSession(tokenizer, "Hello", 1);
        var token = session.NextToken();
        Assert.AreEqual(tokenizer.Decode(new[] { token }, true), session.GetGeneratedText());
    }

    [TestMethod]
    public async Task IncorrectTokenizerIdentityIsRejectedAsync()
    {
        var tokenizer = await LoadAsync();
        Assert.ThrowsException<ArgumentException>(() => Create(tokenizer, "wrong").CreateTokenExecutor().CreateTextSession(tokenizer, "Hello", 1));
    }

    [TestMethod]
    public async Task OversizedPromptIsNotSilentlyTruncatedAsync()
    {
        var tokenizer = await LoadAsync();
        var model = Create(tokenizer, budget: new NetworkBudget(MaxSteps: 2));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => model.CreateTokenExecutor().CreateTextSession(tokenizer, "Hello world!", 1));
    }

    [TestMethod]
    public async Task CancelledTextCreationDoesNotModifyModelAsync()
    {
        var tokenizer = await LoadAsync();
        var model = Create(tokenizer);
        var before = model.ExportCheckpoint();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => model.CreateTokenExecutor().CreateTextSession(tokenizer,
            "Hello", 1, cancellationToken: cancellation.Token));
        CollectionAssert.AreEqual(before, model.ExportCheckpoint());
    }

    private static RecursiveNetwork Create(MiniMindTextTokenizer tokenizer, string? identity = null, NetworkBudget? budget = null)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        var model = new RecursiveNetwork(graph, budget);
        model.BindText(new TokenBindings(identity ?? tokenizer.Identity, new[] { "n" }, new[] { "n" },
            Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new[] { (double)i / tokenizer.VocabularySize }).ToArray()));
        return model;
    }

    private static Task<MiniMindTextTokenizer> LoadAsync() => MiniMindTextTokenizer.LoadAsync(
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"));
}
