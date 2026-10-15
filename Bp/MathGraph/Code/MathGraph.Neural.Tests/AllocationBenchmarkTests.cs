using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class AllocationBenchmarkTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("LocalExperiment")]
    public async Task MeasureFixedCheckpointTrainingAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "minimind_dataset"))) directory = directory.Parent;
        if (directory is null) { Assert.Inconclusive("Local workspace missing."); return; }
        var root = Path.Combine(directory.FullName, "MathGraph", "artifacts", "allocation-benchmark");
        Directory.CreateDirectory(root);
        var initialPath = Path.Combine(root, "initial.json");
        if (!File.Exists(initialPath))
        {
            var five = FiveNodeGraph.Create(17);
            var model = new RecursiveNetwork(five.Graph);
            var nodes = five.Graph.ElementList.Select(n => n.Id).ToArray();
            model.BindText(new TokenBindings("allocation-test", nodes, nodes,
                Enumerable.Range(0, 6400).Select(id => (IReadOnlyList<double>)Enumerable.Range(0, 5)
                    .Select(bit => ((id >> bit) & 1) == 0 ? -1d : 1d).ToArray()).ToArray(), seed: 19));
            await model.SaveCheckpointAsync(initialPath);
        }
        var initial = await File.ReadAllBytesAsync(initialPath);
        var tokens = Enumerable.Range(0, 24).Select(i => i * 37 % 6400).ToArray();
        var warmup = RecursiveNetwork.ImportCheckpoint(initial);
        warmup.TrainTokens(tokens);
        var output = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var measurements = new List<TrainingMeasurement>();
        for (var repeat = 0; repeat < 3; repeat++)
        {
            var model = RecursiveNetwork.ImportCheckpoint(initial);
            model.TrainTokens(tokens);
            measurements.Add(model.LastTrainingMeasurement!);
            var result = model.ExportCheckpoint();
            var reference = Path.Combine(root, "reference-after.json");
            if (!File.Exists(reference)) await File.WriteAllBytesAsync(reference, result);
            var expected = RecursiveNetwork.ImportCheckpoint(await File.ReadAllBytesAsync(reference)).ExportCheckpoint();
            CollectionAssert.AreEqual(expected, result);
        }
        var path = Path.Combine(output, "measurements.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.WriteLine(path);
        TestContext.AddResultFile(path);
    }
}
