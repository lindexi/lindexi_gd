using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;

namespace FractalForge.Research.Deepening;

public sealed record DeepeningConfiguration(string Semantics, ForgeRunConfiguration Run, DeepeningBranch Branch, int CooldownUpdates);

/// <summary>Deepening 独立入口，配置与检查点路径必须显式给出。</summary>
public static class DeepeningRunner
{
    public const string Semantics = "fractalforge.deepening.run.v1";
    private static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    /// <summary>prepare tokenizer tokenizerConfig archive seed 创建显式文本基线，返回源指纹。</summary>
    public static async Task PrepareAsync(string tokenizerPath, string tokenizerConfigPath, string archivePath, int seed,
        TextWriter output, CancellationToken cancellationToken = default)
    {
        var tokenizer = await MiniMindTextTokenizer.LoadAsync(tokenizerPath, tokenizerConfigPath, cancellationToken: cancellationToken).ConfigureAwait(false);
        var model = ForgeModel.Create(ForgeTopology.CreateDiamond(seed, feedback: true));
        model.Network.BindText(new TokenBindings(tokenizer.Identity, ["I"], ["O"],
            Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new[] { (double)i / tokenizer.VocabularySize }).ToArray(), seed: seed));
        var bytes = ForgeArchive.Export(model);
        await using var stream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync(JsonSerializer.Serialize(new { SourceSha256 = ForgeArchive.Fingerprint(bytes), TokenizerIdentity = tokenizer.Identity })).ConfigureAwait(false);
    }

    /// <summary>执行 train/resume/inspect/evaluate/generate/cooldown；运行命令参数为命令、配置、显式检查点，可追加单次更新数。</summary>
    public static async Task<int> RunAsync(IReadOnlyList<string> args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (args.Count == 5 && args[0] == "prepare")
            {
                await PrepareAsync(args[1], args[2], args[3], int.Parse(args[4], CultureInfo.InvariantCulture), output, cancellationToken).ConfigureAwait(false);
                return 0;
            }
            if (args.Count < 3 || args[0] is not ("train" or "resume" or "inspect" or "evaluate" or "generate" or "cooldown")) throw Messages.Invalid();
            if (args[0] == "generate" ? args.Count != 6 : args.Count != 3 && !(args.Count == 4 && args[0] is "train" or "resume" or "cooldown")) throw Messages.Invalid();
            var configFile = new FileInfo(args[1]);
            if (configFile.Length > 1048576) throw Messages.Invalid();
            var configurationBytes = await File.ReadAllBytesAsync(args[1], cancellationToken).ConfigureAwait(false);
            using (var document = JsonDocument.Parse(configurationBytes)) CheckProperties(document.RootElement);
            var config = JsonSerializer.Deserialize<DeepeningConfiguration>(configurationBytes, Json) ?? throw Messages.Invalid();
            var r = config.Run;
            ForgeCorpusPreparation.ValidateUsage(r);
            if (r is null || r.Action is null || r.Training is null || r.Acceptance is null || r.FinalTest is null || r.DataLimits is null || r.Optimizer is null ||
                !Hash(r.SourceSha256) || !Hash(r.Training.Sha256) || !Hash(r.Acceptance.Sha256) || !Hash(r.FinalTest.Sha256) ||
                string.IsNullOrWhiteSpace(r.Training.Identity) || string.IsNullOrWhiteSpace(r.Acceptance.Identity) || string.IsNullOrWhiteSpace(r.FinalTest.Identity) ||
                r.Action.Destination is not null || r.Action.Delay != 1 || r.Action.Alpha != .5 ||
                config.Branch is DeepeningBranch.D0 or DeepeningBranch.D1 && r.Action.Weight != 0) throw Messages.Invalid();
            if (config.Semantics != Semantics || r.Semantics != ForgeRunner.Semantics || r.Direction != "Deepening" ||
                !Segment(r.RunId) || !Enum.IsDefined(config.Branch) || r.Action.Kind != config.Branch.ToString() ||
                string.IsNullOrWhiteSpace(r.Action.Target) || string.IsNullOrWhiteSpace(r.RuleVersion) || config.CooldownUpdates < 0 ||
                r.Training.Identity == r.Acceptance.Identity || r.FinalTest.Identity == r.Training.Identity || r.FinalTest.Identity == r.Acceptance.Identity ||
                r.Training.Sha256 == r.Acceptance.Sha256 || r.FinalTest.Sha256 == r.Training.Sha256 || r.FinalTest.Sha256 == r.Acceptance.Sha256) throw Messages.Invalid();
            var paths = new[] { r.Training.Path, r.Acceptance.Path, r.FinalTest.Path }.Select(Path.GetFullPath).ToArray();
            if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3) throw Messages.Invalid();
            var directory = Path.GetFullPath(Path.Combine(r.OutputRoot, "Deepening", r.RunId));
            Directory.CreateDirectory(directory);
            var checkpoint = Path.GetFullPath(args[2]);
            if (!string.Equals(Path.GetDirectoryName(checkpoint), directory, StringComparison.OrdinalIgnoreCase)) throw Messages.Invalid();
            using var lease = new FileStream(Path.Combine(directory, "writer.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var rules = new DeepeningRules(ForgeArchive.Fingerprint(JsonSerializer.SerializeToUtf8Bytes(config)), r.Training.Identity,
                r.Acceptance.Identity, r.MaxUpdates, r.MaxTokens, r.MaxPairTapeCells, r.MinimumGain, config.CooldownUpdates, r.Optimizer);
            if (new FileInfo(r.SourceArchive).Length > 67108864) throw Messages.Invalid();
            var source = await File.ReadAllBytesAsync(r.SourceArchive, cancellationToken).ConfigureAwait(false);
            if (ForgeArchive.Fingerprint(source) != r.SourceSha256) throw Messages.Invalid();
            var tokenizer = await MiniMindTextTokenizer.LoadAsync(r.TokenizerPath, r.TokenizerConfigPath, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tokenizer.Identity != r.TokenizerIdentity) throw Messages.Invalid();
            DeepeningExperiment experiment;
            if (args[0] == "train")
            {
                if (File.Exists(checkpoint)) throw Messages.Invalid();
                experiment = DeepeningExperiment.Begin(ForgeArchive.Import(source), config.Branch, r.Action.Target, r.Action.Weight, rules, cancellationToken: cancellationToken);
                await experiment.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
            }
            else experiment = await DeepeningExperiment.LoadAsync(checkpoint, rules, cancellationToken).ConfigureAwait(false);
            foreach (var model in new[] { experiment.Keep, experiment.Candidate })
                if (model.Network.TextBindings is not { } binding || binding.TokenizerIdentity != tokenizer.Identity || binding.VocabularySize != tokenizer.VocabularySize ||
                    r.DataLimits.MaxSampleTokens > model.Network.Budget.MaxSteps) throw Messages.Invalid();
            if (args[0] is "train" or "resume" or "cooldown")
            {
                var limit = args.Count == 4 ? int.Parse(args[3], CultureInfo.InvariantCulture) : int.MaxValue;
                if (limit <= 0) throw Messages.Invalid();
                var n = 0;
                await using var data = ForgeJsonlStream.ReadAsync(r.Training, tokenizer, r.DataLimits, experiment.State.Position, cancellationToken).GetAsyncEnumerator(cancellationToken);
                while (n < limit)
                {
                    var state = experiment.State;
                    if (args[0] == "cooldown" ? state.CooldownRemaining == 0 : state.Updates == rules.Updates) break;
                    if (!await data.MoveNextAsync().ConfigureAwait(false)) throw Messages.Invalid();
                    var sample = data.Current;
                    n++;
                    if (args[0] == "cooldown") experiment.Cooldown(sample, cancellationToken);
                    else experiment.Advance(sample, state.Position, cancellationToken);
                    await experiment.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (args[0] == "evaluate")
            {
                await experiment.EvaluateAsync(r.Acceptance.Identity,
                    ForgeJsonlStream.ReadAsync(r.Acceptance, tokenizer, r.DataLimits, 0, cancellationToken), cancellationToken).ConfigureAwait(false);
                await experiment.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
            }
            else if (args[0] == "generate")
            {
                var session = experiment.Selected.Network.CreateTokenExecutor(cancellationToken).CreateTextSession(tokenizer, args[3],
                    int.Parse(args[4], CultureInfo.InvariantCulture), new SamplingOptions(DoSample: false), ulong.Parse(args[5], CultureInfo.InvariantCulture), cancellationToken);
                while (!session.IsCompleted) session.NextToken(cancellationToken);
                await output.WriteLineAsync(session.GetGeneratedText()).ConfigureAwait(false);
            }
            var s = experiment.State;
            await output.WriteLineAsync(JsonSerializer.Serialize(new { s.Semantics, s.Branch, s.Target, s.Position, s.Tokens, s.Updates,
                s.CooldownRemaining, s.Decision, s.Evaluation, Cost = experiment.Candidate.MeasureCost(), LongestPath = DeepeningStrategy.LongestPath(experiment.Candidate) })).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException) { await error.WriteLineAsync(Messages.Get("Cancelled")).ConfigureAwait(false); return 130; }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or InvalidDataException or IOException or JsonException or FormatException or OverflowException or UnauthorizedAccessException)
        { await error.WriteLineAsync(Messages.Get("Failed") + ex.Message).ConfigureAwait(false); return 1; }
    }

    private static bool Hash(string value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F');

    private static void CheckProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw Messages.Invalid();
                CheckProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) CheckProperties(item);
    }

    private static bool Segment(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 80 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

}
