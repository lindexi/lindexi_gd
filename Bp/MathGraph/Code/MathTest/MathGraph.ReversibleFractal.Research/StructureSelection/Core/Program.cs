using System.Globalization;
using System.Text.Json;
using MathGraph.StructureSelection;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var study = Experiment.Run();
if (args.Contains("--json", StringComparer.Ordinal))
{
    Console.WriteLine(JsonSerializer.Serialize(study, new JsonSerializerOptions { WriteIndented = true }));
    return;
}

Console.WriteLine($"Paired trials: {study.Trials.Count}. Positive deltas favor the named expansion.");
foreach (var group in study.Trials.GroupBy(t => (t.Task, t.Checkpoint)))
{
    var pairs = group.Select(t => (Baseline: t.Candidates[0], Random: t.Candidates[1], Signal: t.Candidates[2])).ToArray();
    Console.WriteLine($"{group.Key.Task,-6} @{group.Key.Checkpoint,3}: n={pairs.Length}, " +
        $"retained [continue/random/signal]={group.Count(t => t.RetainedStrategy == "continue")}/" +
        $"{group.Count(t => t.RetainedStrategy == "random")}/{group.Count(t => t.RetainedStrategy == "signal")}, " +
        $"same={group.Count(t => t.SamePoint)}, forward [random/signal]={pairs.Count(p => p.Random.Direction == "forward")}/" +
        $"{pairs.Count(p => p.Signal.Direction == "forward")}, " +
        $"validation gain(signal vs random)={pairs.Average(p => p.Random.ValidationMse - p.Signal.ValidationMse):G6}, " +
        $"test gain(signal vs random)={pairs.Average(p => p.Random.TestMse - p.Signal.TestMse):G6}, " +
        $"test gain(random,signal,retained vs continue)={pairs.Average(p => p.Baseline.TestMse - p.Random.TestMse):G6}, " +
        $"{pairs.Average(p => p.Baseline.TestMse - p.Signal.TestMse):G6}, " +
        $"{group.Average(t => t.Candidates[0].TestMse - t.Candidates.Single(c => c.Strategy == t.RetainedStrategy).TestMse):G6}");
}
foreach (var group in study.FirstAcceptances.GroupBy(t => t.Task))
    Console.WriteLine($"{group.Key} first acceptance: " + string.Join(", ", group.Select(t => $"{t.Seed}:{t.Checkpoint?.ToString() ?? "none"}")));
Console.WriteLine($"Maximum initial validation prediction drift: {study.Trials.SelectMany(t => t.Candidates).Max(c => c.InitialPredictionDrift):G6}");
