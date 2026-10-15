using System.Text.Json;
using MathGraphs.Neural.Cli;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class CpuApplicationTests
{
    public TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(6)]
    public async Task TrainResumeAndGenerateThroughApplicationAsync(int prefixLines)
    {
        var root = Path.Combine(Path.GetTempPath(), "mathgraph-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        TestContext.WriteLine(root);
        await File.WriteAllLinesAsync(Path.Combine(root, "data.jsonl"), new[] {
            "{\"text\":\"Hello world\"}", "{\"text\":\"你好世界\"}", "{\"text\":\"代码训练\"}",
            "{\"text\":\"Testing data\"}", "{\"text\":\"数学网络\"}", "{\"text\":\"Learn tokens\"}" });
        var config = new ApplicationConfig
        {
            TokenizerPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer.json"),
            TokenizerConfigPath = Path.Combine(AppContext.BaseDirectory, "TokenizerAssets", "tokenizer_config.json"),
            DatasetPath = "data.jsonl", ArchiveRoot = "runs", PrefixLines = prefixLines, WarmupUpdates = 1,
            Controller = new StructureOptions(1, UseNodeFeatures: true), MaxNewTokens = 1,
            Archive = new TrainingArchiveOptions(SaveEveryUpdates: 1),
            DataLimits = new JsonlDatasetOptions(MaxSamples: 6, MaxSampleTokens: 8, MaxTotalTokens: 48)
        };
        var path = Path.Combine(root, "config.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(config));
        using var log = new StringWriter();
        Assert.AreEqual(0, await CpuApplication.RunAsync(new[] { "train", path }, log), log.ToString());
        var checkpoints = Directory.GetFiles(Path.Combine(root, "runs"), "*completed.json", SearchOption.AllDirectories);
        var final = checkpoints.Single();
        var periodic = Directory.GetFiles(Path.Combine(root, "runs"), "*periodic.json", SearchOption.AllDirectories).Order().First();
        Assert.AreEqual(0, await CpuApplication.RunAsync(new[] { "inspect", path, final }, log), log.ToString());
        Assert.AreEqual(0, await CpuApplication.RunAsync(new[] { "resume", path, periodic }, log), log.ToString());
        var resumed = Directory.GetFiles(Path.Combine(root, "runs"), "*completed.json", SearchOption.AllDirectories).Single(p => p != final);
        using var expected = JsonDocument.Parse(await File.ReadAllBytesAsync(final));
        using var actual = JsonDocument.Parse(await File.ReadAllBytesAsync(resumed));
        static string[] States(JsonDocument json) => json.RootElement.GetProperty("Parameters").EnumerateArray()
            .Select(p => string.Join("|", new[] { "Value", "First", "Second", "Updates", "Decay" }.Select(name => p.GetProperty(name).GetRawText()))).ToArray();
        CollectionAssert.AreEqual(States(expected), States(actual));
        var expectedModel = await RecursiveNetwork.LoadCheckpointAsync(final, config.Budget);
        var actualModel = await RecursiveNetwork.LoadCheckpointAsync(resumed, config.Budget);
        CollectionAssert.AreEqual(expectedModel.CreateTokenExecutor().Run(new[] { 1, 2 }).Predictions[1].Logits.ToArray(),
            actualModel.CreateTokenExecutor().Run(new[] { 1, 2 }).Predictions[1].Logits.ToArray());
        var latest = CheckpointArchive.FindLatest(Path.Combine(root, "runs"))!;
        var beforeRestart = (await RecursiveNetwork.LoadCheckpointAsync(latest, config.Budget)).ExportCheckpoint();
        Assert.AreEqual(0, await CpuApplication.RunAsync(new[] { "train", path }, log), log.ToString());
        StringAssert.Contains(log.ToString(), "自动恢复最新存档");
        var afterRestart = await RecursiveNetwork.LoadCheckpointAsync(CheckpointArchive.FindLatest(Path.Combine(root, "runs"))!, config.Budget);
        CollectionAssert.AreEqual(beforeRestart, afterRestart.ExportCheckpoint());
        Assert.AreEqual(0, await CpuApplication.RunAsync(new[] { "generate", path, final, "Hello" }, log), log.ToString());
        Assert.AreEqual(0, await CpuApplication.RunAsync(new[] { "chat", path, final, "你好" }, log), log.ToString());
    }

    [TestMethod]
    public async Task MissingArgumentsReturnUsageAsync()
    {
        using var output = new StringWriter();
        Assert.AreEqual(2, await CpuApplication.RunAsync(Array.Empty<string>(), output));
    }
}
