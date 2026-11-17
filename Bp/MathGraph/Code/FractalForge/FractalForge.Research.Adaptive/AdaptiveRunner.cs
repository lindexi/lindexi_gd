using System.Globalization;
using System.Resources;
using System.Text.Json;
using System.Text.Json.Serialization;
using FractalForge.Core;
using FractalForge.Training;
using MathGraphs.Neural;

namespace FractalForge.Research.Adaptive;

/// <summary>Adaptive 入口配置；Base 使用公共运行配置结构，Actions 是固定候选集合，Base.Action 仅用于公共单窗兼容。</summary>
public sealed record AdaptiveConfiguration(string Semantics, ForgeRunConfiguration Base, AdaptiveOptions Policy, ForgeAction[] Actions);

/// <summary>可执行的 Adaptive 方向入口，独立保存同源配对调度，不修改公共单动作运行器。</summary>
public static class AdaptiveRunner
{
    /// <summary>方向配置语义版本。</summary>
    public const string Semantics = "fractalforge.adaptive-config.v1";
    private static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true, WriteIndented = true };

    /// <summary>执行 prepare/train/resume/inspect/evaluate/generate；返回 0 成功、1 运行错误、2 参数错误、130 取消。</summary>
    public static async Task<int> RunAsync(IReadOnlyList<string> args, TextWriter output, TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (args.Count < 2) throw new ArgumentException(AdaptiveText.Get("Usage"));
            if (args[0] == "prepare")
            {
                if (args.Count != 6 || !int.TryParse(args[4], out var seed) || !bool.TryParse(args[5], out var feedback))
                    throw new ArgumentException(AdaptiveText.Get("Usage"));
                var template = await ForgeRunConfiguration.LoadAsync(args[1], cancellationToken).ConfigureAwait(false);
                var policy = Parse<AdaptiveOptions>(args[2]);
                _ = new AdaptivePolicy(policy);
                var tokenizer = await MiniMindTextTokenizer.LoadAsync(template.TokenizerPath, template.TokenizerConfigPath,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var model = ForgeModel.Create(ForgeTopology.CreateDiamond(seed, feedback));
                model.Network.BindText(new TokenBindings(tokenizer.Identity, ["I"], ["O"],
                    Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new double[] { (double)i / tokenizer.VocabularySize }).ToArray(), seed: seed));
                var bytes = ForgeArchive.Export(model);
                var configPath = Path.GetFullPath(args[3]);
                Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? throw new ArgumentException(nameof(args)));
                var sourcePath = configPath + ".source.forge";
                if (File.Exists(configPath) || File.Exists(sourcePath)) throw new InvalidOperationException(AdaptiveText.Get("Exists"));
                await File.WriteAllBytesAsync(sourcePath, bytes, cancellationToken).ConfigureAwait(false);
                var baseline = template with { Direction = "Adaptive", Semantics = ForgeRunner.Semantics,
                    SourceArchive = sourcePath, SourceSha256 = ForgeArchive.Fingerprint(bytes), TokenizerIdentity = tokenizer.Identity };
                var configuration = new AdaptiveConfiguration(Semantics, baseline, policy,
                    [new("clone-widen", "A", Alpha: .3), new("clone-widen", "B", Alpha: .3),
                     new("output-deepen", "A"), new("output-deepen", "B"), new("add-feedback", "O", Destination: "A", Weight: .01)]);
                await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(configuration, Json), cancellationToken).ConfigureAwait(false);
                await output.WriteLineAsync(JsonSerializer.Serialize(new { Configuration = configPath, Source = sourcePath })).ConfigureAwait(false);
                return 0;
            }
            var command = args[0];
            var count = command switch { "train" => 3, "resume" => 4, "inspect" or "evaluate" => 3, "generate" => 6, _ => 0 };
            if (args.Count != count) throw new ArgumentException(AdaptiveText.Get("Usage"));
            var c = Parse<AdaptiveConfiguration>(args[1]);
            Validate(c);
            var b = c.Base;
            ForgeCorpusPreparation.ValidateUsage(b);
            var directory = Path.GetFullPath(Path.Combine(b.OutputRoot, "Adaptive", b.RunId));
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(Path.Combine(directory, "writer.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var path = command == "train" ? Path.Combine(directory, "adaptive.json") : Path.GetFullPath(args[2]);
            if (!string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
            var source = AdaptiveSession.Read(b.SourceArchive, 67108864);
            if (ForgeArchive.Fingerprint(source) != b.SourceSha256) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
            var textTokenizer = await MiniMindTextTokenizer.LoadAsync(b.TokenizerPath, b.TokenizerConfigPath,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (textTokenizer.Identity != b.TokenizerIdentity) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
            var configurationHash = ForgeArchive.Fingerprint(JsonSerializer.SerializeToUtf8Bytes(c, Json));
            var protocol = new TrialProtocol("Adaptive/" + b.RunId + "/" + configurationHash, b.Training.Identity, b.Acceptance.Identity,
                b.RuleVersion, b.MaxUpdates, b.MaxTokens, b.MaxPairTapeCells, b.MinimumGain, b.Optimizer);
            AdaptiveSession session;
            if (command == "train")
            {
                if (File.Exists(path)) throw new InvalidOperationException(AdaptiveText.Get("Exists"));
                session = new(ForgeArchive.Import(source), c.Policy, protocol, c.Actions);
            }
            else session = AdaptiveSession.Load(path, c.Policy, protocol, c.Actions);
            var binding = session.Model.Network.TextBindings;
            if (binding is null || binding.TokenizerIdentity != textTokenizer.Identity || binding.VocabularySize != textTokenizer.VocabularySize ||
                b.DataLimits.MaxSampleTokens > session.Model.Network.Budget.MaxSteps) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
            if (command is "train" or "resume")
            {
                if (!int.TryParse(args[^1], out var updates) || updates <= 0) throw new ArgumentException(AdaptiveText.Get("Usage"));
                await using var training = ForgeJsonlStream.ReadAsync(b.Training, textTokenizer, b.DataLimits, session.Position, cancellationToken).GetAsyncEnumerator(cancellationToken);
                session.Save(path);
                for (var i = 0; i < updates; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (session.ReadyToEvaluate)
                    {
                        await session.EvaluateAsync(b.Acceptance.Identity,
                            ForgeJsonlStream.ReadAsync(b.Acceptance, textTokenizer, b.DataLimits, 0, cancellationToken), cancellationToken).ConfigureAwait(false);
                        session.Save(path);
                    }
                    if (!await training.MoveNextAsync().ConfigureAwait(false)) throw new InvalidDataException(AdaptiveText.Get("DataExhausted"));
                    session.Advance(training.Current, cancellationToken);
                    session.Save(path);
                }
            }
            else if (command == "evaluate")
            {
                await session.EvaluateAsync(b.Acceptance.Identity,
                    ForgeJsonlStream.ReadAsync(b.Acceptance, textTokenizer, b.DataLimits, 0, cancellationToken), cancellationToken).ConfigureAwait(false);
                session.Save(path);
            }
            else if (command == "generate")
            {
                if (session.HasPendingTrial || !int.TryParse(args[4], out var maxTokens) || maxTokens <= 0 ||
                    !ulong.TryParse(args[5], out var seed)) throw new ArgumentException(AdaptiveText.Get("Usage"));
                var generation = session.Model.Network.CreateTokenExecutor(cancellationToken).CreateTextSession(textTokenizer, args[3], maxTokens,
                    new SamplingOptions(DoSample: false), seed, cancellationToken);
                while (!generation.IsCompleted) generation.NextToken(cancellationToken);
                await output.WriteLineAsync(JsonSerializer.Serialize(new { Text = generation.GetGeneratedText() })).ConfigureAwait(false);
            }
            await output.WriteLineAsync(JsonSerializer.Serialize(new { session.Updates, session.Position, session.TrainingTokens,
                session.PairTokens, session.EvaluationCount, session.HasPendingTrial, session.ReadyToEvaluate,
                Policy = session.Policy.Snapshot(), session.Filters, Cost = session.Model.MeasureCost(), Checkpoint = path })).ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return 130; }
        catch (ArgumentException ex) { await error.WriteLineAsync(ex.Message).ConfigureAwait(false); return 2; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or JsonException or ArithmeticException or NotSupportedException)
        { await error.WriteLineAsync(AdaptiveText.Get("Failed") + " " + ex.Message).ConfigureAwait(false); return 1; }
    }

    private static T Parse<T>(string path)
    {
        var bytes = AdaptiveSession.Read(path, 1048576);
        using var document = JsonDocument.Parse(bytes);
        Check(document.RootElement);
        return JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        static void Check(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
                    Check(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
                foreach (var item in element.EnumerateArray()) Check(item);
        }
    }

    private static void Validate(AdaptiveConfiguration c)
    {
        if (c.Semantics != Semantics || c.Base is null || c.Policy is null || c.Actions is null || c.Actions.Length == 0)
            throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        var b = c.Base;
        if (b.Semantics != ForgeRunner.Semantics || b.Direction != "Adaptive" || string.IsNullOrWhiteSpace(b.RunId) ||
            b.RunId.Length > 80 || b.RunId.Any(x => !(char.IsAsciiLetterOrDigit(x) || x is '-' or '_')) ||
            string.IsNullOrWhiteSpace(b.OutputRoot) || b.DataLimits is null || b.Optimizer is null ||
            b.Training is null || b.Acceptance is null || b.FinalTest is null || b.MaxUpdates <= 0 || b.MaxTokens <= 0 ||
            b.MaxPairTapeCells <= 0 || !double.IsFinite(b.MinimumGain)) throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        var corpora = new[] { b.Training, b.Acceptance, b.FinalTest };
        if (corpora.Any(x => string.IsNullOrWhiteSpace(x.Path) || string.IsNullOrWhiteSpace(x.Identity) || x.Sha256 is not { Length: 64 } || x.Sha256.Any(c => !(c is >= '0' and <= '9' or >= 'A' and <= 'F')))  ||
            corpora.Select(x => Path.GetFullPath(x.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3 ||
            corpora.Select(x => x.Identity).Distinct().Count() != 3 || corpora.Select(x => x.Sha256).Distinct().Count() != 3)
            throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        var optimizer = b.Optimizer;
        if (string.IsNullOrWhiteSpace(b.RuleVersion) || string.IsNullOrWhiteSpace(b.SourceArchive) ||
            string.IsNullOrWhiteSpace(b.TokenizerPath) || string.IsNullOrWhiteSpace(b.TokenizerConfigPath) ||
            string.IsNullOrWhiteSpace(b.TokenizerIdentity) || b.SourceSha256 is not { Length: 64 } ||
            b.DataLimits.MaxLineBytes <= 0 || b.DataLimits.MaxFileBytes <= 0 || b.DataLimits.MaxSamples <= 0 ||
            b.DataLimits.MaxSampleTokens < 2 || b.DataLimits.MaxTotalTokens < 2 ||
            !double.IsFinite(optimizer.LearningRate) || optimizer.LearningRate <= 0 ||
            !double.IsFinite(optimizer.Beta1) || optimizer.Beta1 < 0 || optimizer.Beta1 >= 1 ||
            !double.IsFinite(optimizer.Beta2) || optimizer.Beta2 < 0 || optimizer.Beta2 >= 1 ||
            !double.IsFinite(optimizer.Epsilon) || optimizer.Epsilon <= 0 ||
            !double.IsFinite(optimizer.WeightDecay) || optimizer.WeightDecay < 0)
            throw new InvalidDataException(AdaptiveText.Get("Invalid"));
        _ = new AdaptivePolicy(c.Policy);
    }
}

internal static class AdaptiveText
{
    private static readonly ResourceManager Resources = new("FractalForge.Research.Adaptive.Messages", typeof(AdaptiveRunner).Assembly);
    internal static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
