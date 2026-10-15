using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class RealTrainingBenchmarkTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("LocalExperiment")]
    public async Task MeasureCheckpointWithRealTrainingSamplesAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MathGraph", "cpu-training.json")))
            directory = directory.Parent;
        if (directory is null) { Assert.Inconclusive("Local training configuration missing."); return; }
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var ct = timeout.Token;
        var configurationPath = Path.Combine(directory.FullName, "MathGraph", "cpu-training.json");
        using var configuration = JsonDocument.Parse(await File.ReadAllBytesAsync(configurationPath, ct));
        var config = configuration.RootElement;
        var basePath = Path.GetDirectoryName(configurationPath)!;
        string Resolve(string name) => Path.GetFullPath(Path.Combine(basePath, config.GetProperty(name).GetString()!));
        var checkpointPath = CheckpointArchive.FindLatest(Resolve("ArchiveRoot"));
        if (checkpointPath is null) { Assert.Inconclusive("Published training checkpoint missing."); return; }
        var checkpoint = await File.ReadAllBytesAsync(checkpointPath, ct);
        var budget = config.GetProperty("Budget").Deserialize<NetworkBudget>() ?? throw new InvalidDataException();
        var model = RecursiveNetwork.ImportCheckpoint(checkpoint, budget);
        var state = model.PipelineTraining ?? throw new InvalidOperationException();
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(Resolve("TokenizerPath"), Resolve("TokenizerConfigPath"), cancellationToken: ct);
        await using var source = new FileStream(Resolve("DatasetPath"), FileMode.Open, FileAccess.Read, FileShare.Read,
            65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        source.Seek(state.ByteOffset, SeekOrigin.Begin);
        using var reader = new StreamReader(source, new UTF8Encoding(false, true));
        using var prefix = new MemoryStream();
        for (var i = 0; i < 16; i++)
        {
            var line = await reader.ReadLineAsync(ct) ?? throw new EndOfStreamException();
            await prefix.WriteAsync(Encoding.UTF8.GetBytes(line + "\n"), ct);
        }
        prefix.Position = 0;
        var limits = config.GetProperty("DataLimits").Deserialize<JsonlDatasetOptions>() ?? throw new InvalidDataException();
        var dataset = await PretrainingJsonl.LoadAsync(prefix, tokenizer, limits with { MaxSamples = 16 }, ct);
        for (var i = 0; i < dataset.Count; i++)
            model.TrainTokens(dataset[i], options: state.Optimizer, cancellationToken: ct);
        var measurements = new List<TrainingMeasurement>();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        double cpuSeconds = 0, wallSeconds = 0;
        for (var repeat = 0; repeat < 3; repeat++)
        {
            model = RecursiveNetwork.ImportCheckpoint(checkpoint, budget);
            model.TrainTokens(dataset[0], options: state.Optimizer, cancellationToken: ct);
            var cpuStart = process.TotalProcessorTime;
            var wallStart = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var i = 1; i < dataset.Count; i++)
            {
                model.TrainTokens(dataset[i], options: state.Optimizer, cancellationToken: ct);
                measurements.Add(model.LastTrainingMeasurement!);
            }
            wallSeconds += System.Diagnostics.Stopwatch.GetElapsedTime(wallStart).TotalSeconds;
            process.Refresh();
            cpuSeconds += (process.TotalProcessorTime - cpuStart).TotalSeconds;
        }
        var report = new
        {
            Checkpoint = checkpointPath,
            Configuration = configurationPath,
            SourceOffset = state.ByteOffset,
            Build = typeof(RealTrainingBenchmarkTests).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
                .Cast<System.Reflection.AssemblyConfigurationAttribute>().Single().Configuration,
            LibraryBuild = typeof(RecursiveNetwork).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
                .Cast<System.Reflection.AssemblyConfigurationAttribute>().Single().Configuration,
            Repeats = 3,
            LogicalProcessors = Environment.ProcessorCount,
            CpuSeconds = cpuSeconds,
            WallSeconds = wallSeconds,
            AverageBusyCores = cpuSeconds / wallSeconds,
            MachineCpuPercent = 100 * cpuSeconds / wallSeconds / Environment.ProcessorCount,
            MedianMilliseconds = measurements.Select(m => m.Total.TotalMilliseconds).Order().ElementAt(measurements.Count / 2),
            Measurements = measurements,
            TokensPerSecond = measurements.Sum(m => m.SupervisedTokens) / measurements.Sum(m => m.Total.TotalSeconds),
            MeanMilliseconds = measurements.Average(m => m.Total.TotalMilliseconds),
            MeanAllocatedBytes = measurements.Average(m => m.AllocatedBytes)
        };
        var path = Path.Combine(AppContext.BaseDirectory, $"real-training-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), ct);
        TestContext.WriteLine(path);
        TestContext.AddResultFile(path);
        Assert.AreEqual(45, measurements.Count);
    }
}
