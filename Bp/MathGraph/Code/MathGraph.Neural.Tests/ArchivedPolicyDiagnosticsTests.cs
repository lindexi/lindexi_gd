using System.Text.Json.Nodes;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MathGraphs.Neural.Tests;

[TestClass]
public class ArchivedPolicyDiagnosticsTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [TestCategory("LocalExperiment")]
    public async Task AnalyzeExistingFeatureAblationWithoutTrainingAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "minimind_dataset"))) directory = directory.Parent;
        if (directory is null)
        {
            Assert.Inconclusive("Local workspace is unavailable.");
            return;
        }
        var source = Path.Combine(directory.FullName, "MathGraph", "artifacts", "experiments",
            "feature-ablation-c611f8fba01f4095a7ee42b2e914df4c");
        if (!Directory.Exists(source))
        {
            Assert.Inconclusive("The recorded feature ablation archive is unavailable.");
            return;
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var results = new List<object>();
        var errors = new List<double>();
        foreach (var runDirectory in Directory.GetFiles(source, "run.json", SearchOption.AllDirectories).Select(Path.GetDirectoryName).Order())
        {
            var files = Directory.GetFiles(runDirectory!, "checkpoint-*.json").Order().ToArray();
            foreach (var structurePath in files.Where(p => p.EndsWith("-structure.json", StringComparison.Ordinal)))
            {
                var start = System.Array.IndexOf(files, structurePath);
                var feedbackPath = files.Skip(start + 1).First(p => p.EndsWith("-feedback.json", StringComparison.Ordinal));
                var before = (await ReadAsync(structurePath))["Controller"]!;
                var after = (await ReadAsync(feedbackPath))["Controller"]!;
                var options = before["Options"]!;
                var pending = before["Pending"]!;
                var probability = Array(pending["Probabilities"]!);
                var selected = pending["Selected"]!.GetValue<int>();
                var baselineBefore = before["Baseline"]!.GetValue<double>();
                var baselineAfter = after["Baseline"]!.GetValue<double>();
                var advantage = (baselineAfter - baselineBefore) / options["BaselineRate"]!.GetValue<double>();
                var reward = advantage + baselineBefore;
                var cost = options["CostPerNode"]!.GetValue<double>() * pending["Cost"]!.GetValue<int>();
                var weights = Array(before["FeatureWeights"]!);
                var nextWeights = Array(after["FeatureWeights"]!);
                var features = pending["Features"]!.AsArray().Select(row => Array(row!)).ToArray();
                var actions = pending["Actions"]!.AsArray().Select(v => v!.GetValue<string>()).ToArray();
                var baseLogits = actions.Select((id, i) => i == 0 ? before["KeepScore"]!.GetValue<double>() : before["Scores"]![id]!.GetValue<double>()).ToArray();
                var enabled = options["UseNodeFeatures"]!.GetValue<bool>();
                var featureLogits = features.Select(row => enabled ? row.Zip(weights, (x, w) => x * w).Sum() : 0d).ToArray();
                var logits = baseLogits.Zip(featureLogits, (a, b) => a + b).ToArray();
                var reconstructed = Softmax(logits);
                errors.Add(reconstructed.Zip(probability, (a, b) => Math.Abs(a - b)).Max());
                var withoutFeatures = Softmax(baseLogits);
                var uniform = 1d / probability.Length;
                var columns = Enumerable.Range(0, weights.Length).Select(f => new
                {
                    Index = f,
                    Minimum = features.Skip(1).Select(row => row[f]).DefaultIfEmpty(0).Min(),
                    Maximum = features.Skip(1).Select(row => row[f]).DefaultIfEmpty(0).Max()
                }).ToArray();
                results.Add(new
                {
                    Run = Path.GetRelativePath(source, runDirectory!),
                    Decision = Path.GetFileName(structurePath),
                    Feedback = Path.GetFileName(feedbackPath),
                    FeaturesEnabled = enabled,
                    Selected = selected,
                    SelectionLossBefore = pending["InitialLoss"]!.GetValue<double>(),
                    InferredSelectionLossAfter = pending["InitialLoss"]!.GetValue<double>() - (reward + cost),
                    LossImprovement = reward + cost,
                    NodeCost = cost,
                    Reward = reward,
                    Advantage = advantage,
                    BaselineBefore = baselineBefore,
                    BaselineAfter = baselineAfter,
                    FeatureWeightMaxAbsBefore = weights.Max(Math.Abs),
                    FeatureWeightMaxAbsAfter = nextWeights.Max(Math.Abs),
                    FeatureLogitRange = featureLogits.Max() - featureLogits.Min(),
                    BaseLogitRange = baseLogits.Max() - baseLogits.Min(),
                    ProbabilityMaxDeviationFromUniform = probability.Max(p => Math.Abs(p - uniform)),
                    FeatureOnlyTotalVariation = probability.Zip(withoutFeatures, (a, b) => Math.Abs(a - b)).Sum() / 2,
                    FeatureColumns = columns
                });
                if (enabled)
                {
                    var expected = Enumerable.Range(0, weights.Length).Select(f => weights[f] +
                        options["LearningRate"]!.GetValue<double>() * advantage *
                        (features[selected][f] - features.Select((row, i) => probability[i] * row[f]).Sum())).ToArray();
                    errors.Add(expected.Zip(nextWeights, (a, b) => Math.Abs(a - b)).Max());
                }
            }
        }
        var output = Path.Combine(source, "policy-diagnostics-" + Guid.NewGuid().ToString("N") + ".json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
        {
            Source = source,
            Method = "Read-only checkpoint analysis; reward inferred from saved baseline update, not final test loss",
            MaximumReconstructionError = errors.Max(),
            Rounds = results
        }, new JsonSerializerOptions { WriteIndented = true }), timeout.Token);
        TestContext.WriteLine(output);
        TestContext.AddResultFile(output);
        Assert.IsTrue(results.Count == 12 && errors.Max() < 1e-12);

        async Task<JsonNode> ReadAsync(string path)
        {
            await using var stream = File.OpenRead(path);
            return await JsonNode.ParseAsync(stream, cancellationToken: timeout.Token) ?? throw new InvalidDataException();
        }
    }

    private static double[] Array(JsonNode node) => node.AsArray().Select(v => v!.GetValue<double>()).ToArray();
    private static double[] Softmax(double[] values)
    {
        var maximum = values.Max();
        var exp = values.Select(v => Math.Exp(v - maximum)).ToArray();
        var total = exp.Sum();
        return exp.Select(v => v / total).ToArray();
    }
}
