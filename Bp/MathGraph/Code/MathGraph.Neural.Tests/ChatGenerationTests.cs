using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class ChatGenerationTests
{
    [DataTestMethod]
    [DataRow(false, "<|im_start|>assistant\n<think>\n\n</think>\n\n")]
    [DataRow(true, "<|im_start|>assistant\n<think>\n")]
    public async Task PromptUsesMiniMindThinkingModeAsync(bool thinking, string suffix)
    {
        var tokenizer = await LoadAsync();
        Assert.AreEqual("<|im_start|>user\nHello<|im_end|>\n" + suffix,
            tokenizer.RenderChatPrompt(Messages(), thinking));
    }

    [TestMethod]
    public async Task PromptHistoryMatchesTrainingTemplateAsync()
    {
        var tokenizer = await LoadAsync();
        var history = Messages().Concat(new[] { new SupervisedChatMessage("assistant", "你好", "思考") }).ToArray();
        var training = tokenizer.CreateChatDataset(new[] { history });
        var expected = tokenizer.Decode(training[0]);
        StringAssert.StartsWith(tokenizer.RenderChatPrompt(history), expected);
    }

    [TestMethod]
    public async Task ChatSessionMatchesExplicitTextPromptAsync()
    {
        var tokenizer = await LoadAsync();
        var executor = Model(tokenizer).CreateTokenExecutor();
        var chat = executor.CreateChatSession(tokenizer, Messages(), 2, seed: 9);
        var text = executor.CreateTextSession(tokenizer, tokenizer.RenderChatPrompt(Messages()), 2, seed: 9);
        Assert.AreEqual(text.NextToken(), chat.NextToken());
    }

    [TestMethod]
    public async Task ChatSessionRestoresWithoutRerenderingAsync()
    {
        var tokenizer = await LoadAsync();
        var model = Model(tokenizer);
        var chat = model.CreateTokenExecutor().CreateChatSession(tokenizer, Messages(), 2, true, seed: 17);
        var restored = RecursiveNetwork.ImportCheckpoint(model.ExportCheckpoint()).CreateTokenExecutor()
            .RestoreTextSession(tokenizer, chat.ExportCheckpoint());
        Assert.AreEqual(chat.NextToken(), restored.NextToken());
    }

    [TestMethod]
    public async Task ToolMessagesAreRejectedAsync()
    {
        var tokenizer = await LoadAsync();
        Assert.ThrowsException<ArgumentException>(() => tokenizer.RenderChatPrompt(new[] { new SupervisedChatMessage("tool", "result") }));
    }

    [TestMethod]
    public async Task PromptCharacterBudgetIncludesTemplateAsync()
    {
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"), maxTextCharacters: 10);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tokenizer.RenderChatPrompt(Messages()));
    }

    [TestMethod]
    public async Task CancelledRenderingDoesNotProducePromptAsync()
    {
        var tokenizer = await LoadAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => tokenizer.RenderChatPrompt(Messages(), cancellationToken: cancellation.Token));
    }

    [TestMethod]
    public async Task NonAssistantReasoningIsRejectedAsync()
    {
        var tokenizer = await LoadAsync();
        Assert.ThrowsException<ArgumentException>(() => tokenizer.RenderChatPrompt(new[] { new SupervisedChatMessage("user", "x", "reason") }));
    }

    private static SupervisedChatMessage[] Messages() => new[] { new SupervisedChatMessage("user", "Hello") };
    private static string Asset(string name) => Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", name);
    private static Task<MiniMindTextTokenizer> LoadAsync() => MiniMindTextTokenizer.LoadAsync(Asset("tokenizer.json"), Asset("tokenizer_config.json"));

    private static RecursiveNetwork Model(MiniMindTextTokenizer tokenizer)
    {
        var graph = new MathGraph<Neuron, NeuralConnection>();
        graph.CreateAndAddElement(new Neuron(), "n");
        var model = new RecursiveNetwork(graph);
        model.BindText(new TokenBindings(tokenizer.Identity, new[] { "n" }, new[] { "n" },
            Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new[] { (double)i / tokenizer.VocabularySize }).ToArray()));
        return model;
    }
}
