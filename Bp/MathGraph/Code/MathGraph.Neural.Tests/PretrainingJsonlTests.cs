using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class PretrainingJsonlTests
{
    [TestMethod]
    public async Task JsonlMatchesDirectTextDatasetAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream("\uFEFF{\"text\":\"你好\"}\r\n\n{\"text\":\"Hello\",\"source\":1}");
        var dataset = await PretrainingJsonl.LoadAsync(stream, tokenizer);
        Assert.AreEqual(tokenizer.CreateDataset(new[] { "你好", "Hello" }).Identity, dataset.Identity);
    }

    [DataTestMethod]
    [DataRow("{}")]
    [DataRow("[]")]
    [DataRow("{\"text\":null}")]
    [DataRow("{\"text\":\"\"}")]
    [DataRow("{\"text\":\"a\",\"text\":\"b\"}")]
    [DataRow("{broken")]
    public async Task InvalidRecordReportsLineAsync(string record)
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream("\n" + record);
        var error = await Assert.ThrowsExceptionAsync<InvalidDataException>(() => PretrainingJsonl.LoadAsync(stream, tokenizer));
        StringAssert.Contains(error.Message, "2");
    }

    [TestMethod]
    public async Task OversizedLineIsRejectedBeforeParsingAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream(new string('x', 100));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => PretrainingJsonl.LoadAsync(stream, tokenizer,
            new JsonlDatasetOptions(MaxLineBytes: 10)));
    }

    [TestMethod]
    public async Task TotalByteBudgetIncludesEmptyLinesAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream(new string('\n', 100));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => PretrainingJsonl.LoadAsync(stream, tokenizer,
            new JsonlDatasetOptions(MaxFileBytes: 10)));
    }

    [TestMethod]
    public async Task SampleBudgetIsEnforcedAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream("{\"text\":\"a\"}\n{\"text\":\"b\"}");
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => PretrainingJsonl.LoadAsync(stream, tokenizer,
            new JsonlDatasetOptions(MaxSamples: 1)));
    }

    [TestMethod]
    public async Task TokenBudgetIncludesEosAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream("{\"text\":\"a\"}\n{\"text\":\"b\"}");
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => PretrainingJsonl.LoadAsync(stream, tokenizer,
            new JsonlDatasetOptions(MaxTotalTokens: 2)));
    }

    [TestMethod]
    public async Task SampleTruncationReservesEosAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream("{\"text\":\"Hello world!\"}");
        var dataset = await PretrainingJsonl.LoadAsync(stream, tokenizer, new JsonlDatasetOptions(MaxSampleTokens: 2));
        Assert.AreEqual(tokenizer.EosTokenId, dataset[0][1]);
    }

    [TestMethod]
    public async Task CallerStreamRemainsOpenAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream("{\"text\":\"Hello\"}");
        await PretrainingJsonl.LoadAsync(stream, tokenizer);
        Assert.IsTrue(stream.CanRead);
    }

    [TestMethod]
    public async Task CancellationBeforeReadPreservesPositionAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream("{\"text\":\"Hello\"}");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => PretrainingJsonl.LoadAsync(stream, tokenizer, cancellationToken: cancellation.Token));
        Assert.AreEqual(0L, stream.Position);
    }

    [TestMethod]
    public async Task InvalidUtf8IsRejectedAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = new MemoryStream(new byte[] { 0xff });
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => PretrainingJsonl.LoadAsync(stream, tokenizer));
    }

    [TestMethod]
    public async Task EmptyDatasetIsRejectedAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream("\n\r\n");
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => PretrainingJsonl.LoadAsync(stream, tokenizer));
    }

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Utf8CharacterAcrossReadBoundaryIsPreservedAsync()
    {
        var tokenizer = await TokenizerAsync();
        using var stream = Stream(new string(' ', 8182) + "{\"text\":\"你好\"}");
        var dataset = await PretrainingJsonl.LoadAsync(stream, tokenizer);
        Assert.AreEqual(tokenizer.CreateDataset(new[] { "你好" }).Identity, dataset.Identity);
    }

    [TestMethod]
    public async Task FileEntryReleasesFileAfterLoadingAsync()
    {
        var tokenizer = await TokenizerAsync();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".jsonl");
        TestContext.WriteLine(path);
        await File.WriteAllTextAsync(path, "{\"text\":\"Hello\"}");
        await PretrainingJsonl.LoadFileAsync(path, tokenizer);
        await using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.IsTrue(exclusive.CanWrite);
    }

    private static MemoryStream Stream(string text) => new(Encoding.UTF8.GetBytes(text));
    private static Task<MiniMindTextTokenizer> TokenizerAsync() => MiniMindTextTokenizer.LoadAsync(
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
        Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"));
}
