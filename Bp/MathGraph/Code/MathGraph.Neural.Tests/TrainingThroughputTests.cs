using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class TrainingThroughputTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("LocalExperiment")]
    public async Task MeasureFullWindowTrainingAsync()
    {
        var five = FiveNodeGraph.Create(17);
        var model = new RecursiveNetwork(five.Graph);
        var nodes = five.Graph.ElementList.Select(node => node.Id).ToArray();
        model.BindText(new TokenBindings("throughput", nodes, nodes,
            Enumerable.Range(0, 6400).Select(token => (IReadOnlyList<double>)
                Enumerable.Range(0, nodes.Length).Select(bit => ((token >> bit) & 1) == 0 ? -1d : 1d).ToArray()).ToArray(), seed: 19));
        var tokens = Enumerable.Range(0, 512).Select(i => i * 37 % 6400).ToArray();
        model.TrainTokens(tokens);
        var durations = new List<double>();
        var allocations = new List<long>();
        var measurements = new List<TrainingMeasurement>();
        for (var repeat = 0; repeat < 3; repeat++)
        {
            model.TrainTokens(tokens);
            var measurement = model.LastTrainingMeasurement!;
            measurements.Add(measurement);
            durations.Add(measurement.Total.TotalMilliseconds);
            allocations.Add(measurement.AllocatedBytes);
            TestContext.WriteLine($"{measurement}");
        }
        TestContext.WriteLine($"Mean: {durations.Average():F2} ms/window; {511000 / durations.Average():F0} supervised tokens/s; {allocations.Average() / 1048576:F2} MiB/window.");
        var path = Path.Combine(AppContext.BaseDirectory, $"throughput-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, System.Text.Json.JsonSerializer.Serialize(measurements,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        TestContext.WriteLine(path);
        TestContext.AddResultFile(path);
        Assert.AreEqual(4L, model.TextBindings!.Parameters[0].Updates);
    }
}
