using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class ChatJsonlTests
{
    private const string Sample = "{\"conversations\":[{\"role\":\"user\",\"content\":\"Hello\"},{\"role\":\"assistant\",\"content\":\"你好\",\"reasoning_content\":\"思考\"}]}";

    [TestMethod]
    public async Task LoadedChatMatchesDirectDatasetIdentityAsync()
    {
        var tokenizer = await LoadTokenizerAsync();
        using var stream = Bytes("\uFEFF\n" + Sample + "\r\n");
        var actual = await ChatJsonl.LoadAsync(stream, tokenizer);
        var expected = tokenizer.CreateChatDataset(new[] { new[] {
            new SupervisedChatMessage("user", "Hello"), new SupervisedChatMessage("assistant", "你好", "思考") } });
        Assert.AreEqual(expected.Identity, actual.Identity);
    }

    [DataTestMethod]
    [DataRow("{\"conversations\":[],\"conversations\":[]}")]
    [DataRow("{\"conversations\":[{\"role\":\"assistant\",\"role\":\"user\",\"content\":\"x\"}]}")]
    [DataRow("{\"conversations\":[{\"role\":\"tool\",\"content\":\"x\"}]}")]
    [DataRow("{\"conversations\":[{\"role\":\"assistant\",\"content\":\"x\",\"tool_calls\":[]}]}")]
    [DataRow("{\"conversations\":[{\"role\":\"user\",\"content\":\"x\"}]}")]
    [DataRow("{\"text\":\"not chat\"}")]
    [DataRow("[]")]
    public async Task InvalidChatReportsLineAsync(string json)
    {
        var tokenizer = await LoadTokenizerAsync();
        using var stream = Bytes("\n" + json);
        var error = await Assert.ThrowsExceptionAsync<InvalidDataException>(() => ChatJsonl.LoadAsync(stream, tokenizer));
        StringAssert.Contains(error.Message, "2");
    }

    [TestMethod]
    public async Task SampleCountLimitIsEnforcedAsync()
    {
        var tokenizer = await LoadTokenizerAsync();
        using var stream = Bytes(Sample + "\n" + Sample);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => ChatJsonl.LoadAsync(stream, tokenizer, new JsonlDatasetOptions(MaxSamples: 1)));
    }

    [TestMethod]
    public async Task TruncationWithoutTargetsRejectsSampleAsync()
    {
        var tokenizer = await LoadTokenizerAsync();
        using var stream = Bytes(Sample);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => ChatJsonl.LoadAsync(stream, tokenizer, new JsonlDatasetOptions(MaxSampleTokens: 2)));
    }

    [TestMethod]
    public async Task StreamOwnershipRemainsWithCallerAsync()
    {
        var tokenizer = await LoadTokenizerAsync();
        using var stream = Bytes(Sample);
        await ChatJsonl.LoadAsync(stream, tokenizer);
        Assert.IsTrue(stream.CanRead);
    }

    [TestMethod]
    public async Task CancellationBeforeReadDoesNotConsumeInputAsync()
    {
        var tokenizer = await LoadTokenizerAsync();
        using var stream = Bytes(Sample);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => ChatJsonl.LoadAsync(stream, tokenizer, cancellationToken: cancellation.Token));
        Assert.AreEqual(0L, stream.Position);
    }

    private static MemoryStream Bytes(string text) => new(Encoding.UTF8.GetBytes(text));
    private static Task<MiniMindTextTokenizer> LoadTokenizerAsync() => MiniMindTextTokenizer.LoadAsync(
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"));
}
